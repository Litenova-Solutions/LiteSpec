using Claimwick.Kernel.Domain;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

/// <summary>The expense is already decided.</summary>
/// <implements>rule/expense.decided-from-submitted</implements>
internal sealed class ExpenseNotSubmittedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseNotSubmittedException()
        : base("The expense is already decided.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.not-submitted";
}
