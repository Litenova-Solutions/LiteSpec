using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Links;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Rules;
using LiteSpec.CommandLine;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Serve;

/// <summary>
///     The four MCP tools the serve session exposes: the task packet, the open
///     findings, the record impact, and the use-case verify slice. Every tool reads
///     the warm model, so it answers from cache when nothing changed.
/// </summary>
/// <follows>litespec/core/agent#select-task-context</follows>
static class ServeTools
{
    /// <summary>The most characters one task packet carries: 10,000 characters, about 2,500 tokens.</summary>
    public const int PacketCap = 10000;

    /// <summary>The task packet tool name.</summary>
    public const string TaskPacketName = "litespec_task_packet";

    /// <summary>The open findings tool name.</summary>
    public const string CheckName = "litespec_check";

    /// <summary>The record impact tool name.</summary>
    public const string ImpactName = "litespec_impact";

    /// <summary>The use-case verify slice tool name.</summary>
    public const string VerifySliceName = "litespec_verify_slice";

    /// <summary>The tool descriptors in tools/list order, each with its name, description and input schema.</summary>
    /// <returns>The MCP tool objects.</returns>
    internal static JsonArray Descriptors() => new(
        Descriptor(
            TaskPacketName,
            "The record or records the target file path or record id implements, with their fields, the judgment provisions that govern the path, and the neighbouring files. Checked provisions are left to the checks.",
            new JsonObject
            {
                ["target"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "A file path under the consumer root or a record id.",
                },
            },
            ["target"]),
        Descriptor(
            CheckName,
            "The open findings for the given paths, or for every path when paths is omitted, from the warm model.",
            new JsonObject
            {
                ["paths"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "The file or folder paths to keep; every path when omitted.",
                },
            },
            []),
        Descriptor(
            ImpactName,
            "The records, code and tests linked to the record in both directions.",
            new JsonObject
            {
                ["record_id"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "The record id to report the links of.",
                },
            },
            ["record_id"]),
        Descriptor(
            VerifySliceName,
            "The use case's open findings, its scenarios with proof state, and its code and test files.",
            new JsonObject
            {
                ["use_case_id"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "The use-case record id to verify.",
                },
            },
            ["use_case_id"]));

    /// <summary>Caps a packet at <see cref="PacketCap"/> characters, stating the cut.</summary>
    /// <param name="text">The packet text.</param>
    /// <returns>The text unchanged when it fits, else the head with the cut stated.</returns>
    internal static string Cap(string text)
    {
        if (text.Length <= PacketCap)
        {
            return text;
        }

        // The note counts toward the cap, so a capped packet never exceeds it.
        var reserve = $"\n[cut: showing {PacketCap} of {text.Length} characters]".Length;
        var kept = PacketCap - reserve;
        return text[..kept] + $"\n[cut: showing {kept} of {text.Length} characters]";
    }

    /// <summary>Calls one tool against the warm model and projects the MCP result object.</summary>
    /// <param name="state">The warm model the tool answers from.</param>
    /// <param name="name">The tool name.</param>
    /// <param name="args">The tool arguments, or null when the call names none.</param>
    /// <returns>The MCP result object, or null when the name is no tool.</returns>
    internal static JsonObject? Call(ServeModel state, string? name, JsonObject? args)
    {
        state.EnsureFresh();

        var (text, isError) = name switch
        {
            TaskPacketName => TaskPacketText(state, Required(args, "target")),
            CheckName => CheckText(state, Paths(args)),
            ImpactName => ImpactText(state, Required(args, "record_id")),
            VerifySliceName => VerifySliceText(state, Required(args, "use_case_id")),
            _ => (null as string, false),
        };

        if (text is null)
        {
            return null;
        }

        var result = new JsonObject
        {
            ["content"] = new JsonArray((JsonNode)new JsonObject { ["type"] = "text", ["text"] = text }),
        };

        if (isError)
        {
            result["isError"] = true;
        }

        return result;
    }

