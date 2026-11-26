namespace Claimwick.Spending.WebApi.Expenses.ExportExpenses;

/// <summary>The answer to an export request: one row per approved expense.</summary>
internal sealed record ExportExpensesResponseModel(IReadOnlyList<ExportedExpenseResponseItemModel> Rows);
