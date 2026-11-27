using LiteBus.Queries.Abstractions;

namespace __NAMESPACE__.Health.Application.HealthChecks.ListCheckIns;

/// <summary>
/// Lists the stored check-ins, newest first, bounded to the latest hundred.
/// </summary>
internal sealed record ListCheckInsQuery : IQuery<ListCheckInsResult>;