    /// <summary>One tool descriptor: its name, description and object input schema.</summary>
    /// <param name="name">The tool name.</param>
    /// <param name="description">The one-line tool description.</param>
    /// <param name="properties">The input schema properties.</param>
    /// <param name="required">The required property names.</param>
    /// <returns>The MCP tool object.</returns>
    static JsonObject Descriptor(string name, string description, JsonObject properties, IReadOnlyList<string> required) => new()
    {
        ["name"] = name,
        ["description"] = description,
        ["inputSchema"] = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = CliJson.Strings(required),
            ["additionalProperties"] = false,
        },
    };

    /// <summary>The required string argument, or empty when the call names none.</summary>
    /// <param name="args">The tool arguments, or null.</param>
    /// <param name="field">The argument field name.</param>
    /// <returns>The argument value, or empty.</returns>
    static string Required(JsonObject? args, string field) =>
        args is not null && JsonValues.AsString(args[field]) is { Length: > 0 } value ? value : "";

    /// <summary>The path list argument, or empty when the call names none.</summary>
    /// <param name="args">The tool arguments, or null.</param>
    /// <returns>The path entries in order.</returns>
    static IReadOnlyList<string> Paths(JsonObject? args)
    {
        if (args?["paths"] is not JsonArray array)
        {
            return [];
        }

        return [.. array.Select(JsonValues.AsString).OfType<string>().Where(path => path.Length > 0)];
    }

    /// <summary>The task packet text for a file path or record id target.</summary>
    /// <param name="state">The warm model the packet reads.</param>
    /// <param name="target">The file path or record id.</param>
    /// <returns>The packet text and whether the target named nothing.</returns>
    static (string Text, bool IsError) TaskPacketText(ServeModel state, string target)
    {
        if (target.Length == 0)
        {
            return ($"No target: {TaskPacketName} needs a file path or record id.", true);
        }

        var model = state.Model;
        var records = RecordsFor(model, state.Config, target);

        if (records.Count == 0 && !IsFolder(state.Config, target))
        {
            return ($"No record or implements link names target '{target}'.", true);
        }

        var lines = new StringBuilder();
        lines.AppendLine($"Task packet for {target}");
        lines.AppendLine($"Records ({records.Count}):");

        foreach (var record in records)
        {
            lines.AppendLine($"- {record.Id} ({record.Type}): {record.Title}");
            lines.AppendLine($"  file: {PathText.Relative(state.Config.Root, record.File)}");
        }

        // The short sections come first and the full record fields last, so the cap trims the bulkiest part.
        var neighbours = Neighbours(state.Config, records, target);
        lines.AppendLine($"Neighbours ({neighbours.Count}):");

        foreach (var neighbour in neighbours)
        {
            lines.AppendLine($"- {neighbour}");
        }

        var matches = JudgmentFor(state.Config, target);
        var judgment = matches.Where(match => !match.IsChecked).ToList();
        var checkedCount = matches.Count - judgment.Count;

        lines.AppendLine($"Judgment provisions ({judgment.Count}; {checkedCount} checked provisions are left to model check):");

        foreach (var match in judgment)
        {
            lines.AppendLine($"- {match.Entry.Id}: {match.Statement} [{string.Join("; ", match.Reasons)}]");
        }

        lines.AppendLine("Record fields:");

        foreach (var record in records)
        {
            lines.AppendLine($"- {record.Id}:");
            lines.AppendLine(JsonValues.Text(JsonNode.Parse(record.Raw.GetRawText())));
        }

        return (Cap(lines.ToString()), false);
    }

    /// <summary>The open findings text for the paths, or for every path when none are named.</summary>
    /// <param name="state">The warm model the findings are read from.</param>
    /// <param name="paths">The file or folder paths to keep, or empty for every path.</param>
    /// <returns>The findings text, never an error: an empty filter reads as every path.</returns>
    static (string Text, bool IsError) CheckText(ServeModel state, IReadOnlyList<string> paths)
    {
        var absolute = AbsolutePaths(state.Config, paths);
        var findings = state.Findings
            .Where(finding => absolute.Count == 0 || absolute.Any(root => Under(root, finding.Path)))
            .ToList();

        var lines = new StringBuilder();
        lines.AppendLine($"{findings.Count} open findings");

        foreach (var finding in findings)
        {
            lines.AppendLine($"{PathText.Relative(state.Config.Root, finding.Path)}:{finding.Line} [{finding.Code}] {finding.Message}");
        }

        return (lines.ToString(), false);
    }

    /// <summary>The impact text for a record: its links, code and tests in both directions.</summary>
    /// <param name="state">The warm model the links are read from.</param>
    /// <param name="recordId">The record id.</param>
    /// <returns>The impact text and whether the id names no record.</returns>
    static (string Text, bool IsError) ImpactText(ServeModel state, string recordId)
    {
        var model = state.Model;

        if (recordId.Length == 0 || !model.ById.TryGetValue(recordId, out var record))
        {
            return ($"No record with id '{recordId}'.", true);
        }

        var lines = new StringBuilder();
        lines.AppendLine($"Impact for {recordId} ({record.Type}): {record.Title}");
        lines.AppendLine($"file: {PathText.Relative(state.Config.Root, record.File)}");
        lines.AppendLine("Outgoing links:");

        foreach (var (field, targets) in ModelLinks.ResolvedLinks(model, record).OrderBy(link => link.Key, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {field}: {string.Join(", ", targets.OrderBy(target => target, StringComparer.Ordinal))}");
        }

        lines.AppendLine("Incoming links:");

        foreach (var (id, fields) in Incoming(model, recordId).OrderBy(link => link.Id, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {id}: {string.Join(", ", fields.OrderBy(field => field, StringComparer.Ordinal))}");
        }

        lines.AppendLine("Code:");

        foreach (var path in model.Implements.Where(link => link.To == recordId).Select(link => link.Path).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {path}");
        }

        foreach (var follow in model.Follows.Where(link => link.To == recordId).OrderBy(link => link.Path, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {follow.Path} follows ({follow.Kind})");
        }

        lines.AppendLine("Tests:");

        foreach (var link in model.Proves.Where(link => link.Claim == recordId).OrderBy(link => link.Scenario, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {link.Scenario}: confirmed {YesNo(link.Confirmed)}, {PassingText(link.Passing)}");
        }

        lines.AppendLine("Screens:");

        foreach (var screen in model.Calls.Where(link => link.To == recordId).Select(link => link.Screen).Distinct(StringComparer.Ordinal).OrderBy(screen => screen, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {screen}");
        }

        return (lines.ToString(), false);
    }

    /// <summary>The verify slice text for a use case: its findings, scenarios with proof, code and tests.</summary>
    /// <param name="state">The warm model the slice reads.</param>
    /// <param name="useCaseId">The use-case record id.</param>
    /// <returns>The slice text and whether the id names no use case.</returns>
    static (string Text, bool IsError) VerifySliceText(ServeModel state, string useCaseId)
    {
        var model = state.Model;

        if (useCaseId.Length == 0 || !model.ById.TryGetValue(useCaseId, out var record))
        {
            return ($"No record with id '{useCaseId}'.", true);
        }

        if (!record.Type.Equals(RecordTypes.UseCase, StringComparison.Ordinal))
        {
            return ($"Record '{useCaseId}' is not a use case.", true);
        }

        var lines = new StringBuilder();
        lines.AppendLine($"Verify slice for {useCaseId}: {record.Title}");

        if (model.Completion.TryGetValue(useCaseId, out var completion))
        {
            lines.AppendLine($"completion: stage {completion.Stage}, proof {completion.Proof}");
        }

        var findings = state.Findings
            .Where(finding => useCaseId.Equals(finding.Subject, StringComparison.Ordinal)
                || string.Equals(finding.Path, record.File, StringComparison.OrdinalIgnoreCase))
            .ToList();

        lines.AppendLine($"Findings ({findings.Count}):");

        foreach (var finding in findings)
        {
            lines.AppendLine($"- {PathText.Relative(state.Config.Root, finding.Path)}:{finding.Line} [{finding.Code}] {finding.Message}");
        }

        var scenarios = ScenarioEntries(record);

        lines.AppendLine($"Scenarios ({scenarios.Count}):");

        foreach (var (id, title, result) in scenarios)
        {
            var proof = model.ProofStatuses.TryGetValue(id, out var status)
                ? $"proof {status.Status}"
                    + (status.StaleReason is { Length: > 0 } reason ? $", stale: {reason}" : "")
                : "no passing run";
            lines.AppendLine($"- {id}: {title} (result {result}; {proof})");
        }

        var scenarioIds = scenarios.Select(scenario => scenario.Id).ToHashSet(StringComparer.Ordinal);

        lines.AppendLine("Code files:");

        foreach (var path in model.Implements.Where(link => link.To == useCaseId).Select(link => link.Path).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {path}");
        }

        lines.AppendLine("Test files:");

        foreach (var test in model.Proves
            .Where(link => link.Claim == useCaseId || scenarioIds.Contains(link.Claim))
            .Select(link => link.Scenario)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(test => test, StringComparer.Ordinal))
        {
            lines.AppendLine($"- {test}");
        }

        return (lines.ToString(), false);
    }

    /// <summary>The records a task packet target implements: the record itself, or the records a path's marks name.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="target">The file path or record id.</param>
    /// <returns>The records in model order.</returns>
    static List<Record> RecordsFor(ProjectModel model, ConsumerConfig config, string target)
    {
        if (model.ById.TryGetValue(target, out var named))
        {
            return [named];
        }

        var full = AbsolutePath(config, target);

        var ids = model.Implements
            .Where(link => full is not null && SameFile(config, link.Path, full) || Slashed(link.Path).Equals(Slashed(target), StringComparison.OrdinalIgnoreCase))
            .Select(link => link.To)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count > 0)
        {
            return model.Records.Where(record => ids.Contains(record.Id, StringComparer.Ordinal)).ToList();
        }

        return model.Records
            .Where(record => full is not null && string.Equals(Path.GetFullPath(record.File), full, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Whether the target is a folder under the consumer root.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="target">The candidate folder path.</param>
    /// <returns>True when the target is an existing folder.</returns>
    static bool IsFolder(ConsumerConfig config, string target)
    {
        var full = AbsolutePath(config, target);
        return full is not null && Directory.Exists(full);
    }

    /// <summary>The neighbouring files: every file beside the records' files, or inside the target folder.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="records">The packet records.</param>
    /// <param name="target">The packet target, a folder when no record matched.</param>
    /// <returns>At most 20 root-relative paths in order.</returns>
    static List<string> Neighbours(ConsumerConfig config, List<Record> records, string target)
    {
        var folders = records.Count > 0
            ? records.Select(record => Path.GetDirectoryName(record.File)!).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : AbsolutePath(config, target) is { } targetFolder && Directory.Exists(targetFolder) ? [targetFolder] : [];

        var neighbours = new List<string>();

        foreach (var folder in folders)
        {
            string[] files;

            try
            {
                files = Directory.GetFiles(folder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            neighbours.AddRange(files
                .Select(file => PathText.Relative(config.Root, file))
                .OrderBy(path => path, StringComparer.Ordinal));
        }

        return [.. neighbours.Distinct(StringComparer.Ordinal).Take(20)];
    }

    /// <summary>The judgment provisions the rules command returns for the target: every match but the checked ones.</summary>
    /// <param name="config">The consumer configuration the rules target resolves in.</param>
    /// <param name="target">The file path or record id.</param>
    /// <returns>The retrieved provisions, or empty when the release or target cannot be read.</returns>
    static IReadOnlyList<ProvisionMatch> JudgmentFor(ConsumerConfig config, string target)
    {
        try
        {
            var home = LiteSpecHome.RequireRoot();
            var manifest = LiteSpecHome.ReadManifest(home);
            var backend = StackSelection.Stacks(manifest)
                .FirstOrDefault(stack => stack.Kind == "backend" && config.Stacks.Contains(stack.Id, StringComparer.Ordinal))
                ?.Id ?? "";

            if (PathContext.Resolve(config, backend, target) is not { } context)
            {
                return [];
            }

            var catalog = ProvisionCatalog.Load(home, manifest, config.Shape, config.Stacks, config.Extensions);
            var conventions = ConventionFiles.Load(config);
            var waivers = WaiverFiles.Load(config, DateOnly.FromDateTime(DateTime.UtcNow));
            var index = ProvisionRecordIndex.Load(config);
            return ProvisionRetriever.Retrieve(catalog, conventions, waivers, config, context, index);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    /// <summary>The records linking to the record, with the fields that carry the link.</summary>
    /// <param name="model">The built project model.</param>
    /// <param name="id">The linked record id.</param>
    /// <returns>The linking record ids with their link fields.</returns>
    static List<(string Id, IReadOnlyList<string> Fields)> Incoming(ProjectModel model, string id)
    {
        var incoming = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var other in model.Records)
        {
            if (other.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in other.Raw.EnumerateObject())
            {
                if (!RecordTypes.LinkTargets.ContainsKey(property.Name)
                    && !RecordTypes.NestedLinkFields.Contains(property.Name)
                    && property.Name is not RecordFields.Refusal and not "provision")
                {
                    continue;
                }

                if (ModelBuilder.TargetsOf(property.Value).Any(target => string.Equals(target, id, StringComparison.Ordinal)))
                {
                    if (!incoming.TryGetValue(other.Id, out var fields))
                    {
                        fields = [];
                        incoming[other.Id] = fields;
                    }

                    fields.Add(property.Name);
                }
            }
        }

        return incoming.Select(entry => (entry.Key, (IReadOnlyList<string>)entry.Value)).ToList();
    }

    /// <summary>The nested scenario entries a use-case record carries.</summary>
    /// <param name="record">The use-case record.</param>
    /// <returns>One id, title and result per scenario entry.</returns>
    static List<(string Id, string Title, string Result)> ScenarioEntries(Record record)
    {
        var scenarios = new List<(string Id, string Title, string Result)>();

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty("scenarios", out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return scenarios;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = entry.TryGetProperty("id", out var idValue) && idValue.ValueKind == JsonValueKind.String
                ? idValue.GetString() ?? ""
                : "";
            var title = entry.TryGetProperty("title", out var titleValue) && titleValue.ValueKind == JsonValueKind.String
                ? titleValue.GetString() ?? ""
                : "";
            var result = entry.TryGetProperty("result", out var resultValue) && resultValue.ValueKind == JsonValueKind.String
                ? resultValue.GetString() ?? ""
                : "";

            if (id.Length > 0)
            {
                scenarios.Add((id, title, result));
            }
        }

        return scenarios;
    }

    /// <summary>The absolute path a target names, or null when it cannot be resolved.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="target">The file path, absolute or root-relative.</param>
    /// <returns>The absolute path, or null.</returns>
    static string? AbsolutePath(ConsumerConfig config, string target)
    {
        try
        {
            return Path.IsPathFullyQualified(target)
                ? Path.GetFullPath(target)
                : Path.GetFullPath(Path.Combine(config.Root, target));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>The absolute paths the check keeps, skipping what cannot be resolved.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="paths">The file or folder paths.</param>
    /// <returns>The resolved absolute paths.</returns>
    static List<string> AbsolutePaths(ConsumerConfig config, IReadOnlyList<string> paths)
    {
        var absolute = new List<string>();

        foreach (var path in paths)
        {
            if (AbsolutePath(config, path) is { } full)
            {
                absolute.Add(full);
            }
        }

        return absolute;
    }

    /// <summary>Whether the finding path sits on one kept path: the path itself or under it.</summary>
    /// <param name="kept">The kept absolute path.</param>
    /// <param name="finding">The finding's absolute path.</param>
    /// <returns>True when the finding sits on the kept path.</returns>
    static bool Under(string kept, string finding) =>
        string.Equals(finding, kept, StringComparison.OrdinalIgnoreCase)
        || finding.StartsWith(kept.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether an implements-link path names the resolved target file.</summary>
    /// <param name="config">The consumer configuration holding the root.</param>
    /// <param name="linkPath">The implements-link path, relative to the root.</param>
    /// <param name="full">The resolved target file.</param>
    /// <returns>True when both name one file.</returns>
    static bool SameFile(ConsumerConfig config, string linkPath, string full)
    {
        try
        {
            return string.Equals(Path.GetFullPath(Path.Combine(config.Root, linkPath)), full, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>The path with directory separators slashed, for a separator-blind compare.</summary>
    /// <param name="path">The path.</param>
    /// <returns>The path with forward slashes.</returns>
    static string Slashed(string path) => path.Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>Yes or no for a confirmed flag.</summary>
    /// <param name="confirmed">Whether the proof confirmed the claim.</param>
    /// <returns>Yes when confirmed, else no.</returns>
    static string YesNo(bool confirmed) => confirmed ? "yes" : "no";

    /// <summary>The passing text for a proves link.</summary>
    /// <param name="passing">Whether the proving test passed, or null for no run.</param>
    /// <returns>Passing, failing, or no run.</returns>
    static string PassingText(bool? passing) => passing switch
    {
        true => "passing",
        false => "failing",
        _ => "no run",
    };
}
