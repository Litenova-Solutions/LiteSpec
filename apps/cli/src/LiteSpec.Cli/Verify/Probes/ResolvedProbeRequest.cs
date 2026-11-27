using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     One probe request with every placeholder resolved: the HTTP method, the
///     path from the base address and the serialized JSON body.
/// </summary>
/// <param name="Method">The HTTP method, such as POST.</param>
/// <param name="Path">The resolved path from the base address.</param>
/// <param name="BodyJson">The resolved body as JSON text, or null for none.</param>
internal sealed record ResolvedProbeRequest(string Method, string Path, string? BodyJson)
{
    /// <summary>Resolves one probe request from its template and the verify values.</summary>
    /// <param name="probe">The probe holding the request template.</param>
    /// <param name="values">The fixed values a path or body names as {name}.</param>
    /// <param name="personShort">The short person name for {person}, or null.</param>
    /// <returns>The resolved method, path and body.</returns>
    public static ResolvedProbeRequest From(ProbeCase probe, IReadOnlyDictionary<string, string> values, string? personShort)
    {
        var path = Replace(probe.Path, values, personShort);
        string? bodyJson = null;

        if (probe.Body is not null)
        {
            bodyJson = ResolveNode(probe.Body.DeepClone(), values, personShort).ToJsonString();
        }

        return new ResolvedProbeRequest(probe.Method, path, bodyJson);
    }

    /// <summary>Resolves the verify sign-in path with the person and the values.</summary>
    /// <param name="template">The sign-in path template, with {person} and {name} placeholders.</param>
    /// <param name="values">The fixed values a path names as {name}.</param>
    /// <param name="personShort">The short person name, without the person/ prefix.</param>
    /// <returns>The resolved path.</returns>
    public static string ResolvePath(string template, IReadOnlyDictionary<string, string> values, string personShort) =>
        Replace(template, values, personShort);

    /// <summary>Resolves the body of one verify request with the person and the values.</summary>
    /// <param name="body">The body template, or null for none.</param>
    /// <param name="values">The fixed values a body names as {name}.</param>
    /// <param name="personShort">The short person name, without the person/ prefix.</param>
    /// <returns>The resolved body as JSON text, or null for none.</returns>
    public static string? ResolveBody(JsonNode? body, IReadOnlyDictionary<string, string> values, string? personShort) =>
        body is null ? null : ResolveNode(body.DeepClone(), values, personShort).ToJsonString();

    /// <summary>Replaces every {name} placeholder with the matching value and {person} with the short name.</summary>
    /// <param name="template">The text holding the placeholders.</param>
    /// <param name="values">The fixed values by name.</param>
    /// <param name="personShort">The short person name, or null to keep {person}.</param>
    /// <returns>The text with the known placeholders replaced.</returns>
    private static string Replace(string template, IReadOnlyDictionary<string, string> values, string? personShort)
    {
        var resolved = template;

        if (personShort is { Length: > 0 })
        {
            resolved = resolved.Replace("{person}", personShort, StringComparison.Ordinal);
        }

        foreach (var (name, value) in values)
        {
            resolved = resolved.Replace("{" + name + "}", value, StringComparison.Ordinal);
        }

        return resolved;
    }

    /// <summary>Resolves every placeholder inside a JSON node, in strings at any depth.</summary>
    /// <param name="node">The node to resolve, modified in place.</param>
    /// <param name="values">The fixed values by name.</param>
    /// <param name="personShort">The short person name, or null to keep {person}.</param>
    /// <returns>The resolved node.</returns>
    private static JsonNode ResolveNode(JsonNode node, IReadOnlyDictionary<string, string> values, string? personShort)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text) && text is not null)
        {
            return JsonValue.Create(Replace(text, values, personShort))!;
        }

        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(entry => entry.Key).ToList())
            {
                var resolved = ResolveNode(obj[key]!, values, personShort);

                if (!ReferenceEquals(resolved, obj[key]))
                {
                    obj[key] = resolved;
                }
            }

            return obj;
        }

        if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var resolved = ResolveNode(array[index]!, values, personShort);

                if (!ReferenceEquals(resolved, array[index]))
                {
                    array[index] = resolved;
                }
            }

            return array;
        }

        return node;
    }
}
