using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>
///     The domain event outcome checks: each domain event is consumed by a reaction or a
///     translating event, or states that nothing consumes it, but never both and never neither.
/// </summary>
/// <follows>litespec/core/outbox#decide-every-domain-event-outcome</follows>
internal static class DomainEventOutcomeChecks
{
    /// <summary>Reports each domain event with no decided outcome, and each ignored event a consumer names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var consumers = DomainEventConsumers(model);

        foreach (var domainEvent in model.Records.Where(record => record.Type == RecordTypes.DomainEvent && !record.Derived))
        {
            var ignored = domainEvent.Flag(RecordFields.Ignored);
            var hasConsumer = consumers.TryGetValue(domainEvent.Id, out var named) && named.Count > 0;

            if (!hasConsumer && !ignored)
            {
                findings.Add(new Finding(
                    "domain-event-outcome-missing",
                    $"Domain event '{domainEvent.Id}' is answered by no reaction and translated by no module or integration event. State `ignored: true` on the event, or consume it from a reaction or a translating event.",
                    domainEvent.File,
                    domainEvent.Line,
                    SubjectOf(domainEvent)));
            }
            else if (hasConsumer && ignored)
            {
                findings.Add(new Finding(
                    "domain-event-outcome-drift",
                    $"Domain event '{domainEvent.Id}' states `ignored: true` but is consumed by {string.Join(", ", named!.Select(id => $"'{id}'"))}. Remove `ignored` from the event, or remove the consumer.",
                    domainEvent.File,
                    domainEvent.Line,
                    SubjectOf(domainEvent)));
            }
        }
    }
}
