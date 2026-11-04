namespace Claimwick.Spending.Receipts.AttachReceipt;

public sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
