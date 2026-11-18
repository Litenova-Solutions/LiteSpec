namespace Claimwick.Spending.Application.AttachReceipt;

internal sealed record AttachReceiptCommand(Guid ReceiptId, Guid ExpenseId);
