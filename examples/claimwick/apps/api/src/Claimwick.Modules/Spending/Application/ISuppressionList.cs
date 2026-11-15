namespace Claimwick.Spending.Application;

/// <summary>The suppression list of the spending module: the managers who get no approval reminder.</summary>
public interface ISuppressionList
{
    /// <summary>Whether the manager is named in the suppression list.</summary>
    Task<bool> IsSuppressedAsync(Guid managerId, CancellationToken cancellationToken);

    /// <summary>Adds the manager to the suppression list with its reason.</summary>
    Task SuppressAsync(Guid managerId, string reason, CancellationToken cancellationToken);

    /// <summary>Removes the manager from the suppression list.</summary>
    Task ReleaseAsync(Guid managerId, CancellationToken cancellationToken);
}
