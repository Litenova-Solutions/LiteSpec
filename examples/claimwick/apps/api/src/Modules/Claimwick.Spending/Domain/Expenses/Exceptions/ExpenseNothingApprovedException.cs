using Claimwick.Kernel.Domain;

namespace Claimwick.Spending.Domain.Expenses.Exceptions;

/// <summary>No approved expense awaits export.</summary>
/// <implements>rule/expense.export-carries-approved</implements>
internal sealed class ExpenseNothingApprovedException : DomainException
{
    /// <summary>Creates the rejection.</summary>
    public ExpenseNothingApprovedException()
        : base("No approved expense awaits export.")
    {
    }

    /// <summary>The stable failure code.</summary>
    public override string Code => "spending.nothing-approved";
}
