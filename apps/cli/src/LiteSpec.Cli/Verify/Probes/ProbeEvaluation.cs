using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Probes;

/// <summary>
///     One evaluated probe response: the failures of the status, the refusal and
///     the field checks, each as one probe-failed message.
/// </summary>
/// <follows>litespec/quality/testing#prove-each-scenario-through-the-public-boundary</follows>
internal static class ProbeEvaluation
{
    /// <summary>Evaluates one probe response against its expectation.</summary>
    /// <param name="probe">The probe holding the expectation.</param>
    /// <param name="actualStatus">The HTTP status the host answered.</param>
    /// <param name="bodyText">The response body text.</param>
    /// <returns>One message per failed expectation, empty when the probe passes.</returns>
    public static IReadOnlyList<string> Evaluate(ProbeCase probe, int actualStatus, string bodyText)
    {
        var failures = new List<string>();

        if (actualStatus != probe.Status)
        {
            failures.Add($"Probe '{probe.ScenarioId}' expected status {probe.Status} but the host answered {actualStatus}.");
        }

        var body = ParseBody(bodyText);

        if (probe.Refusal is { Length: > 0 } refusal)
        {
            if (!MatchesRefusal(body, refusal))
            {
                failures.Add(RefusalMessage(probe, bodyText, body, refusal));
            }
        }

        foreach (var field in probe.Fields)
        {
            var actual = ResolvePointer(body, field.Path);

            if (field.Present is { } present)
            {
                var exists = actual is not null;

                if (present && !exists)
                {
                    failures.Add($"Probe '{probe.ScenarioId}' expected '{field.Path}' to exist but the response holds no such path.");
                }
                else if (!present && exists)
                {
                    failures.Add($"Probe '{probe.ScenarioId}' expected '{field.Path}' to be absent but the response holds a value.");
                }

                continue;
            }

            if (field.HasExpected)
            {
                var wanted = field.Expected is null ? "null" : field.Expected.ToJsonString();

                if (actual is null && field.Expected is not null)
                {
                    failures.Add($"Probe '{probe.ScenarioId}' expected '{field.Path}' to equal {wanted} but the response holds no such path.");
                }
                else if (actual is null && field.Expected is null)
                {
                    failures.Add($"Probe '{probe.ScenarioId}' expected '{field.Path}' to equal null but the response holds no such path.");
                }
                else if (actual is not null && !MatchesValue(field.Expected, actual))
                {
                    failures.Add($"Probe '{probe.ScenarioId}' expected '{field.Path}' to equal {wanted} but the host answered {actual.ToJsonString()}.");
                }
            }
        }

        return failures;
    }

    /// <summary>Whether the response body carries the expected refusal code.</summary>
    /// <param name="body">The parsed response body, or null when it is no JSON object.</param>
    /// <param name="refusal">The expected code, such as spending.receipt-missing.</param>
    /// <returns>True when the problem code equals it or the problem type ends with /&lt;it&gt;.</returns>
    public static bool MatchesRefusal(JsonNode? body, string refusal)
    {
        if (body is not JsonObject obj)
        {
            return false;
        }

        if (obj["code"] is JsonValue code && code.TryGetValue<string>(out var codeText) && string.Equals(codeText, refusal, StringComparison.Ordinal))
        {
            return true;
        }

        if (obj["type"] is JsonValue type && type.TryGetValue<string>(out var typeText))
        {
            return typeText is not null
                && (string.Equals(typeText, refusal, StringComparison.Ordinal)
                    || typeText.EndsWith("/" + refusal, StringComparison.Ordinal));
        }

        return false;
    }

    /// <summary>Resolves one JSON Pointer against the response body.</summary>
    /// <param name="root">The parsed response body, or null.</param>
    /// <param name="pointer">The pointer, such as /state or /a/0/b.</param>
    /// <returns>The value at the path, or null when no such path exists.</returns>
    public static JsonNode? ResolvePointer(JsonNode? root, string pointer)
    {
        if (root is null || pointer.Length == 0 || pointer == "/")
        {
            return pointer == "/" ? root : null;
        }

        if (!pointer.StartsWith('/'))
        {
            return null;
        }

        var current = root;

        foreach (var raw in pointer.Split('/').Skip(1))
        {
            var token = raw.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);

            if (current is JsonObject obj)
            {
                current = obj[token];
            }
            else if (current is JsonArray array && int.TryParse(token, out var index) && index >= 0 && index < array.Count)
            {
                current = array[index];
            }
            else
            {
                return null;
            }

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>Parses one response body as JSON, or returns null when it is no JSON value.</summary>
    /// <param name="bodyText">The response body text.</param>
    /// <returns>The parsed value, or null.</returns>
    private static JsonNode? ParseBody(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(bodyText);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The refusal failure message with the actual code or type when the body names one.</summary>
    /// <param name="probe">The probe holding the expectation.</param>
    /// <param name="bodyText">The raw response body text.</param>
    /// <param name="body">The parsed response body, or null.</param>
    /// <param name="refusal">The expected code.</param>
    /// <returns>The failure message.</returns>
    private static string RefusalMessage(ProbeCase probe, string bodyText, JsonNode? body, string refusal)
    {
        if (body is JsonObject obj)
        {
            if (obj["code"] is JsonValue code && code.TryGetValue<string>(out var codeText) && codeText is not null)
            {
                return $"Probe '{probe.ScenarioId}' expected refusal '{refusal}' but the host answered code '{codeText}'.";
            }

            if (obj["type"] is JsonValue type && type.TryGetValue<string>(out var typeText) && typeText is not null)
            {
                return $"Probe '{probe.ScenarioId}' expected refusal '{refusal}' but the host answered type '{typeText}'.";
            }
        }

        var shown = bodyText.Trim();

        if (shown.Length > 160)
        {
            shown = shown[..160] + "...";
        }

        return shown.Length == 0
            ? $"Probe '{probe.ScenarioId}' expected refusal '{refusal}' but the response holds no body."
            : $"Probe '{probe.ScenarioId}' expected refusal '{refusal}' but the response holds {shown}.";
    }

    /// <summary>Whether the actual value matches the expected value, by number and by text.</summary>
    /// <param name="expected">The expected value, or null for JSON null.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>True when the values match.</returns>
    private static bool MatchesValue(JsonNode? expected, JsonNode actual)
    {
        if (expected is null)
        {
            return false;
        }

        return JsonNode.DeepEquals(Canonical(expected), Canonical(actual))
            || string.Equals(Normalized(actual), Normalized(expected), StringComparison.Ordinal);
    }

    /// <summary>The canonical form of one JSON value for comparison: numbers by value, strings as written.</summary>
    /// <param name="node">The value to canonicalize.</param>
    /// <returns>The value with numbers normalized.</returns>
    private static JsonNode Canonical(JsonNode node)
    {
        if (node is JsonValue value && value.TryGetValue<double>(out var number))
        {
            return JsonValue.Create(number)!;
        }

        return node;
    }

    /// <summary>The trimmed text of one JSON value for the fallback string comparison.</summary>
    /// <param name="node">The value to read.</param>
    /// <returns>The trimmed JSON text.</returns>
    private static string Normalized(JsonNode node) => node.ToJsonString().Trim();
}
