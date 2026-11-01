using Record = LiteSpec.Model.Record;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the reaction-trigger-unknown check against hand-built models.</summary>
public sealed class ReactionTriggerCheckTests
{
    /// <summary>Reaction Without A Trigger Reports.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_Without_A_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""));

        Assert.Contains("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Reaction With An Unresolved Trigger Reports.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_With_An_Unresolved_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": ["event/loan.no-such-event"], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""));

        Assert.Contains("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Reaction With A Non Event Trigger Reports.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_With_A_Non_Event_Trigger_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": ["use-case/lending.borrow-copy"], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));

        Assert.Contains("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Reaction With An Event Trigger Passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_With_An_Event_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", Path.Combine(temp.Path, "loan.md"), """{"status": "reviewed", "meaning": "A copy was lent.", "carries": []}"""));

        Assert.DoesNotContain("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Reaction With An Integration Event Trigger Passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_With_An_Integration_Event_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.remind-overdue-on-loan-due", Path.Combine(temp.Path, "remind.md"), """{"status": "reviewed", "triggeredBy": ["integration-event/loan-exported"], "effects": [{"runs": "use-case/lending.notify-overdue", "timing": "async"}]}"""),
            TestModel.Record("integration-event", "integration-event/loan-exported", Path.Combine(temp.Path, "exported.md"), """{"status": "reviewed"}"""));

        Assert.DoesNotContain("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Reaction With Two Event Triggers Passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Reaction_With_Two_Event_Triggers_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/lending.assign-seat-on-seat-sold", Path.Combine(temp.Path, "assign.md"), """{"status": "reviewed", "triggeredBy": ["event/ticket.seat-sold", "event/ticket.seat-released"], "effects": [{"runs": "use-case/lending.assign-seat", "timing": "async"}]}"""),
            TestModel.Record("event", "event/ticket.seat-sold", Path.Combine(temp.Path, "sold.md"), """{"status": "reviewed", "meaning": "A seat was sold.", "carries": []}"""),
            TestModel.Record("event", "event/ticket.seat-released", Path.Combine(temp.Path, "released.md"), """{"status": "reviewed", "meaning": "A sold seat was released.", "carries": []}"""));

        Assert.DoesNotContain("reaction-trigger-unknown", TestModel.Codes(model));
    }

    /// <summary>Command Without A Trigger Passes.</summary>
    /// <proves>litespec/core/automation#trigger-every-reaction-from-its-declared-events</proves>
    [Fact]
    public void Command_Without_A_Trigger_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.md"), """{"status": "reviewed", "kind": "command"}"""));

        Assert.DoesNotContain("reaction-trigger-unknown", TestModel.Codes(model));
    }
}
