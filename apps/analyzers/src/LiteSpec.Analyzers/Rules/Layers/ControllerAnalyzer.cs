using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2003: WebApi maps operations through endpoints, never through MVC controllers.</summary>
/// <follows>litespec/backend/dotnet/api#exclude-mvc-controllers-from-the-stack</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2003);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.WebApi;

    /// <summary>Registers the analysis actions for the controller analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var controllerBase = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        var controller = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controller");
        var apiController = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ApiControllerAttribute");
        context.RegisterSymbolAction(
            symbol => Analyze(symbol, controllerBase, controller, apiController), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="controllerBase">The controller base supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="controller">The controller supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="apiController">The api controller supplied for the check, or null when the compilation does not provide it.</param>
    private static void Analyze(
        SymbolAnalysisContext context,
        INamedTypeSymbol? controllerBase,
        INamedTypeSymbol? controller,
        INamedTypeSymbol? apiController)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (Is(current, controllerBase) || Is(current, controller))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2003, type.Locations[0], type.Name, current.Name));
                return;
            }
        }

        foreach (var attribute in type.GetAttributes())
        {
            if (Is(attribute.AttributeClass, apiController))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2003, type.Locations[0], type.Name, ShortName(attribute.AttributeClass!)));
                return;
            }
        }
    }

    /// <summary>Returns true when the candidate is applicable; otherwise false.</summary>
    /// <param name="candidate">The candidate supplied for the check, or null when the compilation does not provide it.</param>
    /// <param name="known">The known supplied for the check, or null when the compilation does not provide it.</param>
    /// <returns>True when the candidate is applicable; otherwise false.</returns>
    private static bool Is(INamedTypeSymbol? candidate, INamedTypeSymbol? known) =>
        candidate != null && known != null
        && SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, known);

    /// <summary>Returns the name.</summary>
    /// <param name="attribute">The attribute type under inspection.</param>
    /// <returns>The name as text.</returns>
    private static string ShortName(INamedTypeSymbol attribute)
    {
        var name = attribute.Name;
        const string suffix = "Attribute";
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }
}
