namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

internal sealed record ExportExpensesQueryResult(IReadOnlyList<ExportedExpenseRow> Rows);
