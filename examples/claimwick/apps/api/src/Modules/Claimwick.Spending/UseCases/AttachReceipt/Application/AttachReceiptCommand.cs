namespace Claimwick.Spending.UseCases.AttachReceipt.Application;

internal sealed record AttachReceiptCommand(Guid ReceiptId, Guid ExpenseId);
