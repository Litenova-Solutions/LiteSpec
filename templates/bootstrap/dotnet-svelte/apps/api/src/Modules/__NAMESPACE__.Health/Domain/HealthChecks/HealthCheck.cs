using __NAMESPACE__.Health.Domain.HealthChecks.Parts;
using __NAMESPACE__.Kernel.Domain;

namespace __NAMESPACE__.Health.Domain.HealthChecks;

/// <summary>
/// One recorded check-in: the moment the sample recorded, with its note and lifecycle.
/// </summary>
/// <implements>aggregate/health.health-check</implements>
internal sealed class HealthCheck : AggregateRoot<CheckInId>
{
    private HealthCheck(CheckInId id, DateTimeOffset recordedAt, string? note)
        : base(id)
    {
        RecordedAt = recordedAt;
        Note = note;
        State = new RecordedState();
    }

    /// <summary>The moment the check-in was recorded.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>The note recorded with the check-in, if any.</summary>
    public string? Note { get; private set; }

    /// <summary>The lifecycle of the check-in.</summary>
    public HealthCheckState State { get; private set; }

    /// <summary>Records a check-in with its time and note.</summary>
    /// <param name="id">The check-in identifier.</param>
    /// <param name="recordedAt">The moment the check-in was recorded.</param>
    /// <param name="note">The note recorded with the check-in, if any.</param>
    /// <exception cref="ArgumentOutOfRangeException">The recorded moment is unset.</exception>
    /// <implements>use-case/health.record-check-in</implements>
    public static HealthCheck Record(CheckInId id, DateTimeOffset recordedAt, string? note)
    {
        if (recordedAt == default)
        {
            throw new ArgumentOutOfRangeException(nameof(recordedAt), "The recorded moment is unset.");
        }

        var checkIn = new HealthCheck(id, recordedAt, note);
        checkIn.RaiseDomainEvent(new Events.CheckInRecordedDomainEvent(id, recordedAt));
        return checkIn;
    }
}
