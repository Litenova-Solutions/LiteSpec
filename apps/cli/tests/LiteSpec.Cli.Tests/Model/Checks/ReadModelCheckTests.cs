using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the read-model checks: each read-model field is taken from a payload field of an outbound integration event, each personal field names its erasing event, each read model is read, and each read model is keyed by another module's aggregate.</summary>
public sealed class ReadModelCheckTests
{
    /// <summary>The four finding codes the read-model checks report.</summary>
    static readonly string[] ReadModelCodes = ["reference-unknown", "read-model-erasure-missing", "read-model-unused", "read-model-key-invalid"];

    /// <summary>A read-model field taken from the employee payload field of the reimbursed event, internal.</summary>
    const string EmployeeField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "internal"}""";

    /// <summary>A read-model field taken from the employee payload field of the reimbursed event, personal.</summary>
    const string PersonalEmployeeField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal"}""";

    /// <summary>A read model clean of every read-model finding: an outbound event, a reading use case, and a key of another module.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void ReadModelReadFromAnOutboundEventPassesEveryReadModelRule()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReimbursedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, EmployeeField),
            Reader(temp.Path));

        Assert.Empty(ReadModelFindings(model));
    }

    /// <summary>A field taken from an event no record names reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void FieldFromAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReadModel(temp.Path, EmployeeField),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "reference-unknown");
        Assert.Contains("integration-event/expense-reimbursed", finding.Message);
    }

    /// <summary>A field taken from an inbound integration event reports reference-unknown, because a read model reads only what the system publishes.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void FieldFromAnInboundEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path, "in"),
            ReadModel(temp.Path, EmployeeField),
            Reader(temp.Path));

        Assert.Contains(ReadModelFindings(model), found => found.Code == "reference-unknown");
    }

    /// <summary>A field naming a payload field the event does not carry reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void FieldNamingAMissingPayloadFieldReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        const string accountField = """{"name": "accountId", "meaning": "The account.", "type": "id", "from": "integration-event/expense-reimbursed#accountId", "classification": "internal"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReimbursedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, accountField),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "reference-unknown");
        Assert.Contains("accountId", finding.Message);
    }

    /// <summary>A removal event that is not an outbound integration event reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void RemovedByAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReimbursedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, EmployeeField, """, "removedBy": ["integration-event/expense-erased"]"""),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "reference-unknown");
        Assert.Contains("integration-event/expense-erased", finding.Message);
    }

    /// <summary>An erasedBy event that is not an outbound integration event reports reference-unknown.</summary>
    /// <proves>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</proves>
    [Fact]
    public void ErasedByAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        const string erasedField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal", "erasedBy": "integration-event/expense-erased"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReimbursedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, erasedField),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "reference-unknown");
        Assert.Contains("integration-event/expense-erased", finding.Message);
    }

    /// <summary>A personal field that names no erasedBy event reports read-model-erasure-missing at tier G.</summary>
    /// <proves>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</proves>
    [Fact]
    public void PersonalFieldWithoutErasedByReportsErasureMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReadModel(temp.Path, PersonalEmployeeField),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "read-model-erasure-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("read-model/reporting.reimbursed-expense", finding.Subject);
    }

    /// <summary>A personal field that names an outbound erasing event passes the erasure rule.</summary>
    /// <proves>litespec/core/automation#erase-a-read-model-personal-field-with-its-owner</proves>
    [Fact]
    public void PersonalFieldErasedByAnOutboundEventPassesErasure()
    {
        using var temp = TestCli.Temp("checks");
        const string erasedField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal", "erasedBy": "integration-event/expense-erased"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ErasedEvent(temp.Path),
            ReimbursedDomainEvent(temp.Path),
            ErasedDomainEvent(temp.Path),
            ExpenseAggregate(temp.Path),
            ReadModel(temp.Path, erasedField),
            Reader(temp.Path));

        Assert.DoesNotContain(ReadModelFindings(model), found => found.Code == "read-model-erasure-missing");
        Assert.DoesNotContain(ReadModelFindings(model), found => found.Code == "reference-unknown");
    }

    /// <summary>A read model no use case reads reports read-model-unused at tier G.</summary>
    /// <proves>litespec/core/automation#declare-each-read-model-field-from-a-published-field</proves>
    [Fact]
    public void ReadModelReadByNoUseCaseReportsUnread()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReadModel(temp.Path, EmployeeField));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "read-model-unused");
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>A read model keyed by an aggregate of its own module reports read-model-key-invalid at tier G.</summary>
    /// <proves>litespec/backend/application#read-another-modules-state-through-a-read-model</proves>
    [Fact]
    public void ReadModelKeyedByItsOwnModuleReportsKeyLocal()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ReadModel(temp.Path, EmployeeField, keyedBy: "aggregate/reporting.expense"),
            Reader(temp.Path));

        var finding = Assert.Single(ReadModelFindings(model), found => found.Code == "read-model-key-invalid");
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>An event handler projection in a module ReadModels folder may carry the read-model mark, which a reaction does not take.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-read-model-in-its-module-read-models-folder</proves>
    [Fact]
    public void ProjectionInAReadModelsFolderMayCarryTheReadModelMark()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "src", "Reporting", "ReadModels", "ReimbursedExpenses", "ReimbursedExpenseOnExpenseReimbursedProjection.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path), ReadModel(temp.Path, EmployeeField), Reader(temp.Path));
        model.Code.Elements.Add(Projection(file));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            found => found.Code == "mark-unknown" && found.Subject == "read-model/reporting.reimbursed-expense");
    }

    /// <summary>An event handler outside a module ReadModels folder may not carry the read-model mark: it takes a reaction mark.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-read-model-in-its-module-read-models-folder</proves>
    [Fact]
    public void ProjectionOutsideAReadModelsFolderReportsTheReadModelMark()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "src", "Reporting", "ReimbursedExpenseOnExpenseReimbursedProjection.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path), ReadModel(temp.Path, EmployeeField), Reader(temp.Path));
        model.Code.Elements.Add(Projection(file));

        Assert.Contains(
            Checker.Check(model).Findings,
            found => found.Code == "mark-unknown" && found.Subject == "read-model/reporting.reimbursed-expense");
    }

    /// <summary>A projection event handler carrying the read-model mark.</summary>
    /// <param name="file">The file the projection sits in.</param>
    /// <returns>The marked event handler element.</returns>
    static MarkedElement Projection(string file) =>
        new(CodeKind.EventHandler, "ReimbursedExpenseOnExpenseReimbursedProjection", null, file, 9, [new CodeMark("read-model/reporting.reimbursed-expense", MarkStyle.Xml, MarkVerb.Implements, file, 8)], InTests: false);

    /// <summary>The read-model findings of a model, in the order the checks raise them.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is one of the four read-model codes.</returns>
    static List<Finding> ReadModelFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => ReadModelCodes.Contains(found.Code, StringComparer.Ordinal)).ToList();

    /// <summary>The outbound integration event of the spending module that a read model reads, with its payload.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="direction">The direction the event declares.</param>
    /// <returns>The integration event record.</returns>
    static Record ReimbursedEvent(string root, string direction = "out") => TestModel.Record(
        "integration-event",
        "integration-event/expense-reimbursed",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "integration-events", "expense-reimbursed", "expense-reimbursed.yaml"),
        "{\"direction\": \"" + direction + "\", \"translates\": \"domain-event/spending.expense-reimbursed\", \"payload\": [{\"name\": \"employeeId\", \"meaning\": \"The employee who was paid.\", \"classification\": \"personal\", \"type\": \"id\", \"required\": true}, {\"name\": \"amount\", \"meaning\": \"The amount paid.\", \"classification\": \"internal\", \"type\": \"money\", \"required\": true}]}");

    /// <summary>The outbound integration event that erases an expense held in the read model.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The integration event record.</returns>
    static Record ErasedEvent(string root) => TestModel.Record(
        "integration-event",
        "integration-event/expense-erased",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "integration-events", "expense-erased", "expense-erased.yaml"),
        """{"direction": "out", "translates": "domain-event/spending.expense-erased", "payload": [{"name": "expenseId", "meaning": "The erased expense.", "classification": "internal", "type": "id", "required": true}]}""");

    /// <summary>The domain event the reimbursed integration event translates, carrying the payload fields.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The domain event record.</returns>
    static Record ReimbursedDomainEvent(string root) => TestModel.Record(
        "domain-event",
        "domain-event/spending.expense-reimbursed",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "domain-events", "expense-reimbursed", "expense-reimbursed.yaml"),
        """{"meaning": "An expense was reimbursed.", "carries": [{"name": "employeeId", "type": "id"}, {"name": "amount", "type": "money"}]}""");

    /// <summary>The domain event the erased integration event translates, carrying the erased expense.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The domain event record.</returns>
    static Record ErasedDomainEvent(string root) => TestModel.Record(
        "domain-event",
        "domain-event/spending.expense-erased",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "domain-events", "expense-erased", "expense-erased.yaml"),
        """{"meaning": "An expense was erased.", "carries": [{"name": "expenseId", "type": "id"}]}""");

    /// <summary>The spending aggregate the read model is keyed by.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The aggregate record.</returns>
    static Record ExpenseAggregate(string root) => TestModel.Record(
        "aggregate",
        "aggregate/spending.expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "aggregates", "expense", "aggregate.yaml"),
        """{"states": [{"name": "Active", "meaning": "The expense is open."}]}""");

    /// <summary>The read model of reimbursed expenses, keyed by an aggregate of the spending module by default.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="field">One field entry as JSON.</param>
    /// <param name="extra">Further JSON members that follow the fields, such as removedBy.</param>
    /// <param name="keyedBy">The aggregate the read model is keyed by.</param>
    /// <returns>The read-model record.</returns>
    static Record ReadModel(string root, string field, string extra = "", string keyedBy = "aggregate/spending.expense") => TestModel.Record(
        "read-model",
        "read-model/reporting.reimbursed-expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "read-models", "reimbursed-expense", "reimbursed-expense.yaml"),
        "{\"summary\": \"What Reporting keeps about each reimbursed expense.\", \"keyedBy\": \"" + keyedBy + "\", \"fields\": [" + field + "], \"freshness\": \"PT5S\"" + extra + "}");

    /// <summary>The query that reads the read model, so the read model is not unread.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The use case record.</returns>
    static Record Reader(string root) => TestModel.Record(
        "use-case",
        "use-case/reporting.total-spending",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "use-cases", "total-spending", "total-spending.yaml"),
        """{"kind": "query", "reads": ["read-model/reporting.reimbursed-expense"]}""");
}
