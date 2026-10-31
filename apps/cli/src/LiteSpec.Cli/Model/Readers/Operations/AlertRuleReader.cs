namespace LiteSpec.Model.Readers.Operations;

/// <summary>
///     Reads alert rules from the groups files the project lists in paths.alerts.
///     Groups hold rules, each rule named by alert or title and carrying labels.
/// </summary>
/// <follows>litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert</follows>
static class AlertRuleReader
{
    /// <summary>Reads every alert rule under the configured alert paths.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>Every alert rule under the configured alert paths, in file order.</returns>
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
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The alert file paths: a named file as named, and every YAML file below a named folder.</returns>
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

    /// <summary>The alert rules one file states; empty when the file cannot be read.</summary>
    /// <param name="file">The source file path to read.</param>
    /// <returns>The alert rules stated in the file; empty when the file cannot be read.</returns>
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
    /// <param name="lines">The file lines in order, aligned with the parsed line information.</param>
    /// <param name="infos">The parsed line information, aligned with the file lines.</param>
    /// <param name="index">The index of the line where the list opens.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <param name="rules">The alert rules collected so far, added to in place.</param>
    /// <returns>The index of the line past the groups list.</returns>
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
    /// <param name="lines">The file lines in order, aligned with the parsed line information.</param>
    /// <param name="infos">The parsed line information, aligned with the file lines.</param>
    /// <param name="start">The index of the first line of the block.</param>
    /// <param name="end">The index one past the last line of the block.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <param name="rules">The alert rules collected so far, added to in place.</param>
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
    /// <param name="lines">The file lines in order, aligned with the parsed line information.</param>
    /// <param name="infos">The parsed line information, aligned with the file lines.</param>
    /// <param name="key">The index of the line that opens the list.</param>
    /// <param name="indent">The indent of the key line that opens the list.</param>
    /// <param name="end">The index one past the last line of the block.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <param name="rules">The alert rules collected so far, added to in place.</param>
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
    /// <param name="lines">The file lines in order, aligned with the parsed line information.</param>
    /// <param name="infos">The parsed line information, aligned with the file lines.</param>
    /// <param name="start">The index of the first line of the block.</param>
    /// <param name="end">The index one past the last line of the block.</param>
    /// <param name="file">The path of the file to read.</param>
    /// <param name="rules">The alert rules collected so far, added to in place.</param>
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
    /// <param name="infos">The parsed line information, aligned with the file lines.</param>
    /// <param name="value">The text after the labels key, holding a flow map or nothing.</param>
    /// <param name="at">The index of the line that carries the key.</param>
    /// <param name="end">The index one past the last line of the block.</param>
    /// <param name="indent">The indent of the key line that opens the list.</param>
    /// <param name="found">The values collected so far, added to in place.</param>
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

    /// <summary>
    ///     The parsed form of one file line: its indent, whether it is blank or a sequence item, and its
    ///     content.
    /// </summary>
    /// <param name="line">The raw file line to parse.</param>
    /// <returns>The line information: its indent, blank and item flags, and its content.</returns>
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
    /// <param name="info">The parsed line whose mapping is read.</param>
    /// <returns>The key and value the line holds, or null when it holds no mapping.</returns>
    private static (string Key, string Value)? KeyOf(LineInfo info) =>
        info is { IsBlank: false, IsItem: false } ? SplitKey(info.Content) : null;

    /// <summary>Splits "key: value" at its first colon, or null when the text holds no key.</summary>
    /// <param name="content">The text to split at its first colon.</param>
    /// <returns>The key and value split at the first colon, or null when the text holds no colon.</returns>
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
    /// <param name="value">The scalar text to strip of a trailing comment and surrounding quotes.</param>
    /// <returns>The value without a trailing comment or surrounding quotes.</returns>
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
    /// <param name="value">The flow map text to split, including its surrounding braces.</param>
    /// <returns>The key and value pairs of the flow map, in written order.</returns>
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
