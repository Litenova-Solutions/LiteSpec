using Claimwick.Application.Abstractions;
using Claimwick.Domain.Spending.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Application.Spending.Expenses.ApproveExpense;

/// <implements>use-case/spending.approve-expense</implements>
internal sealed class ApproveExpenseCommandHandler(
    IExpenseRepository expenses,
    IClock clock) : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
{
    public async Task<ApproveExpenseCommandResult> HandleAsync(
        ApproveExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Approve(command.ApproverId, clock.UtcNow);
        expenses.Store(expense);
        return new ApproveExpenseCommandResult(expense.Id);
    }
}
