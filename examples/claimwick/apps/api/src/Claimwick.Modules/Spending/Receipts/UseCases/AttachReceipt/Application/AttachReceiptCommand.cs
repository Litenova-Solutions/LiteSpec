namespace Claimwick.Spending.Receipts.UseCases.AttachReceipt.Application;

public sealed record AttachReceiptCommand(Guid ReceiptId, Guid ExpenseId);
