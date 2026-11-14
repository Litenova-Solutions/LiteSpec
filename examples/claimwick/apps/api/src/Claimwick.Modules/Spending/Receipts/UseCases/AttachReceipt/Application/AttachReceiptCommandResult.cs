namespace Claimwick.Spending.Receipts.UseCases.AttachReceipt.Application;

public sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
