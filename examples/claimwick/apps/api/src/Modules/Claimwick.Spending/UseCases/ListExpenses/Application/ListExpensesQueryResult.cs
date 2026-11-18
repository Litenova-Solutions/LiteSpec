namespace Claimwick.Spending.UseCases.ListExpenses.Application;

internal sealed record ListExpensesQueryResult(IReadOnlyList<ExpenseSummary> Expenses);
