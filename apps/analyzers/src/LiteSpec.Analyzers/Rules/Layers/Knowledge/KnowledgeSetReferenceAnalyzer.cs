using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Knowledge;

/// <summary>LS2038: a module reads knowledge through the knowledge model, never by naming a knowledge set type.</summary>
/// <follows>litespec/backend/knowledge#read-knowledge-through-its-model</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KnowledgeSetReferenceAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2038);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the knowledge set reference analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var map = CodeMap.For(context.Compilation, context.Options);
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

        if (global::LiteSpec.Analyzers.LayerMap.Knowledge.IsModelFile(node.SyntaxTree.FilePath) || global::LiteSpec.Analyzers.LayerMap.Knowledge.IsSetFile(node.SyntaxTree.FilePath))
        {
            return;
        }

        if (map.ModuleOf(node.SyntaxTree.FilePath) is null)
        {
            return;
        }

        if (!References.TryResolve(context, node, type => global::LiteSpec.Analyzers.LayerMap.Knowledge.SetOf(type, context.Compilation) is not null, out var name, out var type)
            || type is null
            || global::LiteSpec.Analyzers.LayerMap.Knowledge.SetOf(type, context.Compilation) is not string set)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2038, node.Identifier.GetLocation(), name, set));
    }
}
