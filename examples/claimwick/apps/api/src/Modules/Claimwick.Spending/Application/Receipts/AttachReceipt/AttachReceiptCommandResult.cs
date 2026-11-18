namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

internal sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
