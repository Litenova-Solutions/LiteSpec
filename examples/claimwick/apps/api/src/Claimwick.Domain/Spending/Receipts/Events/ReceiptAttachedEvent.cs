using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Receipts.Events;

/// <summary>A receipt was attached.</summary>
/// <implements>event/receipt.attached</implements>
public sealed record ReceiptAttachedEvent(Guid ReceiptId, Guid ExpenseId) : IDomainEvent;
