using LiteSpec.Analyzers.Rules.Traceability;
using LiteSpec.Analyzers.Tests.Support;
using static LiteSpec.Analyzers.Tests.Support.TraceabilityFixtures;

namespace LiteSpec.Analyzers.Tests.Traceability;

/// <summary>Tests that a plain class carrying an automation mark is an automation host: marked it is silent, unmarked it needs a mark, and two marks count.</summary>
public sealed class AutomationMarkTests
{
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5001 accepts a plain class carrying its automation mark")]
    public Task AcceptsAMarkedPlainClass() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>automation/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

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
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5001 ignores a marked class in Domain, which is no automation")]
    public Task IgnoresAMarkedClassInDomain() =>
        Verify.AnalyzerWithRecordsAsync<MarkPresenceAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>automation/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Domain");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 accepts one automation mark on a plain class")]
    public Task AcceptsOneAutomationMark() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>automation/billing.charge-dues</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5003 reports a plain class carrying two automation marks")]
    public Task ReportsTwoAutomationMarks() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>automation/billing.charge-dues</implements>
                /// <implements>automation/billing.charge-dues</implements>
                internal sealed class {|LS5003:ChargeDuesJob|}
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Infrastructure");

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "LS5002 reports an automation mark on a class in Domain, which takes no mark")]
    public Task ReportsAnAutomationMarkInDomain() =>
        Verify.AnalyzerWithRecordsAsync<MarkLinkAnalyzer>(
            [("Source.cs", """
                namespace Shop.Billing;

                /// <implements>{|LS5002:automation/billing.charge-dues|}</implements>
                public sealed class ChargeDuesJob
                {
                    public void Run()
                    {
                    }
                }
                """)],
            [ProjectFile(), Automation("automation/billing.charge-dues", "Charge dues")],
            assemblyName: "Shop.Domain");
}
