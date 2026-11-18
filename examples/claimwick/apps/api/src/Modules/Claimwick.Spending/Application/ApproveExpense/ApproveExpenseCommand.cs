namespace Claimwick.Spending.Application.ApproveExpense;

internal sealed record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverId, DateTimeOffset HandledAt);
