using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The edges of every record: outgoing edges from its fields and computed
///     calls, and incoming edges inverting them with the computed proves,
///     implements and follows edges added.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookEdges
{
    /// <summary>The outgoing edges of every record from its fields and computed calls.</summary>
    /// <param name="index">The model index.</param>
    /// <param name="details">The details built so far, per record id.</param>
    /// <param name="calls">The called ids per screen key.</param>
    /// <param name="routes">The screens entry per screen id.</param>
    /// <returns>The outgoing edges per record id.</returns>
    public static Dictionary<string, List<BookEdge>> OutEdges(BookIndex index, Dictionary<string, JsonObject> details, Dictionary<string, List<string>> calls, Dictionary<string, JsonObject> routes)
    {
        var outs = new Dictionary<string, List<BookEdge>>(StringComparer.Ordinal);
        foreach (var pair in details)
        {
            var id = pair.Key;
            var type = BookRender.StrProp(index.ById.GetValueOrDefault(id), "type")
                ?? BookRender.StrProp(pair.Value, "type")
                ?? "";
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<BookEdge>();
            outs[id] = list;
            void Add(string to, string key)
            {
                if (to == id || !seen.Add(to + "\0" + key))
                {
                    return;
                }

                list.Add(new BookEdge(to, key));
            }

            if (!string.IsNullOrEmpty(type))
            {
                ScanFields(pair.Value["fields"], type, id, index, Add);
            }

            if (type == "screen" && routes.TryGetValue(id, out var route))
            {
                var key = (BookRender.StrProp(route, "frontend") ?? "") + " " + (BookRender.StrProp(route, "route") ?? "");
                if (calls.TryGetValue(key, out var called))
                {
                    foreach (var target in called)
                    {
                        if (index.ById.ContainsKey(target))
                        {
                            Add(target, "calls");
                        }
                    }
                }
            }
        }

        return outs;
    }

    /// <summary>Walks one fields node, adding an edge per resolving record id.</summary>
    /// <param name="node">The fields node, or null.</param>
    /// <param name="path">The field path so far.</param>
    /// <param name="from">The linking record id.</param>
    /// <param name="index">The model index.</param>
    /// <param name="add">Adds one edge.</param>
    public static void ScanFields(JsonNode? node, string path, string from, BookIndex index, Action<string, string> add)
    {
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
        {
            if (LooksLikeRecordId(text))
            {
                var target = BookDetailValues.BaseId(text);
                if (target != from && index.ById.ContainsKey(target))
                {
                    add(target, path);
                }
            }
        }
        else if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                ScanFields(item, path, from, index, add);
            }
        }
        else if (node is JsonObject obj)
        {
            foreach (var pair in obj)
            {
                ScanFields(pair.Value, path + "." + pair.Key, from, index, add);
            }
        }
    }

    /// <summary>The incoming edges of every record, inverting outgoing and adding computed ones.</summary>
    /// <param name="model">The parsed model.</param>
    /// <param name="index">The model index.</param>
    /// <param name="outs">The outgoing edges per record id.</param>
    /// <returns>The incoming edges per record id.</returns>
    public static Dictionary<string, List<BookEdge>> InEdges(JsonObject model, BookIndex index, Dictionary<string, List<BookEdge>> outs)
    {
        var ins = new Dictionary<string, List<BookEdge>>(StringComparer.Ordinal);
        foreach (var id in index.ById.Keys)
        {
            ins[id] = new List<BookEdge>();
        }

        foreach (var pair in outs)
        {
            foreach (var edge in pair.Value)
            {
                if (!ins.TryGetValue(edge.Id, out var list))
                {
                    list = new List<BookEdge>();
                    ins[edge.Id] = list;
                }

                list.Add(new BookEdge(pair.Key, edge.Key));
            }
        }

        foreach (var key in ins.Keys.ToList())
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<BookEdge>();
            foreach (var edge in ins[key])
            {
                if (seen.Add(edge.Id + "\0" + edge.Key))
                {
                    unique.Add(edge);
                }
            }

            ins[key] = unique;
        }

        var links = model["links"] as JsonObject ?? new JsonObject();
        foreach (var link in links["implements"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var to = BookRender.StrProp(entry, "to");
            var path = BookRender.StrProp(entry, "path");
            if (!string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(path) && ins.TryGetValue(to, out var list))
            {
                list.Add(new BookEdge("code/" + path, "implements"));
            }
        }

        foreach (var link in links["proves"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var claim = BookRender.StrProp(entry, "claim");
            var scenario = BookRender.StrProp(entry, "scenario");
            if (!string.IsNullOrEmpty(claim) && !string.IsNullOrEmpty(scenario) && ins.TryGetValue(BookDetailValues.BaseId(claim), out var list))
            {
                list.Add(new BookEdge("test/" + scenario, "proves"));
            }
        }

        foreach (var link in links["follows"] as JsonArray ?? [])
        {
            if (link is not JsonObject entry)
            {
                continue;
            }

            var to = BookRender.StrProp(entry, "to");
            var path = BookRender.StrProp(entry, "path");
            if (!string.IsNullOrEmpty(to) && !string.IsNullOrEmpty(path) && ins.TryGetValue(to, out var list))
            {
                list.Add(new BookEdge("code/" + path, "follows"));
            }
        }

        return ins;
    }

    /// <summary>One edge list as id and key objects.</summary>
    /// <param name="edges">The edges.</param>
    /// <returns>The edge list.</returns>
    public static JsonArray EdgeList(List<BookEdge> edges)
    {
        var list = new JsonArray();
        foreach (var edge in edges)
        {
            list.Add(new JsonObject
            {
                ["id"] = edge.Id,
                ["key"] = edge.Key,
            });
        }

        return list;
    }

    /// <summary>Whether a string has the shape of a record id, such as type/name.</summary>
    /// <param name="value">The candidate id.</param>
    /// <returns>Whether the value has a record id shape.</returns>
    public static bool LooksLikeRecordId(string value)
    {
        var target = BookDetailValues.BaseId(value);
        var slash = target.IndexOf('/');
        if (slash <= 0 || slash + 1 >= target.Length)
        {
            return false;
        }

        foreach (var ch in target[..slash])
        {
            if (ch != '-' && (ch < 'a' || ch > 'z'))
            {
                return false;
            }
        }

        return char.IsLetterOrDigit(target[slash + 1]);
    }
}
