using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2033: Marten refuses a multi-load of documents whose id member is a typed identifier.</summary>
/// <follows>litespec/backend/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedIdentifierLoadManyAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2033);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the typed identifier load many analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => Analyze(c, scope), OperationKind.Invocation);
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!method.Name.Equals("LoadManyAsync", StringComparison.Ordinal)
            && !method.Name.Equals("LoadMany", StringComparison.Ordinal))
        {
            return;
        }

        if (!IsSessionCall(method, invocation))
        {
            return;
        }

        if (method.TypeArguments.Length == 0 || method.TypeArguments[0] is not INamedTypeSymbol document)
        {
            return;
        }

        if (!HasTypedIdentifierId(document))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2033, InvocationLocation(invocation), method.Name, document.Name));
    }

    /// <summary>Returns true when the call runs on a document or query session; otherwise false.</summary>
    /// <param name="method">The invoked LoadMany method.</param>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>True when the receiver is named IDocumentSession or IQuerySession; otherwise false.</returns>
    private static bool IsSessionCall(IMethodSymbol method, IInvocationOperation invocation)
    {
        ITypeSymbol? receiver = method.IsStatic
            ? method.ContainingType
            : invocation.Instance?.Type;
        return receiver is INamedTypeSymbol named
            && (named.Name.Equals("IDocumentSession", StringComparison.Ordinal)
                || named.Name.Equals("IQuerySession", StringComparison.Ordinal));
    }

    /// <summary>Returns true when the document identifies itself with a typed identifier; otherwise false.</summary>
    /// <param name="document">The loaded document type.</param>
    /// <returns>True when an instance Id member is typed as a typed identifier; otherwise false.</returns>
    private static bool HasTypedIdentifierId(INamedTypeSymbol document)
    {
        foreach (var member in document.GetMembers("Id"))
        {
            if (member is IPropertySymbol { IsStatic: false } property && IsTypedIdentifier(property.Type))
            {
                return true;
            }

            if (member is IFieldSymbol { IsStatic: false } field && IsTypedIdentifier(field.Type))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is a typed identifier; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the struct is named *Id or implements IStronglyTypedId; otherwise false.</returns>
    private static bool IsTypedIdentifier(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || named.TypeKind != TypeKind.Struct)
        {
            return false;
        }

        if (named.Name.EndsWith("Id", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in named.AllInterfaces)
        {
            if (contract.Name.Equals("IStronglyTypedId", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the location.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The location.</returns>
    private static Location InvocationLocation(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax syntax && syntax.Expression is MemberAccessExpressionSyntax access
            ? access.Name.Identifier.GetLocation()
            : invocation.Syntax.GetLocation();
}
