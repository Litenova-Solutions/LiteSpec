namespace Claimwick.Spending.Domain.Policy;

/// <summary>Loads and stages the company spending policy.</summary>
internal interface IPolicyRepository
{
    /// <summary>Loads the active policy.</summary>
    Task<Policy> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>Loads the active policy under a shared lock, so a guard on its state holds until the command commits.</summary>
    Task<Policy> GetForGuardAsync(CancellationToken cancellationToken);

    /// <summary>Stages a changed policy for the pipeline's one commit.</summary>
    void Store(Policy policy);
}
