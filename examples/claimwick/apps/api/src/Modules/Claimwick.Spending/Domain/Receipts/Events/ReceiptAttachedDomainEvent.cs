using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Receipts.Events;

/// <summary>A receipt was attached.</summary>
/// <implements>domain-event/receipt.attached</implements>
internal sealed record ReceiptAttachedDomainEvent(Guid ReceiptId, Guid ExpenseId) : IDomainEvent;
