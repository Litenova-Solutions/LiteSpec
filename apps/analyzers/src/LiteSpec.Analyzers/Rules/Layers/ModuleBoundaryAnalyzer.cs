using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2020: a module names another module's types only through its Contracts folder.</summary>
/// <follows>litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleBoundaryAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2020);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer != Layer.None && layer != Layer.Test;

    /// <summary>
    ///     Registers the analysis actions for the module boundary analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var map = CodeMap.For(context.Compilation, context.Options);
        if (map.Layout == BackendLayout.LayerProjects)
        {
            return;
        }

        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (map.ModuleOf(node.SyntaxTree.FilePath) is not string module)
        {
            return;
        }

        if (!References.TryResolve(context, node, type => OutsideContractsOf(type, module, map, context.Compilation) is not null, out var name, out var type)
            || type is null
            || OutsideContractsOf(type, module, map, context.Compilation) is not string other)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2020, node.Identifier.GetLocation(), name, CodeMap.FolderName(other)));
    }

    /// <summary>Returns the other module holding the type outside its Contracts folder, or null when none does.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="module">The module folder holding the file under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The other module folder, or null when the type is reached through Contracts.</returns>
    private static string? OutsideContractsOf(INamedTypeSymbol type, string module, CodeMap map, Compilation compilation)
    {
        if (CodeMap.SourceFileOf(type, compilation) is not string file || CodeMap.IsContracts(file))
        {
            return null;
        }

        if (map.ModuleOf(file) is string other && !other.Equals(module, StringComparison.Ordinal))
        {
            return other;
        }

        return null;
    }
}
