using Record = LiteSpec.Model.Record;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the automation-trigger-unknown check against hand-built models.</summary>
public sealed class ReactionTriggerCheckTests
{
    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_Without_A_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""));

        Assert.Contains("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_With_An_Unresolved_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.no-such-event"}, "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""));

        Assert.Contains("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_With_A_Non_Event_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "use-case/lending.borrow-copy"}, "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));

        Assert.Contains("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_With_An_Event_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "event/loan.loan-borrowed"}, "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"status": "reviewed", "meaning": "A copy was lent.", "carries": []}"""));

        Assert.DoesNotContain("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_With_An_Integration_Event_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": {"event": "integration-event/loan-exported"}, "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("integration-event", "integration-event/loan-exported", Path.Combine(temp.Path, "exported.md"), """{"status": "reviewed"}"""));

        Assert.DoesNotContain("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Automation_With_Two_Event_Triggers_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("automation", "automation/lending.assign-seat-on-seat-sold", Path.Combine(temp.Path, "assign.md"), """{"status": "reviewed", "triggeredBy": {"event": ["event/ticket.seat-sold", "event/ticket.seat-released"]}, "effects": [{"runs": "use-case/lending.assign-seat", "timing": "async"}]}"""),
            TestModel.Record("event", "event/ticket.seat-sold", Path.Combine(temp.Path, "sold.md"), """{"status": "reviewed", "meaning": "A seat was sold.", "carries": []}"""),
            TestModel.Record("event", "event/ticket.seat-released", Path.Combine(temp.Path, "released.md"), """{"status": "reviewed", "meaning": "A sold seat was released.", "carries": []}"""));

        Assert.DoesNotContain("automation-trigger-unknown", TestModel.Codes(model));
    }

    /// <proves>litespec/core/automation#trigger-every-automation-from-its-declared-trigger</proves>
    [Fact]
    public void Command_Without_A_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));

        Assert.DoesNotContain("automation-trigger-unknown", TestModel.Codes(model));
    }
}
