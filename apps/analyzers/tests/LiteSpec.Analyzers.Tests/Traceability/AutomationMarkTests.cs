using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests that a plain class carrying a job mark is a job host: marked it is silent, unmarked it needs a mark, and two marks count.</summary>
public sealed class AutomationMarkTests
{
    /// <summary>LS5001 accepts a plain class carrying its job mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5001 accepts a plain class carrying its job mark")]
    public Task AcceptsAMarkedPlainClass() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>job/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <summary>LS5001 reports the same class unmarked as a technical class.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5001 reports the same class unmarked as a technical class")]
    public Task ReportsAnUnmarkedPlainClass() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                internal sealed class {|LS5001:ChargeDuesJob|}
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <summary>LS5001 ignores a marked class in Domain, which is no job.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5001 ignores a marked class in Domain, which is no job")]
    public Task IgnoresAMarkedClassInDomain() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>job/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Domain");

    /// <summary>LS5002 accepts one job mark on a plain class.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts one job mark on a plain class")]
    public Task AcceptsOneJobMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>job/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <summary>LS5003 reports a plain class carrying two job marks.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports a plain class carrying two job marks")]
    public Task ReportsTwoJobMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>job/billing.charge-dues</implements>
                /// <implements>job/billing.charge-dues</implements>
                internal sealed class {|LS5003:ChargeDuesJob|}
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <summary>LS5002 reports a job mark on a class in Domain, which takes no mark.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports a job mark on a class in Domain, which takes no mark")]
    public Task ReportsAJobMarkInDomain() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>{|LS5002:job/billing.charge-dues|}</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Job("job/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Domain");
}
