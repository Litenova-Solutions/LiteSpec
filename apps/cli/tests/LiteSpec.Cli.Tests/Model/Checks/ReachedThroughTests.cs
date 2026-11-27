using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves that only a use case nothing runs must name an entry point.</summary>
public sealed class ReachedThroughTests
{
    /// <summary>Use Case No Record Runs Must Name An Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void UseCaseNoRecordRunsMustNameAnEntryPoint()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.yaml"), """{"kind": "command", "steps": [{"statement": "The desk sends the due notice."}]}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no via", StringComparison.Ordinal));
    }

    /// <summary>Use Case A Job Runs Needs No Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void UseCaseAJobRunsNeedsNoEntryPoint()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.yaml"), """{"kind": "command", "steps": [{"statement": "The desk sends the due notice."}]}"""),
            TestModel.Record("job", "job/lending.send-due-notices", Path.Combine(temp.Path, "job.yaml"), """{"kind": "scheduled", "effects": [{"runs": "use-case/lending.send-due-notice"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no via", StringComparison.Ordinal));
    }

    /// <summary>A use case a reaction runs only on refusal of the use case it runs needs no entry point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void UseCaseRunOnRefusalNeedsNoEntryPoint()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.record-fine-refused", Path.Combine(temp.Path, "refused.yaml"), """{"kind": "command", "steps": [{"statement": "The desk records the refused fine."}]}"""),
            TestModel.Record("reaction", "reaction/lending.settle-fine-on-loan-returned", Path.Combine(temp.Path, "reaction.yaml"), """{"triggeredBy": ["domain-event/loan.loan-returned"], "effects": [{"runs": "use-case/billing.settle-fine", "timing": "async", "onRefusal": "use-case/lending.record-fine-refused"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no via", StringComparison.Ordinal));
    }

    /// <summary>Use Case Via Modules Only Needs No Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void UseCaseViaModulesOnlyNeedsNoEntryPoint()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.read-expense", Path.Combine(temp.Path, "read.yaml"), """{"kind": "query", "via": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no via", StringComparison.Ordinal));
    }
}
