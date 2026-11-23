using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2032: a Contains inside a queryable expression compares the underlying value of each typed identifier, never the identifier itself.</summary>
/// <follows>litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedIdentifierContainsAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2032);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the typed identifier contains analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var enumerable = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => Analyze(c, scope, enumerable), OperationKind.Invocation);
    }

    /// <summary>Analyzes the invocation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="enumerable">The Enumerable type supplied for the check, or null when the compilation does not provide it.</param>
    private static void Analyze(OperationAnalysisContext context, FileScope scope, INamedTypeSymbol? enumerable)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!method.Name.Equals("Contains", StringComparison.Ordinal))
        {
            return;
        }

        if (ElementTypeOf(method, invocation, enumerable) is not INamedTypeSymbol element || !IsTypedIdentifier(element))
        {
            return;
        }

        if (!InsideQueryableExpression(invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2032, InvocationLocation(invocation), CollectionName(invocation, method), element.Name));
    }

    /// <summary>Returns the element type a Contains call checks, or null when it is no Enumerable or instance Contains.</summary>
    /// <param name="method">The invoked Contains method.</param>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <param name="enumerable">The Enumerable type supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>The checked element type, or null.</returns>
    private static ITypeSymbol? ElementTypeOf(IMethodSymbol method, IInvocationOperation invocation, INamedTypeSymbol? enumerable)
    {
        var original = method.ReducedFrom ?? method;
        if (original.IsStatic
            && enumerable is not null
            && SymbolEqualityComparer.Default.Equals(original.ContainingType.OriginalDefinition, enumerable)
            && method.TypeArguments.Length == 1)
        {
            return method.TypeArguments[0];
        }

        if (!original.IsStatic
            && original.Parameters.Length == 1
            && invocation.Instance is { Type: INamedTypeSymbol receiver })
        {
            return EnumerableElementOf(receiver);
        }

        return null;
    }

    /// <summary>Returns the element type of the enumerable the receiver implements, or null when it implements none.</summary>
    /// <param name="receiver">The type the instance Contains runs on.</param>
    /// <returns>The enumerable element type, or null.</returns>
    private static ITypeSymbol? EnumerableElementOf(INamedTypeSymbol receiver)
    {
        if (receiver.OriginalDefinition.Name.Equals("IEnumerable", StringComparison.Ordinal)
            && receiver.TypeArguments.Length == 1)
        {
            return receiver.TypeArguments[0];
        }

        foreach (var contract in receiver.AllInterfaces)
        {
            if (contract.OriginalDefinition.Name.Equals("IEnumerable", StringComparison.Ordinal)
                && contract.TypeArguments.Length == 1)
            {
                return contract.TypeArguments[0];
            }
        }

        return null;
    }

    /// <summary>Returns true when the Contains sits in a lambda passed directly to a queryable operator; otherwise false.</summary>
    /// <param name="invocation">The Contains invocation under inspection.</param>
    /// <returns>True when an enclosing lambda converts to an expression tree argument of a queryable operator; otherwise false.</returns>
    private static bool InsideQueryableExpression(IInvocationOperation invocation)
    {
        for (var current = invocation.Parent; current is not null; current = current.Parent)
        {
            if (current is IAnonymousFunctionOperation anonymous
                && anonymous.Parent is IConversionOperation conversion
                && conversion.Type is INamedTypeSymbol converted
                && converted.Name.Equals("Expression", StringComparison.Ordinal)
                && conversion.Parent is IArgumentOperation argument
                && argument.Parent is IInvocationOperation outer
                && IsQueryableOperator(outer))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the invocation is a queryable operator; otherwise false.</summary>
    /// <param name="outer">The invocation the lambda is an argument of.</param>
    /// <returns>True when the method belongs to Queryable or the receiver is a queryable; otherwise false.</returns>
    private static bool IsQueryableOperator(IInvocationOperation outer)
    {
        if (outer.TargetMethod.ContainingType.Name.Equals("Queryable", StringComparison.Ordinal))
        {
            return true;
        }

        return outer.Instance is { Type: INamedTypeSymbol receiver } && IsQueryable(receiver);
    }

    /// <summary>Returns true when the type is a queryable; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is named IQueryable or implements it; otherwise false.</returns>
    private static bool IsQueryable(INamedTypeSymbol type)
    {
        if (type.Name.Equals("IQueryable", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name.Equals("IQueryable", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is a typed identifier; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the struct is named *Id or implements IStronglyTypedId; otherwise false.</returns>
    private static bool IsTypedIdentifier(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Struct)
        {
            return false;
        }

        if (type.Name.EndsWith("Id", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name.Equals("IStronglyTypedId", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the name of the collection the Contains checks.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <param name="method">The invoked Contains method.</param>
    /// <returns>The collection type name, or collection when it has none.</returns>
    private static string CollectionName(IInvocationOperation invocation, IMethodSymbol method)
    {
        ITypeSymbol? collection = null;
        if (method.ReducedFrom is null && method.IsStatic && invocation.Arguments.Length > 0)
        {
            collection = invocation.Arguments[0].Value.Type;
        }
        else
        {
            collection = invocation.Instance?.Type;
        }

        return collection is INamedTypeSymbol named ? named.Name : "collection";
    }

    /// <summary>Returns the location.</summary>
    /// <param name="invocation">The invocation operation whose target and arguments the check reads.</param>
    /// <returns>The location.</returns>
    private static Location InvocationLocation(IInvocationOperation invocation) =>
        invocation.Syntax is InvocationExpressionSyntax syntax && syntax.Expression is MemberAccessExpressionSyntax access
            ? access.Name.GetLocation()
            : invocation.Syntax.GetLocation();
}
