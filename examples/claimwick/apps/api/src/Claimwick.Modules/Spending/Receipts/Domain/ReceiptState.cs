namespace Claimwick.Spending.Receipts.Domain;

/// <summary>One lifecycle state of a receipt.</summary>
public abstract record ReceiptState;

/// <summary>The receipt is attached to an expense.</summary>
public sealed record ReceiptAttachedState(Guid ExpenseId, DateTimeOffset AttachedAt) : ReceiptState;
