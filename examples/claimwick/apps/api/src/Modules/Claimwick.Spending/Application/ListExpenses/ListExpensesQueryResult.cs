namespace Claimwick.Spending.Application.ListExpenses;

internal sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
