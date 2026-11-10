using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the job guard and schedule checks against hand-built models.</summary>
public sealed class JobGuardCheckTests
{
    [Fact]
    public void ManualJobWithoutGuardReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "manual", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void ManualJobWithGuardPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "manual", "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void ScheduledJobWithoutGuardPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void ScheduledJobWithoutAScheduleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "scheduled", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.True(Reports(model, "schedule-invalid", "runs on time with no structured schedule"));
    }

    [Fact]
    public void ScheduledJobWithoutAReadableScheduleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"at": "nightly"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("schedule-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void ScheduledJobWithCronPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"cron": "0 3 * * *"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("schedule-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void ManualJobWithAnUnreadableScheduleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.yaml"), """{"status": "reviewed", "kind": "manual", "guard": "An operator starts the scan.", "schedule": {"at": "nightly"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.True(Reports(model, "schedule-invalid", "carries a schedule no tool can read"));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <param name="code">The finding code to look for.</param>
    /// <param name="text">The text the finding message must contain.</param>
    /// <returns>True when a finding carries the code and the text.</returns>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
