namespace Claimwick.Spending.Expenses.ListExpenses;

public sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
