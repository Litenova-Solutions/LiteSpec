namespace Claimwick.Spending.Receipts.UseCases.AttachReceipt;

public sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
