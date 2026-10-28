namespace Claimwick.Acceptance.Tests.Spending.Expenses.RefuseExpense;

public sealed class RefuseExpenseScenarios
{
    /// <proves>scenario/refuse-expense.rejecting-a-submitted-expense-succeeds</proves>
    public Task Rejecting_a_submitted_expense_succeedsAsync() => Runner.RunScenarioAsync(
        given => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
        when => new RefuseExpenseSteps().RejectAnExpenseAsync("expense-1"),
        then => new RefuseExpenseSteps().TheExpenseIsRefusedAsync());
}
