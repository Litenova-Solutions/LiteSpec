using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2017, unknown declared layer names.</summary>
public sealed class LayerNameAnalyzerTests
{
    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 reports an unknown LiteSpecLayer value on the first token")]
    public Task ReportsAnUnknownLayerValue() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            {|LS2017:namespace|} Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Worker", layer: "Staging");

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

    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 accepts Worker")]
    public Task AcceptsWorker() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            namespace Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Core", layer: "Worker");

    /// <proves>litespec/backend/dotnet/architecture#use-five-application-projects</proves>
    [Fact(DisplayName = "LS2017 accepts the None opt-out")]
    public Task AcceptsTheNoneOptOut() =>
        Verify.AnalyzerAsync<LayerNameAnalyzer>("""
            namespace Shop.Billing;

            internal sealed class ChargeDuesJob
            {
            }
            """, assemblyName: "Shop.Worker", layer: "None");

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
