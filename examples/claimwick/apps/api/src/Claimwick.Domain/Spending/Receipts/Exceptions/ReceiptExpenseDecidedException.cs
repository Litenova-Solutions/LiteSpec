namespace Claimwick.Domain.Spending.Receipts.Exceptions;

/// <summary>The expense is already decided.</summary>
/// <implements>rule/receipt.attaches-before-decision</implements>
public sealed class ReceiptExpenseDecidedException()
    : DomainException("The expense is already decided.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.expense-decided";
}
