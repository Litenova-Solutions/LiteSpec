namespace Claimwick.Spending.Application.RefuseExpense;

internal sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason, DateTimeOffset HandledAt);
