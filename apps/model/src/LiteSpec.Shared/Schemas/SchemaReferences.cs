using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using static LiteSpec.Schemas.JsonSchemaSupport;

namespace LiteSpec.Schemas;

/// <summary>
///     Local schema references: resolution of a <c>#/...</c> pointer against the
///     schema root, and the <c>$ref</c> keyword.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class SchemaReferences
{
    /// <summary>Gets the resolved references cached per schema root.</summary>
    private static readonly ConditionalWeakTable<JsonNode, ConcurrentDictionary<string, JsonNode?>> RefCache = new();

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

        foreach (var segment in reference.Substring(2).Split('/'))
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

    /// <summary>
    ///     Resolves a local reference once per schema root and reuses the target: the
    ///     same root resolves the same reference for every value it validates.
    /// </summary>
    /// <param name="root">The schema root.</param>
    /// <param name="reference">The local reference.</param>
    /// <returns>The referenced schema, or null when it does not resolve.</returns>
    internal static JsonNode? CachedRef(JsonNode root, string reference)
    {
        var resolved = RefCache.GetValue(root, _ => new ConcurrentDictionary<string, JsonNode?>(StringComparer.Ordinal));

        if (resolved.TryGetValue(reference, out var target))
        {
            return target;
        }

        target = ResolveRef(root, reference);

        if (target is not null)
        {
            resolved.TryAdd(reference, target);
        }

        return target;
    }

    /// <summary>Validates a value through the <c>$ref</c> keyword of a schema.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="schema">The schema object.</param>
    /// <param name="root">The schema root.</param>
    /// <param name="location">The value location.</param>
    /// <param name="errors">The errors to append to.</param>
    internal static void ValidateReference(JsonNode? value, JsonObject schema, JsonNode root, string location, List<string> errors)
    {
        if (schema.TryGetPropertyValue("$ref", out var refNode) && IsTruthy(refNode))
        {
            var reference = refNode is JsonValue refValue && refValue.TryGetValue<string>(out var refText) ? refText : null;

            if (reference is null)
            {
                throw new InvalidOperationException($"unsupported schema reference '{Stringify(refNode)}'");
            }

            var target = CachedRef(root, reference);

            if (!IsTruthy(target))
            {
                errors.Add($"{location}: unresolved schema reference '{reference}'");
            }
            else
            {
                JsonSchema.Validate(value, target, root, location, errors);
            }
        }
    }
}
