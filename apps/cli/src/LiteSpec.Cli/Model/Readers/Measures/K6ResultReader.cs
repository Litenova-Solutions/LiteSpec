using System.Text.Json;

namespace LiteSpec.Model.Readers.Measures;

/// <summary>
///     Reads k6 summary files (<c>k6 run --summary-export</c>): one result per
///     threshold, passing when k6 marked it <c>ok</c>. A metric without thresholds
///     carries no assertion and yields no result.
/// </summary>
/// <follows>litespec/core/intent#state-the-measure-on-a-counted-requirement</follows>
static class K6ResultReader
{
    /// <summary>Reads the thresholds of a k6 summary object.</summary>
    /// <param name="root">The root element of the parsed document.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <returns>One measure result per threshold in the summary.</returns>
    public static List<MeasureResult> Parse(JsonElement root, string file)
    {
        var measures = new List<MeasureResult>();

        if (!root.TryGetProperty("metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Object)
        {
            return measures;
        }

        foreach (var metric in metrics.EnumerateObject())
        {
            if (metric.Value.ValueKind != JsonValueKind.Object
                || !metric.Value.TryGetProperty("thresholds", out var thresholds)
                || thresholds.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var values = metric.Value.TryGetProperty("values", out var found) && found.ValueKind == JsonValueKind.Object
                ? found
                : (JsonElement?)null;

            foreach (var threshold in thresholds.EnumerateObject())
            {
                var passed = threshold.Value.ValueKind == JsonValueKind.Object
                    && threshold.Value.TryGetProperty("ok", out var ok)
                    && ok.ValueKind == JsonValueKind.True;

                measures.Add(new MeasureResult(
                    "k6",
                    $"{metric.Name} {threshold.Name}",
                    passed,
                    Actual(values, threshold.Name),
                    file));
            }
        }

        return measures;
    }

    /// <summary>
    ///     The measured value the threshold judged: the values entry its left side
    ///     names, such as <c>p(95)</c> in <c>p(95)&lt;200</c>. Null when the file
    ///     holds no such entry.
    /// </summary>
    /// <param name="values">The values object of the k6 summary, or null when it states none.</param>
    /// <param name="expression">The threshold expression whose left side names the measured value.</param>
    /// <returns>The measured value text, or null when the values state no entry the expression names.</returns>
    private static string? Actual(JsonElement? values, string expression)
    {
        if (values is null)
        {
            return null;
        }

        var end = expression.IndexOfAny(['<', '>', '=', '!', ' ']);
        var name = (end < 0 ? expression : expression[..end]).Trim();

        if (name.Length == 0
            || !values.Value.TryGetProperty(name, out var actual))
        {
            return null;
        }

        return actual.ValueKind switch
        {
            JsonValueKind.Number => actual.GetRawText(),
            JsonValueKind.String => actual.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }
}
