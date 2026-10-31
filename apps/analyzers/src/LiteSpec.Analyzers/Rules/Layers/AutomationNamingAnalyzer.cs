using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2007: an automation names its trigger.</summary>
/// <follows>litespec/workspace/naming#use-architectural-suffixes</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AutomationNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2007);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (IsEventAutomation(type) && !HasEventAutomationName(type.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2007, type.Locations[0], type.Name, "event automation", "{Action}On{Fact}Handler"));
        }

        if (IsScheduledAutomation(type) && !HasScheduledAutomationName(type.Name))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2007, type.Locations[0], type.Name, "scheduled automation", "*Job, *Worker, or *Recurring*"));
        }
    }

    private static bool IsEventAutomation(INamedTypeSymbol type)
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

    private static bool IsScheduledAutomation(INamedTypeSymbol type)
    {
        // A one-shot startup hook implementing bare IHostedService is host-lifecycle
        // wiring, not an automation; only a BackgroundService carries scheduled work.
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.Name == "BackgroundService")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEventAutomationName(string name)
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

    private static bool HasScheduledAutomationName(string name) =>
        name.EndsWith("Job", StringComparison.Ordinal)
        || name.EndsWith("Worker", StringComparison.Ordinal)
        || name.Contains("Recurring", StringComparison.Ordinal);
}
