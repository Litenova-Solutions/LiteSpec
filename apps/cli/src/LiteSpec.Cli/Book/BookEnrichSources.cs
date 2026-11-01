using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The lookups the record details read: outline layers, fulfilling counts, journey
///     servers, tests, findings, decisions, effective statuses, screen routes and
///     calls, and code entries, each grouped once from the model and the outline.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookEnrichSources
{
    /// <summary>Gets the layer keys the outline roots may name.</summary>
    static HashSet<string> KnownLayers { get; } = new(StringComparer.Ordinal)
    {
        "intent", "behavior", "implementation", "proof", "governance", "glossary",
    };

    /// <summary>Gets the record types that may serve a journey step.</summary>
    static HashSet<string> StepServerTypes { get; } = new(StringComparer.Ordinal)
    {
        "use-case", "reaction", "job", "notification", "screen",
    };

    /// <summary>The outline parent id, or null for a root.</summary>
    /// <param name="outline">The built outline.</param>
    /// <param name="id">The record id.</param>
    /// <returns>The parent id, or null.</returns>
    public static string? ParentOf(BookOutlineResult outline, string id) =>
        outline.ParentOf.TryGetValue(id, out var parent) ? parent : null;

    /// <summary>Maps every outline node id to its layer key.</summary>
    /// <param name="outline">The built outline.</param>
    /// <returns>The layer key per record id.</returns>
    public static Dictionary<string, string> LayerById(BookOutlineResult outline)
    {
        var layers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in outline.Layers)
        {
            var key = LayerKeyOf(root);
            var stack = new Stack<BookOutlineNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (!string.IsNullOrEmpty(node.Id) && !layers.ContainsKey(node.Id))
                {
                    layers[node.Id] = key;
                }

                foreach (var child in node.Children)
                {
                    stack.Push(child);
                }
            }
        }

        return layers;
    }

    /// <summary>The layer key of one outline root, from its id or label.</summary>
    /// <param name="root">The outline root.</param>
    /// <returns>The layer key.</returns>
    public static string LayerKeyOf(BookOutlineNode root)
    {
        var id = root.Id ?? "";
        if (id.StartsWith("layer/", StringComparison.Ordinal))
        {
            return id["layer/".Length..];
        }

        if (KnownLayers.Contains(id))
        {
            return id;
        }

        var slash = id.IndexOf('/');
        if (slash > 0 && KnownLayers.Contains(id[..slash]))
        {
            return id[..slash];
        }

        var label = (root.Label ?? "").ToLowerInvariant();
        if (KnownLayers.Contains(label))
        {
            return label;
        }

        return id;
    }

    /// <summary>Counts, per fulfilled requirement, how many records of each type fulfill it.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>The fulfilling counts per requirement id and record type.</returns>
    public static Dictionary<string, Dictionary<string, int>> KeptBy(BookIndex index)
    {
        var kept = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var type = BookRender.StrProp(record, "type") ?? "";
            foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(record)?["fulfills"]))
            {
                if (!kept.TryGetValue(target, out var counts))
                {
                    counts = new Dictionary<string, int>(StringComparer.Ordinal);
                    kept[target] = counts;
                }

                counts[type] = counts.TryGetValue(type, out var count) ? count + 1 : 1;
            }
        }

        return kept;
    }

    /// <summary>The records serving each journey step id, such as journey/name#s1.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>The serving record ids per step id.</returns>
    public static Dictionary<string, List<string>> ServedBy(BookIndex index)
    {
        var served = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var type = BookRender.StrProp(record, "type");
            if (type is null || !StepServerTypes.Contains(type))
            {
                continue;
            }

            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(record)?["serves"]))
            {
                if (!target.Contains('#'))
                {
                    continue;
                }

                if (!served.TryGetValue(target, out var list))
                {
                    list = [];
                    served[target] = list;
                }

                list.Add(id);
            }
        }

        return served;
    }

    /// <summary>Groups unit and browser tests by the record id their claim names.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="index">The model index.</param>
    /// <returns>The tests per record id.</returns>
    public static Dictionary<string, List<JsonObject>> TestsByRecord(JsonObject model, BookIndex index)
    {
        var byRecord = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        void Add(string claim, JsonObject entry)
        {
            if (!byRecord.TryGetValue(claim, out var list))
            {
                list = [];
                byRecord[claim] = list;
            }

            list.Add(entry);
        }

        foreach (var test in model["tests"] as JsonArray ?? [])
        {
            if (test is not JsonObject entry)
            {
                continue;
            }

            var scenario = BookRender.StrProp(entry, "scenario") ?? "";
            var name = BookDetailValues.HumanTestName(scenario);
            var file = BookRender.StrProp(entry, "file");
            var line = BookDetailValues.IntProp(entry, "line");
            foreach (var claim in BookDetailValues.StringsOf(entry["claims"]))
            {
                var target = BookDetailValues.BaseId(claim);
                var confirmed = index.ProvesByClaim.TryGetValue(target, out var links)
                    && links.Any(link => BookRender.StrProp(link, "scenario") == scenario && BookRender.IsTrue(link["confirmed"]));
                Add(target, new JsonObject
                {
                    ["name"] = name,
                    ["path"] = BookDetailValues.StrNode(file),
                    ["line"] = BookDetailValues.IntNode(line),
                    ["confirmed"] = confirmed,
                    ["passing"] = null,
                });
            }
        }

        foreach (var browser in model["browserTests"] as JsonArray ?? [])
        {
            if (browser is not JsonObject entry)
            {
                continue;
            }

            var title = BookRender.StrProp(entry, "title") ?? "";
            var file = BookRender.StrProp(entry, "file");
            var line = BookDetailValues.IntProp(entry, "line");
            foreach (var claim in BookDetailValues.StringsOf(entry["proves"]))
            {
                Add(BookDetailValues.BaseId(claim), new JsonObject
                {
                    ["name"] = title,
                    ["path"] = BookDetailValues.StrNode(file),
                    ["line"] = BookDetailValues.IntNode(line),
                    ["confirmed"] = true,
                    ["passing"] = null,
                    ["browser"] = true,
                });
            }
        }

        return byRecord;
    }

    /// <summary>Groups open and known findings by their subject record id.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="index">The model index.</param>
    /// <returns>The findings per record id.</returns>
    public static Dictionary<string, List<JsonObject>> FindingsBySubject(JsonObject model, BookIndex index)
    {
        var bySubject = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        void Add(JsonObject finding, bool known)
        {
            var subject = BookRender.StrProp(finding, "subject");
            if (string.IsNullOrEmpty(subject))
            {
                return;
            }

            var target = BookDetailValues.BaseId(subject);
            if (!index.ById.ContainsKey(target))
            {
                return;
            }

            if (!bySubject.TryGetValue(target, out var list))
            {
                list = [];
                bySubject[target] = list;
            }

            list.Add(new JsonObject
            {
                ["code"] = BookDetailValues.StrNode(BookRender.StrProp(finding, "code")),
                ["tier"] = BookDetailValues.StrNode(BookRender.StrProp(finding, "tier")),
                ["message"] = BookRender.StrProp(finding, "message") ?? "",
                ["known"] = known,
                ["outcome"] = finding["outcome"]?.DeepClone(),
                ["reason"] = finding["reason"]?.DeepClone(),
                ["path"] = finding["path"]?.DeepClone(),
                ["line"] = finding["line"]?.DeepClone(),
            });
        }

        foreach (var finding in FindingEntries(model["findings"]))
        {
            Add(finding, false);
        }

        foreach (var known in FindingEntries(model["known"]))
        {
            Add(known, true);
        }

        return bySubject;
    }

    /// <summary>The finding objects of a findings node, as an array or a wrapped file.</summary>
    /// <param name="node">The findings node, or null.</param>
    /// <returns>The finding objects.</returns>
    public static IEnumerable<JsonObject> FindingEntries(JsonNode? node)
    {
        if (node is JsonArray items)
        {
            foreach (var item in items.OfType<JsonObject>())
            {
                yield return item;
            }
        }
        else if (node is JsonObject wrapper && wrapper["findings"] is JsonArray inner)
        {
            foreach (var item in inner.OfType<JsonObject>())
            {
                yield return item;
            }
        }
    }

    /// <summary>Maps each decided record id to the decisions naming it, in identifier order.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>The decision ids per record id.</returns>
    public static Dictionary<string, List<string>> DecisionsByTarget(BookIndex index)
    {
        var byTarget = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            if (BookRender.StrProp(record, "type") != "decision")
            {
                continue;
            }

            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            foreach (var target in BookDetailValues.StringsOf(BookRender.Fields(record)?["decides"]))
            {
                if (!byTarget.TryGetValue(target, out var list))
                {
                    list = [];
                    byTarget[target] = list;
                }

                list.Add(id);
            }
        }

        foreach (var key in byTarget.Keys.ToList())
        {
            byTarget[key] = byTarget[key].OrderBy(id => id, StringComparer.Ordinal).ToList();
        }

        return byTarget;
    }

    /// <summary>The effective status of every record, defaulting to draft.</summary>
    /// <param name="index">The model index.</param>
    /// <returns>The effective status per record id.</returns>
    public static Dictionary<string, string> EffectiveById(BookIndex index)
    {
        var effective = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in index.Records.OfType<JsonObject>())
        {
            var id = BookRender.StrProp(record, "id");
            if (string.IsNullOrEmpty(id) || effective.ContainsKey(id))
            {
                continue;
            }

            var state = BookRender.StrProp(record, "effectiveStatus");
            if (string.IsNullOrEmpty(state))
            {
                state = BookRender.StrProp(record, "status");
            }

            effective[id] = string.IsNullOrEmpty(state) ? "draft" : state;
        }

        return effective;
    }

    /// <summary>Maps each screen id to its screens entry, the last one winning.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The screens entry per screen id.</returns>
    public static Dictionary<string, JsonObject> RoutesByScreen(JsonObject model)
    {
        var routes = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var screen in model["screens"] as JsonArray ?? [])
        {
            if (screen is JsonObject entry && !string.IsNullOrEmpty(BookRender.StrProp(entry, "screen")))
            {
                routes[BookRender.StrProp(entry, "screen")!] = entry;
            }
        }

        return routes;
    }

    /// <summary>Groups computed calls links by their screen key, such as frontend plus route.</summary>
    /// <param name="model">The parsed model.</param>
    /// <returns>The called ids per screen key.</returns>
    public static Dictionary<string, List<string>> CallsByScreen(JsonObject model)
    {
        var calls = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var links = model["links"] as JsonObject;
        foreach (var call in links?["calls"] as JsonArray ?? [])
        {
            if (call is not JsonObject entry)
            {
                continue;
            }

            var screen = BookRender.StrProp(entry, "screen");
            var to = BookRender.StrProp(entry, "to");
            if (string.IsNullOrEmpty(screen) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (!calls.TryGetValue(screen, out var list))
            {
                list = [];
                calls[screen] = list;
            }

            list.Add(to);
        }

        return calls;
    }

    /// <summary>Groups one code array by the record id its key names.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="array">The code array name.</param>
    /// <param name="key">The property naming the record id.</param>
    /// <returns>The code entries per record id.</returns>
    public static Dictionary<string, List<JsonObject>> CodeByKey(JsonObject model, string array, string key)
    {
        var byKey = new Dictionary<string, List<JsonObject>>(StringComparer.Ordinal);
        var code = model["code"] as JsonObject;
        foreach (var entry in code?[array] as JsonArray ?? [])
        {
            if (entry is not JsonObject obj)
            {
                continue;
            }

            var id = BookRender.StrProp(obj, key);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!byKey.TryGetValue(id, out var list))
            {
                list = [];
                byKey[id] = list;
            }

            list.Add(obj);
        }

        return byKey;
    }

    /// <summary>Maps each reaction and job class path to its code entry, the first one winning.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="arrays">The code array names to read.</param>
    /// <returns>The code entry per class path.</returns>
    public static Dictionary<string, JsonObject> CodeByPaths(JsonObject model, string[] arrays)
    {
        var byPath = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var code = model["code"] as JsonObject;
        foreach (var array in arrays)
        {
            foreach (var entry in code?[array] as JsonArray ?? [])
            {
                if (entry is JsonObject obj && !string.IsNullOrEmpty(BookRender.StrProp(obj, "path")))
                {
                    byPath.TryAdd(BookRender.StrProp(obj, "path")!, obj);
                }
            }
        }

        return byPath;
    }
}
