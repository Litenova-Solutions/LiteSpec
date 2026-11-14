using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the view checks: each view field is taken from a payload field of an outbound integration event, each personal field names its erasing event, each view is read, and each view is keyed by another module's aggregate.</summary>
public sealed class ViewCheckTests
{
    /// <summary>The four finding codes the view checks report.</summary>
    static readonly string[] ViewCodes = ["view-source-unknown", "view-erasure-missing", "view-unused", "view-key-invalid"];

    /// <summary>A view field taken from the employee payload field of the reimbursed event, internal.</summary>
    const string EmployeeField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "internal"}""";

    /// <summary>A view field taken from the employee payload field of the reimbursed event, personal.</summary>
    const string PersonalEmployeeField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal"}""";

    /// <summary>A view clean of every view finding: an outbound event, a reading use case, and a key of another module.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void ViewReadFromAnOutboundEventPassesEveryViewRule()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, EmployeeField),
            Reader(temp.Path));

        Assert.Empty(ViewFindings(model));
    }

    /// <summary>A field taken from an event no record names reports view-source-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void FieldFromAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            View(temp.Path, EmployeeField),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-source-unknown");
        Assert.Contains("integration-event/expense-reimbursed", finding.Message);
    }

    /// <summary>A field taken from an inbound integration event reports view-source-unknown, because a view reads only what the system publishes.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void FieldFromAnInboundEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path, "in"),
            View(temp.Path, EmployeeField),
            Reader(temp.Path));

        Assert.Contains(ViewFindings(model), found => found.Code == "view-source-unknown");
    }

    /// <summary>A field naming a payload field the event does not carry reports view-source-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void FieldNamingAMissingPayloadFieldReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        const string accountField = """{"name": "accountId", "meaning": "The account.", "type": "id", "from": "integration-event/expense-reimbursed#accountId", "classification": "internal"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, accountField),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-source-unknown");
        Assert.Contains("accountId", finding.Message);
    }

    /// <summary>A removal event that is not an outbound integration event reports view-source-unknown.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void RemovedByAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, EmployeeField, """, "removedBy": ["integration-event/expense-erased"]"""),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-source-unknown");
        Assert.Contains("integration-event/expense-erased", finding.Message);
    }

    /// <summary>An erasedBy event that is not an outbound integration event reports view-source-unknown.</summary>
    /// <proves>litespec/core/automation#erase-a-viewed-personal-field-with-its-owner</proves>
    [Fact]
    public void ErasedByAnUnknownEventReportsSourceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        const string erasedField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal", "erasedBy": "integration-event/expense-erased"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, erasedField),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-source-unknown");
        Assert.Contains("integration-event/expense-erased", finding.Message);
    }

    /// <summary>A personal field that names no erasedBy event reports view-erasure-missing at tier G.</summary>
    /// <proves>litespec/core/automation#erase-a-viewed-personal-field-with-its-owner</proves>
    [Fact]
    public void PersonalFieldWithoutErasedByReportsErasureMissing()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, PersonalEmployeeField),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-erasure-missing");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("view/reporting.reimbursed-expense", finding.Subject);
    }

    /// <summary>A personal field that names an outbound erasing event passes the erasure rule.</summary>
    /// <proves>litespec/core/automation#erase-a-viewed-personal-field-with-its-owner</proves>
    [Fact]
    public void PersonalFieldErasedByAnOutboundEventPassesErasure()
    {
        using var temp = TestCli.Temp("checks");
        const string erasedField = """{"name": "employeeId", "meaning": "The employee who was paid.", "type": "id", "from": "integration-event/expense-reimbursed#employeeId", "classification": "personal", "erasedBy": "integration-event/expense-erased"}""";
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            ErasedEvent(temp.Path),
            View(temp.Path, erasedField),
            Reader(temp.Path));

        Assert.DoesNotContain(ViewFindings(model), found => found.Code == "view-erasure-missing");
        Assert.DoesNotContain(ViewFindings(model), found => found.Code == "view-source-unknown");
    }

    /// <summary>A view no use case reads reports view-unused at tier A.</summary>
    /// <proves>litespec/core/automation#declare-each-view-field-from-a-published-field</proves>
    [Fact]
    public void ViewReadByNoUseCaseReportsUnread()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, EmployeeField));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-unused");
        Assert.Equal("A", finding.Tier);
    }

    /// <summary>A view keyed by an aggregate of its own module reports view-key-invalid at tier G.</summary>
    /// <proves>litespec/backend/application#read-another-modules-state-through-a-view</proves>
    [Fact]
    public void ViewKeyedByItsOwnModuleReportsKeyLocal()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            ReimbursedEvent(temp.Path),
            View(temp.Path, EmployeeField, keyedBy: "aggregate/reporting.expense"),
            Reader(temp.Path));

        var finding = Assert.Single(ViewFindings(model), found => found.Code == "view-key-invalid");
        Assert.Equal("G", finding.Tier);
    }

    /// <summary>An event handler projection in a module Views folder may carry the view mark, which a reaction does not take.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-view-in-its-module-views-folder</proves>
    [Fact]
    public void ProjectionInAViewsFolderMayCarryTheViewMark()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "src", "Reporting", "Views", "ReimbursedExpenses", "ReimbursedExpenseOnExpenseReimbursedProjection.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path), View(temp.Path, EmployeeField), Reader(temp.Path));
        model.Code.Elements.Add(Projection(file));

        Assert.DoesNotContain(
            Checker.Check(model).Findings,
            found => found.Code == "mark-unknown" && found.Subject == "view/reporting.reimbursed-expense");
    }

    /// <summary>An event handler outside a module Views folder may not carry the view mark: it takes a reaction mark.</summary>
    /// <proves>litespec/backend/dotnet/architecture#place-each-view-in-its-module-views-folder</proves>
    [Fact]
    public void ProjectionOutsideAViewsFolderReportsTheViewMark()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "src", "Reporting", "ReimbursedExpenseOnExpenseReimbursedProjection.cs");
        var model = TestModel.Model(TestModel.Config(temp.Path), View(temp.Path, EmployeeField), Reader(temp.Path));
        model.Code.Elements.Add(Projection(file));

        Assert.Contains(
            Checker.Check(model).Findings,
            found => found.Code == "mark-unknown" && found.Subject == "view/reporting.reimbursed-expense");
    }

    /// <summary>A projection event handler carrying the view's mark.</summary>
    /// <param name="file">The file the projection sits in.</param>
    /// <returns>The marked event handler element.</returns>
    static MarkedElement Projection(string file) =>
        new(CodeKind.EventHandler, "ReimbursedExpenseOnExpenseReimbursedProjection", null, file, 9, [new CodeMark("view/reporting.reimbursed-expense", MarkStyle.Xml, MarkVerb.Implements, file, 8)], InTests: false);

    /// <summary>The view findings of a model, in the order the checks raise them.</summary>
    /// <param name="model">The model under test.</param>
    /// <returns>The findings whose code is one of the four view codes.</returns>
    static List<Finding> ViewFindings(ProjectModel model) =>
        Checker.Check(model).Findings.Where(found => ViewCodes.Contains(found.Code, StringComparer.Ordinal)).ToList();

    /// <summary>The outbound integration event of the spending module that a view reads, with its payload.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="direction">The direction the event declares.</param>
    /// <returns>The integration event record.</returns>
    static Record ReimbursedEvent(string root, string direction = "out") => TestModel.Record(
        "integration-event",
        "integration-event/expense-reimbursed",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "integration-events", "expense-reimbursed", "expense-reimbursed.yaml"),
        "{\"direction\": \"" + direction + "\", \"translates\": \"event/spending.expense-reimbursed\", \"payload\": [{\"name\": \"employeeId\", \"meaning\": \"The employee who was paid.\", \"classification\": \"personal\", \"type\": \"id\", \"required\": true}, {\"name\": \"amount\", \"meaning\": \"The amount paid.\", \"classification\": \"internal\", \"type\": \"money\", \"required\": true}]}");

    /// <summary>The outbound integration event that erases a viewed expense.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The integration event record.</returns>
    static Record ErasedEvent(string root) => TestModel.Record(
        "integration-event",
        "integration-event/expense-erased",
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", "integration-events", "expense-erased", "expense-erased.yaml"),
        """{"direction": "out", "translates": "event/spending.expense-erased", "payload": [{"name": "expenseId", "meaning": "The erased expense.", "classification": "internal", "type": "id", "required": true}]}""");

    /// <summary>The view of reimbursed expenses, keyed by an aggregate of the spending module by default.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <param name="field">One field entry as JSON.</param>
    /// <param name="extra">Further JSON members that follow the fields, such as removedBy.</param>
    /// <param name="keyedBy">The aggregate the view is keyed by.</param>
    /// <returns>The view record.</returns>
    static Record View(string root, string field, string extra = "", string keyedBy = "aggregate/spending.expense") => TestModel.Record(
        "view",
        "view/reporting.reimbursed-expense",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "views", "reimbursed-expense", "reimbursed-expense.yaml"),
        "{\"summary\": \"What Reporting keeps about each reimbursed expense.\", \"keyedBy\": \"" + keyedBy + "\", \"fields\": [" + field + "], \"freshness\": \"PT5S\"" + extra + "}");

    /// <summary>The query that reads the view, so the view is not unread.</summary>
    /// <param name="root">The consumer folder.</param>
    /// <returns>The use case record.</returns>
    static Record Reader(string root) => TestModel.Record(
        "use-case",
        "use-case/reporting.total-spending",
        Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "use-cases", "total-spending", "total-spending.yaml"),
        """{"kind": "query", "reads": ["view/reporting.reimbursed-expense"]}""");
}
