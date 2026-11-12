using System.Text.RegularExpressions;
using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Checks.Governance;
using LiteSpec.Cli.Model.Checks.Implementation;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Operations;
using LiteSpec.Cli.Model.Records;
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
            "decision-index" => DecisionIndex(model, file),
            "departure-index" => DepartureIndex(model, file),
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

    /// <summary>Renders one table of the decisions beside the page, grouped by kind, then by the computed concern of each decision's followers.</summary>
    /// <param name="model">The built project model holding the records and the read code.</param>
    /// <param name="file">The page the table sits on, for relative links.</param>
    /// <returns>The decision index table as Markdown lines, or empty when the folder holds no decision.</returns>
    private static string DecisionIndex(ProjectModel model, string file)
    {
        var folder = Path.GetDirectoryName(file)!;
        var operations = OperationsMarkReader.Read(model.Config)
            .Where(mark => mark.Id.Length > 0)
            .ToLookup(mark => mark.Id, StringComparer.Ordinal);

        var rows = model.Records
            .Where(record => record.Type == RecordTypes.Decision && SameFolder(folder, record.File))
            .Select(record => DecisionRow(model, folder, record, operations[record.Id]))
            .OrderBy(row => row.KindRank)
            .ThenBy(row => row.Concerns, StringComparer.Ordinal)
            .ThenBy(row => row.Id, StringComparer.Ordinal)
            .ToList();

        if (rows.Count == 0)
        {
            return "";
        }

        var lines = new List<string>
        {
            "| Decision | Kind | Concern | Choice | Followers | Reopened When | Flags |",
            "|:---|:---|:---|:---|:---|:---|:---|",
        };

        lines.AddRange(rows.Select(row => row.Text));

        return string.Join('\n', lines);
    }

    /// <summary>One decision index row with its sort keys.</summary>
    /// <param name="model">The built project model holding the records and the read code.</param>
    /// <param name="folder">The decisions folder the page sits in.</param>
    /// <param name="decision">The decision the row renders.</param>
    /// <param name="operationMarks">The operations follows marks on the decision.</param>
    /// <returns>The row text with its kind rank, concern key and identifier.</returns>
    private static (string Text, int KindRank, string Concerns, string Id) DecisionRow(
        ProjectModel model, string folder, Record decision, IEnumerable<OperationsMark> operationMarks)
    {
        var followers = DecisionChecks.FollowersOf(model, decision.Id);
        var operations = operationMarks.ToList();
        var concerns = followers.Select(follower => ConcernOf(model.Config, follower.File))
            .Concat(operations.Select(mark => ConcernOf(model.Config, mark.File)))
            .Where(concern => concern.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var modules = DecisionChecks.ModulesOf(model, followers);
        var flags = new List<string>();

        if (MarkChecks.UnderImplementationDecisions(model.Config, decision.File))
        {
            if (followers.Count + operations.Count == 0)
            {
                flags.Add("`decision-unimplemented`");
            }

            if (!decision.Ids(RecordFields.BasedOn).Any())
            {
                flags.Add("`decision-basis-missing`");
            }
        }

        if (DecisionChecks.ScopeInvalidModule(model, decision) is not null)
        {
            flags.Add("`decision-scope-invalid`");
        }

        var number = DecisionNumber(decision.Id);
        var link = $"[{Cell($"{number} {decision.Title}")}]({PathText.Relative(folder, decision.File)})";
        var kind = decision.Field(RecordFields.Kind) ?? "-";
        var concernCell = concerns.Count == 0 ? "-" : string.Join(", ", concerns);
        var choice = Cell(decision.Field(RecordFields.Choice) ?? "");
        var reopened = Cell(decision.Field(RecordFields.ReopenedWhen) ?? "");

        return (
            $"| {link} | {Cell(kind)} | {Cell(concernCell)} | {choice} | {FollowersCell(followers.Count + operations.Count, modules)} | {reopened} | {string.Join(", ", flags)} |",
            KindRank(decision.Field(RecordFields.Kind)),
            string.Join(",", concerns),
            decision.Id);
    }

    /// <summary>Renders the departures beside the page, grouped by gap or permanent and ordered by expiry.</summary>
    /// <param name="model">The built project model holding the records.</param>
    /// <param name="file">The page the tables sit on, for relative links.</param>
    /// <returns>The departure index tables as Markdown lines, or empty when the folder holds no departure.</returns>
    private static string DepartureIndex(ProjectModel model, string file)
    {
        var folder = Path.GetDirectoryName(file)!;
        var departures = model.Records
            .Where(record => record.Type == RecordTypes.Departure && SameFolder(folder, record.File))
            .ToList();

        if (departures.Count == 0)
        {
            return "";
        }

        var gaps = departures
            .Where(departure => departure.Field(RecordFields.ExpiresOn) is { Length: > 0 })
            .OrderBy(departure => departure.Field(RecordFields.ExpiresOn)!, StringComparer.Ordinal)
            .ThenBy(departure => departure.Id, StringComparer.Ordinal)
            .ToList();
        var lasting = departures
            .Where(departure => departure.Field(RecordFields.ExpiresOn) is not { Length: > 0 })
            .OrderBy(departure => departure.Id, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>();

        if (gaps.Count > 0)
        {
            lines.Add("### Gaps");
            lines.Add("");
            lines.Add("| Departure | Expires On | Covers |");
            lines.Add("|:---|:---|:---|");

            foreach (var departure in gaps)
            {
                lines.Add($"| {DepartureCell(folder, departure)} | {Cell(departure.Field(RecordFields.ExpiresOn)!)} | {CoversCell(departure)} |");
            }
        }

        if (lasting.Count > 0)
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }

            lines.Add("### Permanent");
            lines.Add("");
            lines.Add("| Departure | Covers |");
            lines.Add("|:---|:---|");

            foreach (var departure in lasting)
            {
                lines.Add($"| {DepartureCell(folder, departure)} | {CoversCell(departure)} |");
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>One departure cell: its title linked to its record file.</summary>
    /// <param name="folder">The departures folder the page sits in, for the relative link.</param>
    /// <param name="departure">The departure the cell renders.</param>
    /// <returns>The linked title.</returns>
    private static string DepartureCell(string folder, Record departure) =>
        $"[{Cell(departure.Title)}]({PathText.Relative(folder, departure.File)})";

    /// <summary>One departure covers cell: every covered code, provision or page as a code span.</summary>
    /// <param name="departure">The departure whose covers are rendered.</param>
    /// <returns>The covers joined with commas, or empty when the departure covers nothing.</returns>
    private static string CoversCell(Record departure) =>
        string.Join(", ", departure.Strings(RecordFields.Covers).Order(StringComparer.Ordinal).Select(cover => $"`{Cell(cover)}`"));

    /// <summary>Whether the record file sits in the folder.</summary>
    /// <param name="folder">The folder the page sits in.</param>
    /// <param name="file">The record file path.</param>
    /// <returns>True when the file sits directly in the folder.</returns>
    private static bool SameFolder(string folder, string file) =>
        string.Equals(Path.GetDirectoryName(file), folder, StringComparison.OrdinalIgnoreCase);

    /// <summary>The decision number of an identifier: the digits before the first dash.</summary>
    /// <param name="id">The decision identifier.</param>
    /// <returns>The number segment, or the whole name when it carries no dash.</returns>
    private static string DecisionNumber(string id)
    {
        var name = id.StartsWith("decision/", StringComparison.Ordinal) ? id["decision/".Length..] : id;
        var dash = name.IndexOf('-');

        return dash < 0 ? name : name[..dash];
    }

    /// <summary>The sort rank of a decision kind, in schema order, with a missing kind last.</summary>
    /// <param name="kind">The decision kind, or null when the decision states none.</param>
    /// <returns>The rank of the kind.</returns>
    private static int KindRank(string? kind) => kind switch
    {
        "provider" => 0,
        "package" => 1,
        "format" => 2,
        "boundary" => 3,
        "topology" => 4,
        "replacement" => 5,
        _ => 6,
    };

    /// <summary>One followers cell: the count with the modules the followers sit in.</summary>
    /// <param name="count">The follower count.</param>
    /// <param name="modules">The distinct modules the followers sit in, in ordinal order.</param>
    /// <returns>The count, with the modules in parentheses when any follower sits in a module.</returns>
    private static string FollowersCell(int count, IReadOnlyList<string> modules) =>
        modules.Count == 0 ? count.ToString() : $"{count} ({string.Join(", ", modules)})";

    /// <summary>The concern of a follower path, from the classification rules applies.</summary>
    /// <param name="config">The consumer configuration holding the roots and frontends.</param>
    /// <param name="file">The follower file path.</param>
    /// <returns>The concern, or empty when the path names none.</returns>
    private static string ConcernOf(ConsumerConfig config, string file)
    {
        var relative = PathText.Relative(config.Root, file);
        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var name = Path.GetFileName(file);
        var extension = Path.GetExtension(file).ToLowerInvariant();

        if (IsTestPath(config, file, segments, name, extension))
        {
            return "tests";
        }

        if (UnderAny(file, config.RecordRoots))
        {
            return "";
        }

        if (UnderAny(file, config.Alerts) || UnderAny(file, config.Operations) || IsOperationsFile(name))
        {
            return "operations";
        }

        if (config.Frontends.Any(frontend => frontend.Path.Length > 0 && Under(file, frontend.Path)))
        {
            return "frontend-ui";
        }

        if (config.ApiClientSrc.Length > 0 && Under(file, config.ApiClientSrc))
        {
            return "frontend-data";
        }

        if (extension is ".svelte" or ".razor" or ".kt")
        {
            return "frontend-ui";
        }

        if (extension is ".csproj" or ".props" or ".targets" or ".sln" or ".slnx")
        {
            return "workspace";
        }

        if (extension is ".cs")
        {
            return BackendConcern(segments, name);
        }

        return "";
    }

    /// <summary>Whether the path holds tests.</summary>
    /// <param name="config">The consumer configuration holding the test roots.</param>
    /// <param name="file">The follower file path.</param>
    /// <param name="segments">The root-relative path segments.</param>
    /// <param name="name">The file name.</param>
    /// <param name="extension">The lowercase file extension.</param>
    /// <returns>True when the path holds tests.</returns>
    private static bool IsTestPath(ConsumerConfig config, string file, string[] segments, string name, string extension)
    {
        if (UnderAny(file, config.TestRoots))
        {
            return true;
        }

        if (segments.Any(segment => segment is "tests" or "test" or "__tests__"))
        {
            return true;
        }

        return name.Contains(".Tests.", StringComparison.Ordinal)
            || name.EndsWith("Test" + extension, StringComparison.Ordinal)
            || name.EndsWith("Tests" + extension, StringComparison.Ordinal)
            || (extension is ".ts" or ".tsx" or ".js" or ".svelte" && name.Contains(".spec.", StringComparison.Ordinal));
    }

    /// <summary>Whether the file is an operations file by name.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>True for a Caddyfile or Dockerfile.</returns>
    private static bool IsOperationsFile(string name) =>
        string.Equals(name, "Caddyfile", StringComparison.Ordinal)
        || name.StartsWith("Dockerfile", StringComparison.Ordinal);

    /// <summary>The concern of a backend C# path, from its layer folder, project suffix or role suffix.</summary>
    /// <param name="segments">The root-relative path segments.</param>
    /// <param name="name">The file name.</param>
    /// <returns>The backend concern.</returns>
    private static string BackendConcern(string[] segments, string name)
    {
        if (HasSegment(segments, "Migrations") || name.EndsWith("Migration.cs", StringComparison.Ordinal) || name.EndsWith("Snapshot.cs", StringComparison.Ordinal))
        {
            return "infrastructure";
        }

        foreach (var segment in segments)
        {
            var concern = segment switch
            {
                "Domain" => "domain",
                "Application" => "application",
                "Infrastructure" => "infrastructure",
                "WebApi" => "web-api",
                "Composition" or "Contracts" or "Jobs" => "application",
                "Migrations" => "infrastructure",
                _ => "",
            };

            if (concern.Length > 0)
            {
                return concern;
            }
        }

        foreach (var segment in segments)
        {
            if (segment.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase))
            {
                return "domain";
            }

            if (segment.EndsWith(".Application", StringComparison.OrdinalIgnoreCase)
                || segment.EndsWith(".Worker", StringComparison.OrdinalIgnoreCase))
            {
                return "application";
            }

            if (segment.EndsWith(".Infrastructure", StringComparison.OrdinalIgnoreCase))
            {
                return "infrastructure";
            }

            if (segment.EndsWith(".WebApi", StringComparison.OrdinalIgnoreCase))
            {
                return "web-api";
            }
        }

        return IsWebApiName(name) ? "web-api" : "application";
    }

    /// <summary>Whether the file name carries a WebApi role suffix.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>True for an endpoint or transport model name.</returns>
    private static bool IsWebApiName(string name) =>
        name.EndsWith("Endpoint.cs", StringComparison.Ordinal)
        || name.EndsWith("RequestModel.cs", StringComparison.Ordinal)
        || name.EndsWith("ResponseModel.cs", StringComparison.Ordinal)
        || name.EndsWith("ResponseItemModel.cs", StringComparison.Ordinal)
        || name.EndsWith("PaginationModel.cs", StringComparison.Ordinal)
        || (name.EndsWith("Model.cs", StringComparison.Ordinal) && !name.EndsWith("ViewModel.cs", StringComparison.Ordinal));

    /// <summary>Whether the segments hold the folder name.</summary>
    /// <param name="segments">The path segments.</param>
    /// <param name="folder">The folder name.</param>
    /// <returns>True when a segment names the folder.</returns>
    private static bool HasSegment(string[] segments, string folder) =>
        segments.Any(segment => segment == folder);

    /// <summary>Whether the path sits under one of the roots.</summary>
    /// <param name="file">The follower file path.</param>
    /// <param name="roots">The absolute roots.</param>
    /// <returns>True when a root holds the path.</returns>
    private static bool UnderAny(string file, IReadOnlyList<string> roots) =>
        roots.Any(root => root.Length > 0 && Under(file, root));

    /// <summary>Whether the folder holds the path.</summary>
    /// <param name="file">The follower file path.</param>
    /// <param name="folder">The absolute folder.</param>
    /// <returns>True when the folder holds the path.</returns>
    private static bool Under(string file, string folder)
    {
        var full = Path.GetFullPath(file);
        var basePath = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), basePath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
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
