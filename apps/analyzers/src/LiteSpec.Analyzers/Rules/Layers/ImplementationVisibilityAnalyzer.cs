using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2001: Application, Infrastructure and WebApi keep implementation types internal sealed.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-implementation-types-internal</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplementationVisibilityAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2001);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.WebApi || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the implementation visibility analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
        {
            return;
        }

        if (!IsImplementation(type) || ((type.IsSealed || type.IsStatic) && IsHidden(type)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2001, type.Locations[0], type.Name));
    }

    /// <summary>Returns true when the type is implementation; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is implementation; otherwise false.</returns>
    private static bool IsImplementation(INamedTypeSymbol type)
    {
        if (HasImplementationSuffix(type.Name))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name is "IEndpoint" or "ICommandHandler" or "IQueryHandler" or "IEventHandler" or "IValidator")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the name has implementation suffix; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>True when the name has implementation suffix; otherwise false.</returns>
    private static bool HasImplementationSuffix(string name) =>
        name.EndsWith("Handler", StringComparison.Ordinal)
        || name.EndsWith("PostHandler", StringComparison.Ordinal)
        || name.EndsWith("PreHandler", StringComparison.Ordinal)
        || name.EndsWith("Validator", StringComparison.Ordinal)
        || name.EndsWith("Endpoint", StringComparison.Ordinal)
        || name.EndsWith("Definition", StringComparison.Ordinal)
        || name.EndsWith("Repository", StringComparison.Ordinal)
        || name.EndsWith("Configuration", StringComparison.Ordinal)
        || name.EndsWith("Projection", StringComparison.Ordinal);

    /// <summary>Returns true when the type is hidden; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is hidden; otherwise false.</returns>
    private static bool IsHidden(INamedTypeSymbol type) =>
        type.DeclaredAccessibility is Accessibility.Private
            or Accessibility.Internal
            or Accessibility.ProtectedAndInternal
            or Accessibility.NotApplicable;
}
