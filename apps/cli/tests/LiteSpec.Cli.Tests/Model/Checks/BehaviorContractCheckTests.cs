using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the contract checks: typed inputs and payloads, scenario exercises, and door auth.</summary>
public sealed class BehaviorContractCheckTests
{
    [Fact]
    public void UseCaseInputWithoutATypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "nights", "meaning": "The nights to hold.", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "holds input field 'nights' with no type"));
    }

    [Fact]
    public void UseCaseInputWithAnUnknownTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "nights", "meaning": "The nights to hold.", "type": "count", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "with type 'count' outside"));
    }

    [Fact]
    public void UseCaseIdInputWithoutAnOfRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "of type id with no of record"));
    }

    [Fact]
    public void UseCaseIdInputNamingNoRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "of": "aggregate/inventory.no-such-room", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "of 'aggregate/inventory.no-such-room', which no record holds"));
    }

    [Fact]
    public void UseCaseChoiceInputWithoutChoicesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "bed", "meaning": "The bed shape.", "type": "choice", "required": true}]}"""));

        Assert.True(Reports(model, "field-invalid", "of type choice with no of choices"));
    }

    [Fact]
    public void TypedInputsWithTypeRequiredAndOfPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/inventory.room", Path.Combine(temp.Path, "room.yaml"), """{"kind": "entity", "states": [{"name": "Free", "meaning": "The room is free."}]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "of": "aggregate/inventory.room", "required": true}, {"name": "nights", "meaning": "The nights to hold.", "type": "integer", "required": true}, {"name": "bed", "meaning": "The bed shape.", "type": "choice", "of": ["single", "double"], "required": false}]}"""));

        Assert.False(Reports(model, "field-invalid", "holds input field"));
    }

    [Fact]
    public void IntegrationEventWithABadDataschemaReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/booking-held", Path.Combine(temp.Path, "held.yaml"), """{"dataschema": "not a url"}"""));

        Assert.True(Reports(model, "field-invalid", "holds dataschema 'not a url', which is no URL"));
    }

    [Fact]
    public void ScenarioExercisingNoRuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/hold-room.second-hold-is-refused", Path.Combine(temp.Path, "hold.yaml"), """{"given": "Ben already holds the room.", "when": "Ana holds the room.", "then": "The hold is refused.", "result": "result/hold-room.refused", "exercises": ["rule/hold.no-such-rule"]}"""));

        Assert.True(Reports(model, "reference-unknown", "exercises 'rule/hold.no-such-rule'"));
    }

    [Fact]
    public void ScenarioExercisingARulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("rule", "rule/hold.one-hold-per-room-night", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "invariant", "statement": "A room night holds one hold.", "governs": ["aggregate/booking.hold"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold-room.yaml"), """{"kind": "command"}"""),
            TestModel.Record("scenario", "scenario/hold-room.second-hold-is-refused", Path.Combine(temp.Path, "hold-room.yaml"), """{"given": "Ben already holds the room.", "when": "Ana holds the room.", "then": "The hold is refused.", "result": "result/hold-room.refused", "exercises": ["rule/hold.one-hold-per-room-night"]}"""));

        Assert.False(Reports(model, "reference-unknown", "exercises"));
        Assert.False(Reports(model, "field-invalid", "exercises"));
    }

    [Fact]
    public void EntryPointWithAnonymousAuthReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "auth": ["anonymous"]}"""));

        Assert.True(Reports(model, "field-invalid", "holds auth 'anonymous' outside"));
    }

    [Fact]
    public void EntryPointWithDoorAuthPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "auth": ["session", "none"]}"""));

        Assert.False(Reports(model, "field-invalid", "holds auth"));
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
