using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Explore;

/// <summary>The explore report: fault lines, the JSON report and the Markdown report.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class ExploreReport
{
    /// <summary>The Markdown report file name inside the output folder.</summary>
    internal const string MarkdownFileName = "explore.md";

    /// <summary>The JSON report file name inside the output folder.</summary>
    internal const string JsonFileName = "explore.json";

    /// <summary>One fault's printed line: its step, request and status, with the problem detail when it has one.</summary>
    /// <param name="fault">The fault to print.</param>
    /// <returns>The printed line for the fault.</returns>
    internal static string Line(ExploreFault fault)
    {
        if (string.Equals(fault.Kind, ExploreFault.Timeout, StringComparison.Ordinal))
        {
            return $"Step {fault.Step}: {fault.Method} {fault.Path} timed out.";
        }

        var line = $"Step {fault.Step}: {fault.Method} {fault.Path} answered {fault.StatusCode}.";

        if (fault.Detail is { Length: > 0 } detail)
        {
            line += $" Detail: {detail}";
        }

        return line;
    }

    /// <summary>The JSON report object: the run, the status counts, the faults and the skipped routes.</summary>
    /// <param name="baseUrl">The walked base URL.</param>
    /// <param name="openapi">The OpenAPI source the walk read.</param>
    /// <param name="seed">The seed that replays the walk.</param>
    /// <param name="result">The walk result.</param>
    /// <returns>The JSON report object.</returns>
    internal static JsonObject AsJson(string baseUrl, string openapi, int seed, ExploreWalk.Result result) => new()
    {
        ["baseUrl"] = baseUrl,
        ["openapi"] = openapi,
        ["seed"] = seed,
        ["calls"] = result.Steps,
        ["operationsListed"] = result.OperationsListed,
        ["skipped"] = new JsonArray([.. result.Skipped.OrderBy(entry => entry.Template, StringComparer.Ordinal).Select(entry => (JsonNode?)new JsonObject
        {
            ["template"] = entry.Template,
            ["reason"] = entry.Reason,
        })]),
        ["statusCounts"] = new JsonObject(result.StatusCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => KeyValuePair.Create<string, JsonNode?>(pair.Key, pair.Value))),
        ["faults"] = new JsonArray([.. result.Faults.OrderBy(fault => fault.Step).Select(fault => (JsonNode?)new JsonObject
        {
            ["kind"] = fault.Kind,
            ["operation"] = fault.Operation,
            ["method"] = fault.Method,
            ["pathTemplate"] = fault.PathTemplate,
            ["path"] = fault.Path,
            ["requestBody"] = fault.RequestBody,
            ["statusCode"] = fault.StatusCode,
            ["responseExcerpt"] = fault.ResponseExcerpt,
            ["Detail"] = fault.Detail,
            ["step"] = fault.Step,
        })]),
        ["stoppedEarly"] = result.StoppedEarly,
    };

    /// <summary>The Markdown report: the run, the calls by status, the faults by operation and the skipped routes.</summary>
    /// <param name="baseUrl">The walked base URL.</param>
    /// <param name="openapi">The OpenAPI source the walk read.</param>
    /// <param name="seed">The seed that replays the walk.</param>
    /// <param name="started">When the walk started, in UTC.</param>
    /// <param name="elapsed">How long the walk ran.</param>
    /// <param name="stepsConfigured">How many calls the walk was asked to make.</param>
    /// <param name="result">The walk result.</param>
    /// <returns>The Markdown report text.</returns>
    internal static string AsMarkdown(
        string baseUrl,
        string openapi,
        int seed,
        DateTime started,
        TimeSpan elapsed,
        int stepsConfigured,
        ExploreWalk.Result result)
    {
        var report = new StringBuilder();
        report.AppendLine("# API Explore Report");
        report.AppendLine();
        report.AppendLine("## Run");
        report.AppendLine();
        report.AppendLine($"- Base URL: {baseUrl}");
        report.AppendLine($"- OpenAPI: {openapi}");
        report.AppendLine($"- Started: {started.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC");
        report.AppendLine($"- Elapsed: {elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} seconds");
        report.AppendLine($"- Seed: {seed}");
        report.AppendLine($"- Calls made: {result.Steps} of up to {stepsConfigured} steps");
        report.AppendLine($"- Callable operations listed: {result.OperationsListed}");
        report.AppendLine($"- Routes skipped by the deny list: {result.Skipped.Count}");

        if (result.StoppedEarly is not null)
        {
            report.AppendLine($"- Stopped early: {result.StoppedEarly}");
        }

        report.AppendLine();
        report.AppendLine("## Calls by Status Code");
        report.AppendLine();

        if (result.StatusCounts.Count == 0)
        {
            report.AppendLine("No calls were made.");
        }
        else
        {
            report.AppendLine("| Status | Calls |");
            report.AppendLine("| --- | --- |");

            foreach (var pair in result.StatusCounts.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                report.AppendLine($"| {pair.Key} | {pair.Value} |");
            }
        }

        report.AppendLine();
        report.AppendLine("## Faults by Operation");
        report.AppendLine();

        if (result.Faults.Count == 0)
        {
            report.AppendLine("No faults found.");
        }
        else
        {
            foreach (var group in result.Faults.GroupBy(static fault => fault.Operation).OrderBy(static group => group.Key, StringComparer.Ordinal))
            {
                var first = group.First();
                report.AppendLine($"### {first.Method} {first.PathTemplate} ({group.Count()} faults)");
                report.AppendLine();

                foreach (var fault in group.OrderBy(static fault => fault.Step))
                {
                    WriteFault(report, fault);
                }

                report.AppendLine();
            }
        }

        report.AppendLine("## Skipped Routes");
        report.AppendLine();

        if (result.Skipped.Count == 0)
        {
            report.AppendLine("No listed operation matched the deny list.");
        }
        else
        {
            foreach (var (template, reason) in result.Skipped.OrderBy(static entry => entry.Template, StringComparer.Ordinal))
            {
                report.AppendLine($"- `{template}`: {reason}");
            }
        }

        report.AppendLine();
        report.AppendLine("The OpenAPI document and health routes are never walked.");
        report.AppendLine();
        report.AppendLine("## Reproduction");
        report.AppendLine();
        report.AppendLine($"The walk uses `new Random({seed})`: the same seed against the same data replays the same calls. Each fault names its step number.");
        return report.ToString();
    }

    /// <summary>Writes one fault with its request body, response excerpt and problem detail.</summary>
    /// <param name="report">The report text.</param>
    /// <param name="fault">The fault to write.</param>
    static void WriteFault(StringBuilder report, ExploreFault fault)
    {
        if (string.Equals(fault.Kind, ExploreFault.Timeout, StringComparison.Ordinal))
        {
            report.AppendLine($"- Step {fault.Step}: `{fault.Method} {fault.Path}` timed out.");
        }
        else
        {
            report.AppendLine($"- Step {fault.Step}: `{fault.Method} {fault.Path}` answered {fault.StatusCode}.");
        }

        if (fault.RequestBody is null)
        {
            report.AppendLine("  Request body: none.");
        }
        else
        {
            report.AppendLine("  Request body:");
            report.AppendLine("  ```json");
            report.AppendLine(fault.RequestBody);
            report.AppendLine("  ```");
        }

        if (string.Equals(fault.Kind, ExploreFault.Timeout, StringComparison.Ordinal))
        {
            report.AppendLine("  No response: the request timed out.");
            return;
        }

        report.AppendLine("  Response excerpt (first 500 characters):");
        report.AppendLine("  ```");
        report.AppendLine(fault.ResponseExcerpt);
        report.AppendLine("  ```");

        if (fault.Detail is { Length: > 0 } detail)
        {
            report.AppendLine($"  Detail: {detail}");
        }
    }
}
