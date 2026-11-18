namespace Claimwick.Spending.Application.ReimburseExpense;

internal sealed record ReimburseExpenseCommand(Guid ExpenseId, DateTimeOffset HandledAt);
