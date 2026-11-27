using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Style;

/// <summary>LS3005: a class or struct declares no primary constructor.</summary>
/// <follows>litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS3005);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the primary constructor analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSyntaxNodeAction(
            c => AnalyzeType(c, scope), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeType(SyntaxNodeAnalysisContext context, FileScope scope)
    {
        var type = (TypeDeclarationSyntax)context.Node;
        if (!scope.AppliesTo(type.SyntaxTree))
        {
            return;
        }

        if (type.ParameterList is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS3005, type.ParameterList.GetLocation(), type.Identifier.ValueText));
    }
}
