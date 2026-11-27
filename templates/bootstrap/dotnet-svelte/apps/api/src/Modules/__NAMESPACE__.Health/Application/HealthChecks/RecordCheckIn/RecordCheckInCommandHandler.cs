using LiteBus.Commands.Abstractions;
using __NAMESPACE__.Health.Domain.HealthChecks;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Records the check-in: loads nothing, calls record on a new aggregate,
/// stages it, and returns the identifier with its state.
/// </summary>
/// <implements>use-case/health.record-check-in</implements>
internal sealed class RecordCheckInCommandHandler : ICommandHandler<RecordCheckInCommand, RecordCheckInResult>
{
    private readonly IHealthCheckRepository _healthCheckRepository;

    /// <summary>Creates the handler over the check-in repository.</summary>
    /// <param name="healthCheckRepository">The check-in repository.</param>
    public RecordCheckInCommandHandler(IHealthCheckRepository healthCheckRepository)
    {
        _healthCheckRepository = healthCheckRepository ?? throw new ArgumentNullException(nameof(healthCheckRepository));
    }

    /// <inheritdoc />
    public Task<RecordCheckInResult> HandleAsync(RecordCheckInCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var checkIn = HealthCheck.Record(command.CheckInId, command.RecordedAt, command.Note);
        _healthCheckRepository.Store(checkIn);
        return Task.FromResult(new RecordCheckInResult(checkIn.Id, checkIn.State.Code));
    }
}
