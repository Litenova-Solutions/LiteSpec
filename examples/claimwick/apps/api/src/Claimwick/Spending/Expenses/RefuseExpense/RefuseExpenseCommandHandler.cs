using Claimwick.Application;
using Claimwick.Spending.Expenses.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Expenses.RefuseExpense;

/// <implements>use-case/spending.refuse-expense</implements>
internal sealed class RefuseExpenseCommandHandler(
    IExpenseRepository expenses,
    IClock clock) : ICommandHandler<RefuseExpenseCommand, RefuseExpenseCommandResult>
{
    public async Task<RefuseExpenseCommandResult> HandleAsync(
        RefuseExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Refuse(command.ApproverId, command.Reason, clock.UtcNow);
        expenses.Store(expense);
        return new RefuseExpenseCommandResult(expense.Id);
    }
}
