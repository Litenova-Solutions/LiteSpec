using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the freshness decider rule: a decidedBy names the command use case or aggregate that decides the figure, in the module that owns the figure's source, and only with snapshot or eventual freshness.</summary>
public sealed class FreshnessDeciderCheckTests
{
    /// <summary>The record schema file, relative to the release root.</summary>
    const string SchemaFile = "schemas/record.schema.yaml";

    /// <summary>A query group and a read model decided by the command that decides their figure pass the decider rule.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void GroupAndReadModelDecidedByTheirSourceCommandPass()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ExpenseAggregate(temp.Path),
            ReimburseCommand(temp.Path),
            DecidedQuery(temp.Path, "use-case/spending.reimburse-expense", "aggregate/spending.expense"),
            DecidedReadModel(temp.Path, "use-case/spending.reimburse-expense"));

        Assert.Empty(DeciderFindings(model));
    }

    /// <summary>A decidedBy naming a query reports freshness-decider-invalid at tier G, because a query decides nothing.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void DecidedByNamingAQueryReportsDeciderInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ExpenseAggregate(temp.Path),
            DecidedQuery(temp.Path, "use-case/reporting.list-reimbursed-expenses", "use-case/spending.reimburse-expense"),
            OtherQuery(temp.Path));

        var finding = Assert.Single(DeciderFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("use-case/reporting.total-spending", finding.Subject);
        Assert.Contains("use-case/reporting.list-reimbursed-expenses", finding.Message);
    }

    /// <summary>A decidedBy naming a command outside the source module reports freshness-decider-invalid.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void DecidedByOutsideTheSourceModuleReportsDeciderInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ExpenseAggregate(temp.Path),
            BillingCommand(temp.Path),
            DecidedQuery(temp.Path, "use-case/billing.close-period", "use-case/spending.reimburse-expense"),
            ReimburseCommand(temp.Path));

        var finding = Assert.Single(DeciderFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Contains("use-case/billing.close-period", finding.Message);
    }

    /// <summary>A read model decided outside the module of its keyed aggregate reports freshness-decider-invalid.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void ReadModelDecidedOutsideItsKeyModuleReportsDeciderInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ExpenseAggregate(temp.Path),
            BillingCommand(temp.Path),
            DecidedReadModel(temp.Path, "use-case/billing.close-period"));

        var finding = Assert.Single(DeciderFindings(model));
        Assert.Equal("G", finding.Tier);
        Assert.Equal("read-model/reporting.reimbursed-expense", finding.Subject);
    }

    /// <summary>The schema accepts a decidedBy naming a command with snapshot freshness.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void SchemaAcceptsDecidedByWithSnapshotFreshness()
    {
        const string json = """{"summary": "What Reporting keeps about each reimbursed expense.", "keyedBy": "aggregate/spending.expense", "fields": [{"name": "amount", "meaning": "The exact amount paid.", "type": "decimal", "from": "integration-event/expense-reimbursed#amount", "classification": "internal"}], "freshness": {"mode": "snapshot", "within": "PT5S", "decidedBy": "use-case/spending.reimburse-expense"}}""";

        Assert.Empty(SchemaProblems("read-model", "read-model/reporting.reimbursed-expense", json));
    }

    /// <summary>The schema rejects a decidedBy with immediate freshness, because a current figure names no decider.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void SchemaRejectsDecidedByWithImmediateFreshness()
    {
        const string json = """{"summary": "What Reporting keeps about each reimbursed expense.", "keyedBy": "aggregate/spending.expense", "fields": [{"name": "amount", "meaning": "The exact amount paid.", "type": "decimal", "from": "integration-event/expense-reimbursed#amount", "classification": "internal"}], "freshness": {"mode": "immediate", "decidedBy": "use-case/spending.reimburse-expense"}}""";

        Assert.NotEmpty(SchemaProblems("read-model", "read-model/reporting.reimbursed-expense", json));
    }

    /// <summary>The schema rejects a decidedBy naming neither a use case nor an aggregate.</summary>
    /// <proves>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</proves>
    [Fact]
    public void SchemaRejectsDecidedByNamingNeitherUseCaseNorAggregate()
    {
        const string json = """{"summary": "What Reporting keeps about each reimbursed expense.", "keyedBy": "aggregate/spending.expense", "fields": [{"name": "amount", "meaning": "The exact amount paid.", "type": "decimal", "from": "integration-event/expense-reimbursed#amount", "classification": "internal"}], "freshness": {"mode": "snapshot", "within": "PT5S", "decidedBy": "rule/spending.positive-amount"}}""";

        Assert.NotEmpty(SchemaProblems("read-model", "read-model/reporting.reimbursed-expense", json));
    }

    /// <summary>The freshness decider findings of a model.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is freshness-decider-invalid.</returns>
    static List<Finding> DeciderFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => found.Code == "freshness-decider-invalid").ToList();

    /// <summary>The problems the record schema reports for the record.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The problem messages, empty when the schema accepts the record.</returns>
    static List<string> SchemaProblems(string type, string id, string json)
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

        var schema = RecordText.Read(LiteSpecHome.PathTo(SchemaFile));

        return JsonSchema.Problems(SchemaFile, record, id, _ => schema);
    }

    /// <summary>The spending expense aggregate that owns the figure's source.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The aggregate record.</returns>
    static Record ExpenseAggregate(string root) => TestModel.Record(
        "aggregate",
        "aggregate/spending.expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "aggregate.yaml"),
        """{"summary": "An expense.", "kind": "entity"}""");

    /// <summary>The spending command that decides each reimbursement.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The command use case record.</returns>
    static Record ReimburseCommand(string root) => TestModel.Record(
        "use-case",
        "use-case/spending.reimburse-expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expenses", "use-cases", "reimburse-expense", "reimburse-expense.yaml"),
        """{"summary": "Finance pays an approved expense.", "kind": "command"}""");

    /// <summary>A command of another module, which owns no figure the query shows.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The command use case record.</returns>
    static Record BillingCommand(string root) => TestModel.Record(
        "use-case",
        "use-case/billing.close-period",
        Path.Combine(root, ".litespec", "behavior", "modules", "billing", "use-cases", "close-period", "close-period.yaml"),
        """{"summary": "Finance closes the period.", "kind": "command"}""");

    /// <summary>A second query, which decides nothing.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The query use case record.</returns>
    static Record OtherQuery(string root) => TestModel.Record(
        "use-case",
        "use-case/reporting.list-reimbursed-expenses",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "use-cases", "list-reimbursed-expenses", "list-reimbursed-expenses.yaml"),
        """{"summary": "Finance lists the reimbursed expenses.", "kind": "query"}""");

    /// <summary>The reporting query showing reimbursed figures from the spending expense, with two decided groups.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="rowsDecidedBy">The decidedBy of the rows group.</param>
    /// <param name="totalDecidedBy">The decidedBy of the total group.</param>
    /// <returns>The query use case record.</returns>
    static Record DecidedQuery(string root, string rowsDecidedBy, string totalDecidedBy) => TestModel.Record(
        "use-case",
        "use-case/reporting.total-spending",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "use-cases", "total-spending", "total-spending.yaml"),
        """{"summary": "Finance reads the reimbursed spending.", "kind": "query", "reads": ["read-model/reporting.reimbursed-expense"], "output": {"include": {"rows": {"fields": [{"name": "amount", "meaning": "The exact amount paid.", "type": "decimal"}], "classification": "internal", "reads": ["aggregate/spending.expense"], "freshness": {"mode": "snapshot", "within": "PT5S", "decidedBy": """ + "\"" + rowsDecidedBy + "\"}" + """}, "total": {"fields": [{"name": "total", "meaning": "The total paid.", "type": "decimal"}], "classification": "internal", "reads": ["aggregate/spending.expense"], "freshness": {"mode": "snapshot", "within": "PT5S", "decidedBy": """ + "\"" + totalDecidedBy + "\"}" + """}}}}""");

    /// <summary>The reporting read model of reimbursed expenses, keyed by the spending expense.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="decidedBy">The decidedBy of the read model freshness.</param>
    /// <returns>The read-model record.</returns>
    static Record DecidedReadModel(string root, string decidedBy) => TestModel.Record(
        "read-model",
        "read-model/reporting.reimbursed-expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "read-models", "reimbursed-expense", "reimbursed-expense.yaml"),
        """{"summary": "What Reporting keeps about each reimbursed expense.", "keyedBy": "aggregate/spending.expense", "fields": [{"name": "amount", "meaning": "The exact amount paid.", "type": "decimal", "from": "integration-event/expense-reimbursed#amount", "classification": "internal"}], "freshness": {"mode": "snapshot", "within": "PT5S", "decidedBy": """ + "\"" + decidedBy + "\"}" + """}""");
}
