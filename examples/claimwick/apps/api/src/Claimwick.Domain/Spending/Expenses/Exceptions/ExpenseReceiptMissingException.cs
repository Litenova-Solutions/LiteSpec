using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Expenses.Exceptions;

/// <summary>An expense above the receipt threshold carries no receipt.</summary>
/// <implements>rule/expense.receipt-required-above-threshold</implements>
public sealed class ExpenseReceiptMissingException()
    : DomainException("An expense above the receipt threshold carries no receipt.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.receipt-missing";
}
