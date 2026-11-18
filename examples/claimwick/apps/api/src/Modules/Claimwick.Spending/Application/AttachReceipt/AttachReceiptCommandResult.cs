namespace Claimwick.Spending.Application.AttachReceipt;

internal sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
