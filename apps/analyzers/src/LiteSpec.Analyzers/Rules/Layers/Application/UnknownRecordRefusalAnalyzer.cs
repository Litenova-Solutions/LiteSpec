using System;
using System.Collections.Immutable;
using System.Threading;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2035: a null check on a value returned by a repository or session load refuses the unknown record, never with a generic exception.</summary>
/// <follows>litespec/packs/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnknownRecordRefusalAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2035);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure;

    /// <summary>
    ///     Registers the analysis actions for the unknown record refusal analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var invalidOperation = context.Compilation.GetTypeByMetadataName("System.InvalidOperationException");
        var exception = context.Compilation.GetTypeByMetadataName("System.Exception");
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => Analyze(c, scope, invalidOperation, exception), OperationKind.Throw);
    }

    /// <summary>Analyzes the throw.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="invalidOperation">The invalid operation exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="exception">The exception type supplied for the check, or null when the compilation does not provide it.</param>
    private static void Analyze(
        OperationAnalysisContext context, FileScope scope, INamedTypeSymbol? invalidOperation, INamedTypeSymbol? exception)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var thrown = Unwrap(((IThrowOperation)context.Operation).Exception);
        if (thrown is not IObjectCreationOperation creation
            || creation.Type is not INamedTypeSymbol thrownType
            || !IsGenericException(thrownType, invalidOperation, exception))
        {
            return;
        }

        var model = context.Operation.SemanticModel;
        if (model is null)
        {
            return;
        }

        if (LoadMethodName(context.Operation.Syntax, model, context.CancellationToken) is not string load)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2035, CreationLocation(thrown.Syntax), load, thrownType.Name));
    }

    /// <summary>Returns the load, find or lookup method behind the guarded null check, or null when there is none.</summary>
    /// <param name="syntax">The throw syntax under inspection.</param>
    /// <param name="model">The semantic model the operation carries.</param>
    /// <param name="cancellationToken">The cancellation token the analysis context carries.</param>
    /// <returns>The load method name, or null.</returns>
    private static string? LoadMethodName(SyntaxNode syntax, SemanticModel model, CancellationToken cancellationToken)
    {
        if (syntax is ThrowStatementSyntax statement)
        {
            return LoadMethodAfterIf(statement, model, cancellationToken);
        }

        if (syntax is ThrowExpressionSyntax expression
            && expression.Parent is BinaryExpressionSyntax coalesce
            && coalesce.IsKind(SyntaxKind.CoalesceExpression)
            && coalesce.Right == expression)
        {
            return LoadMethodForValue(Unparenthesize(coalesce.Left), model, cancellationToken);
        }

        return null;
    }

    /// <summary>Returns the load method behind an if-throw, or null when the shape differs.</summary>
    /// <param name="statement">The throw statement under inspection.</param>
    /// <param name="model">The semantic model the operation carries.</param>
    /// <param name="cancellationToken">The cancellation token the analysis context carries.</param>
    /// <returns>The load method name, or null.</returns>
    private static string? LoadMethodAfterIf(ThrowStatementSyntax statement, SemanticModel model, CancellationToken cancellationToken)
    {
        IfStatementSyntax? guarded = null;
        if (statement.Parent is BlockSyntax block
            && block.Statements.Count > 0
            && block.Statements[0] == statement
            && block.Parent is IfStatementSyntax outer
            && outer.Statement == block)
        {
            guarded = outer;
        }
        else if (statement.Parent is IfStatementSyntax direct && direct.Statement == statement)
        {
            guarded = direct;
        }

        if (guarded is null || NullCheckedIdentifier(Unparenthesize(guarded.Condition)) is not IdentifierNameSyntax identifier)
        {
            return null;
        }

        return LoadMethodForLocal(identifier, model, cancellationToken);
    }

    /// <summary>Returns the identifier a null check guards, or null when the condition is no plain null check.</summary>
    /// <param name="condition">The condition under inspection.</param>
    /// <returns>The guarded identifier, or null.</returns>
    private static IdentifierNameSyntax? NullCheckedIdentifier(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax isNull
            && isNull.Expression is IdentifierNameSyntax guarded
            && isNull.Pattern is ConstantPatternSyntax constant
            && constant.Expression.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return guarded;
        }

        if (condition is BinaryExpressionSyntax equals && equals.IsKind(SyntaxKind.EqualsExpression))
        {
            if (equals.Left is IdentifierNameSyntax left && equals.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return left;
            }

            if (equals.Right is IdentifierNameSyntax right && equals.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return right;
            }
        }

        return null;
    }

    /// <summary>Returns the load method behind a coalesced value, or null when there is none.</summary>
    /// <param name="value">The coalesced value under inspection.</param>
    /// <param name="model">The semantic model the operation carries.</param>
    /// <param name="cancellationToken">The cancellation token the analysis context carries.</param>
    /// <returns>The load method name, or null.</returns>
    private static string? LoadMethodForValue(ExpressionSyntax value, SemanticModel model, CancellationToken cancellationToken)
    {
        if (value is IdentifierNameSyntax identifier)
        {
            return LoadMethodForLocal(identifier, model, cancellationToken);
        }

        if (UnwrapToInvocation(value) is InvocationExpressionSyntax invoked)
        {
            return LoadMethodOfCall(invoked, model, cancellationToken);
        }

        return null;
    }

    /// <summary>Returns the load method a local was declared from, or null when it was not.</summary>
    /// <param name="reference">The local reference.</param>
    /// <param name="model">The semantic model the operation carries.</param>
    /// <param name="cancellationToken">The cancellation token the analysis context carries.</param>
    /// <returns>The load method name, or null.</returns>
    private static string? LoadMethodForLocal(IdentifierNameSyntax reference, SemanticModel model, CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(reference, cancellationToken).Symbol is not ILocalSymbol local
            || local.DeclaringSyntaxReferences.Length != 1
            || local.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not VariableDeclaratorSyntax declarator
            || declarator.Initializer?.Value is not ExpressionSyntax initializer
            || UnwrapToInvocation(initializer) is not InvocationExpressionSyntax invoked)
        {
            return null;
        }

        return LoadMethodOfCall(invoked, model, cancellationToken);
    }

    /// <summary>Returns the invoked load method name when the call is a repository or session load; otherwise null.</summary>
    /// <param name="invoked">The invocation under inspection.</param>
    /// <param name="model">The semantic model the operation carries.</param>
    /// <param name="cancellationToken">The cancellation token the analysis context carries.</param>
    /// <returns>The load method name, or null.</returns>
    private static string? LoadMethodOfCall(InvocationExpressionSyntax invoked, SemanticModel model, CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(invoked, cancellationToken).Symbol is not IMethodSymbol method || !IsLoadMethod(method))
        {
            return null;
        }

        if (invoked.Expression is not MemberAccessExpressionSyntax access)
        {
            return null;
        }

        var receiver = model.GetTypeInfo(access.Expression, cancellationToken).Type;
        return IsRepositoryOrSession(receiver) ? method.Name : null;
    }

    /// <summary>Returns true when the method is a load, find or lookup call; otherwise false.</summary>
    /// <param name="method">The invoked method.</param>
    /// <returns>True when the name starts with Load or Find, or contains Lookup; otherwise false.</returns>
    private static bool IsLoadMethod(IMethodSymbol method) =>
        method.Name.StartsWith("Load", StringComparison.Ordinal)
        || method.Name.StartsWith("Find", StringComparison.Ordinal)
        || method.Name.IndexOf("Lookup", StringComparison.Ordinal) >= 0;

    /// <summary>Returns true when the receiver is a repository or session; otherwise false.</summary>
    /// <param name="receiver">The receiver type, or null when it has none.</param>
    /// <returns>True for a *Repository, a document session, or a database context; otherwise false.</returns>
    private static bool IsRepositoryOrSession(ITypeSymbol? receiver)
    {
        if (receiver is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.Name.EndsWith("Repository", StringComparison.Ordinal)
            || named.Name.Equals("IDocumentSession", StringComparison.Ordinal)
            || named.Name.Equals("IQuerySession", StringComparison.Ordinal))
        {
            return true;
        }

        for (var current = named; current is not null; current = current.BaseType)
        {
            if (current.Name.Equals("DbContext", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the thrown type is a generic exception; otherwise false.</summary>
    /// <param name="thrown">The thrown type.</param>
    /// <param name="invalidOperation">The invalid operation exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="exception">The exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True for System.InvalidOperationException or System.Exception; otherwise false.</returns>
    private static bool IsGenericException(INamedTypeSymbol thrown, INamedTypeSymbol? invalidOperation, INamedTypeSymbol? exception) =>
        (invalidOperation is not null && SymbolEqualityComparer.Default.Equals(thrown, invalidOperation))
        || (exception is not null && SymbolEqualityComparer.Default.Equals(thrown, exception));

    /// <summary>Unwraps conversions from the thrown value.</summary>
    /// <param name="thrown">The thrown operation, or null for a bare rethrow.</param>
    /// <returns>The unwrapped operation, or null.</returns>
    private static IOperation? Unwrap(IOperation? thrown)
    {
        while (thrown is IConversionOperation conversion)
        {
            thrown = conversion.Operand;
        }

        return thrown;
    }

    /// <summary>Unwraps parentheses from the expression.</summary>
    /// <param name="value">The expression under inspection.</param>
    /// <returns>The expression without parentheses.</returns>
    private static ExpressionSyntax Unparenthesize(ExpressionSyntax value)
    {
        while (value is ParenthesizedExpressionSyntax parenthesized)
        {
            value = parenthesized.Expression;
        }

        return value;
    }

    /// <summary>Unwraps await, parentheses and casts down to the invoked call, or null when there is none.</summary>
    /// <param name="value">The expression under inspection.</param>
    /// <returns>The invocation expression, or null.</returns>
    private static InvocationExpressionSyntax? UnwrapToInvocation(ExpressionSyntax value)
    {
        var current = Unparenthesize(value);
        while (true)
        {
            if (current is AwaitExpressionSyntax awaited)
            {
                current = Unparenthesize(awaited.Expression);
            }
            else if (current is CastExpressionSyntax cast)
            {
                current = Unparenthesize(cast.Expression);
            }
            else
            {
                return current as InvocationExpressionSyntax;
            }
        }
    }

    /// <summary>Returns the location.</summary>
    /// <param name="syntax">The syntax node under inspection.</param>
    /// <returns>The location.</returns>
    private static Location CreationLocation(SyntaxNode syntax) =>
        syntax is ObjectCreationExpressionSyntax creation && !creation.Type.IsMissing
            ? creation.Type.GetLocation()
            : syntax.GetLocation();
}
