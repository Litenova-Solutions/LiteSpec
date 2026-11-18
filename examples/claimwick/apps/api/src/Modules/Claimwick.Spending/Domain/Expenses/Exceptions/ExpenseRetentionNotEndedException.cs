using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

/// <summary>The retention of the expense has not ended.</summary>
/// <implements>rule/expense.erased-after-retention</implements>
internal sealed class ExpenseRetentionNotEndedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseRetentionNotEndedException()
        : base("The retention of the expense has not ended.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.retention-not-ended";
}
