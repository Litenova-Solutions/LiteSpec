using Claimwick.Spending.Domain.Expenses;
using Claimwick.Spending.Domain.Receipts;
using LiteBus.Commands.Abstractions;

namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

/// <implements>use-case/spending.attach-receipt</implements>
internal sealed class AttachReceiptCommandHandler : ICommandHandler<AttachReceiptCommand, AttachReceiptCommandResult>
{
    private readonly IExpenseRepository _expenses;
    private readonly IReceiptRepository _receipts;

    public AttachReceiptCommandHandler(IExpenseRepository expenses, IReceiptRepository receipts)
    {
        _expenses = expenses;
        _receipts = receipts;
    }

    public async Task<AttachReceiptCommandResult> HandleAsync(
        AttachReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetAsync(command.ExpenseId, cancellationToken);
        expense.AttachReceipt(command.ReceiptId);
        _expenses.Store(expense);
        _receipts.Store(Receipt.Attach(command.ReceiptId, command.ExpenseId, command.AttachedAt));
        return new AttachReceiptCommandResult(expense.Id, command.ReceiptId, command.Scans.Sum(scan => scan.Length));
    }
}
