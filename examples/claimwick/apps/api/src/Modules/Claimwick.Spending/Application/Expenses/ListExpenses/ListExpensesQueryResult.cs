namespace Claimwick.Spending.Application.Expenses.ListExpenses;

internal sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
