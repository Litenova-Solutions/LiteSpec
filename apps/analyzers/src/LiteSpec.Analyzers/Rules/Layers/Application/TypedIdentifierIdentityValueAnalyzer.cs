using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2037: a queryable expression never reads the underlying value of the document's own typed identifier.</summary>
/// <follows>litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypedIdentifierIdentityValueAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2037);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the typed identifier identity value analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterOperationAction(c => Analyze(c, scope), OperationKind.PropertyReference, OperationKind.FieldReference);
    }

    /// <summary>Analyzes the member access.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(OperationAnalysisContext context, FileScope scope)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        var value = (IMemberReferenceOperation)context.Operation;
        if (!value.Member.Name.Equals("Value", StringComparison.Ordinal))
        {
            return;
        }

        if (value.Instance is not IMemberReferenceOperation identity
            || !identity.Member.Name.Equals("Id", StringComparison.Ordinal))
        {
            return;
        }

        if (identity.Type is not INamedTypeSymbol identifier || !IsTypedIdentifier(identifier))
        {
            return;
        }

        if (identity.Instance is not IParameterReferenceOperation parameter || !IsLambdaParameter(parameter))
        {
            return;
        }

        if (!InsideQueryableExpression(value))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2037, MemberLocation(value), parameter.Parameter.Type.Name, identifier.Name));
    }

    /// <summary>Returns true when the parameter belongs to a lambda; otherwise false.</summary>
    /// <param name="parameter">The parameter reference the identity member hangs off.</param>
    /// <returns>True when the parameter is declared by an anonymous function; otherwise false.</returns>
    private static bool IsLambdaParameter(IParameterReferenceOperation parameter) =>
        parameter.Parameter.ContainingSymbol is IMethodSymbol method
        && method.MethodKind == MethodKind.AnonymousFunction;

    /// <summary>Returns true when the access sits in a lambda passed directly to a queryable operator; otherwise false.</summary>
    /// <param name="operation">The member access under inspection.</param>
    /// <returns>True when an enclosing lambda converts to an expression tree argument of a queryable operator; otherwise false.</returns>
    private static bool InsideQueryableExpression(IOperation operation)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
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

    /// <summary>Returns the location.</summary>
    /// <param name="value">The member access operation whose Value name the check reports.</param>
    /// <returns>The location.</returns>
    private static Location MemberLocation(IMemberReferenceOperation value) =>
        value.Syntax is MemberAccessExpressionSyntax access
            ? access.Name.GetLocation()
            : value.Syntax.GetLocation();
}
