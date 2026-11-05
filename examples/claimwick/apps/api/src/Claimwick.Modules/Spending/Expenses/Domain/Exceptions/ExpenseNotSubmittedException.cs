using Claimwick.Domain;

namespace Claimwick.Spending.Expenses.Domain.Exceptions;

/// <summary>The expense is already decided.</summary>
/// <implements>rule/expense.decided-from-submitted</implements>
public sealed class ExpenseNotSubmittedException()
    : DomainException("The expense is already decided.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.not-submitted";
}
