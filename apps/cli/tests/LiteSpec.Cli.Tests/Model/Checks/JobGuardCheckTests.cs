using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the job guard and schedule checks against hand-built models.</summary>
public sealed class JobGuardCheckTests
{
    [Fact]
    public void Manual_Job_Without_Guard_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "manual", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Manual_Job_With_Guard_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "manual", "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Scheduled_Job_Without_Guard_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("job-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Scheduled_Job_Without_A_Schedule_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "scheduled", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.True(Reports(model, "schedule-invalid", "runs on time with no structured schedule"));
    }

    [Fact]
    public void Scheduled_Job_Without_A_Readable_Schedule_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"at": "nightly"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("schedule-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void Scheduled_Job_With_Cron_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "scheduled", "schedule": {"cron": "0 3 * * *"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("schedule-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void Manual_Job_With_An_Unreadable_Schedule_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "kind": "manual", "guard": "An operator starts the scan.", "schedule": {"at": "nightly"}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.True(Reports(model, "schedule-invalid", "carries a schedule no tool can read"));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
