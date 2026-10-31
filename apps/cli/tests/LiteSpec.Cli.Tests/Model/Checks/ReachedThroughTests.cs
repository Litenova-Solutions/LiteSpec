using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves that only a use case nothing runs must name an entry point.</summary>
public sealed class ReachedThroughTests
{
    /// <summary>Use Case No Record Runs Must Name An Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void Use_Case_No_Record_Runs_Must_Name_An_Entry_Point()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "kind": "command"}"""));

        Assert.Contains(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no reachedThrough", StringComparison.Ordinal));
    }

    /// <summary>Use Case A Job Runs Needs No Entry Point.</summary>
    /// <proves>litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case</proves>
    [Fact]
    public void Use_Case_A_Job_Runs_Needs_No_Entry_Point()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.send-due-notice", Path.Combine(temp.Path, "notice.md"), """{"status": "draft", "kind": "command"}"""),
            TestModel.Record("job", "job/lending.send-due-notices", Path.Combine(temp.Path, "job.md"), """{"status": "draft", "kind": "scheduled", "effects": [{"runs": "use-case/lending.send-due-notice"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code == "record-invalid" && finding.Message.Contains("has no reachedThrough", StringComparison.Ordinal));
    }
}
