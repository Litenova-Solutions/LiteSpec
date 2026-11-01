using System;
using System.Collections.Immutable;
using System.IO;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1013: a domain event is declared in a file under its aggregate's Events folder.</summary>
/// <follows>litespec/backend/domain#organize-a-module-by-aggregate-and-concept</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainEventLocationAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1013);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the domain event location analyzer rule with the
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
        if (type.TypeKind == TypeKind.Interface || !ImplementsDomainEvent(type))
        {
            return;
        }

        var filePath = LocationPath(type);
        if (filePath.Length == 0 || IsUnderEventsFolder(filePath))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS1013, type.Locations[0], type.Name, Path.GetFileName(filePath)));
    }

    /// <summary>Returns true when the implements-domain-event condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the implements-domain-event condition holds; otherwise false.</returns>
    private static bool ImplementsDomainEvent(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IDomainEvent")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the path.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The path as text.</returns>
    private static string LocationPath(INamedTypeSymbol type)
    {
        if (type.Locations.Length == 0)
        {
            return string.Empty;
        }

        return type.Locations[0].SourceTree?.FilePath ?? string.Empty;
    }

    /// <summary>Returns true when the file path is under events folder; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when the file path is under events folder; otherwise false.</returns>
    private static bool IsUnderEventsFolder(string filePath)
    {
        foreach (var segment in filePath.Split('/', '\\'))
        {
            if (segment.Equals("Events", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
