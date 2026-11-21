using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Book.Lexicon;

/// <summary>
///     Reads the connection phrases from the carried record schema. Every link property carries
///     <c>x-reads-as</c> (the phrase from the record that writes it) and <c>x-read-back-as</c>
///     (the phrase from the target); the book takes each connection label from these, never from
///     a list written in the book.
/// </summary>
/// <follows>litespec/core/model#name-book-labels-with-litespec-terms</follows>
static class BookLexiconSchema
{
    /// <summary>The schema keyword holding the phrase read from the carrying record.</summary>
    const string ReadsAs = "x-reads-as";

    /// <summary>The schema keyword holding the phrase read back from the target.</summary>
    const string ReadBackAs = "x-read-back-as";

    /// <summary>The page definition suffix that names the record type a page holds.</summary>
    const string PageSuffix = "Page";

    /// <summary>The schema keywords that hold alternative subschemas, which a link can sit under.</summary>
    static readonly string[] CombinatorKeys = ["allOf", "oneOf", "anyOf"];

    /// <summary>The reference prefix of the schema's own definitions.</summary>
    const string DefsPrefix = "#/$defs/";

    /// <summary>Reads every annotated link property from the schema, one entry per field and phrase pair.</summary>
    /// <param name="schemaFile">The carried record schema.</param>
    /// <returns>The link phrases, in schema order, each with the record types that carry it.</returns>
    /// <exception cref="BookLexiconException">The schema cannot be read or holds no definitions.</exception>
    public static List<LexiconLink> Links(string schemaFile)
    {
        JsonNode? schema;
        try
        {
            schema = RecordText.Parse(File.ReadAllText(schemaFile), yaml: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonTextException)
        {
            throw new BookLexiconException(schemaFile, $"Cannot read '{schemaFile}': {exception.Message}");
        }

        if (schema?["$defs"] is not JsonObject defs)
        {
            throw new BookLexiconException(schemaFile, $"'{schemaFile}' holds no $defs.");
        }

        var found = new List<(string Field, string Carrier, string Forward, string Inverse)>();
        foreach (var (name, definition) in defs)
        {
            if (name.EndsWith(PageSuffix, StringComparison.Ordinal))
            {
                var carrier = KebabOf(name[..^PageSuffix.Length]);
                Collect(definition, "", carrier, defs, new HashSet<string> { name }, found);
            }
        }

        // One entry per field and phrase pair, carried by every record type that writes it.
        var order = new List<(string Field, string Forward, string Inverse)>();
        var carriers = new Dictionary<(string Field, string Forward, string Inverse), List<string>>();
        foreach (var (field, carrier, forward, inverse) in found)
        {
            var key = (field, forward, inverse);
            if (!carriers.TryGetValue(key, out var list))
            {
                list = [];
                carriers[key] = list;
                order.Add(key);
            }

            if (!list.Contains(carrier, StringComparer.Ordinal))
            {
                list.Add(carrier);
            }
        }

        return [.. order.Select(key => new LexiconLink(key.Field, carriers[key], key.Forward, key.Inverse))];
    }

    /// <summary>The record type a typed entry definition names in its <c>type</c> constant, or null when it names none.</summary>
    /// <param name="node">The schema definition to read.</param>
    /// <returns>The record type the definition holds, or null when its <c>type</c> is not a record type.</returns>
    static string? TypedCarrier(JsonNode? node) =>
        node is JsonObject definition
        && definition["properties"] is JsonObject properties
        && properties["type"] is JsonObject type
        && type["const"] is JsonValue value
        && value.TryGetValue<string>(out var text)
        && RecordTypes.All.Contains(text)
            ? text
            : null;

    /// <summary>
    ///     Walks one schema node and its references, recording every property that carries both
    ///     phrases under its dotted path from the page.
    /// </summary>
    /// <param name="node">The schema node to walk.</param>
    /// <param name="prefix">The dotted path of the node from the page, empty at the page itself.</param>
    /// <param name="carrier">The record type the page holds.</param>
    /// <param name="defs">The schema definitions that references resolve against.</param>
    /// <param name="active">The definitions on the current walk, so a reference cycle ends.</param>
    /// <param name="found">The phrases found so far.</param>
    static void Collect(
        JsonNode? node,
        string prefix,
        string carrier,
        JsonObject defs,
        HashSet<string> active,
        List<(string Field, string Carrier, string Forward, string Inverse)> found)
    {
        if (node is not JsonObject obj)
        {
            return;
        }

        if (obj["$ref"] is JsonValue reference
            && reference.TryGetValue<string>(out var target)
            && target.StartsWith(DefsPrefix, StringComparison.Ordinal))
        {
            var name = target[DefsPrefix.Length..];
            if (defs[name] is { } referenced && active.Add(name))
            {
                // A typed entry, such as a rule in a rule set, is the record that writes its own fields, so its field names start at the record.
                var typed = TypedCarrier(referenced);
                Collect(referenced, typed is null ? prefix : "", typed ?? carrier, defs, active, found);
                active.Remove(name);
            }

            return;
        }

        if (obj["properties"] is JsonObject properties)
        {
            foreach (var (property, child) in properties)
            {
                var path = prefix.Length == 0 ? property : prefix + "." + property;
                if (child is JsonObject childObject
                    && childObject[ReadsAs] is JsonValue reads
                    && childObject[ReadBackAs] is JsonValue back
                    && reads.TryGetValue<string>(out var forward)
                    && back.TryGetValue<string>(out var inverse))
                {
                    found.Add((path, carrier, forward, inverse));
                }

                Collect(child, path, carrier, defs, active, found);
            }
        }

        if (obj["items"] is { } items)
        {
            Collect(items, prefix, carrier, defs, active, found);
        }

        if (obj["of"] is { } of)
        {
            Collect(of, prefix, carrier, defs, active, found);
        }

        foreach (var key in CombinatorKeys)
        {
            if (obj[key] is JsonArray alternatives)
            {
                foreach (var alternative in alternatives)
                {
                    Collect(alternative, prefix, carrier, defs, active, found);
                }
            }
        }
    }

    /// <summary>A camel-case definition name as a kebab-case record type, such as use-case for useCase.</summary>
    /// <param name="camel">The camel-case name.</param>
    /// <returns>The kebab-case name.</returns>
    static string KebabOf(string camel)
    {
        var text = new StringBuilder();
        for (var i = 0; i < camel.Length; i++)
        {
            var ch = camel[i];
            if (char.IsUpper(ch))
            {
                if (i > 0)
                {
                    text.Append('-');
                }

                text.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                text.Append(ch);
            }
        }

        return text.ToString();
    }
}
