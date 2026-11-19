using Claimwick.Kernel;
using Claimwick.Spending.Contracts.Expenses;

namespace Claimwick.Spending.Domain.Receipts;

/// <summary>Proof of payment attached to an expense.</summary>
/// <implements>aggregate/spending.receipt</implements>
internal sealed class Receipt : AggregateRoot<Guid>
{
    private Receipt(Guid id, ExpenseId expenseId, DateTimeOffset attachedAt)
    {
        Id = id;
        State = new ReceiptAttachedState(expenseId, attachedAt);
    }

    /// <summary>The current lifecycle state.</summary>
    public ReceiptState State { get; private set; }

    /// <summary>Attaches one receipt to an expense.</summary>
    public static Receipt Attach(Guid id, ExpenseId expenseId, DateTimeOffset attachedAt) =>
        new(id, expenseId, attachedAt);
}
