namespace Claimwick.Spending.Application.Receipts.AttachReceipt;

internal sealed record AttachReceiptCommand(Guid ReceiptId, Guid ExpenseId);
