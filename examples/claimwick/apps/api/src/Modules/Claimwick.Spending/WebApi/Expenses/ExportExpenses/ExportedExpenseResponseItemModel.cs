namespace Claimwick.Spending.WebApi.Expenses.ExportExpenses;

/// <summary>One approved expense row: its identity, amount, currency, approver and approval time.</summary>
internal sealed record ExportedExpenseResponseItemModel(Guid ExpenseId, decimal Amount, string Currency, Guid Approver, DateTimeOffset ApprovedAt);
