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

    /// <summary>Nested Row List With Scalar Rows Passes.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void NestedRowListWithScalarRowsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "boxes", "meaning": "The boxes to hold.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "code", "meaning": "The box code.", "type": "text", "required": true}, {"name": "nights", "meaning": "The nights held.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "night", "meaning": "One night held.", "type": "date", "required": true}]}}]}}]}"""));

        Assert.False(Reports(model, "record-invalid", "row field 'nights'"));
    }

    /// <summary>Nested Row List Without Row Shape Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void NestedRowListWithoutRowShapeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "boxes", "meaning": "The boxes to hold.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "code", "meaning": "The box code.", "type": "text", "required": true}, {"name": "nights", "meaning": "The nights held.", "type": "list", "required": true}]}}]}"""));

        Assert.True(Reports(model, "record-invalid", "holds row field 'nights' of input field 'boxes' of type list with no rowShape"));
    }

    /// <summary>Nested Row List Deeper Than One Level Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void NestedRowListDeeperThanOneLevelReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "boxes", "meaning": "The boxes to hold.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "nights", "meaning": "The nights held.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "spans", "meaning": "The spans.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "night", "meaning": "One night held.", "type": "date", "required": true}]}}]}}]}}]}"""));

        Assert.True(Reports(model, "record-invalid", "rows nest one level at a time"));
    }

    /// <summary>Multi Field Nested Row List Without Row Type Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MultiFieldNestedRowListWithoutRowTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "boxes", "meaning": "The boxes to hold.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "code", "meaning": "The box code.", "type": "text", "required": true}, {"name": "nights", "meaning": "The nights held.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "start", "meaning": "The first night.", "type": "date", "required": true}, {"name": "end", "meaning": "The last night.", "type": "date", "required": true}]}}]}}]}"""));

        Assert.True(Reports(model, "record-invalid", "with several fields and no rowType"));
    }

    /// <summary>Nested Row Type Naming A Missing Part Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void NestedRowTypeNamingAMissingPartReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/booking.hold", Path.Combine(temp.Path, "hold-aggregate.yaml"), """{"states": [{"name": "Open", "meaning": "The hold runs."}], "parts": [{"name": "night"}]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "boxes", "meaning": "The boxes to hold.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "code", "meaning": "The box code.", "type": "text", "required": true}, {"name": "nights", "meaning": "The nights held.", "type": "list", "required": true, "rowShape": {"fields": [{"name": "start", "meaning": "The first night.", "type": "date", "required": true}, {"name": "end", "meaning": "The last night.", "type": "date", "required": true}]}, "rowType": "aggregate/booking.hold#no-such-part"}]}}]}"""));

        Assert.True(Reports(model, "reference-unknown", "names part 'no-such-part' the aggregate does not declare"));
    }

    /// <summary>Use Case Map Input Without Key Type Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithoutKeyTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nightsByRoom", "meaning": "The nights held by room.", "type": "map", "valueType": "integer", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "of type map with no keyType"));
    }

    /// <summary>Use Case Map Input Without Value Type Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithoutValueTypeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nightsByRoom", "meaning": "The nights held by room.", "type": "map", "keyType": "text", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "of type map with no valueType"));
    }

    /// <summary>Use Case Map Input With Choice Values Without Set Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithChoiceValuesWithoutSetReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "bedByRoom", "meaning": "The bed shape by room.", "type": "map", "keyType": "text", "valueType": "choice", "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "of type map with choice values and no set"));
    }

    /// <summary>Use Case Map Input With Choice Values Naming No Record Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithChoiceValuesNamingNoRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "bedByRoom", "meaning": "The bed shape by room.", "type": "map", "keyType": "text", "valueType": "choice", "set": "reference-data/booking.no-such-set", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "cites 'reference-data/booking.no-such-set', which no record holds"));
    }

    /// <summary>Use Case Map Input Identifying No Record Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputIdentifyingNoRecordReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "holdByRoom", "meaning": "The hold by room.", "type": "map", "keyType": "text", "valueType": "id", "identifies": "aggregate/booking.no-such-room", "required": true}]}"""));

        Assert.True(Reports(model, "reference-unknown", "cites 'aggregate/booking.no-such-room', which no record holds"));
    }

    /// <summary>Use Case Map Input With Row Shape Reports.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithRowShapeReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nightsByRoom", "meaning": "The nights held by room.", "type": "map", "keyType": "text", "valueType": "integer", "rowShape": {"fields": [{"name": "night", "meaning": "One night.", "type": "date", "required": true}]}, "required": true}]}"""));

        Assert.True(Reports(model, "record-invalid", "of type map with a rowShape that only list carries"));
    }

    /// <summary>Use Case Map Input With Key And Value Types Passes.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void UseCaseMapInputWithKeyAndValueTypesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("entry-point", "entry-point/web", Path.Combine(temp.Path, "web.yaml"), """{"kind": "api", "audience": ["actor/reader"]}"""),
            TestModel.Record("use-case", "use-case/booking.hold-room", Path.Combine(temp.Path, "hold.yaml"), """{"kind": "command", "reachedThrough": ["entry-point/web"], "exposure": "Ana the manager holds a room for a guest.", "steps": [{"statement": "Ana holds the room for the guest."}], "input": [{"name": "nightsByRoom", "meaning": "The nights held by room.", "type": "map", "keyType": "text", "valueType": "integer", "required": true}]}"""));

        Assert.False(Reports(model, "record-invalid", "of type map"));
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
