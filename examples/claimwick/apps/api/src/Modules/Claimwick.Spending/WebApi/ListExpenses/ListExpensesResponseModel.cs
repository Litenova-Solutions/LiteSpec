namespace Claimwick.Spending.WebApi.ListExpenses;

/// <summary>The answer to a list request: one row per expense, newest first.</summary>
internal sealed record ListExpensesResponseModel(IReadOnlyList<ExpenseResponseItemModel> Expenses);
