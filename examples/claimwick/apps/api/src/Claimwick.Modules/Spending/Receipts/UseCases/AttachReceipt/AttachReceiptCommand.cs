namespace Claimwick.Spending.Receipts.UseCases.AttachReceipt;

public sealed record AttachReceiptCommand(Guid ReceiptId, Guid ExpenseId);
