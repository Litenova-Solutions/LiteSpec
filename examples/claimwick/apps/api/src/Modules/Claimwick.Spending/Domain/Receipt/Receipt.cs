using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Receipt;

/// <summary>Proof of payment attached to an expense.</summary>
/// <implements>aggregate/spending.receipt</implements>
internal sealed class Receipt : AggregateRoot<Guid>
{
    private Receipt(Guid id, Guid expenseId, DateTimeOffset attachedAt)
    {
        Id = id;
        State = new ReceiptAttachedState(expenseId, attachedAt);
    }

    /// <summary>The current lifecycle state.</summary>
    public ReceiptState State { get; private set; }

    /// <summary>Attaches one receipt to an expense.</summary>
    public static Receipt Attach(Guid id, Guid expenseId, DateTimeOffset attachedAt) =>
        new(id, expenseId, attachedAt);
}
