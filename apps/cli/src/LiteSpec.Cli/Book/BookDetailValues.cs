using System.Text.Json.Nodes;

namespace LiteSpec.Book;

/// <summary>
///     The scalar readings the record details share: link strings, JSON nodes for
///     nullable values, line numbers, base ids and test names.
/// </summary>
/// <follows>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</follows>
static class BookDetailValues
{
    /// <summary>The strings of a link field, written as one value or a list.</summary>
    /// <param name="node">The link field value, or null.</param>
    /// <returns>The strings the field holds.</returns>
    public static IEnumerable<string> StringsOf(JsonNode? node)
    {
        if (node is JsonArray items)
        {
            foreach (var item in items)
            {
                if (item is JsonValue scalar && scalar.TryGetValue<string>(out var text) && !string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
        else if (node is JsonValue single && single.TryGetValue<string>(out var one) && !string.IsNullOrEmpty(one))
        {
            yield return one;
        }
    }

    /// <summary>One nullable string as a JSON node, null staying null.</summary>
    /// <param name="value">The string, or null.</param>
    /// <returns>The JSON node, or null.</returns>
    public static JsonNode? StrNode(string? value) => BookRender.StrNode(value);

    /// <summary>One nullable line number as a JSON node, null staying null.</summary>
    /// <param name="value">The line number, or null.</param>
    /// <returns>The JSON node, or null.</returns>
    public static JsonNode? IntNode(int? value) => value.HasValue ? JsonValue.Create(value.Value) : null;

    /// <summary>Reads an integer property, or null when it is missing or not a number.</summary>
    /// <param name="obj">The object holding the property, or null.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The integer property, or null.</returns>
    public static int? IntProp(JsonObject? obj, string name)
    {
        if (obj?[name] is JsonValue scalar && scalar.TryGetValue<int>(out var value))
        {
            return value;
        }

        if (obj?[name] is JsonValue wide && wide.TryGetValue<long>(out var big) && big >= int.MinValue && big <= int.MaxValue)
        {
            return (int)big;
        }

        return null;
    }

    /// <summary>Strips the step fragment of an id, such as journey/name#s1.</summary>
    /// <param name="id">The record id, with or without a step fragment.</param>
    /// <returns>The id without its fragment.</returns>
    public static string BaseId(string id)
    {
        var hash = id.IndexOf('#');
        return hash < 0 ? id : id[..hash];
    }

    /// <summary>One test name in words: the last segment with underscores as spaces.</summary>
    /// <param name="scenario">The scenario name.</param>
    /// <returns>The test name in words.</returns>
    public static string HumanTestName(string scenario)
    {
        var tail = scenario;
        var dot = scenario.LastIndexOf('.');
        if (dot >= 0 && dot + 1 < scenario.Length)
        {
            tail = scenario[(dot + 1)..];
        }

        return tail.Replace('_', ' ');
    }
}
