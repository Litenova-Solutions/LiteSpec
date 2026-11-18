using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Receipt.Exceptions;

/// <summary>The expense is already decided.</summary>
/// <implements>rule/receipt.attaches-before-decision</implements>
internal sealed class ReceiptExpenseDecidedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ReceiptExpenseDecidedException()
        : base("The expense is already decided.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.expense-decided";
}
