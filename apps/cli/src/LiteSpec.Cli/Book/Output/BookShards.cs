using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Details;
using LiteSpec.Cli.Book.Outline;

namespace LiteSpec.Cli.Book.Output;

/// <summary>
///     The book's sharded data: record packs, outline packs, search packs and
///     single-value shards, each produced as one named file of bytes that calls
///     <c>BookData.put</c> once. Shards load through script elements, never
///     through <c>fetch</c>, so the book opens under <c>file://</c>.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static class BookShards
{
    /// <summary>One record or search shard's target payload: packing starts a new shard past this size.</summary>
    public const int PackTargetBytes = 256 * 1024;

    /// <summary>One record or search shard's hard payload cap: a test fails any shard past this size.</summary>
    public const int PackCapBytes = 512 * 1024;

    /// <summary>One layer outline's split point: a layer past this size writes sequential shards.</summary>
    public const int OutlineSplitBytes = 700 * 1024;

    /// <summary>The UTF-8 encoding without a byte order mark, used for every book text file.</summary>
    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>
    ///     Packs items in order into shards: each shard fills to its target, then a
    ///     new shard starts. One item past the target keeps its own shard whole,
    ///     since a record is never split; the cap test names it instead.
    /// </summary>
    /// <typeparam name="T">The type of the items to pack.</typeparam>
    /// <param name="items">The items to pack, in order.</param>
    /// <param name="size">The function that gives the byte size of one item.</param>
    /// <param name="targetBytes">The target byte size each pack fills to before a new pack starts.</param>
    /// <returns>The packs in order, each holding its items in input order, empty when the input is empty.</returns>
    public static IReadOnlyList<IReadOnlyList<T>> Pack<T>(IReadOnlyList<T> items, Func<T, int> size, int targetBytes)
    {
        var packs = new List<IReadOnlyList<T>>();
        var current = new List<T>();
        var currentBytes = 0;
        foreach (var item in items)
        {
            var bytes = size(item);
            if (current.Count > 0 && currentBytes + bytes > targetBytes)
            {
                packs.Add(current);
                current = new List<T>();
                currentBytes = 0;
            }

            current.Add(item);
            currentBytes += bytes;
        }

        if (current.Count > 0)
        {
            packs.Add(current);
        }

        return packs;
    }

    /// <summary>
    ///     One shard file's text: exactly one <c>BookData.put</c> call with the key
    ///     and the compact JSON. The payload can never close its script element:
    ///     the serializer escapes markup, and any surviving closing tag or line
    ///     separator is escaped again here, which parses back to the same text.
    /// </summary>
    /// <param name="key">The shard key the client reads the value under.</param>
    /// <param name="value">The shard payload value.</param>
    /// <returns>The shard file text with one BookData.put call holding the key and the escaped compact JSON.</returns>
    public static string ShardScript(string key, JsonNode value) =>
        ShardScriptFromJson(key, value.ToJsonString());

    /// <summary>One shard file's text from pre-serialized compact JSON.</summary>
    /// <param name="key">The shard key the client reads the value under.</param>
    /// <param name="json">The compact JSON payload.</param>
    /// <returns>The shard file text with one BookData.put call holding the key and the escaped JSON.</returns>
    public static string ShardScriptFromJson(string key, string json) =>
        $"BookData.put({JsonValue.Create(key)!.ToJsonString()}, {EscapeJson(json)});";

    /// <summary>Escapes one JSON payload for its script element in one pass: closing tags and line separators.</summary>
    /// <param name="json">The compact JSON payload.</param>
    /// <returns>The payload with every closing tag and line separator escaped, parsing back to the same text.</returns>
    public static string EscapeJson(string json)
    {
        const char lineSeparator = '\u2028';
        const char paragraphSeparator = '\u2029';
        if (json.IndexOf('<') < 0 && json.IndexOf(lineSeparator) < 0 && json.IndexOf(paragraphSeparator) < 0)
        {
            return json;
        }

        var built = new StringBuilder(json.Length);
        for (var at = 0; at < json.Length; at++)
        {
            var ch = json[at];
            if (ch == '<' && at + 1 < json.Length && json[at + 1] == '/')
            {
                built.Append("<\\/");
                at++;
            }
            else if (ch == lineSeparator)
            {
                built.Append("\\u2028");
            }
            else if (ch == paragraphSeparator)
            {
                built.Append("\\u2029");
            }
            else
            {
                built.Append(ch);
            }
        }

        return built.ToString();
    }

    /// <summary>Adds the shard <c>data/&lt;key&gt;.js</c> holding one shard script to the book's files.</summary>
    /// <param name="into">The book files the shard joins.</param>
    /// <param name="key">The shard key, which names the file and the client value.</param>
    /// <param name="value">The shard payload value.</param>
    public static void AddShard(List<BookFile> into, string key, JsonNode value) =>
        into.Add(BookFile.Of(DataPath(key), Utf8NoBom.GetBytes(ShardScript(key, value) + "\n")));

    /// <summary>The book path of one shard: <c>data/&lt;key&gt;.js</c>.</summary>
    /// <param name="key">The shard key.</param>
    /// <returns>The path inside the book.</returns>
    public static string DataPath(string key) => "data/" + key + ".js";

    /// <summary>
    ///     Adds the record packs in identifier order as <c>records-&lt;nnn&gt;.js</c>,
    ///     and returns the shard id per record id for the manifest and the outline.
    ///     Each detail serializes once; sizing, escaping and packing read that text.
    /// </summary>
    /// <param name="into">The book files the record shards join.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <returns>The shard identifier for each record identifier packed.</returns>
    public static IReadOnlyDictionary<string, string> AddRecords(List<BookFile> into, IReadOnlyDictionary<string, JsonObject> details) =>
        AddRecords(into, details, details.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList());

    /// <summary>
    ///     Adds the record packs in the given order as <c>records-&lt;nnn&gt;.js</c>,
    ///     and returns the shard id per record id for the manifest and the outline.
    ///     A detail the order omits packs last in identifier order. Each detail
    ///     serializes once; sizing, escaping and packing read that text.
    /// </summary>
    /// <param name="into">The book files the record shards join.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <param name="order">The record identifiers in pack order.</param>
    /// <returns>The shard identifier for each record identifier packed.</returns>
    public static IReadOnlyDictionary<string, string> AddRecords(List<BookFile> into, IReadOnlyDictionary<string, JsonObject> details, IReadOnlyList<string> order)
    {
        var json = new Dictionary<string, string>(StringComparer.Ordinal);
        var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
        var ids = new List<string>();
        void Add(string id)
        {
            if (json.ContainsKey(id) || !details.TryGetValue(id, out var detail))
            {
                return;
            }

            var text = detail.ToJsonString();
            json[id] = text;
            sizes[id] = Encoding.UTF8.GetByteCount(text) + Encoding.UTF8.GetByteCount(id) + 8;
            ids.Add(id);
        }

        foreach (var id in order)
        {
            Add(id);
        }

        foreach (var id in details.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            Add(id);
        }

        var shardOf = new Dictionary<string, string>(StringComparer.Ordinal);
        var packs = Pack(ids, id => sizes[id], PackTargetBytes);
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"records-{shard:000}";
            var built = new StringBuilder("{");
            var first = true;
            foreach (var id in packs[shard])
            {
                if (!first)
                {
                    built.Append(',');
                }

                first = false;
                built.Append(JsonValue.Create(id)!.ToJsonString());
                built.Append(':');
                built.Append(json[id]);
                shardOf[id] = key;
            }

            built.Append('}');
            into.Add(BookFile.Of(DataPath(key), Utf8NoBom.GetBytes(ShardScriptFromJson(key, built.ToString()) + "\n")));
        }

        return shardOf;
    }

    /// <summary>The record ids in outline order: each layer depth-first, so parents pack with children.</summary>
    /// <param name="outline">The built outline.</param>
    /// <returns>The outline node ids in depth-first order.</returns>
    public static IReadOnlyList<string> OutlineOrder(BookOutlineResult outline)
    {
        var order = new List<string>();
        var stack = new Stack<BookOutlineNode>();
        foreach (var root in outline.Layers.Reverse())
        {
            stack.Push(root);
        }

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            order.Add(node.Id);
            foreach (var child in node.Children.AsEnumerable().Reverse())
            {
                stack.Push(child);
            }
        }

        return order;
    }

    /// <summary>
    ///     One outline node as the client walks it: identity, kind, label, type,
    ///     nested children, and the records shard id on a sharded node.
    /// </summary>
    /// <param name="node">The outline node to convert, with its children converted recursively.</param>
    /// <param name="shardOf">The shard identifier for each record identifier.</param>
    /// <returns>The client form of the node with its nested children and its shard identifier when sharded.</returns>
    public static JsonObject OutlineNodeJson(BookOutlineNode node, IReadOnlyDictionary<string, string> shardOf)
    {
        var json = new JsonObject
        {
            ["id"] = node.Id,
            ["kind"] = node.Kind,
            ["label"] = node.Label,
            ["type"] = StringOrNull(node.Type),
        };

        if (!string.Equals(node.Kind, BookOutlineKinds.Group, StringComparison.Ordinal)
            && shardOf.TryGetValue(node.Id, out var shard))
        {
            json["shard"] = shard;
        }

        var children = new JsonArray();
        foreach (var child in node.Children)
        {
            children.Add((JsonNode)OutlineNodeJson(child, shardOf));
        }

        json["children"] = children;
        return json;
    }

    /// <summary>
    ///     Adds one layer's top nodes as sequential <c>outline-&lt;layer&gt;-&lt;n&gt;.js</c>
    ///     shards, splitting past the outline budget, and returns the shard keys in
    ///     order. An empty layer adds no shard; the client reads an empty list.
    /// </summary>
    /// <param name="into">The book files the outline shards join.</param>
    /// <param name="layer">The layer key, which names the shard files.</param>
    /// <param name="tops">The top outline nodes of the layer.</param>
    /// <param name="shardOf">The shard identifier for each record identifier.</param>
    /// <returns>The outline shard keys in order, empty when the layer holds no node.</returns>
    public static IReadOnlyList<string> AddLayer(
        List<BookFile> into,
        string layer,
        IReadOnlyList<BookOutlineNode> tops,
        IReadOnlyDictionary<string, string> shardOf)
    {
        var texts = tops.Select(node => OutlineNodeJson(node, shardOf).ToJsonString()).ToList();
        var packs = Pack(texts, text => Encoding.UTF8.GetByteCount(text), OutlineSplitBytes);
        var keys = new List<string>();
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"outline-{layer}-{shard}";
            into.Add(BookFile.Of(DataPath(key), Utf8NoBom.GetBytes(ShardScriptFromJson(key, "[" + string.Join(",", packs[shard]) + "]") + "\n")));
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>
    ///     Adds the search rows as sequential <c>search-&lt;n&gt;.js</c> shards
    ///     packed like records, and returns the shard keys in order.
    /// </summary>
    /// <param name="into">The book files the search shards join.</param>
    /// <param name="rows">The search rows to pack, each holding identifier, title, type and excerpt.</param>
    /// <returns>The search shard keys in order, empty when no row is given.</returns>
    public static IReadOnlyList<string> AddSearch(List<BookFile> into, JsonArray rows)
    {
        var items = rows.OfType<JsonNode>().Select(row => row.ToJsonString()).ToList();
        var packs = Pack(items, text => Encoding.UTF8.GetByteCount(text), PackTargetBytes);
        var keys = new List<string>();
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"search-{shard}";
            into.Add(BookFile.Of(DataPath(key), Utf8NoBom.GetBytes(ShardScriptFromJson(key, "[" + string.Join(",", packs[shard]) + "]") + "\n")));
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>The fields the search excerpt prefers, in order.</summary>
    static readonly IReadOnlyList<string> ExcerptFields =
        ["statement", "summary", "question", "conclusion"];

    /// <summary>
    ///     One record's search excerpt: its first preferred field, else its
    ///     explanation's head, flattened to one line and cut to 200 characters.
    /// </summary>
    /// <param name="record">The record object that holds the fields and the explanation.</param>
    /// <returns>The flattened excerpt cut to 200 characters, or an empty string when the record holds no preferred field or explanation.</returns>
    public static string SearchExcerpt(JsonObject record)
    {
        var fields = record["fields"] as JsonObject;
        foreach (var name in ExcerptFields)
        {
            if (fields?[name] is JsonValue scalar
                && scalar.TryGetValue<string>(out var text)
                && text.Length > 0)
            {
                return Flatten(text);
            }
        }

        if (record["explanation"] is JsonValue body
            && body.TryGetValue<string>(out var prose)
            && prose.Length > 0)
        {
            return Flatten(BookBodyDetail.WithoutTitle(prose));
        }

        return "";
    }

    /// <summary>One excerpt on one line: whitespace runs become one space, then the head is cut.</summary>
    /// <param name="text">The excerpt text with arbitrary whitespace.</param>
    /// <returns>The text on one line with whitespace runs as single spaces, cut to 200 characters.</returns>
    static string Flatten(string text)
    {
        var single = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return single.Length > 200 ? single[..200] : single;
    }

    /// <summary>A JSON string, or JSON null when the text is absent.</summary>
    /// <param name="value">The text to convert, or null.</param>
    /// <returns>A JSON string node for the text, or null when the text is null.</returns>
    static JsonNode? StringOrNull(string? value) => BookRender.StrNode(value);
}
