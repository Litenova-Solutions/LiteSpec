using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The journey and screen details of one record: the servers, screens and
///     exercised steps of a journey, and the route and called use cases of a screen.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookJourneyDetail
{
    /// <summary>Adds the step servers, step screens and exercised steps of one journey.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="served">The serving record ids per step id.</param>
    /// <param name="index">The model index.</param>
    /// <param name="record">The journey record.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddSteps(JsonObject detail, Dictionary<string, List<string>> served, BookIndex index, JsonObject record, string id, string type)
    {
        if (type != "journey")
        {
            return;
        }

        var servers = new JsonObject();
        var screens = new JsonObject();
        var exercised = new JsonArray();
        foreach (var step in BookRender.Fields(record)?["steps"] as JsonArray ?? [])
        {
            if (step is not JsonObject entry)
            {
                continue;
            }

            var stepId = BookRender.StrProp(entry, "id");
            if (string.IsNullOrEmpty(stepId))
            {
                continue;
            }

            var full = id + "#" + stepId;
            var all = served.TryGetValue(full, out var list) ? list : new List<string>();
            var use = new JsonArray();
            var onScreen = new JsonArray();
            foreach (var server in all)
            {
                if (server.StartsWith("screen/", StringComparison.Ordinal))
                {
                    onScreen.Add(server);
                }
                else
                {
                    use.Add(server);
                }
            }

            servers[stepId] = use;
            screens[stepId] = onScreen;
            if (index.ExercisedSteps.Contains(full))
            {
                exercised.Add(stepId);
            }
        }

        detail["stepServers"] = servers;
        detail["stepScreens"] = screens;
        detail["exercised"] = exercised;
    }

    /// <summary>Adds the route and called use cases of one screen.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="routeEntry">The screen's screens entry, or null.</param>
    /// <param name="calls">The called ids per screen key.</param>
    public static void AddRoute(JsonObject detail, JsonObject? routeEntry, Dictionary<string, List<string>> calls)
    {
        if (routeEntry is null)
        {
            detail["route"] = null;
            detail["calls"] = new JsonArray();
            return;
        }

        var frontend = BookRender.StrProp(routeEntry, "frontend");
        var route = BookRender.StrProp(routeEntry, "route");
        detail["route"] = new JsonObject
        {
            ["frontend"] = BookDetailValues.StrNode(frontend),
            ["route"] = BookDetailValues.StrNode(route),
            ["file"] = BookDetailValues.StrNode(BookRender.StrProp(routeEntry, "file")),
            ["reader"] = BookDetailValues.StrNode(BookRender.StrProp(routeEntry, "reader")),
        };

        var called = new JsonArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (calls.TryGetValue((frontend ?? "") + " " + (route ?? ""), out var list))
        {
            foreach (var target in list)
            {
                if (seen.Add(target))
                {
                    called.Add(target);
                }
            }
        }

        detail["calls"] = called;
    }
}
