using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2017, unknown declared layer names.</summary>
public sealed class LayerNameAnalyzerTests
{
    /// <summary>LS2017 reports an unknown LiteSpecLayer value on the first token.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 reports an unknown LiteSpecLayer value on the first token")]
    public Task ReportsAnUnknownLayerValue() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            {|LS2017:namespace|} Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Worker", layer: "Staging");

    /// <summary>LS2017 reports once on the first file across two files.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 reports once on the first file across two files")]
    public Task ReportsOnceAcrossTwoFiles() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>(
            [
                ("First.cs", """
                    {|LS2017:namespace|} Shop.Billing;

                    internal sealed class ChargeDuesJob
                    {
                    }
                    """),
                ("Second.cs", """
                    namespace Shop.Billing;

                    internal sealed class CollectDuesJob
                    {
                    }
                    """),
            ],
            assemblyName: "Shop.Worker",
            layer: "Staging");

    /// <summary>LS2017 accepts Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 accepts Worker")]
    public Task AcceptsWorker() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            namespace Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Core", layer: "Worker");

    /// <summary>LS2017 accepts the None opt-out.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 accepts the None opt-out")]
    public Task AcceptsTheNoneOptOut() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            namespace Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Worker", layer: "None");

    /// <summary>LS2017 stays quiet without a declared value.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 stays quiet without a declared value")]
    public Task StaysQuietWithoutADeclaredValue() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            namespace Shop.Core;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Core");
}
