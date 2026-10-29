namespace LiteSpec.Model.Readers.Operations;

/// <summary>One alert rule: its name, labels and own text.</summary>
/// <param name="Name">The rule name from its alert or title key.</param>
/// <param name="File">The absolute file path.</param>
/// <param name="Line">The 1-based line of the rule item.</param>
/// <param name="Labels">The rule labels by key.</param>
/// <param name="Text">The rule's own lines, for the threshold comparison.</param>
sealed record AlertRule(string Name, string File, int Line, IReadOnlyDictionary<string, string> Labels, string Text);

/// <summary>
///     Reads alert rules from the groups files the project lists in paths.alerts.
///     Groups hold rules, each rule named by alert or title and carrying labels.
/// </summary>
/// <follows>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</follows>
static class AlertRuleReader
{
    /// <summary>Reads every alert rule under the configured alert paths.</summary>
    public static List<AlertRule> Read(ConsumerConfig config)
    {
        var rules = new List<AlertRule>();

        foreach (var file in Files(config))
        {
            rules.AddRange(ReadFile(file));
        }

        return rules;
    }

    /// <summary>Every alert file: a named file as named, every YAML file below a named folder.</summary>
    internal static IReadOnlyList<string> Files(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var entry in config.Alerts)
        {
            if (File.Exists(entry))
            {
                files.Add(entry);
            }
            else if (Directory.Exists(entry))
            {
                files.AddRange(SourceTree.Files(entry, ".yaml", ".yml"));
            }
        }

        files.Sort(StringComparer.Ordinal);

