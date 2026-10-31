using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the automation guard and schedule checks against hand-built models.</summary>
public sealed class AutomationGuardCheckTests
{
    [Fact]
    public void Manual_Automation_Without_Guard_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"manual": true}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("automation-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Manual_Automation_With_Guard_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"manual": true}, "guard": "An operator starts the scan.", "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("automation-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Scheduled_Automation_Without_Guard_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"schedule": {"every": "PT1H"}}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("automation-guard-missing", TestModel.Codes(model));
    }

    [Fact]
    public void Event_Automation_Holding_Guard_Reports_Invalid_Field()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.send-due-notice-on-loan-borrowed", Path.Combine(temp.Path, "notice.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "guard": "The notice was not sent yet.", "effects": [{"sends": "notification/lending.loan-due"}]}"""));

        Assert.True(Reports(model, "field-invalid", "holds guard with an event trigger"));
    }

    [Fact]
    public void Scheduled_Automation_Without_A_Readable_Schedule_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"schedule": {"at": "nightly"}}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.Contains("schedule-invalid", TestModel.Codes(model));
    }

    [Fact]
    public void Scheduled_Automation_With_Cron_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.scan-due", Path.Combine(temp.Path, "scan.md"), """{"status": "reviewed", "triggeredBy": {"schedule": {"cron": "0 3 * * *"}}, "effects": [{"runs": "use-case/lending.mark-due", "timing": "async"}]}"""));

        Assert.DoesNotContain("schedule-invalid", TestModel.Codes(model));
    }

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
