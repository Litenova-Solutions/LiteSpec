namespace Claimwick.Spending.Expenses.UseCases.ListExpenses.Application;

public sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
