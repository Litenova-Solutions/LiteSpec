using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.ExportExpenses;

/// <summary>One approved expense row of the export file.</summary>
internal sealed record ExportedExpenseRow(ExpenseId ExpenseId, decimal Amount, string Currency, Guid Approver, DateTimeOffset ApprovedAt);
