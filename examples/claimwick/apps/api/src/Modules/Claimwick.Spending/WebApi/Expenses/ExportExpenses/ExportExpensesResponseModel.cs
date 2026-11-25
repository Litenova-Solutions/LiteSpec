namespace Claimwick.Spending.WebApi.Expenses.ExportExpenses;

/// <summary>The answer to an export request: one row per approved expense, with the approved expenses counted by currency code.</summary>
internal sealed record ExportExpensesResponseModel(IReadOnlyList<ExportedExpenseResponseItemModel> Rows, IReadOnlyDictionary<string, int> CountByCurrency);
