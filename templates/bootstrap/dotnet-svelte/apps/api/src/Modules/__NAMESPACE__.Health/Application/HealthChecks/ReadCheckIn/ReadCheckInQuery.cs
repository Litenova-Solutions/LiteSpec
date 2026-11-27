using LiteBus.Queries.Abstractions;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.ReadCheckIn;

/// <summary>
/// Reads one check-in by its identifier.
/// </summary>
/// <param name="CheckInId">The check-in identifier.</param>
internal sealed record ReadCheckInQuery(CheckInId CheckInId) : IQuery<ReadCheckInResult?>;
