using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Receipt.Events;

/// <summary>A receipt was attached.</summary>
/// <implements>domain-event/receipt.attached</implements>
internal sealed record ReceiptAttachedEvent(Guid ReceiptId, Guid ExpenseId) : IDomainEvent;
