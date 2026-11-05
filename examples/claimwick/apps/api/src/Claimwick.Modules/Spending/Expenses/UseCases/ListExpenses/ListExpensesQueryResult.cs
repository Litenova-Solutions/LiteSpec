namespace Claimwick.Spending.Expenses.UseCases.ListExpenses;

public sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
