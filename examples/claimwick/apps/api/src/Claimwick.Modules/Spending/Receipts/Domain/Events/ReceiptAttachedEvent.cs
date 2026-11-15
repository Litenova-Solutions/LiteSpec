using Claimwick.Domain;

namespace Claimwick.Spending.Receipts.Domain.Events;

/// <summary>A receipt was attached.</summary>
/// <implements>domain-event/receipt.attached</implements>
public sealed record ReceiptAttachedEvent(Guid ReceiptId, Guid ExpenseId) : IDomainEvent;
