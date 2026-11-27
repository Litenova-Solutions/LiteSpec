using Marten;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Infrastructure.Persistence.HealthChecks;

/// <summary>
/// Loads and stages check-ins through the scoped session. The aggregate itself
/// is the stored document; there is no mapping.
/// </summary>
/// <implements>aggregate/health.health-check</implements>
internal sealed class MartenHealthCheckRepository : IHealthCheckRepository
{
    private readonly IDocumentSession _sessions;

    /// <summary>Creates the repository over the scoped session.</summary>
    /// <param name="sessions">The scoped session.</param>
    public MartenHealthCheckRepository(IDocumentSession sessions)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <inheritdoc />
    public async Task<HealthCheck?> GetByIdAsync(CheckInId id, CancellationToken cancellationToken)
    {
        return await _sessions.LoadAsync<HealthCheck>(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Store(HealthCheck checkIn)
    {
        ArgumentNullException.ThrowIfNull(checkIn);
        _sessions.Store(checkIn);
    }
}
