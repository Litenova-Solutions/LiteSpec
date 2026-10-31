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

    static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    /// <summary>The compact JSON of one node in UTF-8 bytes, the unit every packing budget counts in.</summary>
    public static int Utf8Bytes(JsonNode? node) =>
        node is null ? 4 : Encoding.UTF8.GetByteCount(node.ToJsonString());

    /// <summary>
    ///     Packs items in order into shards: each shard fills to its target, then a
    ///     new shard starts. One item past the target keeps its own shard whole,
    ///     since a record is never split; the cap test names it instead.
    /// </summary>
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
    public static string ShardScript(string key, JsonNode value)
    {
        var json = value.ToJsonString().Replace("</", "<\\/", StringComparison.Ordinal);
        json = json.Replace(Separator(0x2028), "\\u2028", StringComparison.Ordinal);
        json = json.Replace(Separator(0x2029), "\\u2029", StringComparison.Ordinal);
        return $"BookData.put({JsonValue.Create(key)!.ToJsonString()}, {json});";
    }

    /// <summary>One Unicode line separator from its code point, kept out of the source as text.</summary>
    static string Separator(int code) => ((char)code).ToString();

    /// <summary>Writes <c>data/&lt;key&gt;.js</c> holding one shard script.</summary>
    public static void WriteShard(string dataDir, string key, JsonNode value)
    {
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, key + ".js"), ShardScript(key, value) + "\n", Utf8NoBom);
    }

    /// <summary>
    ///     Writes the record packs in identifier order as <c>records-&lt;nnn&gt;.js</c>,
    ///     and returns the shard id per record id for the manifest and the outline.
    /// </summary>
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
    static string Flatten(string text)
    {
        var single = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return single.Length > 200 ? single[..200] : single;
    }

    /// <summary>A JSON string, or JSON null when the text is absent.</summary>
    static JsonNode? StringOrNull(string? value) => BookRender.StrNode(value);
}
