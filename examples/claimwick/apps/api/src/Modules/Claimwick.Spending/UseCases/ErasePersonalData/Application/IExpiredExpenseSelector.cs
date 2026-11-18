namespace Claimwick.Spending.UseCases.ErasePersonalData.Application;

/// <summary>Selects the decided expenses whose retention ended, so the erase job can erase each one.</summary>
internal interface IExpiredExpenseSelector
{
    /// <summary>Returns the identities of the decided expenses past their retention.</summary>
    Task<IReadOnlyList<Guid>> SelectAsync(CancellationToken cancellationToken);
}
