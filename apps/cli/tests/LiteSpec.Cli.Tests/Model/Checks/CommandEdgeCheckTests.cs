using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves commands edges live only on reactions and jobs, every commands edge names a command use case listing the sending module, and every listed module commands it.</summary>
public sealed class CommandEdgeCheckTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>Use Case Declaring Commands Reports Command Edge Invalid.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseDeclaringCommandsReportsCommandEdgeInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-edge-invalid");
        Assert.Equal("use-case/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Use Case Commands Edge Counts For No Consumer.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseCommandsEdgeCountsForNoConsumer()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.Contains(Checker.Check(model).Findings, found => found.Code == "command-consumer-missing");
    }

    /// <summary>Reaction Commanding Query Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void ReactionCommandingQueryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/spending.read-expense"),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("reaction/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Reaction Commanding Command That Lists No Sending Module Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void ReactionCommandingCommandThatListsNoSendingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/spending.reimburse-expense"),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/finance"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("reaction/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Reaction Commanding Its Own Module Command Reports.</summary>
    /// <proves>litespec/backend/application#declare-every-commands-edge-on-the-sender</proves>
    [Fact]
    public void ReactionCommandingItsOwnModuleCommandReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/reporting.reimburse-expense"),
            TestModel.Record("use-case", "use-case/reporting.reimburse-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("reaction/reporting.confirm-payout", finding.Subject);
        Assert.Contains("its own module", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Job Commanding Its Own Module Command Reports.</summary>
    /// <proves>litespec/backend/application#declare-every-commands-edge-on-the-sender</proves>
    [Fact]
    public void JobCommandingItsOwnModuleCommandReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/reporting.confirm-payout", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "jobs", "confirm-payout", "confirm-payout.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"commands": "use-case/reporting.reimburse-expense", "timing": "async"}], "outcomes": [{"name": "completed", "meaning": "The payout is confirmed."}], "onFailure": {"mode": "discard"}, "idempotency": {"mode": "none", "reason": "A second run repeats the same selection."}}"""),
            TestModel.Record("use-case", "use-case/reporting.reimburse-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("job/reporting.confirm-payout", finding.Subject);
        Assert.Contains("its own module", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>Reaction Commanding Listed Public Command Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void ReactionCommandingListedPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/spending.reimburse-expense"),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "command-edge-invalid" or "command-target-invalid" or "command-consumer-missing");
    }

    /// <summary>Job Commanding Listed Public Command Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void JobCommandingListedPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("job", "job/reporting.confirm-payout", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "jobs", "confirm-payout", "confirm-payout.yaml"), """{"kind": "scheduled", "schedule": {"every": "PT1H"}, "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "async"}], "outcomes": [{"name": "completed", "meaning": "The payout is confirmed."}], "onFailure": {"mode": "discard"}, "idempotency": {"mode": "none", "reason": "A second run repeats the same selection."}}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "command-edge-invalid" or "command-target-invalid" or "command-consumer-missing");
    }

    /// <summary>Public Command No Module Commands Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void PublicCommandNoModuleCommandsReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-consumer-missing");
        Assert.Equal("use-case/spending.reimburse-expense", finding.Subject);
    }

    /// <summary>Public Command Draws No Query Consumer Finding.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void PublicCommandDrawsNoQueryConsumerFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/spending.reimburse-expense"),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "query-target-invalid" or "query-consumer-missing");
    }

    /// <summary>Reaction Commanding Notification Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void ReactionCommandingNotificationReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "notification/spending.approval-reminder"),
            TestModel.Record("notification", "notification/spending.approval-reminder", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "notifications.yaml"), """{"summary": "The approver is reminded."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("reaction/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Commands Edge To Unknown Target Reports Only Reference Unknown.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void CommandsEdgeToUnknownTargetReportsOnlyReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            SenderReaction(temp.Path, "reaction/reporting.confirm-payout", "use-case/spending.reimburse-batch"));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "command-target-invalid");
        Assert.Contains(Checker.Check(model).Findings, finding => finding.Code == "reference-unknown");
    }

    /// <summary>Use Case Effects With Commands Are Rejected By The Schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact]
    public void UseCaseEffectsWithCommandsAreRejectedByTheSchema()
    {
        var record = Node("use-case", "use-case/reporting.confirm-payout", """{"summary": "Reporting confirms the payout.", "kind": "command", "reachedThrough": ["entry-point/library-app"], "access": {"grant": "permission/confirm", "tenant": "own"}, "steps": [{"statement": "Reporting confirms the payout."}], "idempotency": {"mode": "none", "reason": "A second confirmation repeats the payout."}, "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}""");

        Assert.NotEmpty(SchemaProblems("use-case/reporting.confirm-payout", record));
    }

    /// <summary>Reaction Effects With Commands Are Accepted By The Schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact]
    public void ReactionEffectsWithCommandsAreAcceptedByTheSchema()
    {
        var record = Node("reaction", "reaction/reporting.confirm-payout", """{"summary": "Reporting confirms the payout.", "triggeredBy": ["module-event/spending.expense-reimbursed"], "effects": [{"commands": "use-case/spending.reimburse-expense"}], "outcomes": [{"name": "completed", "meaning": "The payout is confirmed."}], "onFailure": {"mode": "discard"}, "idempotency": {"mode": "state", "recognizedBy": "state"}}""");

        Assert.Empty(SchemaProblems("reaction/reporting.confirm-payout", record));
    }

    /// <summary>One sender use case in the reporting module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record Sender(string root, string id, string json) =>
        TestModel.Record("use-case", id, Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "confirm-payout.yaml"), json);

    /// <summary>One sender reaction in the reporting module commanding the target.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="target">The commands edge target identifier.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record SenderReaction(string root, string id, string target) =>
        TestModel.Record("reaction", id, Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "reactions", "confirm-payout", "confirm-payout.yaml"), "{\"triggeredBy\": [\"module-event/spending.expense-reimbursed\"], \"effects\": [{\"commands\": \"" + target + "\"}], \"outcomes\": [{\"name\": \"completed\", \"meaning\": \"The payout is confirmed.\"}], \"onFailure\": {\"mode\": \"discard\"}, \"idempotency\": {\"mode\": \"state\", \"recognizedBy\": \"state\"}}");

    /// <summary>One provider file in the spending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string ProviderFile(string root, string file) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", file);

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

    /// <summary>The problems the record schema reports for the record.</summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="record">The record node.</param>
    /// <returns>The problem messages, empty when the schema accepts the record.</returns>
    static List<string> SchemaProblems(string id, JsonNode record)
    {
        var schema = RecordText.Read(LiteSpecHome.PathTo(SchemaFile));

        return JsonSchema.Problems(SchemaFile, record, id, _ => schema);
    }
}
