namespace Claimwick.Acceptance.Tests.Spending.Expenses.ApproveExpense;

public sealed class ApproveExpenseScenarios
{
    /// <proves>scenario/approve-expense.approving-a-submitted-expense-succeeds</proves>
    [Fact]
    public Task Approving_a_submitted_expense_succeedsAsync() => Runner.RunScenarioAsync(
        given => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
        when => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        then => new ApproveExpenseSteps().TheExpenseIsApprovedAsync());

    /// <proves>scenario/approve-expense.approving-an-own-expense-is-refused</proves>
    [Fact]
    public Task Approving_an_own_expense_is_refusedAsync() => Runner.RunScenarioAsync(
        given => new SubmitExpenseSteps().SubmitAnExpenseAsync("manager-1"),
        when => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        then => new ApproveExpenseSteps().TheExpenseIsRefusedAsOwnExpenseAsync());

    /// <proves>scenario/approve-expense.approving-a-decided-expense-is-refused</proves>
    [Fact]
    public Task Approving_a_decided_expense_is_refusedAsync() => Runner.RunScenarioAsync(
        given => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        when => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        then => new ApproveExpenseSteps().TheExpenseIsRefusedAsNotSubmittedAsync());
}
