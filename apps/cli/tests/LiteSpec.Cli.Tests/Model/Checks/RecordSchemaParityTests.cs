using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves that the record schema and the model check agree on every shape rule both of them carry.
///     Each case is a record one rule away from valid: the schema reports it and so does the model check,
///     and the valid baseline of each type passes both. Graph rules are outside this test, because the
///     schema cannot read the record graph.
/// </summary>
/// <follows>litespec/core/model#keep-shape-rules-in-the-schema</follows>
public sealed class RecordSchemaParityTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>The finding codes the model check uses for a record shape failure.</summary>
    static readonly string[] ShapeCodes = ["record-invalid", "field-invalid"];

    /// <summary>The valid baseline of each page record type: its identifier and its fields, as JSON.</summary>
    static readonly IReadOnlyDictionary<string, (string Id, string Json)> Baselines = new Dictionary<string, (string Id, string Json)>(StringComparer.Ordinal)
    {
        ["product"] = ("product/library", """{"summary": "The lending desk.", "boundary": "The lending desk only.", "addresses": ["actor/reader"]}"""),
        ["goal"] = ("goal/faster-pickup", """{"summary": "Readers wait less.", "statement": "Readers wait less.", "measure": "Median pickup wait.", "target": "Under two days."}"""),
        ["vision"] = ("vision/open-shelves", """{"summary": "Open shelves.", "statement": "Every reader finds a copy."}"""),
        ["constraint"] = ("constraint/privacy-law", """{"summary": "Reader data is personal.", "authority": "Privacy law.", "sources": [{"key": "privacy-law", "url": "https://example.com/privacy-law", "readOn": "2026-01-02"}], "kind": "legal", "applies": true, "appliesBecause": "We hold reader data."}"""),
        ["research"] = ("research/loan-terms", """{"summary": "Loan terms.", "sources": [{"key": "lib-manual", "url": "https://example.com/manual", "readOn": "2026-01-02"}], "question": "How long is a loan?", "conclusion": "Loans run for two weeks."}"""),
        ["decision"] = ("decision/0001-loan-store", """{"summary": "Loan storage.", "options": [{"name": "Documents", "pros": "Simple.", "cons": "Slow."}], "choice": "Documents.", "consequences": "Loans are documents.", "reopenedWhen": "Reads exceed one second."}"""),
        ["requirement"] = ("requirement/lending.loan-limit", """{"kind": "policy", "statement": "A reader holds five loans.", "rationale": "Fair shares.", "values": [{"value": "5", "effectiveFrom": "2026-01-01"}]}"""),
        ["problem"] = ("problem/lost-copies", """{"summary": "Copies go missing.", "statement": "Copies go missing on the shelf.", "affects": ["actor/reader"]}"""),
        ["feature"] = ("feature/hold-copy", """{"summary": "Hold a copy.", "offering": "Readers hold a copy."}"""),
        ["actor"] = ("actor/reader", """{"summary": "A reader.", "kind": "person", "belongsToTeam": true, "holds": ["role/lending-desk"]}"""),
        ["role"] = ("role/lending-desk", """{"summary": "Runs the lending desk.", "scope": "lending-desk", "allows": ["permission/borrow"]}"""),
        ["journey"] = ("journey/submit-expense", """{"summary": "A reader borrows.", "actor": "actor/reader", "serves": ["goal/faster-pickup"], "startedWhen": "The reader is at the desk with a copy.", "finishedWhen": "The copy is out.", "steps": [{"id": "s1", "statement": "The reader borrows the copy.", "actor": "actor/reader"}], "branches": [{"step": "s1", "condition": "The shelf is empty.", "nextStep": "end"}]}"""),
        ["module"] = ("module/lending", """{"summary": "Lending.", "dependsOn": []}"""),
        ["aggregate"] = ("aggregate/copy.loan", """{"summary": "A loan.", "kind": "entity", "states": [{"name": "on-loan", "meaning": "The copy is out."}]}"""),
        ["use-case"] = ("use-case/lending.borrow-copy", """{"summary": "Borrow a copy.", "kind": "command", "reachedThrough": ["entry-point/library-app"], "access": {"grant": "permission/borrow", "tenant": "own"}, "idempotency": {"mode": "none", "reason": "A second borrow is refused."},"audit": {"level": "none", "target": "No audit entry."}}"""),
        ["reaction"] = ("reaction/lending.send-due-notice", """{"summary": "Send a due notice.", "triggeredBy": ["event/loan.loan-borrowed"], "effects": [{"sends": "notification/lending.loan-due", "timing": "async"}]}"""),
        ["job"] = ("job/lending.scan-due", """{"summary": "Scan for due loans.", "kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"sends": "notification/lending.loan-due", "timing": "async"}]}"""),
        ["setting"] = ("setting/lending.loan-days", """{"summary": "Loan length.", "valueType": "integer", "setBy": "operator"}"""),
        ["external-system"] = ("external-system/payments", """{"summary": "The payment provider."}"""),
        ["runbook"] = ("runbook/restart-queue", """{"summary": "Restart the queue.", "trigger": "The queue stalls.", "impact": "Notices pause.", "prerequisites": "Access to the queue.", "procedure": "Restart the worker.", "verification": "Notices flow again.", "recovery": "Restart again.", "escalation": "Page the on-call."}"""),
        ["departure"] = ("departure/legacy-export", """{"summary": "A legacy export.", "reason": "The export predates the schema.", "covers": ["litespec/core/model#keep-each-name-unique-in-the-product"], "subject": ["module/lending"]}"""),
        ["screen"] = ("screen/lending.loan-list", """{"summary": "The loan list.", "address": "/loans"}"""),
        ["entry-point"] = ("entry-point/library-app", """{"summary": "The library app.", "kind": "browser", "auth": ["session"]}"""),
    };

    /// <summary>Each valid baseline, which both validators must accept.</summary>
    /// <returns>The type, identifier and fields of each baseline.</returns>
    public static TheoryData<string, string, string> ValidRecords()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var (type, baseline) in Baselines)
        {
            data.Add(type, baseline.Id, baseline.Json);
        }

        return data;
    }

    /// <summary>Every rule case: a baseline with one field removed, or one value changed, which both validators must reject.</summary>
    /// <returns>The label, type, identifier and fields of each invalid record.</returns>
    public static TheoryData<string, string, string, string> InvalidRecords()
    {
        var data = new TheoryData<string, string, string, string>();

        // Each required field the model check reads, removed from its baseline.
        foreach (var (type, field) in RemovedFields)
        {
            data.Add($"{type} without {field}", type, Baselines[type].Id, Without(Baselines[type].Json, field));
        }

        // Each value the model check bounds or names, set outside its shape.
        foreach (var (label, type, edit) in ChangedValues)
        {
            data.Add(label, type, Baselines[type].Id, edit(Baselines[type].Json));
        }

        return data;
    }

    /// <summary>The required fields the model check reads, one removal case each.</summary>
    static readonly (string Type, string Field)[] RemovedFields =
    [
        ("product", "boundary"), ("product", "addresses"),
        ("goal", "statement"), ("goal", "measure"), ("goal", "target"),
        ("vision", "statement"),
        ("constraint", "authority"), ("constraint", "sources"), ("constraint", "kind"), ("constraint", "applies"), ("constraint", "appliesBecause"),
        ("research", "sources"), ("research", "conclusion"),
        ("decision", "options"), ("decision", "reopenedWhen"), ("decision", "choice"),
        ("requirement", "kind"), ("requirement", "statement"), ("requirement", "rationale"), ("requirement", "values"),
        ("problem", "affects"), ("problem", "statement"),
        ("actor", "kind"),
        ("role", "scope"), ("role", "allows"),
        ("journey", "actor"), ("journey", "serves"), ("journey", "startedWhen"), ("journey", "finishedWhen"), ("journey", "steps"),
        ("module", "dependsOn"),
        ("aggregate", "states"),
        ("use-case", "kind"),
        ("reaction", "effects"),
        ("job", "effects"),
        ("runbook", "trigger"),
        ("departure", "reason"), ("departure", "covers"),
        ("screen", "address"),
        ("entry-point", "kind"),
    ];

    /// <summary>The value rules: a labelled case, its record type and the edit that breaks the value.</summary>
    static readonly (string Label, string Type, Func<string, string> Edit)[] ChangedValues =
    [
        ("product with no addresses", "product", json => Replace(json, "addresses", "[]")),
        ("research with no sources", "research", json => Replace(json, "sources", "[]")),
        ("decision with no options", "decision", json => Replace(json, "options", "[]")),
        ("role with no allowed permission", "role", json => Replace(json, "allows", "[]")),
        ("role with a scope that is not kebab-case", "role", json => Replace(json, "scope", "\"Lending Desk\"")),
        ("journey with no steps", "journey", json => Replace(json, "steps", "[]")),
        ("journey with a betweenSteps field", "journey", json => Replace(json, "betweenSteps", "\"The reader waits.\"")),
        ("journey with no branches", "journey", json => Replace(json, "branches", "[]")),
        ("journey with a branch that has no condition", "journey", json => Replace(json, "branches", "[{\"step\": \"s1\", \"nextStep\": \"end\"}]")),
        ("journey with a branch that has no step", "journey", json => Replace(json, "branches", "[{\"condition\": \"The shelf is empty.\", \"nextStep\": \"end\"}]")),
        ("journey with a branch that has no nextStep", "journey", json => Replace(json, "branches", "[{\"step\": \"s1\", \"condition\": \"The shelf is empty.\"}]")),
        ("requirement with an unknown kind", "requirement", json => Replace(json, "kind", "\"bogus\"")),
        ("requirement with a policy and no values", "requirement", json => Replace(json, "values", "[]")),
        ("constraint with no sources", "constraint", json => Replace(json, "sources", "[]")),
        ("constraint with an unknown kind", "constraint", json => Replace(json, "kind", "\"bogus\"")),
        ("actor with an unknown kind", "actor", json => Replace(json, "kind", "\"robot\"")),
        ("actor holds a role with a scope", "actor", json => Replace(json, "holds", "[{\"role\": \"role/lending-desk\", \"scope\": \"lending-desk\"}]")),
        ("actor holds a name that is not a role identifier", "actor", json => Replace(json, "holds", "[\"lending-desk\"]")),
        ("use case with an unknown kind", "use-case", json => Replace(json, "kind", "\"bogus\"")),
        ("entry point with an unknown kind", "entry-point", json => Replace(json, "kind", "\"bogus\"")),
        ("reaction with no effects", "reaction", json => Replace(json, "effects", "[]")),
        ("job with no effects", "job", json => Replace(json, "effects", "[]")),
        ("aggregate with no states", "aggregate", json => Replace(json, "states", "[]")),
        ("problem with no affects", "problem", json => Replace(json, "affects", "[]")),
        ("departure with no covers", "departure", json => Replace(json, "covers", "[]")),
        ("departure with an expiry and no exit", "departure", json => Replace(json, "expiresOn", "\"2027-01-01\"")),
        ("screen with an address and a host", "screen", json => Replace(json, "host", "\"library.example\"")),
        ("setting with an unknown setBy", "setting", json => Replace(json, "setBy", "\"robot\"")),
        ("use case with an unknown idempotency scope", "use-case", json => Replace(json, "idempotency", "{\"mode\": \"key\", \"key\": \"loan:{copyId}\", \"scope\": \"branch\"}")),
    ];

    /// <summary>A valid baseline must pass the schema and the model check.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    [Theory]
    [MemberData(nameof(ValidRecords))]
    public void SchemaAndModelCheckAcceptValidRecord(string type, string id, string json)
    {
        Assert.Empty(SchemaProblems(type, id, json));
        Assert.Empty(ModelShapeMessages(type, id, json));
    }

    /// <summary>A record one rule away from valid must fail the schema and the model check together.</summary>
    /// <param name="label">The case label.</param>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    [Theory]
    [MemberData(nameof(InvalidRecords))]
    public void SchemaAndModelCheckRejectInvalidRecord(string label, string type, string id, string json)
    {
        Assert.True(SchemaProblems(type, id, json).Count > 0, $"The schema accepts {label}.");
        Assert.True(ModelShapeMessages(type, id, json).Count > 0, $"The model check accepts {label}.");
    }

    /// <summary>The problems the record schema reports for the record.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The problem messages, empty when the schema accepts the record.</returns>
    static List<string> SchemaProblems(string type, string id, string json)
    {
        var record = Node(type, id, json);
        var schema = RecordText.Read(LiteSpecHome.PathTo(SchemaFile));

        return JsonSchema.Problems(SchemaFile, record, id, _ => schema);
    }

    /// <summary>The shape messages the model check reports for the record.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The messages of the record-invalid and field-invalid findings, empty when the check accepts the record.</returns>
    static List<string> ModelShapeMessages(string type, string id, string json)
    {
        using var temp = TestCli.Temp("parity");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(type, id, Path.Combine(temp.Path, "record.yaml"), json));

        return Checker.Check(model).Findings
            .Where(finding => ShapeCodes.Contains(finding.Code, StringComparer.Ordinal))
            .Select(finding => finding.Message)
            .ToList();
    }

    /// <summary>The record as the schema reads it: the type, identifier and title, then the fields.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The record node.</returns>
    static JsonNode Node(string type, string id, string json)
    {
        var record = new JsonObject
        {
            [RecordFields.Type] = type,
            [RecordFields.Id] = id,
            [RecordFields.Title] = id,
        };

        foreach (var property in (JsonNode.Parse(json) as JsonObject)!)
        {
            record[property.Key] = property.Value?.DeepClone();
        }

        return record;
    }

    /// <summary>The fields of a baseline without one field.</summary>
    /// <param name="json">The baseline fields as JSON.</param>
    /// <param name="field">The field to remove.</param>
    /// <returns>The fields as JSON.</returns>
    static string Without(string json, string field)
    {
        var fields = (JsonNode.Parse(json) as JsonObject)!;
        fields.Remove(field);
        return fields.ToJsonString();
    }

    /// <summary>The fields of a baseline with one field set to a JSON value.</summary>
    /// <param name="json">The baseline fields as JSON.</param>
    /// <param name="field">The field to set, which the baseline already holds.</param>
    /// <param name="value">The JSON value.</param>
    /// <returns>The fields as JSON.</returns>
    static string Replace(string json, string field, string value)
    {
        var fields = (JsonNode.Parse(json) as JsonObject)!;
        fields[field] = JsonNode.Parse(value);
        return fields.ToJsonString();
    }
}
