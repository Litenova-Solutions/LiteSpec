using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Naming;

/// <summary>LS2007: a reaction or job names its trigger.</summary>
/// <follows>litespec/workspace/naming#use-architectural-suffixes</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutomationNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2007);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer == ArchitectureLayer.Application || layer == ArchitectureLayer.Infrastructure || layer == ArchitectureLayer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the automation naming analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => Analyze(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (IsReaction(type) && !HasReactionName(type.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2007, type.Locations[0], type.Name, "reaction", "{Action}On{Fact}Handler"));
        }

        if (IsJob(type) && !HasJobName(type.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2007, type.Locations[0], type.Name, "job", "*Job, *Worker, or *Recurring*"));
        }
    }

    /// <summary>Returns true when the type is reaction; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is reaction; otherwise false.</returns>
    private static bool IsReaction(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name is "IEventHandler" or "IDomainEventReaction")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is job; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is job; otherwise false.</returns>
    private static bool IsJob(INamedTypeSymbol type)
    {
        // A one-shot startup hook implementing bare IHostedService is host-lifecycle
        // wiring, not a job; only a BackgroundService carries scheduled work.
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.Name == "BackgroundService")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the name has reaction name; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>True when the name has reaction name; otherwise false.</returns>
    private static bool HasReactionName(string name)
    {
        // {Action}On{Fact}Handler, such as NotifySubscribersOnPostPublishedHandler.
        const string suffix = "Handler";
        if (!name.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        var marker = name.IndexOf("On", StringComparison.Ordinal);
        return marker > 0 && marker + "On".Length < name.Length - suffix.Length;
    }

    /// <summary>Returns true when the name has job name; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>True when the name has job name; otherwise false.</returns>
    private static bool HasJobName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal)
        || name.EndsWith("Worker", StringComparison.Ordinal)
        || name.Contains("Recurring", StringComparison.Ordinal);
}
