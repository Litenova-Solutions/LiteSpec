using System.Text.RegularExpressions;

namespace LiteSpec.Cli.Context;

/// <summary>
///     Compacts record and explanation text for the context file. Every step
///     deletes text or collapses whitespace; values, identifiers and wording
///     stay untouched.
/// </summary>
/// <follows>litespec/core/model#organize-the-book-by-the-record-layers</follows>
static partial class ContextCompact
{
    /// <summary>Normalizes line endings to LF.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The text with LF line endings.</returns>
    public static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    /// <summary>
    ///     Compacts one YAML record file: drops comments and blank lines, drops
    ///     keys with an empty value, keeps the indentation, and collapses runs
    ///     of spaces inside scalar text to one.
    /// </summary>
    /// <param name="text">The record file text.</param>
    /// <returns>The compacted text.</returns>
    public static string Yaml(string text)
    {
        var stripped = Normalize(text).Split('\n')
            .Select(line => StripYamlComment(line).TrimEnd())
            .ToList();
        var kept = new List<string>(stripped.Count);

        for (var index = 0; index < stripped.Count; index++)
        {
            var line = stripped[index];

            if (line.Length == 0 || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (IsEmptyKey(line, stripped, index))
            {
                continue;
            }

            kept.Add(CollapseSpaces(line));
        }

        return string.Join("\n", kept);
    }

    /// <summary>
    ///     Compacts one Markdown explanation file: strips HTML comments and
    ///     trailing whitespace, collapses repeated blank lines to one, and
    ///     drops the first title line when it equals the record's title.
    /// </summary>
    /// <param name="text">The explanation file text.</param>
    /// <param name="title">The title of the record the file explains.</param>
    /// <returns>The compacted text.</returns>
    public static string Markdown(string text, string title)
    {
        var lines = HtmlComment().Replace(Normalize(text), "")
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();
        SkipLeadingBlanks(lines);

        if (lines.Count > 0
            && title.Length > 0
            && TitleOf(lines[0]) is { } heading
            && string.Equals(heading, title, StringComparison.Ordinal))
        {
            lines.RemoveAt(0);
            SkipLeadingBlanks(lines);
        }

        var kept = new List<string>(lines.Count);
        var blank = false;

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                if (!blank)
                {
                    kept.Add("");
                }

                blank = true;
            }
            else
            {
                kept.Add(line);
                blank = false;
            }
        }

        while (kept.Count > 0 && kept[^1].Length == 0)
        {
            kept.RemoveAt(kept.Count - 1);
        }

        return string.Join("\n", kept);
    }

    /// <summary>Drops the leading blank lines of the list, in place.</summary>
    /// <param name="lines">The lines to trim.</param>
    static void SkipLeadingBlanks(List<string> lines)
    {
        while (lines.Count > 0 && lines[0].Length == 0)
        {
            lines.RemoveAt(0);
        }
    }

    /// <summary>The title a Markdown first line carries, or null when it is not a level-one heading.</summary>
    /// <param name="line">The first line.</param>
    /// <returns>The heading text, or null.</returns>
    static string? TitleOf(string line)
    {
        if (!line.StartsWith('#') || (line.Length > 1 && line[1] == '#'))
        {
            return null;
        }

        var heading = line[1..].Trim();

        return heading.Length > 0 ? heading : null;
    }

    /// <summary>Strips the YAML comment a line carries, keeping text inside quotes.</summary>
    /// <param name="line">The line to strip.</param>
    /// <returns>The line without its comment.</returns>
    static string StripYamlComment(string line)
    {
        var single = false;
        var dbl = false;

        for (var index = 0; index < line.Length; index++)
        {
            var current = line[index];

            if (dbl && current == '\\')
            {
                index++;
                continue;
            }

            if (current == '\'' && !dbl)
            {
                single = !single;
                continue;
            }

            if (current == '"' && !single)
            {
                dbl = !dbl;
                continue;
            }

            if (current == '#'
                && !single
                && !dbl
                && (index == 0 || line[index - 1] == ' ' || line[index - 1] == '\t'))
            {
                return line[..index];
            }
        }

        return line;
    }

    /// <summary>
    ///     Whether the line states a key with an empty value: a flow empty
    ///     (`[]`, `{}`), an empty string, `null`, or a bare key that heads no
    ///     nested block.
    /// </summary>
    /// <param name="line">The comment-stripped line.</param>
    /// <param name="lines">Every comment-stripped line of the file.</param>
    /// <param name="index">The line's position in the file.</param>
    /// <returns>True when the line states a key with an empty value.</returns>
    static bool IsEmptyKey(string line, List<string> lines, int index)
    {
        var indent = IndentOf(line);
        var rest = line[indent..];

        if (rest.Length == 0 || rest[0] is '-' or '?' or ':')
        {
            return false;
        }

        var colon = KeyColon(rest);

        if (colon < 0 || rest[..colon].Length == 0)
        {
            return false;
        }

        var value = rest[(colon + 1)..].Trim();

        if (value.Length > 0)
        {
            return value is "[]" or "{}" or "\"\"" or "''" or "null" or "Null" or "NULL" or "~";
        }

        // A bare key heads a nested block when the next content line stands
        // deeper; otherwise it states a null.
        for (var next = index + 1; next < lines.Count; next++)
        {
            if (string.IsNullOrWhiteSpace(lines[next]))
            {
                continue;
            }

            // YAML lets a list stand at its key's indent, so a dash line there
            // is the key's value too.
            var nextIndent = IndentOf(lines[next]);
            return nextIndent < indent || (nextIndent == indent && !lines[next][nextIndent..].StartsWith('-'));
        }

        return true;
    }

    /// <summary>The position of the colon ending the line's key, outside quotes, or -1.</summary>
    /// <param name="rest">The line without its indentation.</param>
    /// <returns>The colon position, or -1 when the line states no key.</returns>
    static int KeyColon(string rest)
    {
        var single = false;
        var dbl = false;

        for (var index = 0; index < rest.Length; index++)
        {
            var current = rest[index];

            if (dbl && current == '\\')
            {
                index++;
                continue;
            }

            if (current == '\'' && !dbl)
            {
                single = !single;
                continue;
            }

            if (current == '"' && !single)
            {
                dbl = !dbl;
                continue;
            }

            if (current == ':' && !single && !dbl)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>The leading whitespace width of the line.</summary>
    /// <param name="line">The line to measure.</param>
    /// <returns>How many leading spaces and tabs the line carries.</returns>
    static int IndentOf(string line)
    {
        var indent = 0;

        while (indent < line.Length && line[indent] is ' ' or '\t')
        {
            indent++;
        }

        return indent;
    }

    /// <summary>Collapses runs of spaces after the indentation to one space.</summary>
    /// <param name="line">The line to collapse.</param>
    /// <returns>The line with single spaces.</returns>
    static string CollapseSpaces(string line)
    {
        var indent = IndentOf(line);

        return line[..indent] + SpaceRun().Replace(line[indent..], " ");
    }

    /// <summary>A run of two or more spaces.</summary>
    /// <returns>The regular expression matching a space run.</returns>
    [GeneratedRegex(" {2,}")]
    private static partial Regex SpaceRun();

    /// <summary>An HTML comment, on one line or across lines.</summary>
    /// <returns>The regular expression matching an HTML comment.</returns>
    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlComment();
}
