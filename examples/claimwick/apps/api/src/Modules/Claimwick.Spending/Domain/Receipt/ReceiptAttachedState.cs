namespace Claimwick.Spending.Domain.Receipt;

/// <summary>The receipt is attached to an expense.</summary>
internal sealed record ReceiptAttachedState(Guid ExpenseId, DateTimeOffset AttachedAt) : ReceiptState;
