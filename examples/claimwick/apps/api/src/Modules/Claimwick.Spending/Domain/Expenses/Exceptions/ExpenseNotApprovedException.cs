using Claimwick.Kernel.Domain;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

/// <summary>The expense is not approved.</summary>
/// <implements>rule/expense.approved-before-reimbursement</implements>
internal sealed class ExpenseNotApprovedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseNotApprovedException()
        : base("The expense is not approved.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => SpendingRefusalCodes.NotApproved;
}
