using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>The values the walk has seen in earlier responses, kept by the property name they arrived under, so later calls address real records.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
sealed class ExploreIdentifiers
{
    /// <summary>The most values kept per property name.</summary>
    const int MaxValuesPerName = 1000;

    /// <summary>The longest collected value; longer strings are prose, not values a parameter takes.</summary>
    const int MaxValueLength = 120;

    /// <summary>The collected values by property name.</summary>
    readonly Dictionary<string, List<string>> byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every collected GUID, for parameters whose name was never seen.</summary>
    readonly List<string> guids = [];

    /// <summary>Initializes a new instance of the <see cref="ExploreIdentifiers"/> class.</summary>
    internal ExploreIdentifiers()
    {
    }

    /// <summary>Gets the number of values collected.</summary>
    internal int Count { get; private set; }

    /// <summary>Collects every parameter-worthy value in a JSON response.</summary>
    /// <param name="response">The parsed response, or null.</param>
    internal void Collect(JsonNode? response)
    {
        CollectNode(response, null);
    }

    /// <summary>Picks a collected value for a parameter name.</summary>
    /// <param name="parameterName">The path or query parameter name.</param>
    /// <param name="random">The walk's random source.</param>
    /// <returns>A collected value, or null when nothing fits.</returns>
    internal string? Pick(string parameterName, Random random)
    {
        if (byName.TryGetValue(parameterName, out var exact) && exact.Count > 0)
        {
            return exact[random.Next(exact.Count)];
        }

        if (parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && parameterName.Length > 2)
        {
            var shortened = parameterName[..^2];

            if (byName.TryGetValue(shortened, out var byShort) && byShort.Count > 0)
            {
                return byShort[random.Next(byShort.Count)];
            }
        }

        if (parameterName.Contains("id", StringComparison.OrdinalIgnoreCase) && guids.Count > 0)
        {
            return guids[random.Next(guids.Count)];
        }

        return null;
    }

    /// <summary>Collects the values in one JSON node.</summary>
    /// <param name="node">The node to collect from.</param>
    /// <param name="name">The property name the node arrived under, or null.</param>
    void CollectNode(JsonNode? node, string? name)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    CollectNode(property.Value, property.Key);
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    CollectNode(item, name);
                }

                break;
            case JsonValue value when value.GetValueKind() == JsonValueKind.String:
                CollectString(name, value.GetValue<string>());
                break;
            case JsonValue value when value.GetValueKind() == JsonValueKind.Number
                && value.TryGetValue<long>(out var number):
                CollectNumber(name, number);
                break;
            default:
                break;
        }
    }

    /// <summary>Collects one string when it looks like a value a parameter takes.</summary>
    /// <param name="name">The property name the value arrived under, or null.</param>
    /// <param name="text">The value.</param>
    void CollectString(string? name, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxValueLength)
        {
            return;
        }

        if (Guid.TryParse(text, out _))
        {
            Add(name ?? "id", text);

            if (!guids.Contains(text) && guids.Count < MaxValuesPerName)
            {
                guids.Add(text);
                Count++;
            }

            return;
        }

        if (IsCollectedName(name))
        {
            Add(name!, text);
        }
    }

    /// <summary>Collects one integer when its name takes it as a value.</summary>
    /// <param name="name">The property name the value arrived under, or null.</param>
    /// <param name="number">The value.</param>
    void CollectNumber(string? name, long number)
    {
        if (name is null)
        {
            return;
        }

        if (name.EndsWith("year", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("id", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("number", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("period", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("month", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("quarter", StringComparison.OrdinalIgnoreCase))
        {
            Add(name, number.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Adds one value under one name, ignoring duplicates past the cap.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    void Add(string name, string value)
    {
        if (!byName.TryGetValue(name, out var values))
        {
            values = [];
            byName[name] = values;
        }

        if (values.Count >= MaxValuesPerName || values.Contains(value))
        {
            return;
        }

        values.Add(value);
        Count++;
    }

    /// <summary>Whether values under a property name are worth collecting.</summary>
    /// <param name="name">The property name, or null.</param>
    /// <returns>Whether the name takes identifier-like or filter-like values.</returns>
    static bool IsCollectedName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.EndsWith("id", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("ids", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("code", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("number", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("no", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("period", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("year", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("month", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("quarter", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("cursor", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("key", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("name", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("status", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("kind", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("type", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("email", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("reference", StringComparison.OrdinalIgnoreCase);
    }
}
