using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

internal sealed record RefuseExpenseCommand(ExpenseId ExpenseId, Guid ApproverId, string Reason, DateTimeOffset HandledAt) : ICommand<RefuseExpenseCommandResult>;