        return files;
    }

    private static List<AlertRule> ReadFile(string file)
    {
        string[] lines;

        try
        {
            lines = File.ReadAllLines(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var infos = lines.Select(ParseLine).ToList();
        var rules = new List<AlertRule>();
        var index = 0;

        while (index < infos.Count)
        {
            if (infos[index].Indent == 0 && KeyOf(infos[index]) is { Key: "groups", Value: "" })
            {
                index = ReadGroups(lines, infos, index, file, rules);
            }
            else
            {
                index++;
            }
        }

        return rules;
    }

    /// <summary>Reads the groups list opening at the line, adding its rules, and returns the line past it.</summary>
    private static int ReadGroups(string[] lines, List<LineInfo> infos, int index, string file, List<AlertRule> rules)
    {
        var indent = infos[index].Indent;
        var itemIndent = -1;
        var start = -1;

        index++;

        while (index < infos.Count)
        {
            var info = infos[index];

            if (info.IsBlank)
            {
                index++;
                continue;
            }

            if (info.Indent <= indent)
            {
                break;
            }

            if (info.IsItem && (itemIndent < 0 || info.Indent == itemIndent))
            {
                if (itemIndent < 0)
                {
                    itemIndent = info.Indent;
                }

                if (start >= 0)
                {
                    ReadGroup(lines, infos, start, index, file, rules);
                }

                start = index;
            }

            index++;
        }

        if (start >= 0)
        {
            ReadGroup(lines, infos, start, index, file, rules);
        }

        return index;
    }

    /// <summary>Reads one group block, adding the rules its rules list holds.</summary>
    private static void ReadGroup(string[] lines, List<LineInfo> infos, int start, int end, string file, List<AlertRule> rules)
    {
        var itemIndent = infos[start].Indent;

        if (SplitKey(infos[start].Content) is { Key: "rules", Value: "" })
        {
            ReadRules(lines, infos, start, itemIndent + 2, end, file, rules);

            return;
        }

        for (var index = start + 1; index < end; index++)
        {
            var info = infos[index];

            if (info.IsBlank || info.Indent <= itemIndent)
            {
                continue;
            }

            if (KeyOf(info) is { Key: "rules", Value: "" })
            {
                ReadRules(lines, infos, index, info.Indent, end, file, rules);

                return;
            }
        }
    }

    /// <summary>Reads the rules list under the key indent, adding each named rule.</summary>
    private static void ReadRules(string[] lines, List<LineInfo> infos, int key, int indent, int end, string file, List<AlertRule> rules)
    {
        var itemIndent = -1;
        var start = -1;
        var index = key + 1;

        while (index < end)
        {
            var info = infos[index];

            if (info.IsBlank)
            {
                index++;
                continue;
            }

            if (info.Indent <= indent)
            {
                break;
            }

            if (info.IsItem && (itemIndent < 0 || info.Indent == itemIndent))
            {
                if (itemIndent < 0)
                {
                    itemIndent = info.Indent;
                }

                if (start >= 0)
                {
                    ReadRule(lines, infos, start, index, file, rules);
                }

                start = index;
            }

            index++;
        }

        if (start >= 0)
        {
            ReadRule(lines, infos, start, index, file, rules);
        }
    }

    /// <summary>Reads one rule block, keeping the rule when alert or title names it.</summary>
    private static void ReadRule(string[] lines, List<LineInfo> infos, int start, int end, string file, List<AlertRule> rules)
    {
        var itemIndent = infos[start].Indent;
        var keyIndent = itemIndent + 2;

        for (var index = start + 1; index < end; index++)
        {
            var info = infos[index];

            if (!info.IsBlank && !info.IsItem && info.Indent > itemIndent && SplitKey(info.Content) is not null)
            {
                keyIndent = info.Indent;

                break;
            }
        }

        var keys = new List<(string Key, string Value, int At)>();

        if (SplitKey(infos[start].Content) is { } inline)
        {
            keys.Add((inline.Key, inline.Value, start));
        }

        for (var index = start + 1; index < end; index++)
        {
            var info = infos[index];

            if (info.IsBlank || info.IsItem || info.Indent != keyIndent)
            {
                continue;
            }

            if (SplitKey(info.Content) is { } split)
            {
                keys.Add((split.Key, split.Value, index));
            }
        }

        string? alert = null;
        string? title = null;

        foreach (var (key, value, _) in keys)
        {
            if (key == "alert" && alert is null)
            {
                alert = Clean(value);
            }
            else if (key == "title" && title is null)
            {
                title = Clean(value);
            }
        }

        var name = alert is { Length: > 0 } ? alert : title;

        if (name is not { Length: > 0 })
        {
            return;
        }

        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var labelAt = keys.FindIndex(entry => entry.Key == "labels");

        if (labelAt >= 0)
        {
            var at = keys[labelAt];
            var indent = at.At == start ? keyIndent : infos[at.At].Indent;

            ReadLabels(infos, at.Value, at.At, end, indent, labels);
        }

        rules.Add(new AlertRule(name, file, start + 1, labels, string.Join('\n', lines.Skip(start).Take(end - start))));
    }

    /// <summary>Reads the labels of one rule, from a flow map or the entries below the key.</summary>
    private static void ReadLabels(List<LineInfo> infos, string value, int at, int end, int indent, Dictionary<string, string> found)
    {
        if (value.StartsWith('{'))
        {
            foreach (var (key, entry) in SplitFlow(value))
            {
                found.TryAdd(key, entry);
            }

            return;
        }

        if (value.Length > 0)
        {
            return;
        }

        var entryIndent = -1;

        for (var index = at + 1; index < end; index++)
        {
            var info = infos[index];

            if (info.IsBlank)
            {
                continue;
            }

            if (info.Indent <= indent)
            {
                break;
            }

            if (info.IsItem)
            {
                continue;
            }

            if (entryIndent < 0)
            {
                if (SplitKey(info.Content) is null)
                {
                    continue;
                }

                entryIndent = info.Indent;
            }

            if (info.Indent != entryIndent)
            {
                continue;
            }

            if (SplitKey(info.Content) is { } split)
            {
                found.TryAdd(split.Key, Clean(split.Value));
            }
        }
    }

    /// <summary>One source line prepared for the reader: indent, item shape and content.</summary>
    /// <param name="Indent">The leading whitespace width.</param>
    /// <param name="IsBlank">Whether the line carries no mapping: empty, a comment or a document marker.</param>
    /// <param name="IsItem">Whether the line opens a sequence item.</param>
    /// <param name="Content">The line past its indent, or past "- " for an item.</param>
    sealed record LineInfo(int Indent, bool IsBlank, bool IsItem, string Content);

    private static LineInfo ParseLine(string line)
    {
        var indent = 0;

        while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
        {
            indent++;
        }

        var content = line[indent..].TrimEnd();

        if (content.Length == 0 || content.StartsWith('#') || content is "---" or "...")
        {
            return new LineInfo(indent, true, false, "");
        }

        if (content.StartsWith('-') && (content.Length == 1 || content[1] == ' ' || content[1] == '\t'))
        {
            return new LineInfo(indent, false, true, content[1..].Trim());
        }

        return new LineInfo(indent, false, false, content);
    }

    /// <summary>The mapping the line holds, or null when it holds none.</summary>
    private static (string Key, string Value)? KeyOf(LineInfo info) =>
        info is { IsBlank: false, IsItem: false } ? SplitKey(info.Content) : null;

    /// <summary>Splits "key: value" at its first colon, or null when the text holds no key.</summary>
    private static (string Key, string Value)? SplitKey(string content)
    {
        var colon = content.IndexOf(':');

        if (colon <= 0)
        {
            return null;
        }

        var key = content[..colon].Trim();

        if (key.Length == 0 || key.Any(char.IsWhiteSpace))
        {
            return null;
        }

        return (key, content[(colon + 1)..].Trim());
    }

    /// <summary>Strips a trailing comment and surrounding quotes from a scalar value.</summary>
    private static string Clean(string value)
    {
        var text = value.Trim();
        var single = false;
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];

            if (current == '\'' && !quoted)
            {
                single = !single;
            }
            else if (current == '"' && !single)
            {
                quoted = !quoted;
            }
            else if (current == '#' && !single && !quoted && index > 0 && char.IsWhiteSpace(text[index - 1]))
            {
                text = text[..index].TrimEnd();

                break;
            }
        }

        if (text.Length >= 2
            && ((text.StartsWith('"') && text.EndsWith('"')) || (text.StartsWith('\'') && text.EndsWith('\''))))
        {
            text = text[1..^1];
        }

        return text;
    }

    /// <summary>Splits a flow map into its pairs, keeping quoted commas whole.</summary>
    private static List<(string Key, string Value)> SplitFlow(string value)
    {
        var pairs = new List<(string Key, string Value)>();
        var inner = value.Trim();

        if (inner.StartsWith('{') && inner.EndsWith('}'))
        {
            inner = inner[1..^1];
        }

        var single = false;
        var quoted = false;
        var depth = 0;
        var start = 0;

        for (var index = 0; index <= inner.Length; index++)
        {
            var end = index == inner.Length;
            var current = end ? ',' : inner[index];

            if (!end && current == '\'' && !quoted)
            {
                single = !single;
            }
            else if (!end && current == '"' && !single)
            {
                quoted = !quoted;
            }
            else if (!end && !single && !quoted && (current == '{' || current == '['))
            {
                depth++;
            }
            else if (!end && !single && !quoted && (current == '}' || current == ']'))
            {
                depth--;
            }
            else if ((end || current == ',') && !single && !quoted && depth == 0)
            {
                if (SplitKey(inner[start..index].Trim()) is { } split)
                {
                    pairs.Add((split.Key, Clean(split.Value)));
                }

                start = index + 1;
            }
        }

        return pairs;
    }
}
