using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The effect edges a record declares, as verb and target, and whether a dispatch in code matches one of them.</summary>
/// <follows>litespec/core/system#declare-every-edge-between-records</follows>
internal static class EffectEdges
{
    /// <summary>Every edge a use case declares in its effects entries: its sends, calls and queries. A use case declares no commands edge.</summary>
    /// <param name="useCase">The use case record whose effects entries are read.</param>
    /// <returns>Each declared edge verb with its target identifier.</returns>
    internal static List<(string Verb, string Target)> UseCaseEdges(Record useCase)
    {
        var edges = new List<(string Verb, string Target)>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Effects, out var effects)
            || effects.ValueKind != JsonValueKind.Array)
        {
            return edges;
        }

        foreach (var entry in effects.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var verb in new[] { RecordFields.Sends, RecordFields.Calls, RecordFields.Queries })
            {
                if (entry.TryGetProperty(verb, out var target)
                    && target.ValueKind == JsonValueKind.String
                    && target.GetString() is { Length: > 0 } id)
                {
                    edges.Add((verb, id));
                }
            }
        }

        return edges;
    }

    /// <summary>Every edge a reaction or job declares: its runs, sends, calls, queries and commands.</summary>
    /// <param name="record">The reaction or job record whose edge fields are read.</param>
    /// <returns>Each declared edge verb with its target identifier.</returns>
    internal static List<(string Verb, string Target)> HolderEdges(Record record)
    {
        var edges = new List<(string Verb, string Target)>();

        foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Calls, RecordFields.Queries, RecordFields.Commands })
        {
            foreach (var id in record.EdgeIds(verb))
            {
                edges.Add((verb, id));
            }
        }

        return edges;
    }

    /// <summary>
    ///     Whether a declared edge of the same verb names the dispatch's target. A runs dispatch
    ///     answers to a runs edge for its own module's command and to a commands edge for another
    ///     module's public command, a queries dispatch answers to a queries edge, and a calls
    ///     dispatch answers to a calls edge or to a sends edge whose notification the called port
    ///     delivers. An event publish is never declared: no record names it, because a module event
    ///     and an outbound integration event are published by the use cases whose raised event they
    ///     translate.
    /// </summary>
    /// <param name="model">The loaded project model that resolves dispatched messages and notifications.</param>
    /// <param name="edges">The edges the holder record declares.</param>
    /// <param name="dispatch">The scanned dispatch under test.</param>
    /// <returns>True when a declared edge of the same verb names the dispatch target; otherwise false.</returns>
    internal static bool EdgeDeclared(ProjectModel model, List<(string Verb, string Target)> edges, CodeDispatch dispatch) =>
        dispatch.Verb switch
        {
            "runs" => ResolveMessageUseCases(model, dispatch.Target)
                .Any(id => edges.Any(edge => (edge.Verb == "runs" || edge.Verb == "commands") && edge.Target == id)),
            "queries" => ResolveMessageUseCases(model, dispatch.Target)
                .Any(id => edges.Any(edge => edge.Verb == "queries" && edge.Target == id)),
            "publishes" => false,
            "sends" => edges.Any(edge => edge.Verb == "sends" && NotificationMatches(model, dispatch, edge.Target)),
            "calls" => edges.Any(edge => edge.Verb == "calls" && OperationMatches(dispatch.Target, edge.Target))
                || edges.Any(edge => edge.Verb == "sends" && PortDeliversNotification(model, dispatch.Detail, edge.Target)),
            _ => true,
        };

    /// <summary>The use cases a dispatched message names, by its operation stem and its handler's mark, in identifier order.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="message">The dispatched message type name.</param>
    /// <returns>The use case identifiers the message names, in identifier order.</returns>
    private static List<string> ResolveMessageUseCases(ProjectModel model, string message)
    {
        var stems = new HashSet<string>(StringComparer.Ordinal) { Naming.Kebab(message) };

        foreach (var suffix in new[] { "Command", "Query" })
        {
            if (message.EndsWith(suffix, StringComparison.Ordinal) && message.Length > suffix.Length)
            {
                stems.Add(Naming.Kebab(message[..^suffix.Length]));
            }
        }

        var found = stems
            .SelectMany(stem => model.RecordsByName[stem])
            .Where(record => record.Type == RecordTypes.UseCase)
            .Select(record => record.Id)
            .ToList();

        // The mark is the link: a message whose handler marks a use case runs
        // that use case even when the message name says otherwise.
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MessageType == message
                && handler.UseCaseId.Length > 0
                && model.ById.TryGetValue(handler.UseCaseId, out var record)
                && record.Type == RecordTypes.UseCase
                && !found.Contains(handler.UseCaseId, StringComparer.Ordinal))
            {
                found.Add(handler.UseCaseId);
            }
        }

        found.Sort(StringComparer.Ordinal);

        return found;
    }

    /// <summary>
    ///     Whether a dispatched message names a declared event edge: the message stem meets the
    ///     identifier after its slash, or its last segment, with dots reading as dashes.
    /// </summary>
    /// <param name="message">The dispatched message type name.</param>
    /// <param name="id">The declared event edge target identifier.</param>
    /// <returns>True when the message stem meets the identifier name or its last segment; otherwise false.</returns>
    private static bool MessageMatches(string message, string id)
    {
        var stem = message;

        foreach (var suffix in new[] { "IntegrationEvent", "ModuleEvent", "Notification", "Event", "Message" })
        {
            if (stem.EndsWith(suffix, StringComparison.Ordinal) && stem.Length > suffix.Length)
            {
                stem = stem[..^suffix.Length];
                break;
            }
        }

        var kebab = Naming.Kebab(stem);
        var rest = id.Contains('/') ? id[(id.IndexOf('/') + 1)..] : id;
        var normalized = rest.Replace('.', '-');
        var last = rest.Contains('.') ? rest[(rest.LastIndexOf('.') + 1)..] : rest;

        return kebab == normalized || kebab == last;
    }

    /// <summary>
    ///     Whether a dispatched send names a declared notification: a template key meets the
    ///     notification's template, and a message type meets its identifier.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the notification record.</param>
    /// <param name="dispatch">The scanned send dispatch under test.</param>
    /// <param name="id">The declared notification edge target identifier.</param>
    /// <returns>True when the dispatch names the notification by template key or message type; otherwise false.</returns>
    private static bool NotificationMatches(ProjectModel model, CodeDispatch dispatch, string id)
    {
        if (dispatch.Detail == "template")
        {
            return model.ById.TryGetValue(id, out var notification)
                && notification.Type == RecordTypes.Notification
                && notification.Field(RecordFields.Template) is { } template
                && template.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .Contains(dispatch.Target, StringComparer.Ordinal);
        }

        return MessageMatches(dispatch.Target, id);
    }

    /// <summary>Whether a port call names a declared operation: the method, without Async, meets the fragment.</summary>
    /// <param name="method">The called port method name.</param>
    /// <param name="id">The declared operation target with its fragment.</param>
    /// <returns>True when the method name without its Async suffix meets the fragment; otherwise false.</returns>
    private static bool OperationMatches(string method, string id)
    {
        var hash = id.IndexOf('#');

        if (hash < 0 || hash == id.Length - 1)
        {
            return false;
        }

        var operation = id[(hash + 1)..];
        var name = method;

        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > "Async".Length)
        {
            name = name[..^"Async".Length];
        }

        return Naming.Kebab(name) == operation;
    }

    /// <summary>Whether the called port delivers the notification a sends edge names: the port's marked external system is the system the notification names in deliveredThrough. A port named for its record delivers notifications under that name, so the call answers to the sends edge without a calls edge.</summary>
    /// <param name="model">The loaded project model that holds every notification and each marked port.</param>
    /// <param name="portKind">The called port type name the dispatch carries.</param>
    /// <param name="id">The declared notification edge target identifier.</param>
    /// <returns>True when the called port delivers the notification's system; otherwise false.</returns>
    private static bool PortDeliversNotification(ProjectModel model, string portKind, string id)
    {
        if (!model.ById.TryGetValue(id, out var notification)
            || notification.Type != RecordTypes.Notification
            || notification.Field(RecordFields.DeliveredThrough) is not { } system)
        {
            return false;
        }

        return model.Code.Elements.Any(element =>
            element.Kind == CodeKind.Port
            && string.Equals(element.Class, portKind, StringComparison.Ordinal)
            && element.Marks.Any(mark => mark.Verb != MarkVerb.Follows && mark.RecordId == system));
    }
}
