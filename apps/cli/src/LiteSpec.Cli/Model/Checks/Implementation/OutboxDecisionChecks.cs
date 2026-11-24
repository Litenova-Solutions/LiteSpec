using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The outbox decision checks: each domain event type carries one outbox decision in code,
///     and the decision matches the outcome the records state, in a project selecting the outbox extension.
/// </summary>
/// <follows>litespec/ext/outbox#decide-every-domain-event-outcome</follows>
internal static class OutboxDecisionChecks
{
    /// <summary>Reports each domain event type with no outbox decision, and each decision contradicting the records.</summary>
    /// <param name="model">The loaded project model that holds every record and each scanned outbox decision.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        if (!model.Config.Extensions.Contains("outbox", StringComparer.Ordinal))
        {
            return;
        }

        var translated = new HashSet<string>(StringComparer.Ordinal);
        var ignored = new HashSet<string>(StringComparer.Ordinal);
        var decidedAt = new Dictionary<string, (string File, int Line)>(StringComparer.Ordinal);

        foreach (var decision in model.Code.OutboxDecisions)
        {
            decidedAt.TryAdd(decision.EventType, (decision.File, decision.Line));

            if (decision.Ignored)
            {
                ignored.Add(decision.EventType);
            }
            else
            {
                translated.Add(decision.EventType);
            }
        }

        var eventsByClass = EventsByClass(model);
        var consumers = DomainEventConsumers(model);

        foreach (var codeEvent in model.Code.Events)
        {
            if (codeEvent.EventId is null
                || !eventsByClass.TryGetValue(codeEvent.Class, out var eventId)
                || !string.Equals(eventId, codeEvent.EventId, StringComparison.Ordinal)
                || !model.ById.TryGetValue(eventId, out var record)
                || record.Type != RecordTypes.DomainEvent)
            {
                continue;
            }

            var isTranslated = translated.Contains(codeEvent.Class);
            var isIgnored = !isTranslated && ignored.Contains(codeEvent.Class);
            var hasConsumer = consumers.TryGetValue(eventId, out var named) && named.Count > 0;
            var outcome = DomainEventOutcomes.FromRecord(record.Flag(RecordFields.Ignored), hasConsumer);

            if (!isTranslated && !isIgnored)
            {
                findings.Add(new Finding(
                    "outbox-decision-missing",
                    $"Domain event type '{codeEvent.Class}' ({eventId}) carries no outbox decision. Decide it with `IgnoreDomainEvent<{codeEvent.Class}>()` or `TranslateDomainEvent<{codeEvent.Class}>()`.",
                    codeEvent.File,
                    codeEvent.Line,
                    eventId));
                continue;
            }

            var (file, line) = decidedAt[codeEvent.Class];

            if (isTranslated && outcome == DomainEventOutcome.Ignored)
            {
                findings.Add(new Finding(
                    "outbox-decision-drift",
                    $"Domain event type '{codeEvent.Class}' ({eventId}) is translated in code, but its record states `ignored: true`. Ignore the event in code, or remove `ignored` from the record.",
                    file,
                    line,
                    eventId));
            }
            else if (isIgnored && hasConsumer)
            {
                findings.Add(new Finding(
                    "outbox-decision-drift",
                    $"Domain event type '{codeEvent.Class}' ({eventId}) is ignored in code, but is consumed by {string.Join(", ", named!.Select(id => $"'{id}'"))} in the records. Translate the event in code, or remove the consumer.",
                    file,
                    line,
                    eventId));
            }
        }
    }

    /// <summary>The event record each unambiguous event class carries, by simple class name.</summary>
    /// <param name="model">The loaded project model that holds each scanned domain event class.</param>
    /// <returns>The event record identifier of each event class no other record claims, by simple class name.</returns>
    private static Dictionary<string, string> EventsByClass(ProjectModel model)
    {
        var eventsByClass = new Dictionary<string, string>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);

        foreach (var domainEvent in model.Code.Events)
        {
            if (domainEvent.EventId is null || ambiguous.Contains(domainEvent.Class))
            {
                continue;
            }

            if (eventsByClass.TryGetValue(domainEvent.Class, out var held))
            {
                if (!string.Equals(held, domainEvent.EventId, StringComparison.Ordinal))
                {
                    eventsByClass.Remove(domainEvent.Class);
                    ambiguous.Add(domainEvent.Class);
                }

                continue;
            }

            eventsByClass[domainEvent.Class] = domainEvent.EventId;
        }

        return eventsByClass;
    }
}
