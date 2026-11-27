using __NAMESPACE__.Kernel.Domain;

namespace __NAMESPACE__.Health.Domain.HealthChecks.Events;

/// <summary>
/// A check-in was recorded. The sample answers it with no reaction yet; the
/// outbox decision drops it, and a later reaction subscribes to this fact.
/// </summary>
/// <param name="CheckInId">The recorded check-in.</param>
/// <param name="RecordedAt">The moment the check-in was recorded.</param>
/// <implements>domain-event/health-check.check-in-recorded</implements>
internal sealed partial record CheckInRecordedDomainEvent(CheckInId CheckInId, DateTimeOffset RecordedAt) : IDomainEvent;
