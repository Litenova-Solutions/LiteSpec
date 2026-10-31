using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the wave 6 contract checks: saga steps and drivers, typed inputs and payloads, scenario exercises, and door auth.</summary>
public sealed class BehaviorContractCheckTests
{
    [Fact]
    public void Saga_Step_Running_No_Record_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Saga(temp.Path, """{"id": "release", "runs": "use-case/booking.no-such-use-case", "compensation": {"mode": "none", "reason": "Nothing was written."}}"""));

        Assert.True(Reports(model, "reference-unknown", "runs 'use-case/booking.no-such-use-case' from step 'release'"));
    }

    [Fact]
    public void Saga_Step_Without_Compensation_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.release-hold", Path.Combine(temp.Path, "release.md"), """{"kind": "command"}"""),
            Saga(temp.Path, """{"id": "release", "runs": "use-case/booking.release-hold"}"""));

        Assert.True(Reports(model, "field-invalid", "holds step 'release' with no compensation"));
    }

    [Fact]
    public void Saga_Step_With_None_Compensation_And_No_Reason_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.release-hold", Path.Combine(temp.Path, "release.md"), """{"kind": "command"}"""),
            Saga(temp.Path, """{"id": "release", "runs": "use-case/booking.release-hold", "compensation": {"mode": "none"}}"""));

        Assert.True(Reports(model, "field-invalid", "with a none compensation naming no reason"));
    }

    [Fact]
    public void Saga_Driven_By_No_Record_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.release-hold", Path.Combine(temp.Path, "release.md"), """{"kind": "command"}"""),
            DrivenSaga(temp.Path, """{"automation": "automation/booking.no-such-automation"}"""));

        Assert.True(Reports(model, "reference-unknown", "is driven by 'automation/booking.no-such-automation'"));
    }

    [Fact]
    public void Saga_Driven_By_A_Person_Without_A_Runbook_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.release-hold", Path.Combine(temp.Path, "release.md"), """{"kind": "command"}"""),
            DrivenSaga(temp.Path, """{"person": "actor/clerk", "dueWithin": "P1D"}"""));

        Assert.True(Reports(model, "field-invalid", "is driven by a person with no runbook"));
    }

    [Fact]
    public void Saga_Driven_By_A_Person_With_A_Bad_Duration_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.release-hold", Path.Combine(temp.Path, "release.md"), """{"kind": "command"}"""),
            TestModel.Record("runbook", "runbook/refund-stay", Path.Combine(temp.Path, "refund.md"), """{"trigger": "A guest cancels after payment."}"""),
            DrivenSaga(temp.Path, """{"person": "actor/clerk", "runbook": "runbook/refund-stay", "dueWithin": "one day"}"""));

        Assert.True(Reports(model, "field-invalid", "with dueWithin 'one day', which is not an ISO 8601 duration"));
    }

    [Fact]
    public void Use_Case_Input_Without_A_Type_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "nights", "meaning": "The nights to hold.", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "holds input field 'nights' with no type"));
    }

    [Fact]
    public void Use_Case_Input_With_An_Unknown_Type_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "nights", "meaning": "The nights to hold.", "type": "count", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "with type 'count' outside"));
    }

    [Fact]
    public void Use_Case_Id_Input_Without_An_Of_Record_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "of type id with no of record"));
    }

    [Fact]
    public void Use_Case_Id_Input_Naming_No_Record_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "of": "aggregate/inventory.no-such-room", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "of 'aggregate/inventory.no-such-room', which no record holds"));
    }

    [Fact]
    public void Use_Case_Choice_Input_Without_Choices_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "bed", "meaning": "The bed shape.", "type": "choice", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "of type choice with no of choices"));
    }

    [Fact]
    public void Typed_Inputs_With_Type_Required_And_Of_Pass()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/inventory.room", Path.Combine(temp.Path, "room.md"), """{"kind": "entity", "states": [{"name": "Free", "meaning": "The room is free."}]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "of": "aggregate/inventory.room", "required": true}, {"name": "nights", "meaning": "The nights to hold.", "type": "integer", "required": true}, {"name": "bed", "meaning": "The bed shape.", "type": "choice", "of": ["single", "double"], "required": false}]}"""));

        Assert.False(Reports(model, "field-invalid", "holds input field"));
    }

    [Fact]
    public void Integration_Event_With_A_Bad_Dataschema_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/booking-held", Path.Combine(temp.Path, "held.md"), """{"dataschema": "not a url"}"""));

        Assert.True(Reports(model, "field-invalid", "holds dataschema 'not a url', which is no URL"));
    }

    [Fact]
    public void Scenario_Exercising_No_Rule_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/hold-room.second-hold-is-refused", Path.Combine(temp.Path, "hold.md"), """{"given": "Ben already holds the room.", "when": "Ana holds the room.", "then": "The hold is refused.", "result": "result/hold-room.refused", "exercises": ["rule/hold.no-such-rule"]}"""));

        Assert.True(Reports(model, "reference-unknown", "exercises 'rule/hold.no-such-rule'"));
    }

    [Fact]
    public void Scenario_Exercising_A_Rule_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/hold.one-hold-per-room-night", Path.Combine(temp.Path, "hold.md"), """{"kind": "invariant", "statement": "A room night holds one hold.", "governs": ["aggregate/booking.hold"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold-room.md"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/hold-room.second-hold-is-refused", Path.Combine(temp.Path, "hold-room.md"), """{"given": "Ben already holds the room.", "when": "Ana holds the room.", "then": "The hold is refused.", "result": "result/hold-room.refused", "exercises": ["rule/hold.one-hold-per-room-night"]}"""));

        Assert.False(Reports(model, "reference-unknown", "exercises"));
        Assert.False(Reports(model, "field-invalid", "exercises"));
    }

    [Fact]
    public void Entry_Point_With_Anonymous_Auth_Reports()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.md"), """{"kind": "browser", "auth": ["anonymous"]}"""));

        Assert.True(Reports(model, "field-invalid", "holds auth 'anonymous' outside"));
    }

    [Fact]
    public void Entry_Point_With_Door_Auth_Passes()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.md"), """{"kind": "browser", "auth": ["session", "none"]}"""));

        Assert.False(Reports(model, "field-invalid", "holds auth"));
    }

    /// <summary>A saga holding one step entry.</summary>
    static LiteSpec.Model.Record Saga(string root, string step) =>
        TestModel.Record(
            "aggregate",
            "aggregate/booking.refund-stay",
            Path.Combine(root, "refund.md"),
            """{"kind": "saga", "states": [{"name": "Open", "meaning": "The saga runs."}], "steps": [""" + step + """], "exits": [{"name": "refunded"}], "drivenBy": {"automation": "automation/booking.refund-on-cancellation"}}""");

    /// <summary>A saga holding one step and the drivenBy entry.</summary>
    static LiteSpec.Model.Record DrivenSaga(string root, string drivenBy) =>
        TestModel.Record(
            "aggregate",
            "aggregate/booking.refund-stay",
            Path.Combine(root, "refund.md"),
            """{"kind": "saga", "states": [{"name": "Open", "meaning": "The saga runs."}], "steps": [{"id": "release", "runs": "use-case/booking.release-hold", "compensation": {"mode": "none", "reason": "Nothing was written."}}], "exits": [{"name": "refunded"}], "drivenBy": """ + drivenBy + "}");

    /// <summary>Whether the check reports the code with the text in its message.</summary>
    static bool Reports(ProjectModel model, string code, string text) =>
        Checker.Check(model).Findings.Any(finding =>
            finding.Code == code && finding.Message.Contains(text, StringComparison.Ordinal));
}
