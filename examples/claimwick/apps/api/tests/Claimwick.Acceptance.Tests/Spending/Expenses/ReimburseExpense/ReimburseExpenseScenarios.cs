namespace Claimwick.Acceptance.Tests.Spending.Expenses.ReimburseExpense;

public sealed class ReimburseExpenseScenarios
{
    /// <proves>scenario/reimburse-expense.reimbursing-an-approved-expense-succeeds</proves>
    [Fact]
    public Task Reimbursing_an_approved_expense_succeedsAsync() => Runner.RunScenarioAsync(
        given => new ApproveExpenseSteps().ApproveAnExpenseAsync("expense-1"),
        when => new ReimburseExpenseSteps().ReimburseAnExpenseAsync("expense-1"),
        then => new ReimburseExpenseSteps().TheExpenseIsReimbursedAsync());

    /// <proves>scenario/reimburse-expense.reimbursing-an-unapproved-expense-is-refused</proves>
    [Fact]
    public Task Reimbursing_an_unapproved_expense_is_refusedAsync() => Runner.RunScenarioAsync(
        given => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
        when => new ReimburseExpenseSteps().ReimburseAnExpenseAsync("expense-1"),
        then => new ReimburseExpenseSteps().TheExpenseIsRefusedAsNotApprovedAsync());

    /// <proves>scenario/reimburse-expense.reimbursing-a-reimbursed-expense-is-refused</proves>
    [Fact]
    public Task Reimbursing_a_reimbursed_expense_is_refusedAsync() => Runner.RunScenarioAsync(
        given => new ReimburseExpenseSteps().ReimburseAnExpenseAsync("expense-1"),
        when => new ReimburseExpenseSteps().ReimburseAnExpenseAsync("expense-1"),
        then => new ReimburseExpenseSteps().TheExpenseIsRefusedAsAlreadyReimbursedAsync());
}
