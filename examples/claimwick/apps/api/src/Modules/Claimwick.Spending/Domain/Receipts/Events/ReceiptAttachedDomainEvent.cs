using Claimwick.Kernel.Domain;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Receipts.Events;

/// <summary>A receipt was attached.</summary>
/// <implements>domain-event/receipt.attached</implements>
internal sealed record ReceiptAttachedDomainEvent(Guid ReceiptId, ExpenseId ExpenseId) : IDomainEvent;
