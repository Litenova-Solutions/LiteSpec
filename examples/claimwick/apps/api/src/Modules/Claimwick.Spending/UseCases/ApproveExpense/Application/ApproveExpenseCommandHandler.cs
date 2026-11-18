using Claimwick.Spending.Domain.Expense;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.UseCases.ApproveExpense.Application;

/// <implements>use-case/spending.approve-expense</implements>
internal sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
{
    private readonly IExpenseRepository _expenses;

    public ApproveExpenseCommandHandler(IExpenseRepository expenses)
    {
        _expenses = expenses;
    }

    public async Task<ApproveExpenseCommandResult> HandleAsync(
        ApproveExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Approve(command.ApproverId, command.HandledAt);
        _expenses.Store(expense);
        return new ApproveExpenseCommandResult(expense.Id);
    }
}
