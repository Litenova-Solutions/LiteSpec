using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>LS2027: an Application file names no archive, serialization or document library.</summary>
/// <follows>litespec/backend/architecture#produce-file-formats-in-infrastructure</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InfrastructureWorkAnalyzer : LayerAnalyzer
{
    // The user-facing copy of this list lives on the LS2027 rule page.
    /// <summary>The file, archive and document namespaces that belong in Infrastructure.</summary>
    private static readonly string[] InfrastructureNamespaces =
    [
        "System.IO.Compression",
        "System.Text.Json",
        "CsvHelper",
        "Csv",
        "ClosedXML",
        "EPPlus",
        "NPOI",
        "iTextSharp",
        "itext",
        "PdfSharp",
        "QuestPDF",
        "DinkToPdf",
        "PuppeteerSharp",
        "SkiaSharp",
        "SixLabors",
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2027);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Application;

    /// <summary>
    ///     Registers the analysis actions for the infrastructure work analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (!References.TryResolve(context, node, type => IsInfrastructure(type.ContainingNamespace, out _), out var name, out var type)
            || type is null
            || !IsInfrastructure(type.ContainingNamespace, out var namespaceName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2027, node.Identifier.GetLocation(), name, namespaceName));
    }

    /// <summary>Returns true when the namespace does file, archive or document work; otherwise false.</summary>
    /// <param name="containing">The containing namespace, or null when the compilation does not provide it.</param>
    /// <param name="namespaceName">The namespace name the check resolved.</param>
    /// <returns>True when the namespace matches the infrastructure list.</returns>
    private static bool IsInfrastructure(INamespaceSymbol? containing, out string namespaceName)
    {
        if (containing is null || containing.IsGlobalNamespace)
        {
            namespaceName = string.Empty;
            return false;
        }

        namespaceName = containing.ToDisplayString();
        foreach (var prefix in InfrastructureNamespaces)
        {
            if (namespaceName.StartsWith(prefix, StringComparison.Ordinal)
                && (namespaceName.Length == prefix.Length || namespaceName[prefix.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }
}
