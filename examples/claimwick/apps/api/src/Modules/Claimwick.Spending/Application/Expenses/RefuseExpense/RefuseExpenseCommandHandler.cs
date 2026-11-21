using Claimwick.Spending.Domain.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.RefuseExpense;

/// <implements>use-case/spending.refuse-expense</implements>
internal sealed class RefuseExpenseCommandHandler : ICommandHandler<RefuseExpenseCommand, RefuseExpenseCommandResult>
{
    private readonly IExpenseRepository _expenses;

    public RefuseExpenseCommandHandler(IExpenseRepository expenses)
    {
        _expenses = expenses;
    }

    public async Task<RefuseExpenseCommandResult> HandleAsync(
        RefuseExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Refuse(command.ApproverId, command.Reason, command.HandledAt);
        _expenses.Store(expense);
        return new RefuseExpenseCommandResult(expense.Id);
    }
}
