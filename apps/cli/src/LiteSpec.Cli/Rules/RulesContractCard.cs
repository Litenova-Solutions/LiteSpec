using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Rules;

/// <summary>
///     The contract card of <c>litespec rules</c>: one compact restatement of each use case,
///     reaction or job the target names, printed before the provisions so the agent reads the
///     record's contract without opening its file.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class RulesContractCard
{
    /// <summary>An XML implements mark.</summary>
    static readonly Regex XmlImplements = new("<implements>([^<>]+)</implements>", RegexOptions.Compiled);

    /// <summary>A generated C# implements mark attribute.</summary>
    static readonly Regex AttributeImplements = new("\\[Implements\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>A frontend <c>litespec</c> constant implements entry.</summary>
    static readonly Regex ConstImplements = new("\\bimplements\\s*:\\s*[\"']([^\"']+)[\"']", RegexOptions.Compiled);

    /// <summary>The effect verbs in record order.</summary>
    static readonly string[] EffectVerbs = ["runs", "sends", "calls", "queries", "commands"];

    /// <summary>The access field naming the identity the use case runs as beside its grant.</summary>
    const string RunsAs = "runsAs";

    /// <summary>Prints one card per use case, reaction or job the target names: the record id itself, or each such record a code file implements.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="target">The rules target.</param>
    public static void Print(ConsumerConfig config, PathContext target)
    {
        foreach (var id in CardIds(config, target))
        {
            if (Load(config, id) is not { } root)
            {
                continue;
            }

            foreach (var line in Lines(root))
            {
                CliHost.Out.WriteLine(line);
            }
        }
    }

    /// <summary>The record ids a card restates: the record id itself, or each use case, reaction or job the code file implements, in file order.</summary>
    /// <param name="config">The consumer configuration holding the consumer root.</param>
    /// <param name="target">The rules target.</param>
    /// <returns>The card record ids, each once.</returns>
    static List<string> CardIds(ConsumerConfig config, PathContext target)
    {
        if (target.IsRecord)
        {
            return IsCardId(target.Display) ? [target.Display] : [];
        }

        if (target.RelativePath.Length == 0)
        {
            return [];
        }

        var absolute = Path.Combine(config.Root, target.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        string text;

        try
        {
            if (!File.Exists(absolute))
            {
                return [];
            }

            text = File.ReadAllText(absolute);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        return ImplementsOf(text).Where(IsCardId).Distinct(StringComparer.Ordinal).ToList();
    }

    /// <summary>Whether the identifier names a record a card restates: a use case, reaction or job.</summary>
    /// <param name="id">The record identifier.</param>
    /// <returns>True for a use case, reaction or job.</returns>
    static bool IsCardId(string id) =>
        PathContext.ArtifactOf(id) is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job;

    /// <summary>The implements marks in the file text, in file order.</summary>
    /// <param name="text">The file text.</param>
    /// <returns>Each marked identifier, trimmed.</returns>
    static List<string> ImplementsOf(string text)
    {
        var marks = new List<(int Index, string Id)>();

        foreach (var pattern in new[] { XmlImplements, AttributeImplements, ConstImplements })
        {
            foreach (Match match in pattern.Matches(text))
            {
                var id = match.Groups[1].Value.Trim();

                if (id.Length > 0)
                {
                    marks.Add((match.Index, id));
                }
            }
        }

        marks.Sort((left, right) => left.Index.CompareTo(right.Index));
        return marks.Select(mark => mark.Id).ToList();
    }

    /// <summary>Loads one card record from the consumer's record roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="id">The record identifier.</param>
    /// <returns>The parsed record object, or null when no record file holds the id.</returns>
    static JsonElement? Load(ConsumerConfig config, string id)
    {
        foreach (var root in config.RecordRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(root, "*.yaml", SearchOption.AllDirectories);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.OrderBy(name => name, StringComparer.Ordinal))
            {
                string text;

                try
                {
                    text = File.ReadAllText(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                if (!RecordFile.TryParse(text, out var parsed, out _, out _)
                    || parsed.ValueKind != JsonValueKind.Object
                    || !string.Equals(Text(parsed, RecordFields.Id), id, StringComparison.Ordinal))
                {
                    continue;
                }

                return parsed;
            }
        }

        return null;
    }

    /// <summary>One card's lines: the header, then one line per contract item, leaving out what the record does not state.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The card lines.</returns>
    static List<string> Lines(JsonElement root)
    {
        var lines = new List<string>();
        var id = Text(root, RecordFields.Id);
        var title = Text(root, RecordFields.Title);
        lines.Add(title.Length > 0 ? $"Contract: {id} - {title}" : $"Contract: {id}");

        var head = new List<string>();
        var kind = Text(root, RecordFields.Kind);

        if (kind.Length > 0)
        {
            head.Add($"kind: {kind}");
        }

        if (AccessOf(root) is { Length: > 0 } access)
        {
            head.Add(access);
        }

        if (head.Count > 0)
        {
            lines.Add(string.Join("; ", head));
        }

        foreach (var entry in Objects(root, RecordFields.Input))
        {
            var name = Text(entry, RecordFields.Name);
            var type = Text(entry, RecordFields.Type);

            if (name.Length == 0 || type.Length == 0)
            {
                continue;
            }

            lines.Add(RequiredOf(entry) is { } required
                ? $"input {name}: {type}, {(required ? "required" : "optional")}"
                : $"input {name}: {type}");
        }

        lines.AddRange(OutputLines(root));

        foreach (var entry in Objects(root, RecordFields.Records))
        {
            if (Text(entry, RecordFields.Type) != RecordTypes.Result)
            {
                continue;
            }

            var resultId = Text(entry, RecordFields.Id);
            var outcome = Text(entry, RecordFields.Outcome);

            if (resultId.Length == 0 || outcome.Length == 0)
            {
                continue;
            }

            if (outcome == "refusal")
            {
                var code = RefusalCode.CodeOf(Text(entry, RecordFields.Refusal));
                var sentence = Text(entry, RecordFields.Sentence);
                lines.Add(sentence.Length > 0
                    ? $"refusal {resultId}: {code} - {sentence}"
                    : $"refusal {resultId}: {code}");
            }
            else
            {
                var resultTitle = Text(entry, RecordFields.Title);
                lines.Add(resultTitle.Length > 0
                    ? $"result {resultId}: {outcome} - {resultTitle}"
                    : $"result {resultId}: {outcome}");
            }
        }

        foreach (var entry in Objects(root, RecordFields.Outcomes))
        {
            var name = Text(entry, RecordFields.Name);
            var meaning = Text(entry, RecordFields.Meaning);

            if (name.Length > 0 && meaning.Length > 0)
            {
                lines.Add($"outcome {name}: {meaning}");
            }
        }

        ListLine(lines, root, RecordFields.TriggeredBy);
        ListLine(lines, root, RecordFields.Raises);
        ListLine(lines, root, RecordFields.Reads);
        ListLine(lines, root, RecordFields.Writes);

        foreach (var entry in Objects(root, RecordFields.Effects))
        {
            if (EffectOf(entry) is { Length: > 0 } effect)
            {
                lines.Add(effect);
            }
        }

        ListLine(lines, root, RecordFields.ComputedBy);

        foreach (var entry in Objects(root, RecordFields.Records))
        {
            if (Text(entry, RecordFields.Type) != RecordTypes.Scenario)
            {
                continue;
            }

            var scenarioId = Text(entry, RecordFields.Id);
            var scenarioTitle = Text(entry, RecordFields.Title);

            if (scenarioId.Length == 0)
            {
                continue;
            }

            lines.Add(scenarioTitle.Length > 0
                ? $"scenario {scenarioId}: {scenarioTitle}"
                : $"scenario {scenarioId}");
        }

        return lines;
    }

    /// <summary>The access display of a use case: its grant, tenant and runsAs.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The access display, or empty when the record states no access.</returns>
    static string AccessOf(JsonElement root)
    {
        if (!root.TryGetProperty(RecordFields.Access, out var access) || access.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        var grant = Text(access, RecordFields.Grant);
        var tenant = Text(access, RecordFields.Tenant);
        var runsAs = Text(access, RunsAs);

        if (grant.Length == 0 && tenant.Length == 0 && runsAs.Length == 0)
        {
            return "";
        }

        var parts = new List<string>();

        if (grant.Length > 0)
        {
            parts.Add($"grant={grant}");
        }

        if (tenant.Length > 0)
        {
            parts.Add($"tenant={tenant}");
        }

        if (runsAs.Length > 0)
        {
            parts.Add($"{RunsAs}={runsAs}");
        }

        return $"access: {string.Join(' ', parts)}";
    }

    /// <summary>One record's output lines: one per field, with the group name for a query's grouped output.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <returns>The output lines.</returns>
    static List<string> OutputLines(JsonElement root)
    {
        var lines = new List<string>();

        if (!root.TryGetProperty(RecordFields.Output, out var output))
        {
            return lines;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = Text(entry, RecordFields.Name);
                var type = Text(entry, RecordFields.Type);

                if (name.Length > 0 && type.Length > 0)
                {
                    lines.Add($"output {name}: {type}");
                }
            }

            return lines;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind != JsonValueKind.Object
                    || !group.Value.TryGetProperty(RecordFields.Fields, out var fields)
                    || fields.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var entry in fields.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var name = Text(entry, RecordFields.Name);
                    var type = Text(entry, RecordFields.Type);

                    if (name.Length > 0 && type.Length > 0)
                    {
                        lines.Add($"output {group.Name}.{name}: {type}");
                    }
                }
            }
        }

        return lines;
    }

    /// <summary>One effect's display: its verb and target with its timing.</summary>
    /// <param name="entry">The parsed effect object.</param>
    /// <returns>The effect display, or empty when the entry names no edge.</returns>
    static string EffectOf(JsonElement entry)
    {
        foreach (var verb in EffectVerbs)
        {
            if (entry.TryGetProperty(verb, out var target)
                && target.ValueKind == JsonValueKind.String
                && target.GetString() is { Length: > 0 } id)
            {
                var timing = Text(entry, RecordFields.Timing);
                return timing.Length > 0 ? $"effect {verb} {id} ({timing})" : $"effect {verb} {id}";
            }
        }

        return "";
    }

    /// <summary>Adds one comma-separated line for a string array field, leaving out an empty field.</summary>
    /// <param name="lines">The card lines the field joins.</param>
    /// <param name="root">The parsed record object.</param>
    /// <param name="name">The field name.</param>
    static void ListLine(List<string> lines, JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
        {
            var ids = value.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.String)
                .Select(entry => entry.GetString()!)
                .Where(id => id.Length > 0)
                .ToList();

            if (ids.Count > 0)
            {
                lines.Add($"{name}: {string.Join(", ", ids)}");
            }
        }
    }

    /// <summary>Whether the contract field must be present: its required flag, or null when it states none.</summary>
    /// <param name="entry">The parsed contract field object.</param>
    /// <returns>The required flag, or null.</returns>
    static bool? RequiredOf(JsonElement entry) =>
        entry.TryGetProperty(RecordFields.Required, out var required)
        && required.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? required.GetBoolean()
            : null;

    /// <summary>The objects of an array field, in written order.</summary>
    /// <param name="root">The parsed record object.</param>
    /// <param name="name">The field name.</param>
    /// <returns>Each object entry of the array field, or none when the field holds no array.</returns>
    static List<JsonElement> Objects(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object).ToList();
    }

    /// <summary>One string property of a JSON object, or empty when it holds none.</summary>
    /// <param name="item">The JSON object.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property text, or empty.</returns>
    static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
