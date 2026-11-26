using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

internal sealed record ReimburseExpenseCommand(ExpenseId ExpenseId, DateTimeOffset HandledAt);
