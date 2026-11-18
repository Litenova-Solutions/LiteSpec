using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Application;

/// <summary>
///     LS2022: user code names no event mediator and no event publisher. The pipeline publishes each outbound
///     integration event a use case raises, so an event goes out only through the declared translation. The
///     platform holds that pipeline and is exempt, with the shared kernel of a one-modules-project solution;
///     every other type is checked, entry points included.
/// </summary>
/// <follows>litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventPublishedByUserCodeAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2022);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the event published by user code analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        if (IsPipelineAssembly(context.Compilation.AssemblyName))
        {
            return;
        }

        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    /// <summary>Analyzes a written name that resolves to an event mediator or event publisher type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (!References.TryResolve(context, node, IsPublisher, out var name, out _)
            || context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol is not INamedTypeSymbol)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2022, node.Identifier.GetLocation(), name));
    }

    /// <summary>Returns true when the type is the event mediator or an event publisher, by its simple name; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is the event mediator or an event publisher; otherwise false.</returns>
    private static bool IsPublisher(INamedTypeSymbol type) =>
        type.Name == "IEventMediator" || type.Name.EndsWith("EventPublisher", StringComparison.Ordinal);

    /// <summary>Returns true when the assembly holds the pipeline: the platform, or the shared kernel of a one-modules-project solution; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name, or null when it has none.</param>
    /// <returns>True when the assembly name ends with the Platform or SharedKernel segment; otherwise false.</returns>
    private static bool IsPipelineAssembly(string? assemblyName) =>
        assemblyName is not null
        && (assemblyName.Equals("Platform", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Platform", StringComparison.Ordinal)
            || assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
            || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal));
}
