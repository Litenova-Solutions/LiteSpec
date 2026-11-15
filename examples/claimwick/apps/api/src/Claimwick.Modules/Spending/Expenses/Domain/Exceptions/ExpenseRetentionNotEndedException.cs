using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Exceptions;

/// <summary>The retention of the expense has not ended.</summary>
/// <implements>rule/expense.erased-after-retention</implements>
public sealed class ExpenseRetentionNotEndedException()
    : DomainException("The retention of the expense has not ended.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.retention-not-ended";
}
