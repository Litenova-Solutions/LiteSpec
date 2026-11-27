namespace Claimwick.Reporting.WebApi.ListReimbursedExpenses;

/// <summary>The answer to a list request: one row per reimbursed expense, newest first.</summary>
internal sealed record ListReimbursedExpensesResponseModel(IReadOnlyList<ReimbursedExpenseResponseItemModel> Expenses);
