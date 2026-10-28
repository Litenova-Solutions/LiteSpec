namespace Claimwick.Domain.Spending.Expenses.Exceptions;

/// <summary>No approved expense awaits export.</summary>
/// <implements>rule/expense.export-carries-approved</implements>
public sealed class ExpenseNothingApprovedException()
    : DomainException("No approved expense awaits export.")
{
    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.nothing-approved";
}
