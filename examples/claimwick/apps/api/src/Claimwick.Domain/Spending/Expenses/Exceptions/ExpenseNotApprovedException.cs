using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Expenses.Exceptions;

/// <summary>The expense is not approved.</summary>
/// <implements>rule/expense.approved-before-reimbursement</implements>
public sealed class ExpenseNotApprovedException()
    : DomainException("The expense is not approved.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.not-approved";
}
