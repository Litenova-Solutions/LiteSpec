namespace Claimwick.Reporting.ReadModels.ReimbursedExpenses;

/// <summary>Reads the reimbursed expenses that the read model keeps.</summary>
/// <implements>read-model/reporting.reimbursed-expense</implements>
public interface IReimbursedExpenseReadModel
{
    /// <summary>Lists the reimbursed expenses, newest first.</summary>
    Task<IReadOnlyList<ReimbursedExpense>> ListAsync(CancellationToken cancellationToken);
}
