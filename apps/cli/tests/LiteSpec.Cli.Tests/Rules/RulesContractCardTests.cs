using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules</c> prints the record's contract card before the provisions.</summary>
public sealed class RulesContractCardTests
{
    /// <summary>A handler prints its use case's contract card first, one line per item.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesHandlerPrintsContractCardFirst()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: use-case/spending.submit-expense - Submit Expense", result.Output);
        Assert.Contains("kind: command; access: grant=permission/request-expense tenant=own", result.Output);
        Assert.Contains("input amount: decimal, required", result.Output);
        Assert.Contains("input receipt: id, optional", result.Output);
        Assert.Contains("result result/submit-expense.submitted: success - Submitted", result.Output);
        Assert.Contains("refusal result/submit-expense.receipt-missing: spending.receipt-missing - Attach the receipt for this expense, then submit again.", result.Output);
        Assert.Contains("raises: domain-event/expense.submitted", result.Output);
        Assert.Contains("reads: aggregate/spending.policy", result.Output);
        Assert.Contains("writes: aggregate/spending.expense", result.Output);
        Assert.Contains("scenario scenario/submit-expense.submitting-with-a-receipt-succeeds: Submitting with a Receipt Succeeds", result.Output);
        Assert.True(
            result.Output.IndexOf("Contract: use-case/spending.submit-expense", StringComparison.Ordinal)
                < result.Output.IndexOf("Required:", StringComparison.Ordinal),
            "The contract card prints before the judgment provisions.");
    }

    /// <summary>A use case record id prints the same card as its handler.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesRecordIdPrintsContractCard()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "use-case/spending.submit-expense");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: use-case/spending.submit-expense - Submit Expense", result.Output);
        Assert.Contains("kind: command; access: grant=permission/request-expense tenant=own", result.Output);
        Assert.Contains("input expenseId: id, required", result.Output);
    }

    /// <summary>A query handler prints each output field with its group.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesQueryHandlerPrintsGroupedOutput()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/ListExpenses/ListExpensesQueryHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: use-case/spending.list-expenses - List Expenses", result.Output);
        Assert.Contains("kind: query; access: grant=permission/list-expenses tenant=own", result.Output);
        Assert.Contains("output rows.expenses: list", result.Output);
    }

    /// <summary>A reaction record id prints its trigger and outcomes.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesReactionIdPrintsTriggerAndOutcomes()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "reaction/spending.send-approver-reminders-on-expense-submitted");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: reaction/spending.send-approver-reminders-on-expense-submitted - Send Approver Reminders on Expense Submitted", result.Output);
        Assert.Contains("triggeredBy: domain-event/expense.submitted", result.Output);
        Assert.Contains("outcome completed: The reminder was sent to the approver.", result.Output);
        Assert.Contains("effect sends notification/spending.approval-reminder", result.Output);
        Assert.Contains("scenario scenario/send-approver-reminders-on-expense-submitted.waiting-expense-earns-a-reminder: Waiting Expense Earns a Reminder", result.Output);
    }

    /// <summary>A job record id prints its kind, effects with timing, and outcomes.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesJobIdPrintsKindEffectsAndOutcomes()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run("rules", "--root", root, "job/spending.erase-expired-personal-data");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: job/spending.erase-expired-personal-data - Erase Expired Personal Data", result.Output);
        Assert.Contains("kind: scheduled", result.Output);
        Assert.Contains("effect runs use-case/spending.erase-personal-data (async)", result.Output);
        Assert.Contains("outcome completed: Every selected expense was erased.", result.Output);
    }

    /// <summary>With --all, the card still prints before the full checked list.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesAllKeepsContractCard()
    {
        var root = Path.Combine(TestCli.Repository, "examples", "claimwick");
        var result = TestCli.Run(
            "rules",
            "--root", root,
            "apps/api/src/Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs",
            "--all");

        Assert.Equal(0, result.Status);
        Assert.Contains("Contract: use-case/spending.submit-expense - Submit Expense", result.Output);
        Assert.Contains("Checked by a tool:", result.Output);
    }

    /// <summary>A file implementing only an aggregate prints no card.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesAggregateFilePrintsNoCard()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/Domain/Invoice.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("Contract:", result.Output);
    }

    /// <summary>A file whose record has no record file prints no card.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void RulesMissingRecordPrintsNoCard()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("Contract:", result.Output);
    }
}
