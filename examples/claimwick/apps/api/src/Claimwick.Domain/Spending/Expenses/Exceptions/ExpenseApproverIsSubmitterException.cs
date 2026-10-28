namespace Claimwick.Domain.Spending.Expenses.Exceptions;

/// <summary>The approver submitted the expense.</summary>
/// <implements>rule/expense.approver-is-not-submitter</implements>
public sealed class ExpenseApproverIsSubmitterException()
    : DomainException("The approver submitted the expense.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.own-expense";
}
