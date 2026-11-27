namespace __NAMESPACE__.Health.Domain.HealthChecks;

/// <summary>
/// Loads and stages check-ins. Load and stage only, no queries beyond the load
/// the record use case needs; reads project through the read session.
/// </summary>
internal interface IHealthCheckRepository
{
    /// <summary>Loads a check-in by id, or null when the check-in is unknown.</summary>
    /// <param name="id">The check-in identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The check-in, or null when the check-in is unknown.</returns>
    Task<HealthCheck?> GetByIdAsync(CheckInId id, CancellationToken cancellationToken);

    /// <summary>Stages a check-in into the current commit.</summary>
    /// <param name="checkIn">The check-in.</param>
    void Store(HealthCheck checkIn);
}
