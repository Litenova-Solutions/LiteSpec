using LiteBus.Queries.Abstractions;
using Marten;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// Lists the stored check-ins through the read session, newest first, bounded
/// to the latest hundred. Takes no repository and stages nothing.
/// </summary>
/// <implements>use-case/health.list-check-ins</implements>
internal sealed class ListCheckInsQueryHandler : IQueryHandler<ListCheckInsQuery, ListCheckInsResult>
{
    /// <summary>The most check-ins one list carries.</summary>
    private const int MaxCheckIns = 100;

    private readonly IQuerySession _queries;

    /// <summary>Creates the handler over the read session.</summary>
    /// <param name="queries">The read session.</param>
    public ListCheckInsQueryHandler(IQuerySession queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    /// <inheritdoc />
    public async Task<ListCheckInsResult> HandleAsync(ListCheckInsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var stored = await _queries
            .Query<HealthCheck>()
            .OrderByDescending(checkIn => checkIn.RecordedAt)
            .Take(MaxCheckIns)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var rows = stored
            .Select(checkIn => new CheckInRow(checkIn.Id, checkIn.RecordedAt, checkIn.Note, checkIn.State.Code))
            .ToList();
        return new ListCheckInsResult(rows);
    }
}
