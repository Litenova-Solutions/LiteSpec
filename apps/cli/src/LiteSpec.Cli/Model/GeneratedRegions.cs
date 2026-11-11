using System.Text.RegularExpressions;
using LiteSpec.Cli.Files;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model;

/// <summary>
///     Rewrites the fenced generated regions, leaving all other text untouched.
/// </summary>
/// <follows>litespec/core/model#render-generated-regions-from-the-model</follows>
static partial class GeneratedRegions
{
    /// <summary>Renders every generated region the builder writes, returning the changed files.</summary>
    /// <param name="model">The built project model the regions render from.</param>
    /// <returns>The pages rewritten, relative to the root, in read order.</returns>
    public static IReadOnlyList<string> Render(ProjectModel model)
    {
        var changed = new List<string>();

        foreach (var root in model.Config.RecordRoots)
        {
            foreach (var file in SourceTree.Files(root, ".md"))
            {
                var text = SourceTree.Read(file);

                var rewritten = Region().Replace(text, match =>
                {
                    var name = match.Groups["name"].Value;
                    var content = ContentFor(model, file, name);

                    if (content is null)
                    {
                        return match.Value;
                    }

                    var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

                    // The region keeps the page's own line ending, so a CRLF page never gains LF lines.
                    return $"<!-- generated:{name} -->{newline}{content.ReplaceLineEndings(newline)}{newline}<!-- /generated:{name} -->";
                });

                if (rewritten != text)
                {
                    File.WriteAllText(file, rewritten);
                    changed.Add(PathText.Relative(model.Config.Root, file));
                }
            }
        }

        return changed;
    }

    /// <summary>The rendered content of one region on one page.</summary>
    /// <param name="model">The built project model the region renders from.</param>
    /// <param name="file">The page the region sits on.</param>
    /// <param name="name">The region name inside its fence.</param>
    /// <returns>The region content, or null when the name names no known region.</returns>
    private static string? ContentFor(ProjectModel model, string file, string name) =>
        name switch
        {
            "alert-inventory" => AlertInventory(model, file),
            "operations-log" => OperationsLog(file),
            "runbook-index" => RunbookIndex(model, file),
            _ => null,
        };

    /// <summary>Renders the index list of runbooks with their triggers.</summary>
    /// <param name="model">The built project model holding the runbooks.</param>
    /// <param name="file">The page the list sits on, for relative links.</param>
    /// <returns>The runbook index as Markdown lines.</returns>
    private static string RunbookIndex(ProjectModel model, string file)
    {
        var lines = new List<string>();

        foreach (var runbook in model.Records
                     .Where(record => record.Type == RecordTypes.Runbook)
                     .OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            var trigger = runbook.Field(RecordFields.Trigger) ?? "";

            lines.Add($"- [{runbook.Title}]({PathText.Relative(Path.GetDirectoryName(file)!, runbook.File)}): {trigger}");
        }

        return string.Join('\n', lines);
    }

    /// <summary>Renders links to the sibling month pages of an operations log.</summary>
    /// <param name="file">The operations log page the list sits on.</param>
    /// <returns>The month link list as Markdown lines.</returns>
    private static string OperationsLog(string file)
    {
        var folder = Path.GetDirectoryName(file)!;
        var lines = new List<string>();

        foreach (var month in SourceTree.Files(folder, ".md")
                     .Where(path => !path.Equals(file, StringComparison.Ordinal))
                     .Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(month);

            lines.Add($"- [{name}]({Path.GetFileName(month)})");
        }

        return string.Join('\n', lines);
    }

    /// <summary>Renders the inventory of alert rules with their linked records.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="file">The page the table sits on, for relative links.</param>
    /// <returns>The alert inventory table as Markdown lines.</returns>
    private static string AlertInventory(ProjectModel model, string file)
    {
        var lines = new List<string>
        {
            "| Requirement | Alert | Runbook | Severity | File |",
            "|:---|:---|:---|:---|:---|",
        };

        foreach (var rule in Readers.Operations.AlertRuleReader.Read(model.Config)
                     .OrderBy(rule => RuleLabel(rule, "requirement"), StringComparer.Ordinal)
                     .ThenBy(rule => rule.Name, StringComparer.Ordinal))
        {
            var requirement = RecordCell(model, file, RuleLabel(rule, "requirement"), RecordTypes.Requirement);
            var runbook = RecordCell(model, file, RuleLabel(rule, "runbook"), RecordTypes.Runbook);
            var severity = Cell(RuleLabel(rule, "severity"));
            var source = Cell(PathText.Relative(model.Config.Root, rule.File));

            lines.Add($"| {requirement} | {Cell(rule.Name)} | {runbook} | {severity} | {source} |");
        }

        return string.Join('\n', lines);
    }

    /// <summary>One rule label, or empty when the rule carries no value for the key.</summary>
    /// <param name="rule">The alert rule to read a label from.</param>
    /// <param name="key">The label key to read.</param>
    /// <returns>The label value, or empty when the rule carries no value for the key.</returns>
    private static string RuleLabel(Readers.Operations.AlertRule rule, string key) =>
        rule.Labels.TryGetValue(key, out var value) ? value : "";

    /// <summary>One inventory record cell: a link when the id resolves, a code span when not, empty when missing.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="file">The page the cell sits on, for the relative link.</param>
    /// <param name="id">The record id the cell names.</param>
    /// <param name="type">The record type the id must resolve to for a link.</param>
    /// <returns>A linked code span when the id resolves to that type, a bare code span otherwise, or empty for no id.</returns>
    private static string RecordCell(ProjectModel model, string file, string id, string type) =>
        id.Length == 0
            ? ""
            : model.ById.TryGetValue(id, out var record) && record.Type == type
                ? $"[`{id}`]({PathText.Relative(Path.GetDirectoryName(file)!, record.File)})"
                : $"`{id}`";

    /// <summary>Escapes one cell for a Markdown table row.</summary>
    /// <param name="text">The cell text to escape.</param>
    /// <returns>The text with bars escaped and line breaks flattened to spaces.</returns>
    private static string Cell(string text) => text.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Replace("\r", "", StringComparison.Ordinal);

    [GeneratedRegex(@"<!-- generated:(?<name>[A-Za-z0-9-]+) -->(?<body>.*?)<!-- /generated:\k<name> -->", RegexOptions.Singleline)]
    private static partial Regex Region();
}
