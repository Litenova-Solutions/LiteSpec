using System.Text;
using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The book's sharded data: record packs, outline packs, search packs and
///     single-value shards, each written as one script-tag-callable file that
///     calls <c>BookData.put</c> once. Shards load through script elements,
///     never through <c>fetch</c>, so the book opens under <c>file://</c>.
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

    /// <summary>The UTF-8 encoding without a byte order mark, used for the written shard files.</summary>
    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>The compact JSON of one node in UTF-8 bytes, the unit every packing budget counts in.</summary>
    /// <param name="node">The JSON node to measure, or null.</param>
    /// <returns>The byte count of the node compact JSON in UTF-8, or 4 for the null value.</returns>
    public static int Utf8Bytes(JsonNode? node) =>
        node is null ? 4 : Encoding.UTF8.GetByteCount(node.ToJsonString());

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
    public static string ShardScript(string key, JsonNode value)
    {
        var json = value.ToJsonString().Replace("</", "<\\/", StringComparison.Ordinal);
        json = json.Replace(Separator(0x2028), "\\u2028", StringComparison.Ordinal);
        json = json.Replace(Separator(0x2029), "\\u2029", StringComparison.Ordinal);
        return $"BookData.put({JsonValue.Create(key)!.ToJsonString()}, {json});";
    }

    /// <summary>One Unicode line separator from its code point, kept out of the source as text.</summary>
    /// <param name="code">The Unicode code point of the separator character.</param>
    /// <returns>The one-character string for the code point.</returns>
    static string Separator(int code) => ((char)code).ToString();

    /// <summary>Writes <c>data/&lt;key&gt;.js</c> holding one shard script.</summary>
    /// <param name="dataDir">The data folder that receives the shard file, created when missing.</param>
    /// <param name="key">The shard key, which names the file and the client value.</param>
    /// <param name="value">The shard payload value.</param>
    public static void WriteShard(string dataDir, string key, JsonNode value)
    {
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, key + ".js"), ShardScript(key, value) + "\n", Utf8NoBom);
    }

    /// <summary>
    ///     Writes the record packs in identifier order as <c>records-&lt;nnn&gt;.js</c>,
    ///     and returns the shard id per record id for the manifest and the outline.
    /// </summary>
    /// <param name="dataDir">The data folder that receives the record shard files.</param>
    /// <param name="details">The enriched record details keyed by record identifier.</param>
    /// <returns>The shard identifier for each record identifier packed.</returns>
    public static IReadOnlyDictionary<string, string> WriteRecords(string dataDir, IReadOnlyDictionary<string, JsonObject> details)
    {
        var ids = details.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
        var sizes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            sizes[id] = Utf8Bytes(details[id]) + Encoding.UTF8.GetByteCount(id) + 8;
        }

        var shardOf = new Dictionary<string, string>(StringComparer.Ordinal);
        var packs = Pack(ids, id => sizes[id], PackTargetBytes);
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"records-{shard:000}";
            var pack = new JsonObject();
            foreach (var id in packs[shard])
            {
                pack[id] = (JsonObject)details[id].DeepClone();
                shardOf[id] = key;
            }

            WriteShard(dataDir, key, pack);
        }

        return shardOf;
    }

    /// <summary>
    ///     One outline node as the client walks it: identity, kind, label, type,
    ///     status, nested children, and the records shard id on a sharded node.
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
            ["status"] = StringOrNull(node.Status),
        };

        if (!string.Equals(node.Kind, "group", StringComparison.Ordinal)
            && shardOf.TryGetValue(node.Id, out var shard))
        {
            json["shard"] = shard;
        }

        var children = new JsonArray();
        foreach (var child in node.Children)
        {
            children.Add(OutlineNodeJson(child, shardOf));
        }

        json["children"] = children;
        return json;
    }

    /// <summary>
    ///     Writes one layer's top nodes as sequential <c>outline-&lt;layer&gt;-&lt;n&gt;.js</c>
    ///     shards, splitting past the outline budget, and returns the shard keys in
    ///     order. An empty layer writes no shard; the client reads an empty list.
    /// </summary>
    /// <param name="dataDir">The data folder that receives the outline shard files.</param>
    /// <param name="layer">The layer key, which names the shard files.</param>
    /// <param name="tops">The top outline nodes of the layer.</param>
    /// <param name="shardOf">The shard identifier for each record identifier.</param>
    /// <returns>The outline shard keys in order, empty when the layer holds no node.</returns>
    public static IReadOnlyList<string> WriteLayer(
        string dataDir,
        string layer,
        IReadOnlyList<BookOutlineNode> tops,
        IReadOnlyDictionary<string, string> shardOf)
    {
        var nodes = tops.Select(node => OutlineNodeJson(node, shardOf)).ToList();
        var packs = Pack(nodes, Utf8Bytes, OutlineSplitBytes);
        var keys = new List<string>();
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"outline-{layer}-{shard}";
            var array = new JsonArray();
            foreach (var node in packs[shard])
            {
                array.Add(node);
            }

            WriteShard(dataDir, key, array);
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>
    ///     Writes the search rows as sequential <c>search-&lt;n&gt;.js</c> shards
    ///     packed like records, and returns the shard keys in order.
    /// </summary>
    /// <param name="dataDir">The data folder that receives the search shard files.</param>
    /// <param name="rows">The search rows to pack, each holding identifier, title, type and excerpt.</param>
    /// <returns>The search shard keys in order, empty when no row is given.</returns>
    public static IReadOnlyList<string> WriteSearch(string dataDir, JsonArray rows)
    {
        var items = rows.OfType<JsonNode>().ToList();
        var packs = Pack(items, Utf8Bytes, PackTargetBytes);
        var keys = new List<string>();
        for (var shard = 0; shard < packs.Count; shard++)
        {
            var key = $"search-{shard}";
            var array = new JsonArray();
            foreach (var row in packs[shard])
            {
                array.Add(row.DeepClone());
            }

            WriteShard(dataDir, key, array);
            keys.Add(key);
        }

        return keys;
    }

    /// <summary>The statement fields the search excerpt prefers, in order.</summary>
    static readonly IReadOnlyList<string> ExcerptFields =
        ["statement", "summary", "question", "conclusion"];

    /// <summary>
    ///     One record's search excerpt: its first statement-like field, else its
    ///     body's head, flattened to one line and cut to 200 characters.
    /// </summary>
    /// <param name="record">The record object that holds the fields and the body.</param>
    /// <returns>The flattened excerpt cut to 200 characters, or an empty string when the record holds no statement-like field or body.</returns>
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

        if (record["body"] is JsonValue body
            && body.TryGetValue<string>(out var prose)
            && prose.Length > 0)
        {
            return Flatten(prose);
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
