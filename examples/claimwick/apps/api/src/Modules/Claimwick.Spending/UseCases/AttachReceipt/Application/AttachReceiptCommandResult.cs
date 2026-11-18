namespace Claimwick.Spending.UseCases.AttachReceipt.Application;

internal sealed record AttachReceiptCommandResult(Guid ExpenseId, Guid ReceiptId);
