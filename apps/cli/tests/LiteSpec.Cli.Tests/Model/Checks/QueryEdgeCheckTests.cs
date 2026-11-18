using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every queries edge names a query use case listing the calling module, and every listed module queries it.</summary>
public sealed class QueryEdgeCheckTests
{
    /// <summary>Use Case Querying Command Reports.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void UseCaseQueryingCommandReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}], "effects": [{"queries": "use-case/spending.submit-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.submit-expense", ProviderFile(temp.Path, "submit-expense.yaml"), """{"kind": "command", "steps": [{"statement": "Spending submits the expense."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "query-target-invalid");
        Assert.Equal("use-case/reporting.record-reimbursement", finding.Subject);
    }

    /// <summary>Use Case Querying Query That Lists No Calling Module Reports.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void UseCaseQueryingQueryThatListsNoCallingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}], "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "reachedThrough": ["module/finance"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "query-target-invalid");
        Assert.Equal("use-case/reporting.record-reimbursement", finding.Subject);
    }

    /// <summary>Use Case Querying Listed Public Query Passes.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void UseCaseQueryingListedPublicQueryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}], "effects": [{"queries": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "query-target-invalid" or "query-consumer-missing");
    }

    /// <summary>Reaction Querying Listed Public Query Passes.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void ReactionQueryingListedPublicQueryPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/reporting.confirm-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "reactions.yaml"), """{"triggeredBy": ["module-event/spending.expense-reimbursed"], "effects": [{"queries": "use-case/spending.read-expense"}], "outcomes": [{"name": "completed", "meaning": "The expense is confirmed."}], "onFailure": {"mode": "discard"}, "idempotency": {"mode": "state", "recognizedBy": "state"}}"""),
            TestModel.Record("module-event", "module-event/spending.expense-reimbursed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events.yaml"), """{"summary": "The expense was reimbursed."}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "query-target-invalid" or "query-consumer-missing");
    }

    /// <summary>Public Query No Module Queries Reports.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void PublicQueryNoModuleQueriesReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "reachedThrough": ["module/reporting"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "query-consumer-missing");
        Assert.Equal("use-case/spending.read-expense", finding.Subject);
    }

    /// <summary>Use Case Querying Notification Reports.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void UseCaseQueryingNotificationReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}], "effects": [{"queries": "notification/spending.approval-reminder", "timing": "inline"}]}"""),
            TestModel.Record("notification", "notification/spending.approval-reminder", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "notifications.yaml"), """{"summary": "The approver is reminded."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "query-target-invalid");
        Assert.Equal("use-case/reporting.record-reimbursement", finding.Subject);
    }

    /// <summary>Queries Edge To Unknown Target Reports Only Reference Unknown.</summary>
    /// <proves>litespec/backend/application#ask-another-module-one-question-through-a-decision-port</proves>
    [Fact]
    public void QueriesEdgeToUnknownTargetReportsOnlyReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Consumer(temp.Path, "use-case/reporting.record-reimbursement", """{"kind": "command", "steps": [{"statement": "Reporting records the reimbursement."}], "effects": [{"queries": "use-case/spending.read-ledger", "timing": "inline"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "query-target-invalid");
        Assert.Contains(Checker.Check(model).Findings, finding => finding.Code == "reference-unknown");
    }

    /// <summary>One consumer use case in the reporting module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record Consumer(string root, string id, string json) =>
        TestModel.Record("use-case", id, Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "record-reimbursement.yaml"), json);

    /// <summary>One provider file in the spending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string ProviderFile(string root, string file) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", file);
}
