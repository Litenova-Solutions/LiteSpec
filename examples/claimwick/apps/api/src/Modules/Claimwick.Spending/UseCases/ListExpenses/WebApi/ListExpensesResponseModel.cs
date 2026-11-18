namespace Claimwick.Spending.UseCases.ListExpenses.WebApi;

/// <summary>The answer to a list request: one row per expense, newest first.</summary>
internal sealed record ListExpensesResponseModel(IReadOnlyList<ExpenseResponseItemModel> Expenses);
