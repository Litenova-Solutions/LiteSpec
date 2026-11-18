using Claimwick.Kernel;

namespace Claimwick.Spending.Domain.Expense.Exceptions;

/// <summary>The approver submitted the expense.</summary>
/// <implements>rule/expense.approver-is-not-submitter</implements>
internal sealed class ExpenseApproverIsSubmitterException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseApproverIsSubmitterException()
        : base("The approver submitted the expense.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.own-expense";
}
