using LiteBus.Queries.Abstractions;
using Marten;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;

/// <summary>
/// Reads one check-in through the read session, projecting its stored form.
/// Takes no repository and stages nothing.
/// </summary>
/// <implements>use-case/health.read-check-in</implements>
internal sealed class ReadCheckInQueryHandler : IQueryHandler<ReadCheckInQuery, ReadCheckInResult?>
{
    private readonly IQuerySession _queries;

    /// <summary>Creates the handler over the read session.</summary>
    /// <param name="queries">The read session.</param>
    public ReadCheckInQueryHandler(IQuerySession queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    /// <inheritdoc />
    public async Task<ReadCheckInResult?> HandleAsync(ReadCheckInQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var checkIn = await _queries.LoadAsync<HealthCheck>(query.CheckInId, cancellationToken).ConfigureAwait(false);
        if (checkIn is null)
        {
            return null;
        }

        return new ReadCheckInResult(checkIn.Id, checkIn.RecordedAt, checkIn.Note, checkIn.State.Code);
    }
}
