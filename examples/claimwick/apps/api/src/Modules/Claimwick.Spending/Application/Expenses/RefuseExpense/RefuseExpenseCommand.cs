using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

internal sealed record RefuseExpenseCommand(ExpenseId ExpenseId, Guid ApproverId, string Reason, DateTimeOffset HandledAt);
