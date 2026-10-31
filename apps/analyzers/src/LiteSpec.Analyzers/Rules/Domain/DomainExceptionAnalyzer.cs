using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1005 and LS1006: Domain rejects business violations with sealed DomainException subclasses.</summary>
/// <follows>litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainExceptionAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1005, Descriptors.LS1006);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the domain exception analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        var argumentExceptionType = compilation.GetTypeByMetadataName("System.ArgumentException");
        var unreachableExceptionType = compilation.GetTypeByMetadataName("System.Diagnostics.UnreachableException");

        context.RegisterSymbolAction(
            symbolContext => AnalyzeType(symbolContext, exceptionType),
            SymbolKind.NamedType);
        context.RegisterOperationAction(
            operationContext => AnalyzeThrow(operationContext, exceptionType, argumentExceptionType, unreachableExceptionType),
            OperationKind.Throw);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="exceptionType">The exception type supplied for the check, or null when the compilation does not provide it.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol? exceptionType)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
        {
            return;
        }

        if (exceptionType is null || !HasDomainExceptionAncestor(type.BaseType, exceptionType))
        {
            return;
        }

        if (!type.IsSealed || !type.Name.EndsWith("Exception", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1006, type.Locations[0], type.Name));
        }
    }

    /// <summary>Analyzes the throw.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="exceptionType">The exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="argumentExceptionType">The argument exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="unreachableExceptionType">The unreachable exception type supplied for the check, or null when the compilation does not provide it.</param>
    private static void AnalyzeThrow(
        OperationAnalysisContext context,
        INamedTypeSymbol? exceptionType,
        INamedTypeSymbol? argumentExceptionType,
        INamedTypeSymbol? unreachableExceptionType)
    {
        var thrown = ((IThrowOperation)context.Operation).Exception;
        if (thrown is null)
        {
            return;
        }

        CheckThrown(thrown, context, exceptionType, argumentExceptionType, unreachableExceptionType);
    }

    /// <summary>Checks the thrown.</summary>
    /// <param name="thrown">The thrown exception type under inspection.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="exceptionType">The exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="argumentExceptionType">The argument exception type supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="unreachableExceptionType">The unreachable exception type supplied for the check, or null when the compilation does not provide it.</param>
    private static void CheckThrown(
        IOperation thrown,
        OperationAnalysisContext context,
        INamedTypeSymbol? exceptionType,
        INamedTypeSymbol? argumentExceptionType,
        INamedTypeSymbol? unreachableExceptionType)
    {
        while (thrown is IConversionOperation conversion)
        {
            thrown = conversion.Operand;
        }

        if (thrown is IConditionalOperation conditional)
        {
            CheckThrown(conditional.WhenTrue, context, exceptionType, argumentExceptionType, unreachableExceptionType);

            if (conditional.WhenFalse is { } whenFalse)
            {
                CheckThrown(whenFalse, context, exceptionType, argumentExceptionType, unreachableExceptionType);
            }

            return;
        }

        if (thrown is ICoalesceOperation coalesce)
        {
            CheckThrown(coalesce.Value, context, exceptionType, argumentExceptionType, unreachableExceptionType);
            CheckThrown(coalesce.WhenNull, context, exceptionType, argumentExceptionType, unreachableExceptionType);
            return;
        }

        if (thrown is ISwitchExpressionOperation switchExpression)
        {
            foreach (var arm in switchExpression.Arms)
            {
                CheckThrown(arm.Value, context, exceptionType, argumentExceptionType, unreachableExceptionType);
            }

            return;
        }

        if (thrown is ILocalReferenceOperation || thrown is IParameterReferenceOperation)
        {
            return;
        }

        var thrownType = thrown.Type as INamedTypeSymbol;
        if (thrownType is null)
        {
            return;
        }

        if (exceptionType is not null && HasDomainExceptionAncestor(thrownType, exceptionType))
        {
            return;
        }

        if (DerivesFromOrEquals(thrownType, argumentExceptionType)
            || DerivesFromOrEquals(thrownType, unreachableExceptionType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1005, thrown.Syntax.GetLocation(), thrownType.Name));
    }

    /// <summary>Returns true when the start has domain exception ancestor; otherwise false.</summary>
    /// <param name="start">The start supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="exceptionType">The exception type this operation works with.</param>
    /// <returns>True when the start has domain exception ancestor; otherwise false.</returns>
    private static bool HasDomainExceptionAncestor(INamedTypeSymbol? start, INamedTypeSymbol exceptionType)
    {
        for (var current = start; current is not null; current = current.BaseType)
        {
            if (current.Name == "DomainException" && DerivesFromOrEquals(current, exceptionType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the derives-from-or-equals condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="baseType">The base type supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the derives-from-or-equals condition holds; otherwise false.</returns>
    private static bool DerivesFromOrEquals(ITypeSymbol? type, INamedTypeSymbol? baseType)
    {
        if (type is null || baseType is null)
        {
            return false;
        }

        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
