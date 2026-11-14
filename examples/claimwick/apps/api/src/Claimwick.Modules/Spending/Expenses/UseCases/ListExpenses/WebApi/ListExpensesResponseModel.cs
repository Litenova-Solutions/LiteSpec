namespace Claimwick.Spending.Expenses.UseCases.ListExpenses.WebApi;

/// <summary>The answer to a list request: one row per expense, newest first.</summary>
public sealed record ListExpensesResponseModel(IReadOnlyList<ExpenseResponseItemModel> Expenses);
