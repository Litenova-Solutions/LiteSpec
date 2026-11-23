using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Behavior;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the automation access checks: a reaction or job that runs, commands, or queries a use case
///     whose access admits no system identity is reported, and a system or anonymous grant, or runsAs system
///     beside the grant, passes. The tests call the check directly, so they hold with or without its
///     registration in the check order.
/// </summary>
public sealed class AutomationAccessCheckTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>A reaction running a permission-granted use case with no runsAs is reported with the use case it runs.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A reaction running a permission-granted use case with no runsAs is reported")]
    public void ReactionRunningPermissionGrantedUseCaseIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/spending.erase-expired-personal-data-on-expense-decided", "runs", "use-case/spending.erase-personal-data"),
            UseCase(temp.Path, "use-case/spending.erase-personal-data", """{"grant": "permission/remove-personal-data", "tenant": "own"}"""));

        var findings = Run(model);

        var finding = Assert.Single(findings, found => found.Code == "system-access-missing");
        Assert.Equal("reaction/spending.erase-expired-personal-data-on-expense-decided", finding.Subject);
        Assert.Equal("Reaction 'reaction/spending.erase-expired-personal-data-on-expense-decided' runs 'use-case/spending.erase-personal-data', whose access admits no system identity; state `runsAs: system` on the use case.", finding.Message);
    }

    /// <summary>A job running a signed-in-granted use case is reported.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A job running a signed-in-granted use case is reported")]
    public void JobRunningSignedInGrantedUseCaseIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerJob(temp.Path, "job/spending.erase-expired-personal-data", "runs", "use-case/spending.erase-personal-data"),
            UseCase(temp.Path, "use-case/spending.erase-personal-data", """{"grant": "signed-in", "tenant": "own"}"""));

        var findings = Run(model);

        var finding = Assert.Single(findings, found => found.Code == "system-access-missing");
        Assert.Equal("job/spending.erase-expired-personal-data", finding.Subject);
    }

    /// <summary>A reaction commanding a permission-granted use case of another module is reported.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A reaction commanding a permission-granted use case is reported")]
    public void ReactionCommandingPermissionGrantedUseCaseIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/reporting.confirm-payout-on-expense-reimbursed", "commands", "use-case/spending.reimburse-expense"),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "steps": [{"statement": "Spending reimburses the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "permission/post-reimbursement", "tenant": "own"}}"""));

        var findings = Run(model);

        var finding = Assert.Single(findings, found => found.Code == "system-access-missing");
        Assert.Equal("reaction/reporting.confirm-payout-on-expense-reimbursed", finding.Subject);
    }

    /// <summary>A reaction querying a permission-granted use case of another module is reported.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A reaction querying a permission-granted use case is reported")]
    public void ReactionQueryingPermissionGrantedUseCaseIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/reporting.confirm-payout-on-expense-reimbursed", "queries", "use-case/spending.read-expense"),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "permission/read-expense", "tenant": "own"}}"""));

        var findings = Run(model);

        Assert.Single(findings, found => found.Code == "system-access-missing");
    }

    /// <summary>A reaction running a system-granted use case passes.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A reaction running a system-granted use case passes")]
    public void ReactionRunningSystemGrantedUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/spending.archive-expense-on-expense-erased", "runs", "use-case/spending.archive-expense"),
            UseCase(temp.Path, "use-case/spending.archive-expense", """{"grant": "system", "tenant": "none"}"""));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A reaction running an anonymous-granted use case passes.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A reaction running an anonymous-granted use case passes")]
    public void ReactionRunningAnonymousGrantedUseCasePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/spending.archive-expense-on-expense-erased", "runs", "use-case/spending.archive-expense"),
            UseCase(temp.Path, "use-case/spending.archive-expense", """{"grant": "anonymous", "tenant": "none"}"""));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A job running a permission-granted use case with runsAs system passes.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A job running a permission-granted use case with runsAs system passes")]
    public void JobRunningUseCaseWithRunsAsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerJob(temp.Path, "job/spending.erase-expired-personal-data", "runs", "use-case/spending.erase-personal-data"),
            UseCase(temp.Path, "use-case/spending.erase-personal-data", """{"grant": "permission/remove-personal-data", "tenant": "own", "runsAs": "system"}"""));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A runs edge to an unknown target draws no system-run finding.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A runs edge to an unknown target draws no system-run finding")]
    public void RunsEdgeToUnknownTargetDrawsNoFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/spending.erase-expired-personal-data-on-expense-decided", "runs", "use-case/spending.erase-batch"));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A runs edge to a record that is no use case draws no system-run finding.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A runs edge to a record that is no use case draws no system-run finding")]
    public void RunsEdgeToNonUseCaseDrawsNoFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RunnerReaction(temp.Path, "reaction/spending.remind-approver-on-expense-submitted", "runs", "notification/spending.approval-reminder"),
            TestModel.Record("notification", "notification/spending.approval-reminder", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "notifications.yaml"), """{"summary": "The approver is reminded."}"""));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A use case querying a permission-granted query draws no system-run finding.</summary>
    /// <proves>litespec/core/access#admit-the-system-identity-on-every-system-run-use-case</proves>
    [Fact(DisplayName = "A use case querying a permission-granted query draws no system-run finding")]
    public void UseCaseQueryingPermissionGrantedQueryDrawsNoFinding()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/reporting.confirm-payout", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "confirm-payout.yaml"), """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}], "reachedThrough": ["module/reporting"], "access": {"grant": "permission/read-expense", "tenant": "own"}}"""));

        Assert.DoesNotContain(Run(model), finding => finding.Code == "system-access-missing");
    }

    /// <summary>A use case stating runsAs system beside a permission grant is accepted by the schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "A use case stating runsAs system beside a permission grant is accepted by the schema")]
    public void RunsAsBesidePermissionGrantIsAcceptedByTheSchema()
    {
        var record = Node("use-case", "use-case/spending.erase-personal-data", """{"summary": "Finance erases the personal data of an old expense.", "kind": "command", "access": {"grant": "permission/remove-personal-data", "tenant": "own", "runsAs": "system"}, "steps": [{"statement": "Spending erases the personal data."}], "idempotency": {"mode": "none", "reason": "A second erasure finds nothing to erase."}}""");

        Assert.Empty(SchemaProblems("use-case/spending.erase-personal-data", record));
    }

    /// <summary>A use case stating runsAs beside a system grant is rejected by the schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "A use case stating runsAs beside a system grant is rejected by the schema")]
    public void RunsAsBesideSystemGrantIsRejectedByTheSchema()
    {
        var record = Node("use-case", "use-case/spending.archive-expense", """{"summary": "Spending archives an old expense.", "kind": "command", "access": {"grant": "system", "tenant": "none", "runsAs": "system"}, "steps": [{"statement": "Spending archives the expense."}], "idempotency": {"mode": "none", "reason": "A second archive finds nothing to archive."}}""");

        Assert.NotEmpty(SchemaProblems("use-case/spending.archive-expense", record));
    }

    /// <summary>A use case stating runsAs beside an anonymous grant is rejected by the schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "A use case stating runsAs beside an anonymous grant is rejected by the schema")]
    public void RunsAsBesideAnonymousGrantIsRejectedByTheSchema()
    {
        var record = Node("use-case", "use-case/spending.archive-expense", """{"summary": "Spending archives an old expense.", "kind": "command", "access": {"grant": "anonymous", "tenant": "none", "runsAs": "system"}, "steps": [{"statement": "Spending archives the expense."}], "idempotency": {"mode": "none", "reason": "A second archive finds nothing to archive."}}""");

        Assert.NotEmpty(SchemaProblems("use-case/spending.archive-expense", record));
    }

    /// <summary>A use case stating runsAs outside system is rejected by the schema.</summary>
    /// <proves>litespec/core/model#keep-shape-rules-in-the-schema</proves>
    [Fact(DisplayName = "A use case stating runsAs outside system is rejected by the schema")]
    public void RunsAsOutsideSystemIsRejectedByTheSchema()
    {
        var record = Node("use-case", "use-case/spending.erase-personal-data", """{"summary": "Finance erases the personal data of an old expense.", "kind": "command", "access": {"grant": "permission/remove-personal-data", "tenant": "own", "runsAs": "manager"}, "steps": [{"statement": "Spending erases the personal data."}], "idempotency": {"mode": "none", "reason": "A second erasure finds nothing to erase."}}""");

        Assert.NotEmpty(SchemaProblems("use-case/spending.erase-personal-data", record));
    }

    /// <summary>The findings the automation access check reports for the model.</summary>
    /// <param name="model">The model to run the check against.</param>
    /// <returns>The findings in report order.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        AutomationAccessChecks.Run(model, findings);
        return findings;
    }

    /// <summary>One use case with the access object, in the folder its identifier names.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="access">The access object as JSON.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record UseCase(string root, string id, string access) =>
        TestModel.Record("use-case", id, Path.Combine(root, ".litespec", "behavior", "modules", ModuleOf(id), NameOf(id) + ".yaml"), "{\"kind\": \"command\", \"steps\": [{\"statement\": \"Spending erases the personal data.\"}], \"access\": " + access + "}");

    /// <summary>One runner reaction with one edge to the target, in the folder its identifier names.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="verb">The edge verb: runs, commands, or queries.</param>
    /// <param name="target">The edge target identifier.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record RunnerReaction(string root, string id, string verb, string target) =>
        TestModel.Record("reaction", id, Path.Combine(root, ".litespec", "behavior", "modules", ModuleOf(id), "reactions", NameOf(id), NameOf(id) + ".yaml"), "{\"triggeredBy\": [\"domain-event/expense.decided\"], \"effects\": [{\"" + verb + "\": \"" + target + "\"}], \"outcomes\": [{\"name\": \"completed\", \"meaning\": \"The personal data was erased.\"}], \"onFailure\": {\"mode\": \"discard\"}, \"idempotency\": {\"mode\": \"state\", \"recognizedBy\": \"state\"}}");

    /// <summary>One runner job with one edge to the target, in the folder its identifier names.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="verb">The edge verb: runs, commands, or queries.</param>
    /// <param name="target">The edge target identifier.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record RunnerJob(string root, string id, string verb, string target) =>
        TestModel.Record("job", id, Path.Combine(root, ".litespec", "behavior", "modules", ModuleOf(id), "jobs", NameOf(id), NameOf(id) + ".yaml"), "{\"kind\": \"scheduled\", \"schedule\": {\"every\": \"P1D\"}, \"effects\": [{\"" + verb + "\": \"" + target + "\", \"timing\": \"async\"}], \"outcomes\": [{\"name\": \"completed\", \"meaning\": \"The personal data was erased.\"}], \"onFailure\": {\"mode\": \"discard\"}, \"idempotency\": {\"mode\": \"state\", \"recognizedBy\": \"state\"}}");

    /// <summary>The module an identifier names between its slash and its dot.</summary>
    /// <param name="id">The record id.</param>
    /// <returns>The module name.</returns>
    static string ModuleOf(string id) => id.Split('/')[1].Split('.')[0];

    /// <summary>The record name an identifier carries after its module and dot.</summary>
    /// <param name="id">The record id.</param>
    /// <returns>The record name.</returns>
    static string NameOf(string id) => id.Split('.')[1];

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
