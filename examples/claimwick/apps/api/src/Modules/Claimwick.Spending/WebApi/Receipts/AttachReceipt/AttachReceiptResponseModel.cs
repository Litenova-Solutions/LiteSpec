namespace Claimwick.Spending.WebApi.Receipts.AttachReceipt;

/// <summary>The answer to an attach request: the expense, the receipt, and the stored bytes.</summary>
internal sealed record AttachReceiptResponseModel(Guid ExpenseId, Guid ReceiptId, long SizeBytes);
