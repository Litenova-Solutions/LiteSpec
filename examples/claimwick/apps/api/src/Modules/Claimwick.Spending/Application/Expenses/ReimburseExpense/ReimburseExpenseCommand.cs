using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

internal sealed record ReimburseExpenseCommand(ExpenseId ExpenseId, DateTimeOffset HandledAt) : ICommand<ReimburseExpenseCommandResult>;
