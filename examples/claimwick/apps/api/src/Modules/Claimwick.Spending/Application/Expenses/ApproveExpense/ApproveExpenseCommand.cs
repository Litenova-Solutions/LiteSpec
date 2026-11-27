using Claimwick.Spending.Contracts.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ApproveExpense;

internal sealed record ApproveExpenseCommand(ExpenseId ExpenseId, Guid ApproverId, DateTimeOffset HandledAt) : ICommand<ApproveExpenseCommandResult>;
