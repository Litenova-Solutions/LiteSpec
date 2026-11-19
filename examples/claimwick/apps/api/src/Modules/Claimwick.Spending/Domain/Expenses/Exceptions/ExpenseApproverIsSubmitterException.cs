using Claimwick.Kernel.Domain;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

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
