using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Audit;

/// <summary>The routes <c>litespec ui audit</c> visits: from the screen records, or from a routes file.</summary>
/// <follows>litespec/frontend/ui#pass-every-route-suite-check</follows>
static class AuditRoutes
{
    /// <summary>Collects the static route of every written screen record, skipping routes with parameters.</summary>
    /// <param name="records">Every written record.</param>
    /// <returns>The screen routes in path order, each naming its screen id.</returns>
    internal static IReadOnlyList<AuditRoute> Collect(IReadOnlyList<Record> records) =>
        [.. records
            .Where(record => !record.Derived && !record.IsEntry && record.Type == RecordTypes.Screen)
            .Select(record => (Id: record.Id, Route: record.Field(RecordFields.Route)))
            .Where(entry => entry.Route is { Length: > 0 } && !HasParameters(entry.Route))
            .OrderBy(entry => entry.Route, StringComparer.Ordinal)
            .Select(entry => new AuditRoute(entry.Route!, entry.Id))];

    /// <summary>Whether the route holds a parameter the audit cannot fill: braces, brackets, a colon, or a star.</summary>
    /// <param name="path">The route path.</param>
    /// <returns>True when the path holds a parameter marker.</returns>
    internal static bool HasParameters(string path) =>
        path.Contains('{', StringComparison.Ordinal)
        || path.Contains('}', StringComparison.Ordinal)
        || path.Contains('[', StringComparison.Ordinal)
        || path.Contains(']', StringComparison.Ordinal)
        || path.Contains(':', StringComparison.Ordinal)
        || path.Contains('*', StringComparison.Ordinal);

    /// <summary>Reads a routes file: a YAML or JSON list of entries with a path and an optional name.</summary>
    /// <param name="file">The absolute routes file path.</param>
    /// <returns>The routes in file order, each naming its name or its path, or the reason the file cannot be read.</returns>
    internal static (IReadOnlyList<AuditRoute>? Routes, string? Error) ReadFile(string file)
    {
        JsonNode? root;

        try
        {
            root = RecordText.Read(file);
        }
        catch (JsonTextException exception)
        {
            return (null, $"Routes file '{file}' is not valid YAML or JSON: {exception.Message}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, $"Cannot read routes file '{file}': {exception.Message}");
        }

        if (root is not JsonArray entries)
        {
            return (null, $"Routes file '{file}' holds no list of routes: write a list of entries with a path.");
        }

        var routes = new List<AuditRoute>();
        var index = 0;

        foreach (var entry in entries)
        {
            index++;

            if (entry is not JsonObject route || TextOf(route, "path") is not { Length: > 0 } path)
            {
                return (null, $"Routes file '{file}' entry {index} needs a path.");
            }

            var name = TextOf(route, "name");
            routes.Add(new AuditRoute(path, string.IsNullOrEmpty(name) ? path : name));
        }

        return (routes, null);
    }

    /// <summary>Reads one optional string member of a routes file entry.</summary>
    /// <param name="entry">The entry object.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The string value, or null when the member is missing or not a string.</returns>
    static string? TextOf(JsonObject entry, string name) =>
        entry[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
