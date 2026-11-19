using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

internal sealed record ApproveExpenseCommand(ExpenseId ExpenseId, Guid ApproverId, DateTimeOffset HandledAt);
