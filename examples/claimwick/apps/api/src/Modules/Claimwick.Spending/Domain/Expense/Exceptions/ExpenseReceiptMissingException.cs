using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expense.Exceptions;

/// <summary>An expense above the receipt threshold carries no receipt.</summary>
/// <implements>rule/expense.receipt-required-above-threshold</implements>
internal sealed class ExpenseReceiptMissingException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseReceiptMissingException()
        : base("An expense above the receipt threshold carries no receipt.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.receipt-missing";
}
