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
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.yaml"), """{"kind": "command"}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no reachedThrough", StringComparison.Ordinal));
    }

    /// <summary>Use Case A Job Runs Needs No Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void UseCaseAJobRunsNeedsNoEntryPoint()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.yaml"), """{"kind": "command"}"""),
            TestModel.Record("job", "job/lending.send-due-notices", Path.Combine(temp.Path, "job.yaml"), """{"kind": "scheduled", "effects": [{"runs": "use-case/lending.send-due-notice"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no reachedThrough", StringComparison.Ordinal));
    }
}
