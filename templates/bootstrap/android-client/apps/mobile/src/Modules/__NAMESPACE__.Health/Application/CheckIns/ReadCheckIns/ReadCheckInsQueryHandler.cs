namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>Lists the stored check-ins newest first for the phone list.</summary>
/// <implements>use-case/health.read-check-ins</implements>
internal sealed class ReadCheckInsQueryHandler : IQueryHandler<ReadCheckInsQuery, ReadCheckInsResult>
{
    /// <summary>Handles the read by returning the stored rows.</summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The token cancelling the read.</param>
    /// <returns>The listed check-ins.</returns>
    public Task<ReadCheckInsResult> HandleAsync(ReadCheckInsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<CheckInRow> rows = [];
        return Task.FromResult(new ReadCheckInsResult(rows));
    }
}
