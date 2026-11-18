namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

internal sealed record ReimburseExpenseCommand(Guid ExpenseId, DateTimeOffset HandledAt);
