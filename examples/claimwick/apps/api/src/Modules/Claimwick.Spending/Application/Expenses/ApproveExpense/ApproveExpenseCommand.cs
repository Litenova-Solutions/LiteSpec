namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

internal sealed record ApproveExpenseCommand(Guid ExpenseId, Guid ApproverId, DateTimeOffset HandledAt);
