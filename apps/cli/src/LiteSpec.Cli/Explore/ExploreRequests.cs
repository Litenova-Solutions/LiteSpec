using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>Builds the concrete paths and JSON bodies of the walk: valid-looking values most of the time, boundary values with a small probability to find unhandled input. Every choice comes from the walk's random source, so the seed reproduces every call.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreRequests
{
    /// <summary>How often a scalar value is a boundary value instead of a valid-looking one.</summary>
    const double BoundaryProbability = 0.08;

    /// <summary>How often an optional object property is sent.</summary>
    const double OptionalPropertyProbability = 0.8;

    /// <summary>How often an optional query parameter is sent.</summary>
    const double OptionalQueryProbability = 0.6;

    /// <summary>How often a nullable value is sent as null.</summary>
    const double NullProbability = 0.05;

    /// <summary>The deepest objects and arrays nest in a built body.</summary>
    const int MaxDepth = 6;

    /// <summary>The length of the very-long-string boundary value.</summary>
    const int VeryLongLength = 5000;

    /// <summary>Builds the concrete path of an operation call, query string included.</summary>
    /// <param name="operation">The operation to call.</param>
    /// <param name="identifiers">The values collected from earlier responses.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The concrete path.</returns>
    internal static string BuildPath(ExploreOperation operation, ExploreIdentifiers identifiers, Random random)
    {
        var path = operation.PathTemplate;

        foreach (var parameter in operation.PathParameters)
        {
            var value = identifiers.Pick(parameter, random) ?? FallbackPathValue(parameter, random);
            path = path.Replace(
                "{" + parameter + "}",
                Uri.EscapeDataString(value),
                StringComparison.Ordinal);
        }

        return path + BuildQueryString(operation, identifiers, random);
    }

    /// <summary>Builds the JSON body of an operation call.</summary>
    /// <param name="operation">The operation to call.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The body, or null when the call sends none.</returns>
    internal static JsonNode? BuildBody(ExploreOperation operation, Random random)
    {
        if (operation.RequestSchema is null)
        {
            return null;
        }

        return BuildValue(operation.RequestSchema, null, random, 0);
    }

    /// <summary>Builds the query string of an operation call.</summary>
    /// <param name="operation">The operation to call.</param>
    /// <param name="identifiers">The values collected from earlier responses.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The query string, starting with <c>?</c>, or empty.</returns>
    static string BuildQueryString(
        ExploreOperation operation,
        ExploreIdentifiers identifiers,
        Random random)
    {
        var parts = new List<string>();

        foreach (var parameter in operation.QueryParameters)
        {
            if (!parameter.Required && random.NextDouble() >= OptionalQueryProbability)
            {
                continue;
            }

            var collected = identifiers.Pick(parameter.Name, random);
            var text = collected ?? QueryText(BuildValue(parameter.Schema, parameter.Name, random, 0));
            parts.Add($"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(text)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    /// <summary>Writes one built value as query text.</summary>
    /// <param name="value">The built value.</param>
    /// <returns>The value without JSON quoting for strings, else its JSON.</returns>
    static string QueryText(JsonNode? value)
    {
        if (value is JsonValue scalar
            && scalar.TryGetValue<string>(out var text)
            && text is not null)
        {
            return text;
        }

        return value?.ToJsonString() ?? string.Empty;
    }

    /// <summary>Builds a fallback path value when nothing collected fits.</summary>
    /// <param name="parameter">The path parameter name.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>A valid-shaped value of the parameter's likely kind.</returns>
    static string FallbackPathValue(string parameter, Random random)
    {
        if (parameter.Contains("year", StringComparison.OrdinalIgnoreCase))
        {
            return "2026";
        }

        if (parameter.Contains("period", StringComparison.OrdinalIgnoreCase))
        {
            return "2026-Q4";
        }

        if (parameter.Contains("quarter", StringComparison.OrdinalIgnoreCase))
        {
            return "Q4";
        }

        if (parameter.Contains("month", StringComparison.OrdinalIgnoreCase))
        {
            return "12";
        }

        if (parameter.Contains("code", StringComparison.OrdinalIgnoreCase))
        {
            return "0000";
        }

        return DeterministicGuid(random);
    }

    /// <summary>Builds one value from a JSON schema.</summary>
    /// <param name="schema">The schema, or null for an open value.</param>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>The built value; a null node means JSON null.</returns>
    static JsonNode? BuildValue(JsonNode? schema, string? name, Random random, int depth)
    {
        if (depth > MaxDepth)
        {
            return JsonValue.Create("sample");
        }

        if (schema is not JsonObject obj)
        {
            return ValidString(null, name, random);
        }

        if (obj["enum"] is JsonArray options && options.Count > 0)
        {
            if (random.NextDouble() < BoundaryProbability)
            {
                return JsonValue.Create("UNKNOWN-CODE");
            }

            return options[random.Next(options.Count)]?.DeepClone() ?? JsonValue.Create("sample");
        }

        if (obj["allOf"] is JsonArray all && all.Count > 0)
        {
            return BuildValue(MergeAllOf(all), name, random, depth + 1);
        }

        if (obj["oneOf"] is JsonArray one && one.Count > 0)
        {
            return BuildValue(one[random.Next(one.Count)], name, random, depth + 1);
        }

        if (obj["anyOf"] is JsonArray any && any.Count > 0)
        {
            return BuildValue(any[random.Next(any.Count)], name, random, depth + 1);
        }

        var types = JsonTypes(obj);

        if (types.Contains("null") && random.NextDouble() < NullProbability)
        {
            return null;
        }

        var effective = types.FirstOrDefault(static type => type != "null") ?? GuessType(obj);

        switch (effective)
        {
            case "object":
                return BuildObject(obj, random, depth);
            case "array":
                return BuildArray(obj, name, random, depth);
            case "boolean":
                return JsonValue.Create(random.Next(2) == 0);
            case "integer":
                return BuildInteger(obj, name, random);
            case "number":
                return BuildNumber(random);
            default:
                return BuildString(obj, name, random);
        }
    }

    /// <summary>Builds an object from its schema.</summary>
    /// <param name="schema">The object schema.</param>
    /// <param name="random">The walk's random source.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>The built object.</returns>
    static JsonObject BuildObject(JsonObject schema, Random random, int depth)
    {
        var built = new JsonObject();
        var required = new HashSet<string>(StringComparer.Ordinal);

        if (schema["required"] is JsonArray names)
        {
            foreach (var item in names)
            {
                if (item is JsonValue value
                    && value.TryGetValue<string>(out var name)
                    && name is not null)
                {
                    required.Add(name);
                }
            }
        }

        if (schema["properties"] is JsonObject properties)
        {
            foreach (var property in properties)
            {
                if (!required.Contains(property.Key) && random.NextDouble() >= OptionalPropertyProbability)
                {
                    continue;
                }

                built[property.Key] = BuildValue(property.Value, property.Key, random, depth + 1);
            }
        }

        return built;
    }

    /// <summary>Builds an array of up to two items from its schema.</summary>
    /// <param name="schema">The array schema.</param>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>The built array.</returns>
    static JsonArray BuildArray(JsonObject schema, string? name, Random random, int depth)
    {
        var built = new JsonArray();
        var items = schema["items"] ?? (schema["prefixItems"] as JsonArray)?[0];
        var count = random.Next(3);

        for (var index = 0; index < count; index++)
        {
            built.Add(BuildValue(items, name, random, depth + 1));
        }

        return built;
    }

    /// <summary>Builds an integer, within the schema bounds when it states any.</summary>
    /// <param name="schema">The integer schema.</param>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The built integer.</returns>
    static JsonValue BuildInteger(JsonObject schema, string? name, Random random)
    {
        if (random.NextDouble() < BoundaryProbability)
        {
            return JsonValue.Create(random.Next(4) switch
            {
                0 => -1L,
                1 => -999999L,
                2 => 0L,
                _ => (long)int.MaxValue,
            });
        }

        if (name is not null && name.Contains("year", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(2026L + random.Next(2));
        }

        var minimum = JsonLong(schema["minimum"], 1L);
        var maximum = JsonLong(schema["maximum"], Math.Max(minimum, 999L));

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        var range = maximum - minimum + 1;

        if (range <= 0 || range > int.MaxValue)
        {
            return JsonValue.Create(minimum);
        }

        return JsonValue.Create(minimum + random.Next((int)range));
    }

    /// <summary>Builds a number.</summary>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The built number.</returns>
    static JsonValue BuildNumber(Random random)
    {
        if (random.NextDouble() < BoundaryProbability)
        {
            return JsonValue.Create(random.Next(3) switch
            {
                0 => -0.01,
                1 => 0.0,
                _ => 999999999999.99,
            });
        }

        return JsonValue.Create(random.Next(1, 100000) / 100.0);
    }

    /// <summary>Builds a string: valid-looking, or a boundary value.</summary>
    /// <param name="schema">The string schema.</param>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>The built string.</returns>
    static JsonValue BuildString(JsonObject schema, string? name, Random random)
    {
        if (random.NextDouble() < BoundaryProbability)
        {
            return BoundaryString(name, random);
        }

        return ValidString(schema, name, random);
    }

    /// <summary>Builds a boundary string for a name.</summary>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>An empty, very long, unknown or oddly shaped string.</returns>
    static JsonValue BoundaryString(string? name, Random random)
    {
        if (name is not null && name.Contains("date", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(random.Next(2) == 0 ? "1900-01-01" : "2999-12-31");
        }

        return JsonValue.Create(random.Next(4) switch
        {
            0 => string.Empty,
            1 => new string('x', VeryLongLength),
            2 => "UNKNOWN-CODE",
            _ => "quo\"te\nline",
        });
    }

    /// <summary>Builds a valid-looking string for a format or name.</summary>
    /// <param name="schema">The string schema, or null for an open value.</param>
    /// <param name="name">The property or parameter name, or null.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>A valid-shaped string.</returns>
    static JsonValue ValidString(JsonObject? schema, string? name, Random random)
    {
        string? format = null;

        if (schema?["format"] is JsonValue formatValue
            && formatValue.TryGetValue<string>(out var parsed)
            && parsed is not null)
        {
            format = parsed;
        }

        if (string.Equals(format, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(DeterministicGuid(random));
        }

        if (string.Equals(format, "date", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("2026-10-15");
        }

        if (string.Equals(format, "date-time", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("2026-10-15T10:30:00Z");
        }

        if (string.Equals(format, "email", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create($"walker{random.Next(1000):D3}@example.com");
        }

        if (string.Equals(format, "uri", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "url", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("https://example.com/walk");
        }

        if (name is not null)
        {
            var byName = ValidStringByName(name, random);

            if (byName is not null)
            {
                return byName;
            }
        }

        var text = $"Sample {name ?? "text"} {random.Next(1, 1000)}";

        if (schema?["maxLength"] is JsonValue max
            && max.TryGetValue<int>(out var limit)
            && limit >= 0
            && text.Length > limit)
        {
            text = limit == 0 ? string.Empty : text[..limit];
        }

        if (schema?["minLength"] is JsonValue min
            && min.TryGetValue<int>(out var floor)
            && text.Length < floor)
        {
            text = text.PadRight(floor, 'x');
        }

        return JsonValue.Create(text);
    }

    /// <summary>Builds a valid-looking string from a property or parameter name.</summary>
    /// <param name="name">The property or parameter name.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>A valid-shaped string, or null when the name suggests none.</returns>
    static JsonValue? ValidStringByName(string name, Random random)
    {
        if (name.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create($"walker{random.Next(1000):D3}@example.com");
        }

        if (name.Contains("phone", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("+15550001000");
        }

        if (name.Contains("date", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("2026-10-15");
        }

        if (name.Contains("year", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("2026");
        }

        if (name.Contains("period", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("2026-Q4");
        }

        if (name.EndsWith("id", StringComparison.OrdinalIgnoreCase)
            || name.Contains("guid", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(DeterministicGuid(random));
        }

        if (name.Contains("language", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("en");
        }

        if (name.Contains("timezone", StringComparison.OrdinalIgnoreCase)
            || name.Contains("time-zone", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create("UTC");
        }

        return null;
    }

    /// <summary>Merges the branches of an allOf schema into one object schema.</summary>
    /// <param name="branches">The branches.</param>
    /// <returns>The merged schema.</returns>
    static JsonObject MergeAllOf(JsonArray branches)
    {
        var merged = new JsonObject();
        var properties = new JsonObject();
        var required = new JsonArray();
        string? type = null;

        foreach (var branch in branches)
        {
            if (branch is not JsonObject obj)
            {
                continue;
            }

            if (type is null
                && obj["type"] is JsonValue kind
                && kind.TryGetValue<string>(out var parsed)
                && parsed is not null)
            {
                type = parsed;
            }

            if (obj["properties"] is JsonObject props)
            {
                foreach (var property in props)
                {
                    properties[property.Key] = property.Value?.DeepClone();
                }
            }

            if (obj["required"] is JsonArray names)
            {
                foreach (var item in names)
                {
                    required.Add(item?.DeepClone());
                }
            }
        }

        merged["type"] = type ?? "object";
        merged["properties"] = properties;
        merged["required"] = required;
        return merged;
    }

    /// <summary>Reads the JSON types of a schema, including OpenAPI nullability.</summary>
    /// <param name="schema">The schema.</param>
    /// <returns>The listed types.</returns>
    static List<string> JsonTypes(JsonObject schema)
    {
        var types = new List<string>();

        if (schema["type"] is JsonValue single
            && single.TryGetValue<string>(out var one)
            && one is not null)
        {
            types.Add(one);
        }
        else if (schema["type"] is JsonArray many)
        {
            foreach (var item in many)
            {
                if (item is JsonValue value
                    && value.TryGetValue<string>(out var each)
                    && each is not null)
                {
                    types.Add(each);
                }
            }
        }

        if (schema["nullable"] is JsonValue flag
            && flag.TryGetValue<bool>(out var nullable)
            && nullable)
        {
            types.Add("null");
        }

        return types;
    }

    /// <summary>Guesses the type of a schema that states none.</summary>
    /// <param name="schema">The schema.</param>
    /// <returns><c>object</c>, <c>array</c> or <c>string</c>.</returns>
    static string GuessType(JsonObject schema)
    {
        if (schema["properties"] is not null)
        {
            return "object";
        }

        if (schema["items"] is not null || schema["prefixItems"] is not null)
        {
            return "array";
        }

        return "string";
    }

    /// <summary>Reads an integer from a schema keyword.</summary>
    /// <param name="node">The keyword value.</param>
    /// <param name="fallback">The value when the keyword is absent.</param>
    /// <returns>The keyword value, or the fallback.</returns>
    static long JsonLong(JsonNode? node, long fallback) =>
        node is JsonValue value && value.TryGetValue<long>(out var number) ? number : fallback;

    /// <summary>Builds a GUID from the walk's random source, so the seed reproduces it.</summary>
    /// <param name="random">The walk's random source.</param>
    /// <returns>A GUID string.</returns>
    static string DeterministicGuid(Random random)
    {
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes).ToString();
    }
}
