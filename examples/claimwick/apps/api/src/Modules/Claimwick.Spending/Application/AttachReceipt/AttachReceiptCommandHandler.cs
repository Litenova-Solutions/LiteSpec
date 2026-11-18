using Claimwick.Spending.Domain.Expense;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.AttachReceipt;

/// <implements>use-case/spending.attach-receipt</implements>
internal sealed class AttachReceiptCommandHandler : ICommandHandler<AttachReceiptCommand, AttachReceiptCommandResult>
{
    private readonly IExpenseRepository _expenses;

    public AttachReceiptCommandHandler(IExpenseRepository expenses)
    {
        _expenses = expenses;
    }

    public async Task<AttachReceiptCommandResult> HandleAsync(
        AttachReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.AttachReceipt(command.ReceiptId);
        _expenses.Store(expense);
        return new AttachReceiptCommandResult(expense.Id, command.ReceiptId);
    }
}
