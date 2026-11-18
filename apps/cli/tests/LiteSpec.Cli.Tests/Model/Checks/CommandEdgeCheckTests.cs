using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves every commands edge names a command use case listing the sending module, and every listed module commands it.</summary>
public sealed class CommandEdgeCheckTests
{
    /// <summary>Use Case Commanding Query Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseCommandingQueryReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.read-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.read-expense", ProviderFile(temp.Path, "read-expense.yaml"), """{"kind": "query", "steps": [{"statement": "Spending reads the expense."}]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("use-case/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Use Case Commanding Command That Lists No Sending Module Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseCommandingCommandThatListsNoSendingModuleReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/finance"]}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("use-case/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Use Case Commanding Listed Public Command Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseCommandingListedPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "command-target-invalid" or "command-consumer-missing");
    }

    /// <summary>Reaction Commanding Listed Public Command Passes.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void ReactionCommandingListedPublicCommandPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("reaction", "reaction/reporting.confirm-expense", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "reporting", "reactions.yaml"), """{"triggeredBy": ["module-event/spending.expense-reimbursed"], "effects": [{"commands": "use-case/spending.reimburse-expense"}], "outcomes": [{"name": "completed", "meaning": "The expense is confirmed."}], "onFailure": {"mode": "discard"}, "idempotency": {"mode": "state", "recognizedBy": "state"}}"""),
            TestModel.Record("module-event", "module-event/spending.expense-reimbursed", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "module-events.yaml"), """{"summary": "The expense was reimbursed."}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "command-target-invalid" or "command-consumer-missing");
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
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-expense", "timing": "inline"}]}"""),
            TestModel.Record("use-case", "use-case/spending.reimburse-expense", ProviderFile(temp.Path, "reimburse-expense.yaml"), """{"kind": "command", "reachedThrough": ["module/reporting"]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding =>
            finding.Code is "query-target-invalid" or "query-consumer-missing");
    }

    /// <summary>Use Case Commanding Notification Reports.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void UseCaseCommandingNotificationReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "notification/spending.approval-reminder", "timing": "inline"}]}"""),
            TestModel.Record("notification", "notification/spending.approval-reminder", Path.Combine(temp.Path, ".litespec", "behavior", "modules", "spending", "notifications.yaml"), """{"summary": "The approver is reminded."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "command-target-invalid");
        Assert.Equal("use-case/reporting.confirm-payout", finding.Subject);
    }

    /// <summary>Commands Edge To Unknown Target Reports Only Reference Unknown.</summary>
    /// <proves>litespec/core/model#connect-across-modules-only-through-declared-dependencies</proves>
    [Fact]
    public void CommandsEdgeToUnknownTargetReportsOnlyReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            Sender(temp.Path, "use-case/reporting.confirm-payout", """{"kind": "command", "steps": [{"statement": "Reporting confirms the payout."}], "effects": [{"commands": "use-case/spending.reimburse-batch", "timing": "inline"}]}"""));

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "command-target-invalid");
        Assert.Contains(Checker.Check(model).Findings, finding => finding.Code == "reference-unknown");
    }

    /// <summary>One sender use case in the reporting module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="id">The record id.</param>
    /// <param name="json">The record fields as a JSON object.</param>
    /// <returns>The record in its module folder.</returns>
    static LiteSpec.Cli.Model.Records.Record Sender(string root, string id, string json) =>
        TestModel.Record("use-case", id, Path.Combine(root, ".litespec", "behavior", "modules", "reporting", "confirm-payout.yaml"), json);

    /// <summary>One provider file in the spending module.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    static string ProviderFile(string root, string file) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "spending", file);
}
