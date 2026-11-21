using Claimwick.Spending.Domain.Expenses;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Expenses.ReimburseExpense;

/// <implements>use-case/spending.reimburse-expense</implements>
internal sealed class ReimburseExpenseCommandHandler : ICommandHandler<ReimburseExpenseCommand, ReimburseExpenseCommandResult>
{
    private readonly IExpenseRepository _expenses;

    public ReimburseExpenseCommandHandler(IExpenseRepository expenses)
    {
        _expenses = expenses;
    }

    public async Task<ReimburseExpenseCommandResult> HandleAsync(
        ReimburseExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Reimburse(command.HandledAt);
        _expenses.Store(expense);
        return new ReimburseExpenseCommandResult(expense.Id);
    }
}
