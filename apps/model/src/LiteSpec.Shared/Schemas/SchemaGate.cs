using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace LiteSpec.Schemas;

/// <summary>
///     The keyword gate: every keyword the evaluator does not implement fails the
///     schema instead of passing unread.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaGate
{
    // Every position a subschema can occupy, so the gate reaches each one. A keyword
    // the gate never descends into hides its whole subtree from the check.
    /// <summary>Gets the subschema positions that hold a list of schemas.</summary>
    private static readonly string[] ChildLists = ["allOf", "anyOf", "oneOf", "prefixItems"];

    /// <summary>Gets the subschema positions that hold a map of schemas.</summary>
    private static readonly string[] ChildMaps = ["properties", "patternProperties", "$defs"];

    /// <summary>Gets the subschema positions that hold one schema.</summary>
    private static readonly string[] ChildValues = ["items", "contains", "propertyNames", "additionalProperties", "if", "then", "else", "not"];

    /// <summary>Gets the gate messages cached per schema root.</summary>
    private static readonly ConditionalWeakTable<JsonNode, string[]> GateCache = new();

    /// <summary>
    ///     Reports each keyword the evaluator does not implement, descending into every
    ///     subschema position. <paramref name="add"/> takes the relative path, line, code
    ///     and message.
    /// </summary>
    /// <param name="schema">The schema to gate.</param>
    /// <param name="relative">The schema file, relative to its root.</param>
    /// <param name="pointer">The JSON pointer of the schema.</param>
    /// <param name="add">The diagnostic sink, taking the relative path, line, code, and message.</param>
    internal static void UnsupportedKeywords(JsonNode? schema, string relative, string pointer, Action<string, int, string, string> add)
    {
        if (schema is not JsonObject obj)
        {
            return;
        }

        foreach (var pair in obj)
        {
            if (!JsonSchema.Keywords.Contains(pair.Key))
            {
                add(relative, 1, "SCHEMA_UNSUPPORTED_KEYWORD", $"unsupported JSON Schema keyword '{pair.Key}' at '{pointer}'");
            }
        }

        foreach (var key in ChildLists)
        {
            if (!obj.TryGetPropertyValue(key, out var children) || children is not JsonArray list)
            {
                continue;
            }

            for (var index = 0; index < list.Count; index++)
            {
                UnsupportedKeywords(list[index], relative, $"{pointer}/{key}/{index}", add);
            }
        }

        foreach (var key in ChildMaps)
        {
            if (!obj.TryGetPropertyValue(key, out var map))
            {
                continue;
            }

            if (map is JsonObject fields)
            {
                foreach (var (name, child) in fields)
                {
                    UnsupportedKeywords(child, relative, $"{pointer}/{key}/{name}", add);
                }
            }
            else if (map is JsonArray entries)
            {
                // A malformed schema can hold an array where a map belongs; the gate
                // then reads it by index, so every entry is still checked.
                for (var index = 0; index < entries.Count; index++)
                {
                    UnsupportedKeywords(entries[index], relative, $"{pointer}/{key}/{index}", add);
                }
            }
        }

        foreach (var key in ChildValues)
        {
            if (!obj.TryGetPropertyValue(key, out var child))
            {
                continue;
            }

            if (child is JsonObject or JsonArray)
            {
                UnsupportedKeywords(child, relative, $"{pointer}/{key}", add);
            }
        }
    }

    /// <summary>
    ///     The keyword gate's messages for a schema, walked once per schema root.
    ///     The caller prefixes each one with its own location.
    /// </summary>
    /// <param name="schema">The schema to gate.</param>
    /// <param name="schemaFile">The schema file name.</param>
    /// <returns>The gate messages.</returns>
    internal static string[] GateMessages(JsonNode schema, string schemaFile) =>
        GateCache.GetValue(schema, _ =>
        {
            var found = new List<string>();
            UnsupportedKeywords(schema, schemaFile, "#", (_, _, _, message) => found.Add(message));
            return found.ToArray();
        });
}
