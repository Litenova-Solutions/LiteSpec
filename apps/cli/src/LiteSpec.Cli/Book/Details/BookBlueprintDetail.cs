using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Book.Output;
using LiteSpec.Model;

namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The UI blueprint details of one record: the wireframe text of a screen
///     from its explanation, the navigation map of an entry point, and the
///     storyboard of a flow.
/// </summary>
/// <follows>litespec/core/model#show-only-computed-judgments-in-the-book</follows>
static class BookBlueprintDetail
{
    /// <summary>Adds the wireframe text of one screen from its explanation's wireframe block.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="record">The screen record.</param>
    /// <param name="type">The record type.</param>
    public static void AddWireframe(JsonObject detail, JsonObject record, string type)
    {
        if (type != RecordTypes.Screen)
        {
            return;
        }

        if (WireframeOf(BookRender.StrProp(record, "explanation") ?? "") is { } text
            && detail["fields"] is JsonObject fields)
        {
            fields["wireframe"] = text;
        }
    }

    /// <summary>The text of the first fenced block with the info string wireframe, or null when the explanation holds none.</summary>
    /// <param name="explanation">The explanation file text.</param>
    /// <returns>The block text without its fences, or null.</returns>
    public static string? WireframeOf(string explanation)
    {
        var lines = explanation.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var at = 0; at < lines.Length; at++)
        {
            if (!lines[at].StartsWith("```", StringComparison.Ordinal) || lines[at][3..].Trim() != "wireframe")
            {
                continue;
            }

            var body = new List<string>();
            at++;
            while (at < lines.Length && !lines[at].StartsWith("```", StringComparison.Ordinal))
            {
                body.Add(lines[at]);
                at++;
            }

            return string.Join("\n", body);
        }

        return null;
    }

    /// <summary>Adds the navigation map of one entry point: its screens and each screen's leadsTo.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="index">The model index.</param>
    /// <param name="outline">The built outline.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddNavigationMap(JsonObject detail, BookIndex index, BookOutlineResult outline, string id, string type)
    {
        if (type != RecordTypes.EntryPoint)
        {
            return;
        }

        var screens = new List<string>();
        foreach (var screen in index.ByType.TryGetValue(RecordTypes.Screen, out var found) ? found : [])
        {
            var screenId = BookRender.StrProp(screen, "id");
            if (string.IsNullOrEmpty(screenId) || screens.Contains(screenId, StringComparer.Ordinal))
            {
                continue;
            }

            var placedBelow = outline.ParentOf.TryGetValue(screenId, out var parent) && parent == id;
            var namesEntryPoint = BookDetailValues.StringsOf(BookRender.Fields(screen)?["entryPoint"]).Contains(id, StringComparer.Ordinal)
                || BookDetailValues.StringsOf(BookRender.Links(screen)?["entryPoint"]).Contains(id, StringComparer.Ordinal);
            if (placedBelow || namesEntryPoint)
            {
                screens.Add(screenId);
            }
        }

        screens.Sort(StringComparer.Ordinal);
        var ids = new JsonArray();
        var leads = new JsonObject();
        foreach (var screenId in screens)
        {
            ids.Add((JsonNode)screenId);
            var targets = new JsonArray();
            if (index.ById.TryGetValue(screenId, out var screen))
            {
                foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(screen)?[RecordFields.LeadsTo]))
                {
                    targets.Add((JsonNode)target);
                }
            }

            leads[screenId] = targets;
        }

        detail["navigationMap"] = new JsonObject
        {
            ["screens"] = ids,
            ["leadsTo"] = leads,
        };
    }

    /// <summary>Adds the storyboard of one flow: each step with the screens that serve it, in step order.</summary>
    /// <param name="detail">The detail being built.</param>
    /// <param name="served">The serving record ids per step id.</param>
    /// <param name="record">The flow record.</param>
    /// <param name="id">The record id.</param>
    /// <param name="type">The record type.</param>
    public static void AddStoryboard(JsonObject detail, Dictionary<string, List<string>> served, JsonObject record, string id, string type)
    {
        if (type != RecordTypes.Flow)
        {
            return;
        }

        var storyboard = new JsonArray();
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

            var onScreen = new JsonArray();
            if (served.TryGetValue(id + "#" + stepId, out var list))
            {
                foreach (var server in list)
                {
                    if (server.StartsWith("screen/", StringComparison.Ordinal))
                    {
                        onScreen.Add((JsonNode)server);
                    }
                }
            }

            storyboard.Add((JsonNode)new JsonObject
            {
                ["step"] = stepId,
                ["statement"] = BookRender.StrProp(entry, "statement") ?? "",
                ["screens"] = onScreen,
            });
        }

        detail["storyboard"] = storyboard;
    }
}
