using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>The OpenAPI operations <c>litespec api explore</c> walks: listed from the document with resolved schemas.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreOpenApi
{
    /// <summary>Lists the callable operations in the OpenAPI document.</summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="denies">The deny entries with their reasons.</param>
    /// <param name="skipped">Collects the denied templates with their reasons, without duplicates.</param>
    /// <returns>The callable operations in document order.</returns>
    internal static List<ExploreOperation> ParseOperations(
        JsonObject document,
        IReadOnlyList<(ExploreDeny Deny, string Reason)> denies,
        List<(string Template, string Reason)> skipped)
    {
        var operations = new List<ExploreOperation>();
        var components = document["components"] as JsonObject ?? new JsonObject();
        var schemas = components["schemas"] as JsonObject ?? new JsonObject();
        var requestBodies = components["requestBodies"] as JsonObject ?? new JsonObject();
        var parameters = components["parameters"] as JsonObject ?? new JsonObject();

        if (document["paths"] is not JsonObject paths)
        {
            return operations;
        }

        foreach (var path in paths)
        {
            if (path.Value is not JsonObject item)
            {
                continue;
            }

            foreach (var candidate in item)
            {
                if (candidate.Value is not JsonObject operation || !TryGetMethod(candidate.Key, out var method))
                {
                    continue;
                }

                if (IsSkippedRoute(path.Key))
                {
                    continue;
                }

                if (IsDenied(method, path.Key, denies, out var reason))
                {
                    if (!skipped.Contains((path.Key, reason)))
                    {
                        skipped.Add((path.Key, reason));
                    }

                    continue;
                }

                var name = operation["operationId"] is JsonValue id
                    && id.TryGetValue<string>(out var parsed)
                    && !string.IsNullOrWhiteSpace(parsed)
                    ? parsed
                    : $"{method} {path.Key}";

                var (requestSchema, requestContentType) = RequestBody(operation, requestBodies, schemas);
                operations.Add(new ExploreOperation(
                    method,
                    path.Key,
                    name,
                    PathParameterNames(path.Key),
                    QueryParameters(operation, item, parameters, schemas),
                    requestSchema,
                    requestContentType));
            }
        }

        return operations;
    }

    /// <summary>Maps an OpenAPI verb to its HTTP method.</summary>
    /// <param name="verb">The verb, as written in the document.</param>
    /// <param name="method">The HTTP method in upper case.</param>
    /// <returns>Whether the verb names an HTTP method the walk calls.</returns>
    internal static bool TryGetMethod(string verb, out string method)
    {
        string? found = verb.ToLowerInvariant() switch
        {
            "get" => "GET",
            "head" => "HEAD",
            "options" => "OPTIONS",
            "trace" => "TRACE",
            "post" => "POST",
            "put" => "PUT",
            "patch" => "PATCH",
            "delete" => "DELETE",
            _ => null,
        };
        method = found ?? string.Empty;
        return found is not null;
    }

    /// <summary>Reads the parameter names from a path template.</summary>
    /// <param name="template">The path template.</param>
    /// <returns>The names in braces, in template order.</returns>
    internal static IReadOnlyList<string> PathParameterNames(string template)
    {
        var names = new List<string>();
        var span = template.AsSpan();
        var index = 0;

        while (index < span.Length)
        {
            var open = span[index..].IndexOf('{');

            if (open < 0)
            {
                break;
            }

            var absoluteOpen = index + open;
            var close = span[(absoluteOpen + 1)..].IndexOf('}');

            if (close < 0)
            {
                break;
            }

            var name = span.Slice(absoluteOpen + 1, close).ToString();

            if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
            {
                names.Add(name);
            }

            index = absoluteOpen + 1 + close + 1;
        }

        return names;
    }

    /// <summary>Reads the query parameters of an operation, path-level first.</summary>
    /// <param name="operation">The operation object.</param>
    /// <param name="item">The path item object.</param>
    /// <param name="components">The shared parameters.</param>
    /// <param name="schemas">The shared schemas.</param>
    /// <returns>The query parameters.</returns>
    static IReadOnlyList<ExploreParameter> QueryParameters(
        JsonObject operation,
        JsonObject item,
        JsonObject components,
        JsonObject schemas)
    {
        var found = new Dictionary<string, ExploreParameter>(StringComparer.OrdinalIgnoreCase);
        CollectParameters(item["parameters"], found, components, schemas);
        CollectParameters(operation["parameters"], found, components, schemas);
        return [.. found.Values];
    }

    /// <summary>Collects the query parameters of one parameter list.</summary>
    /// <param name="list">The parameter list, or null.</param>
    /// <param name="found">Collects the parameters by name.</param>
    /// <param name="components">The shared parameters.</param>
    /// <param name="schemas">The shared schemas.</param>
    static void CollectParameters(
        JsonNode? list,
        Dictionary<string, ExploreParameter> found,
        JsonObject components,
        JsonObject schemas)
    {
        if (list is not JsonArray array)
        {
            return;
        }

        foreach (var entry in array)
        {
            var parameter = DereferenceParameter(entry as JsonObject, components);

            if (parameter is null)
            {
                continue;
            }

            if (!string.Equals(JsonString(parameter["in"]), "query", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = JsonString(parameter["name"]);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var required = parameter["required"] is JsonValue flag
                && flag.TryGetValue<bool>(out var needed)
                && needed;
            var schema = ResolveSchema(parameter["schema"], schemas, new HashSet<string>(StringComparer.Ordinal));
            found[name] = new ExploreParameter(name, required, schema);
        }
    }

    /// <summary>Follows a parameter reference to the shared parameter it names.</summary>
    /// <param name="node">The parameter object, or null.</param>
    /// <param name="components">The shared parameters.</param>
    /// <returns>The referenced parameter, or the object itself.</returns>
    static JsonObject? DereferenceParameter(JsonObject? node, JsonObject components)
    {
        const string Prefix = "#/components/parameters/";
        var current = node;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (current is not null)
        {
            var reference = JsonString(current["$ref"]);

            if (string.IsNullOrWhiteSpace(reference) || !seen.Add(reference))
            {
                return current;
            }

            if (!reference.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return current;
            }

            current = components[reference[Prefix.Length..]] as JsonObject;
        }

        return null;
    }

    /// <summary>Reads the resolved JSON request schema of an operation with the media type that declares it.</summary>
    /// <param name="operation">The operation object.</param>
    /// <param name="requestBodies">The shared request bodies.</param>
    /// <param name="schemas">The shared schemas.</param>
    /// <returns>The resolved schema with its media type, or nulls when the call sends no body.</returns>
    static (JsonNode? Schema, string? ContentType) RequestBody(
        JsonObject operation, JsonObject requestBodies, JsonObject schemas)
    {
        var body = operation["requestBody"] as JsonObject;

        if (body is not null)
        {
            const string Prefix = "#/components/requestBodies/";
            var reference = JsonString(body["$ref"]);

            if (!string.IsNullOrWhiteSpace(reference) && reference.StartsWith(Prefix, StringComparison.Ordinal))
            {
                body = requestBodies[reference[Prefix.Length..]] as JsonObject;
            }
        }

        if (body?["content"] is not JsonObject content)
        {
            return (null, null);
        }

        const string JsonMediaType = "application/json";
        var mediaType = JsonMediaType;
        var media = content[mediaType] as JsonObject;

        if (media is null)
        {
            var declared = content.FirstOrDefault(static pair =>
                pair.Value is JsonObject mediaObject && mediaObject["schema"] is not null);
            media = declared.Value as JsonObject;
            mediaType = declared.Key;
        }

        if (media?["schema"] is not JsonNode schema)
        {
            return (null, null);
        }

        return (ResolveSchema(schema, schemas, new HashSet<string>(StringComparer.Ordinal)), mediaType);
    }

    /// <summary>Resolves the schema references in a copy of a schema.</summary>
    /// <param name="schema">The schema, or null.</param>
    /// <param name="schemas">The shared schemas.</param>
    /// <param name="visiting">The references on the current path, against cycles.</param>
    /// <returns>The resolved copy, or null.</returns>
    static JsonNode? ResolveSchema(JsonNode? schema, JsonObject schemas, HashSet<string> visiting)
    {
        if (schema is null)
        {
            return null;
        }

        var node = schema.DeepClone();
        ResolveInPlace(node, schemas, visiting);
        return node;
    }

    /// <summary>Resolves the schema references in a schema in place.</summary>
    /// <param name="node">The schema.</param>
    /// <param name="schemas">The shared schemas.</param>
    /// <param name="visiting">The references on the current path, against cycles.</param>
    static void ResolveInPlace(JsonNode? node, JsonObject schemas, HashSet<string> visiting)
    {
        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                ResolveInPlace(item, schemas, visiting);
            }

            return;
        }

        if (node is not JsonObject obj)
        {
            return;
        }

        const string Prefix = "#/components/schemas/";
        var reference = JsonString(obj["$ref"]);

        if (!string.IsNullOrWhiteSpace(reference))
        {
            if (reference.StartsWith(Prefix, StringComparison.Ordinal)
                && schemas[reference[Prefix.Length..]] is JsonNode target
                && visiting.Add(reference))
            {
                var resolved = target.DeepClone();
                ResolveInPlace(resolved, schemas, visiting);
                visiting.Remove(reference);
                obj.Remove("$ref");

                if (resolved is JsonObject resolvedObject)
                {
                    foreach (var property in resolvedObject)
                    {
                        obj[property.Key] ??= property.Value?.DeepClone();
                    }
                }
            }
            else
            {
                obj.Remove("$ref");
            }
        }

        foreach (var property in obj)
        {
            ResolveInPlace(property.Value, schemas, visiting);
        }
    }

    /// <summary>Whether a deny entry skips a route.</summary>
    /// <param name="method">The HTTP method in upper case.</param>
    /// <param name="template">The path template.</param>
    /// <param name="denies">The deny entries with their reasons.</param>
    /// <param name="reason">Why the route is denied.</param>
    /// <returns>Whether a deny entry matches.</returns>
    static bool IsDenied(string method, string template, IReadOnlyList<(ExploreDeny Deny, string Reason)> denies, out string reason)
    {
        foreach (var (deny, denied) in denies)
        {
            if (deny.Matches(method, template))
            {
                reason = denied;
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>Whether a route is API plumbing the walk skips silently.</summary>
    /// <param name="template">The path template.</param>
    /// <returns>Whether the route serves the OpenAPI document or health.</returns>
    static bool IsSkippedRoute(string template) =>
        template.Contains("/openapi", StringComparison.OrdinalIgnoreCase)
        || template.Contains("/health", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads a string from a JSON node.</summary>
    /// <param name="node">The node, or null.</param>
    /// <returns>The string, or null.</returns>
    static string? JsonString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
