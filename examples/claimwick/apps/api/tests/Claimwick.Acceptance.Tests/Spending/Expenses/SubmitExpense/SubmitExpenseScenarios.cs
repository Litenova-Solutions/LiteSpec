namespace Claimwick.Acceptance.Tests.Spending.Expenses.SubmitExpense;

public sealed class SubmitExpenseScenarios
{
    /// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
    [Fact]
    public Task Submitting_with_a_receipt_succeedsAsync() => Runner.RunScenarioAsync(
        when => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
        then => new SubmitExpenseSteps().TheExpenseIsSubmittedAsync());

    /// <proves>scenario/submit-expense.submitting-without-a-receipt-is-refused</proves>
    [Fact]
    public Task Submitting_without_a_receipt_is_refusedAsync() => Runner.RunScenarioAsync(
        when => new SubmitExpenseSteps().SubmitAnExpenseWithoutAReceiptAsync("employee-1"),
        then => new SubmitExpenseSteps().TheExpenseIsRefusedAsReceiptMissingAsync());
}
