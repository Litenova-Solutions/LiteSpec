namespace Claimwick.Reporting.Views.ReimbursedExpenses;

/// <summary>Reads the reimbursed expenses that the view keeps.</summary>
/// <implements>view/reporting.reimbursed-expense</implements>
public interface IReimbursedExpenseView
{
    /// <summary>Lists the reimbursed expenses, newest first.</summary>
    Task<IReadOnlyList<ReimbursedExpense>> ListAsync(CancellationToken cancellationToken);
}
