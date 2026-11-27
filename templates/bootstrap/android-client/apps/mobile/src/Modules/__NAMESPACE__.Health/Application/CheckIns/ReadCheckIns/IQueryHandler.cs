namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>The query handler contract for the Health read.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public interface IQueryHandler<TQuery, TResult>
{
    /// <summary>Handles the query.</summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">The token cancelling the read.</param>
    /// <returns>The result of the query.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
