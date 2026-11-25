using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the contract checks: typed inputs and payloads, scenario exercises, and entry point auth.</summary>
public sealed class BehaviorContractCheckTests
{
    /// <summary>Use Case Input Without A Type Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseInputWithoutATypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nights", "meaning": "The nights to hold.", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "holds input field 'nights' with no type"));
    }

    /// <summary>Use Case Input With An Unknown Type Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseInputWithAnUnknownTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nights", "meaning": "The nights to hold.", "type": "count", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "with type 'count' outside"));
    }

    /// <summary>Use Case Untyped Id Input Without Identifies Passes.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseUntypedIdInputPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "required": true}]}"""));

        Assert.False(Reports(model, "record-invalid", "of type id with no identifies"));
    }

    /// <summary>Use Case Id Input Naming No Record Reports.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void UseCaseIdInputNamingNoRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "identifies": "aggregate/inventory.no-such-room", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "cites 'aggregate/inventory.no-such-room', which no record holds"));
    }

    /// <summary>Use Case Choice Input Without Set Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseChoiceInputWithoutSetReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "bed", "meaning": "The bed shape.", "type": "choice", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "of type choice with no set"));
    }

    /// <summary>Typed Inputs With Type Required And Identifies Pass.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TypedInputsWithTypeRequiredAndOfPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/inventory.room", Path.Combine(temp.Path, "room.yaml"), """{"states": [{"name": "Free", "meaning": "The room is free."}]}"""),
            TestModel.Record("reference-data", "reference-data/booking.bed-shape", Path.Combine(temp.Path, "bed-shape.yaml"), """{"title": "Bed Shape", "choices": ["single", "double"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "roomId", "meaning": "The room to hold.", "type": "id", "identifies": "aggregate/inventory.room", "required": true}, {"name": "nights", "meaning": "The nights to hold.", "type": "integer", "required": true}, {"name": "bed", "meaning": "The bed shape.", "type": "choice", "set": "reference-data/booking.bed-shape", "required": false}]}"""));

        Assert.False(Reports(model, "record-invalid", "holds input field"));
    }

    /// <summary>Integration Event With A Bad Dataschema Reports.</summary>
    /// <proves>litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives</proves>
    [Fact]
    public void IntegrationEventWithABadDataschemaReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("integration-event", "integration-event/booking-held", Path.Combine(temp.Path, "held.yaml"), """{"dataschema": "not a url"}"""));

        Assert.True(Reports(model, "record-invalid", "holds dataschema 'not a url', which is no URL"));
    }

    /// <summary>Scenario Exercising No Rule Reports.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
    [Fact]
    public void ScenarioExercisingNoRuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command"}"""),
            TestModel.Record("result", "result/hold-room.refused", Path.Combine(temp.Path, "hold.yaml"), """{"outcome": "success", "meaning": "The second hold is refused."}"""),
            TestModel.Record("scenario", "scenario/hold-room.second-hold-is-refused", Path.Combine(temp.Path, "hold.yaml"), """{"given": "Ben already holds the room.", "when": "Ana holds the room.", "then": "The hold is refused.", "result": "result/hold-room.refused", "exercises": ["rule/hold.no-such-rule"]}"""));

        Assert.True(Reports(model, "reference-unknown", "cites 'rule/hold.no-such-rule', which no record holds"));
    }

    /// <summary>Scenario Exercising A Rule Passes.</summary>
    /// <proves>litespec/core/model#write-links-on-the-record-that-points</proves>
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
        Assert.False(Reports(model, "record-invalid", "exercises"));
    }

    /// <summary>Entry Point With Anonymous Auth Reports.</summary>
    /// <proves>litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind</proves>
    [Fact]
    public void EntryPointWithAnonymousAuthReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "authentication": ["anonymous"], "audience": ["actor/reader"]}"""));

        Assert.True(Reports(model, "record-invalid", "holds auth 'anonymous' outside"));
    }

    /// <summary>Entry Point With Declared Auth Passes.</summary>
    /// <proves>litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind</proves>
    [Fact]
    public void EntryPointWithDeclaredAuthPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/library-app", Path.Combine(temp.Path, "entry.yaml"), """{"kind": "browser", "authentication": ["session", "none"], "audience": ["actor/reader"]}"""));

        Assert.False(Reports(model, "record-invalid", "holds auth"));
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
