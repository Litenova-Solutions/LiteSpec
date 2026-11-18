namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

internal sealed record RefuseExpenseCommand(Guid ExpenseId, Guid ApproverId, string Reason, DateTimeOffset HandledAt);
