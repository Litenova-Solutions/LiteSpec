using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

/// <summary>The expense is already reimbursed.</summary>
/// <implements>rule/expense.reimbursed-once</implements>
internal sealed class ExpenseAlreadyReimbursedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseAlreadyReimbursedException()
        : base("The expense is already reimbursed.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.already-reimbursed";
}
