namespace Claimwick.Spending.UseCases.RefuseExpense.Application;

internal sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason, DateTimeOffset HandledAt);
