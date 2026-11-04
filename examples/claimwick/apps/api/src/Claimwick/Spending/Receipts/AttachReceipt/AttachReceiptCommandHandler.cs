using Claimwick.Spending.Expenses.Domain;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Receipts.AttachReceipt;

/// <implements>use-case/spending.attach-receipt</implements>
internal sealed class AttachReceiptCommandHandler(
    IExpenseRepository expenses) : ICommandHandler<AttachReceiptCommand, AttachReceiptCommandResult>
{
    public async Task<AttachReceiptCommandResult> HandleAsync(
        AttachReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.AttachReceipt(command.ReceiptId);
        expenses.Store(expense);
        return new AttachReceiptCommandResult(expense.Id, command.ReceiptId);
    }
}
