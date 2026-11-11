using System.Text.Json;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The effect edges a record declares, as verb and target, and whether a dispatch in code matches one of them.</summary>
/// <follows>litespec/core/system#declare-every-edge-between-records</follows>
internal static class EffectEdges
{
    /// <summary>Every edge a use case declares in its effects entries.</summary>
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

            foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Emits, RecordFields.Calls })
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

    /// <summary>Every edge a reaction or job declares: its runs, sends, emits and calls.</summary>
    /// <param name="record">The reaction or job record whose edge fields are read.</param>
    /// <returns>Each declared edge verb with its target identifier.</returns>
    internal static List<(string Verb, string Target)> HolderEdges(Record record)
    {
        var edges = new List<(string Verb, string Target)>();

        foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Emits, RecordFields.Calls })
        {
            foreach (var id in record.EdgeIds(verb))
            {
                edges.Add((verb, id));
            }
        }

        return edges;
    }

    /// <summary>Whether a declared edge of the same verb names the dispatch's target.</summary>
    /// <param name="model">The loaded project model that resolves dispatched messages and notifications.</param>
    /// <param name="edges">The edges the holder record declares.</param>
    /// <param name="dispatch">The scanned dispatch under test.</param>
    /// <returns>True when a declared edge of the same verb names the dispatch target; otherwise false.</returns>
    internal static bool EdgeDeclared(ProjectModel model, List<(string Verb, string Target)> edges, CodeDispatch dispatch) =>
        dispatch.Verb switch
        {
            "runs" => ResolveMessageUseCases(model, dispatch.Target)
                .Any(id => edges.Any(edge => edge.Verb == "runs" && edge.Target == id)),
            "emits" => edges.Any(edge => edge.Verb == "emits" && MessageMatches(dispatch.Target, edge.Target)),
            "sends" => edges.Any(edge => edge.Verb == "sends" && NotificationMatches(model, dispatch, edge.Target)),
            "calls" => edges.Any(edge => edge.Verb == "calls" && OperationMatches(dispatch.Target, edge.Target)),
            _ => true,
        };

    /// <summary>The use cases a dispatched message names, by its operation stem, in identifier order.</summary>
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

        foreach (var suffix in new[] { "IntegrationEvent", "Notification", "Event", "Message" })
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
}
