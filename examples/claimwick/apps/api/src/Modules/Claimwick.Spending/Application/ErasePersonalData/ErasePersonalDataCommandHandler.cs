using Claimwick.Spending.Domain.Expense;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.ErasePersonalData;

/// <implements>use-case/spending.erase-personal-data</implements>
internal sealed class ErasePersonalDataCommandHandler : ICommandHandler<ErasePersonalDataCommand, ErasePersonalDataCommandResult>
{
    private readonly IExpenseRepository _expenses;

    public ErasePersonalDataCommandHandler(IExpenseRepository expenses)
    {
        _expenses = expenses;
    }

    public async Task<ErasePersonalDataCommandResult> HandleAsync(
        ErasePersonalDataCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.Erase();
        _expenses.Delete(expense.Id);
        return new ErasePersonalDataCommandResult(expense.Id);
    }
}
