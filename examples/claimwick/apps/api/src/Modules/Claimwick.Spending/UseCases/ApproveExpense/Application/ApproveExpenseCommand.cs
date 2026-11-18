namespace Claimwick.Spending.UseCases.ApproveExpense.Application;

internal sealed record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverId, DateTimeOffset HandledAt);
