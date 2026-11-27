using System.Text;
using System.Text.Json.Nodes;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Audit;

/// <summary>The audit report: merged findings, their text lines, and the JSON report file.</summary>
/// <follows>litespec/core/release#report-verification-exactly</follows>
static class AuditReport
{
    /// <summary>The JSON report file name inside the output folder.</summary>
    internal const string ReportFileName = "audit.json";

    /// <summary>Merges raw findings across widths: one finding per code, route, scheme, selector and message.</summary>
    /// <param name="raw">Every script finding, each at its own width.</param>
    /// <param name="routes">The audited routes, naming each finding's subject.</param>
    /// <returns>The merged findings in route, scheme, code, selector and message order.</returns>
    internal static IReadOnlyList<AuditFinding> Merge(IReadOnlyList<AuditRawFinding> raw, IReadOnlyList<AuditRoute> routes)
    {
        var subjects = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var route in routes)
        {
            subjects.TryAdd(route.Path, route.Subject);
        }

        return [.. raw
            .GroupBy(finding => (finding.Code, finding.Route, finding.Scheme, finding.Selector, finding.Message))
            .Select(group => new AuditFinding(
                group.Key.Code,
                subjects.TryGetValue(group.Key.Route, out var subject) ? subject : group.Key.Route,
                group.Key.Message,
                group.Key.Route,
                group.Key.Scheme,
                [.. group.Select(finding => finding.Width).Distinct().Order()],
                group.Key.Selector))
            .OrderBy(finding => finding.Route, StringComparer.Ordinal)
            .ThenBy(finding => finding.Scheme, StringComparer.Ordinal)
            .ThenBy(finding => finding.Code, StringComparer.Ordinal)
            .ThenBy(finding => finding.Selector, StringComparer.Ordinal)
            .ThenBy(finding => finding.Message, StringComparer.Ordinal)];
    }

    /// <summary>One finding's printed line: its route, scheme, widths, code, message and selector.</summary>
    /// <param name="finding">The finding to print.</param>
    /// <returns>The printed line for the finding.</returns>
    internal static string Line(AuditFinding finding) =>
        $"{finding.Route} [{finding.Scheme} {string.Join(",", finding.Widths)}] [{finding.Code}] {finding.Message} ({finding.Selector})";

    /// <summary>The JSON report object: the base URL, routes, widths, schemes, findings and screenshots.</summary>
    /// <param name="baseUrl">The audited base URL.</param>
    /// <param name="routes">The audited routes.</param>
    /// <param name="widths">The audited viewport widths.</param>
    /// <param name="schemes">The audited color schemes.</param>
    /// <param name="findings">The merged findings.</param>
    /// <param name="screenshots">The screenshot file names inside the output folder, in run order.</param>
    /// <returns>The JSON report object.</returns>
    internal static JsonObject AsJson(
        string baseUrl,
        IReadOnlyList<AuditRoute> routes,
        IReadOnlyList<int> widths,
        IReadOnlyList<string> schemes,
        IReadOnlyList<AuditFinding> findings,
        IReadOnlyList<string> screenshots) => new()
        {
            ["baseUrl"] = baseUrl,
            ["routes"] = new JsonArray([.. routes.Select(route => (JsonNode?)new JsonObject
            {
                ["path"] = route.Path,
                ["subject"] = route.Subject,
            })]),
            ["widths"] = new JsonArray([.. widths.Select(width => (JsonNode?)width)]),
            ["schemes"] = CliJson.Strings(schemes),
            ["findings"] = new JsonArray([.. findings.Select(finding => (JsonNode?)new JsonObject
            {
                ["code"] = finding.Code,
                ["subject"] = finding.Subject,
                ["message"] = finding.Message,
                ["route"] = finding.Route,
                ["scheme"] = finding.Scheme,
                ["widths"] = new JsonArray([.. finding.Widths.Select(width => (JsonNode?)width)]),
                ["selector"] = finding.Selector,
            })]),
            ["screenshots"] = CliJson.Strings(screenshots),
        };

    /// <summary>The screenshot file name for one route, scheme and width, following the capture pattern.</summary>
    /// <param name="route">The route path.</param>
    /// <param name="scheme">The color scheme.</param>
    /// <param name="width">The viewport width.</param>
    /// <returns>The file name, such as expenses-new--light--390.png.</returns>
    internal static string FileName(string route, string scheme, int width) =>
        $"{SlugOf(route)}--{scheme}--{width}.png";

    /// <summary>The route slug for a screenshot file name: letters and digits kept, each other run one dash.</summary>
    /// <param name="route">The route path.</param>
    /// <returns>The slug, or home for the root route.</returns>
    internal static string SlugOf(string route)
    {
        var trimmed = route.Trim('/');

        if (trimmed.Length == 0)
        {
            return "home";
        }

        var slug = new StringBuilder(trimmed.Length);
        var dash = false;

        foreach (var current in trimmed)
        {
            if ((current >= 'a' && current <= 'z') || (current >= 'A' && current <= 'Z') || (current >= '0' && current <= '9'))
            {
                slug.Append(current);
                dash = false;
            }
            else if (!dash)
            {
                slug.Append('-');
                dash = true;
            }
        }

        var text = slug.ToString().Trim('-');

        return text.Length == 0 ? "home" : text;
    }
}
