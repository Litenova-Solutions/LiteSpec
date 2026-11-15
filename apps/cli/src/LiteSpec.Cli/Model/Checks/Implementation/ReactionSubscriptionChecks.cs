using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>The reaction subscription checks: each marked handler subscribes to a declared trigger, and each declared trigger has a marked handler.</summary>
/// <follows>litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers</follows>
internal static class ReactionSubscriptionChecks
{
    /// <summary>Runs reaction-subscription-drift and reaction-trigger-unhandled.</summary>
    /// <param name="model">The loaded project model that holds every reaction record and each scanned event handler.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var eventsByClass = EventsByClass(model);
        var handlersByReaction = model.Code.Reactions
            .Where(handler => handler.RecordId is not null)
            .GroupBy(handler => handler.RecordId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var reaction in model.Records)
        {
            if (reaction.Type != RecordTypes.Reaction)
            {
                continue;
            }

            // A reaction with no marked handler yields nothing: that case is reaction-unimplemented.
            if (!handlersByReaction.TryGetValue(reaction.Id, out var handlers) || handlers.Count == 0)
            {
                continue;
            }

            var triggers = ReactionEventTriggers(reaction)
                .Select(Identifiers.BaseId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            var handled = new HashSet<string>(StringComparer.Ordinal);
            var unresolved = false;

            foreach (var handler in handlers)
            {
                if (handler.EventType.Length == 0 || !eventsByClass.TryGetValue(handler.EventType, out var subscribed))
                {
                    // A handler subscribed to something the map cannot resolve may
                    // answer a declared trigger, so no absence is claimed below. A
                    // handler with no event type subscribes to nothing at all.
                    if (handler.EventType.Length > 0)
                    {
                        unresolved = true;
                    }

                    continue;
                }

                handled.Add(subscribed);

                if (!triggers.Contains(subscribed, StringComparer.Ordinal))
                {
                    findings.Add(new Finding(
                        "reaction-subscription-drift",
                        $"Event handler '{handler.Class}' marked with reaction '{reaction.Id}' subscribes to '{subscribed}', which the reaction does not name in `triggeredBy`.",
                        handler.File,
                        handler.Line,
                        reaction.Id));
                }
            }

            if (unresolved)
            {
                continue;
            }

            foreach (var trigger in triggers)
            {
                if (handled.Contains(trigger))
                {
                    continue;
                }

                if (!model.ById.TryGetValue(trigger, out var resolved)
                    || resolved.Type is not (RecordTypes.DomainEvent or RecordTypes.IntegrationEvent))
                {
                    continue;
                }

                findings.Add(new Finding(
                    "reaction-trigger-unhandled",
                    $"Reaction '{reaction.Id}' names trigger '{trigger}' with no marked handler subscribed to it.",
                    reaction.File,
                    reaction.Line,
                    SubjectOf(reaction)));
            }
        }
    }

    /// <summary>The event record each marked event class carries: domain events from the event rows, integration events from their technical marks.</summary>
    /// <param name="model">The loaded project model that holds each scanned event class and marked element.</param>
    /// <returns>The event record identifier of each marked event class, by simple class name.</returns>
    private static Dictionary<string, string> EventsByClass(ProjectModel model)
    {
        var eventsByClass = new Dictionary<string, string>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);

        foreach (var domainEvent in model.Code.Events)
        {
            if (domainEvent.EventId is not null)
            {
                Add(eventsByClass, ambiguous, domainEvent.Class, domainEvent.EventId);
            }
        }

        foreach (var element in model.Code.Elements)
        {
            if (element.Member is not null || element.Kind != CodeKind.Technical)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Follows
                    && (mark.RecordId.StartsWith("domain-event/", StringComparison.Ordinal)
                        || mark.RecordId.StartsWith("integration-event/", StringComparison.Ordinal)))
                {
                    Add(eventsByClass, ambiguous, element.Class, mark.RecordId);
                }
            }
        }

        return eventsByClass;
    }

    /// <summary>Adds the event record of one event class, leaving out a simple name two records claim.</summary>
    /// <param name="eventsByClass">The event record identifier of each unambiguous event class, by simple class name.</param>
    /// <param name="ambiguous">The simple names two records claim, which no handler resolves through.</param>
    /// <param name="name">The simple class name.</param>
    /// <param name="id">The event record identifier the class carries.</param>
    private static void Add(Dictionary<string, string> eventsByClass, HashSet<string> ambiguous, string name, string id)
    {
        if (ambiguous.Contains(name))
        {
            return;
        }

        if (eventsByClass.TryGetValue(name, out var held))
        {
            if (!string.Equals(held, id, StringComparison.Ordinal))
            {
                eventsByClass.Remove(name);
                ambiguous.Add(name);
            }

            return;
        }

        eventsByClass[name] = id;
    }
}
