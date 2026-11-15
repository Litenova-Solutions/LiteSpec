namespace Claimwick.Spending.Receipts.Domain;

/// <summary>The receipt is attached to an expense.</summary>
public sealed record ReceiptAttachedState(Guid ExpenseId, DateTimeOffset AttachedAt) : ReceiptState;
