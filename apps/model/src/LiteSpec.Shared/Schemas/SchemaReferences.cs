using System.Globalization;
using System.Text.Json.Nodes;

namespace LiteSpec.Schemas;

/// <summary>
///     Local schema references: resolution of a <c>#/...</c> pointer against the
///     schema root.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaReferences
{
    /// <summary>
    ///     Resolves a local <c>#/...</c> reference against the schema root, unescaping
    ///     <c>~1</c> to <c>/</c> and <c>~0</c> to <c>~</c> in each pointer segment.
    /// </summary>
    /// <param name="root">The schema root.</param>
    /// <param name="reference">The local reference.</param>
    /// <returns>The referenced schema, or null when it does not resolve.</returns>
    /// <exception cref="InvalidOperationException">The reference does not start with <c>#/</c>.</exception>
    internal static JsonNode? ResolveRef(JsonNode root, string reference)
    {
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"unsupported schema reference '{reference}'");
        }

        JsonNode? current = root;

        foreach (var segment in reference[2..].Split('/'))
        {
            var key = segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

            if (current is JsonObject currentObject)
            {
                current = currentObject.TryGetPropertyValue(key, out var child) ? child : null;
            }
            else if (current is JsonArray currentArray && TryArrayIndex(key, currentArray, out var element))
            {
                current = element;
            }
            else
            {
                current = null;
            }
        }

        return current;
    }

    /// <summary>Tries to read an array index from a pointer segment.</summary>
    /// <param name="key">The pointer segment.</param>
    /// <param name="array">The array to index.</param>
    /// <param name="element">The element, when the segment is a valid index.</param>
    /// <returns>True when the segment is a valid index.</returns>
    internal static bool TryArrayIndex(string key, JsonArray array, out JsonNode? element)
    {
        element = null;

        if (key.Length == 0 || (key.Length > 1 && key[0] == '0'))
        {
            return false;
        }

        foreach (var ch in key)
        {
            if (ch < '0' || ch > '9')
            {
                return false;
            }
        }

        if (!int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= array.Count)
        {
            return false;
        }

        element = array[index];
        return true;
    }
}
