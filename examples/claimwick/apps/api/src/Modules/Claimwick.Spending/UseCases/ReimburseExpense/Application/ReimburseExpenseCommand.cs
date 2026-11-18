namespace Claimwick.Spending.UseCases.ReimburseExpense.Application;

internal sealed record ReimburseExpenseCommand(Guid ExpenseId, DateTimeOffset HandledAt);
