using Claimwick.Domain.Shared;

namespace Claimwick.Domain.Spending.Expenses.Exceptions;

/// <summary>The expense is already reimbursed.</summary>
/// <implements>rule/expense.reimbursed-once</implements>
public sealed class ExpenseAlreadyReimbursedException()
    : DomainException("The expense is already reimbursed.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.already-reimbursed";
}
