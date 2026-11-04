using Claimwick.Application;
using Claimwick.Spending.Expenses.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Expenses.ReimburseExpense;

/// <implements>use-case/spending.reimburse-expense</implements>
internal sealed class ReimburseExpenseCommandHandler(
    IExpenseRepository expenses,
    IClock clock) : ICommandHandler<ReimburseExpenseCommand, ReimburseExpenseCommandResult>
{
    public async Task<ReimburseExpenseCommandResult> HandleAsync(
        ReimburseExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Reimburse(clock.UtcNow);
        expenses.Store(expense);
        return new ReimburseExpenseCommandResult(expense.Id);
    }
}
