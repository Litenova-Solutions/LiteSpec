using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Reads the strict YAML subset a record file is written in, into the same values
///     <see cref="JsonText"/> holds. The subset is block style only: mappings, sequences,
///     plain, double-quoted and single-quoted scalars, and literal block scalars
///     (<c>|</c>, <c>|-</c>, <c>|+</c>). Comments, anchors, aliases, tags, document
///     markers, folded scalars, flow collections other than <c>[]</c> and <c>{}</c>,
///     multi-line plain scalars, tabs in indentation and duplicate keys are refused,
///     each with the line it sits on. Scalars resolve by the YAML 1.2 core schema; a
///     scalar the core schema reads as a number that JSON cannot hold is refused.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class YamlText
{
    /// <summary>A plain mapping key followed by its colon.</summary>
    private static readonly Regex PlainKey = new Regex("^([A-Za-z0-9_$][A-Za-z0-9_$.\\-/]*):(?: |$)", RegexOptions.CultureInvariant);

    /// <summary>A double-quoted mapping key followed by its colon.</summary>
    private static readonly Regex QuotedKey = new Regex("^\"((?:[^\"\\\\]|\\\\.)*)\":(?: |$)", RegexOptions.CultureInvariant);

    /// <summary>A number JSON can hold.</summary>
    private static readonly Regex JsonNumber = new Regex("^-?(0|[1-9][0-9]*)(\\.[0-9]+)?([eE][-+]?[0-9]+)?$", RegexOptions.CultureInvariant);

    /// <summary>Any number the YAML 1.2 core schema resolves.</summary>
    private static readonly Regex CoreNumber = new Regex(
        "^([-+]?[0-9]+|0o[0-7]+|0x[0-9a-fA-F]+|[-+]?(\\.[0-9]+|[0-9]+(\\.[0-9]*)?)([eE][-+]?[0-9]+)?|[-+]?\\.(inf|Inf|INF)|\\.(nan|NaN|NAN))$",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     Parses the text as one YAML document.
    /// </summary>
    /// <param name="text">The YAML text.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="JsonTextException">The text is outside the subset; the message names the line.</exception>
    public static JsonText Parse(string text) => new Parser(text).Parse();

    /// <summary>Whether the plain text would resolve to anything but a string.</summary>
    /// <param name="text">The scalar text.</param>
    /// <returns>True when the core schema reads it as null, a boolean or a number.</returns>
    public static bool ResolvesToNonString(string text) =>
        text.Length == 0
        || text == "~"
        || text == "null" || text == "Null" || text == "NULL"
        || text == "true" || text == "True" || text == "TRUE"
        || text == "false" || text == "False" || text == "FALSE"
        || CoreNumber.IsMatch(text);

    /// <summary>The parser over the document's lines.</summary>
    private sealed class Parser
    {
        /// <summary>The deepest nesting the parser accepts.</summary>
        private const int MaxDepth = 64;

        /// <summary>The document's lines, without line ends.</summary>
        private readonly string[] lines;

        /// <summary>The indentation of each line; -1 for a blank line.</summary>
        private readonly int[] indents;

        /// <summary>The index of the next line to read.</summary>
        private int at;

        /// <summary>The current nesting depth.</summary>
        private int depth;

        /// <summary>Initializes a new instance of the <see cref="Parser"/> class.</summary>
        /// <param name="text">The YAML text.</param>
        public Parser(string text)
        {
            if (text.Length > 0 && text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            // A checkout may write CRLF line ends, and a block cut from a longer file
            // (document frontmatter) can end on the carriage return of its last line.
            text = text.Replace("\r\n", "\n");

            if (text.EndsWith("\r", StringComparison.Ordinal))
            {
                text = text.Substring(0, text.Length - 1);
            }

            lines = text.Split('\n');
            indents = new int[lines.Length];

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                if (line.EndsWith("\r", StringComparison.Ordinal))
                {
                    throw Fail(index, "no carriage return inside a line");
                }

                var spaces = 0;

                while (spaces < line.Length && line[spaces] == ' ')
                {
                    spaces++;
                }

                if (spaces < line.Length && line[spaces] == '\t' && line.Trim().Length > 0)
                {
                    throw Fail(index, "spaces, not tabs, for indentation");
                }

                indents[index] = line.Trim().Length == 0 ? -1 : spaces;
            }
        }

        /// <summary>Parses the whole document as one block value.</summary>
        /// <returns>The parsed value.</returns>
        public JsonText Parse()
        {
            SkipBlank();

            if (at >= lines.Length)
            {
                throw Fail(0, "a value; the document is empty");
            }

            if (indents[at] != 0)
            {
                throw Fail(at, "the document to start at the first column");
            }

            var value = ParseBlock(0);
            SkipBlank();

            if (at < lines.Length)
            {
                throw Fail(at, "no text after the document's last value at this indentation");
            }

            return value;
        }

        /// <summary>Parses the mapping or sequence that starts on the current line.</summary>
        /// <param name="indent">The block's indentation.</param>
        /// <returns>The block value.</returns>
        private JsonText ParseBlock(int indent)
        {
            if (++depth > MaxDepth)
            {
                throw Fail(at, "less nesting");
            }

            try
            {
                return IsItem(Content(at)) ? ParseSequence(indent) : ParseMapping(indent);
            }
            finally
            {
                depth--;
            }
        }

        /// <summary>Parses a block mapping whose keys sit at the indentation.</summary>
        /// <param name="indent">The mapping's indentation.</param>
        /// <returns>The mapping.</returns>
        private JsonText ParseMapping(int indent)
        {
            var start = at;
            var properties = new List<KeyValuePair<string, JsonText>>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            while (true)
            {
                SkipBlank();

                if (at >= lines.Length || indents[at] < indent)
                {
                    break;
                }

                if (indents[at] > indent)
                {
                    throw Fail(at, "a key at the mapping's indentation; continued text belongs in a | block");
                }

                var content = Content(at);

                if (IsItem(content))
                {
                    break;
                }

                CheckDocumentMarker(at, content);

                if (!TryKey(content, out var key, out var rest))
                {
                    throw Fail(at, "a 'key: value' line");
                }

                if (!seen.Add(key))
                {
                    throw Fail(at, $"no second '{key}' key in one mapping");
                }

                var keyLine = at;
                at++;
                properties.Add(new KeyValuePair<string, JsonText>(key, ParseValue(rest, keyLine, indent, true)));
            }

            return JsonText.Of(JsonKind.Object, start + 1, string.Empty, properties);
        }

        /// <summary>Parses a block sequence whose dashes sit at the indentation.</summary>
        /// <param name="indent">The sequence's indentation.</param>
        /// <returns>The sequence.</returns>
        private JsonText ParseSequence(int indent)
        {
            var start = at;
            var items = new List<JsonText>();

            while (true)
            {
                SkipBlank();

                if (at >= lines.Length || indents[at] != indent || !IsItem(Content(at)))
                {
                    if (at < lines.Length && indents[at] > indent)
                    {
                        throw Fail(at, "an item at the sequence's indentation; continued text belongs in a | block");
                    }

                    break;
                }

                var content = Content(at);
                var after = content.Length == 1 ? string.Empty : content.Substring(1);
                var gap = 0;

                while (gap < after.Length && after[gap] == ' ')
                {
                    gap++;
                }

                var itemText = after.Substring(gap);
                var itemLine = at;

                if (itemText.Length > 0 && (TryKey(itemText, out _, out _) || IsItem(itemText)))
                {
                    // A mapping or a nested sequence that starts on the dash line: its
                    // first line continues at the column after the dash and its gap.
                    var column = indent + 1 + gap;
                    lines[at] = new string(' ', column) + itemText;
                    indents[at] = column;

                    if (++depth > MaxDepth)
                    {
                        throw Fail(at, "less nesting");
                    }

                    try
                    {
                        items.Add(IsItem(itemText) ? ParseSequence(column) : ParseMapping(column));
                    }
                    finally
                    {
                        depth--;
                    }

                    continue;
                }

                at++;
                items.Add(ParseValue(itemText, itemLine, indent, false));
            }

            return JsonText.Of(JsonKind.Array, start + 1, string.Empty, null, items);
        }

        /// <summary>Parses the value after a key or a dash: inline, a block scalar, or a nested block.</summary>
        /// <param name="rest">The text after the key's colon or the dash.</param>
        /// <param name="line">The line index of the key or dash.</param>
        /// <param name="indent">The indentation of the key or dash.</param>
        /// <param name="mappingValue">Whether the value belongs to a mapping key.</param>
        /// <returns>The value.</returns>
        private JsonText ParseValue(string rest, int line, int indent, bool mappingValue)
        {
            if (rest.Length > 0)
            {
                if (rest[0] == '|')
                {
                    return ParseLiteral(rest, line, indent);
                }

                return ParseInline(rest, line);
            }

            var next = NextContentLine();

            if (next < lines.Length && indents[next] > indent)
            {
                at = next;
                return ParseBlock(indents[next]);
            }

            // A mapping key may hold a sequence whose dashes sit at the key's own column.
            if (mappingValue && next < lines.Length && indents[next] == indent && IsItem(Content(next)))
            {
                at = next;
                return ParseSequence(indent);
            }

            return JsonText.Of(JsonKind.Null, line + 1, string.Empty);
        }

        /// <summary>Parses a literal block scalar whose header is on the line.</summary>
        /// <param name="header">The header text: | with an optional chomping indicator.</param>
        /// <param name="line">The header's line index.</param>
        /// <param name="indent">The indentation of the key or dash that holds it.</param>
        /// <returns>The string.</returns>
        private JsonText ParseLiteral(string header, int line, int indent)
        {
            var chomp = header.Length > 1 ? header[1] : ' ';

            if (header.Length > 2 || (header.Length == 2 && chomp != '-' && chomp != '+'))
            {
                throw Fail(line, "a block header of |, |- or |+ and nothing after it");
            }

            var next = NextContentLine();
            var contentIndent = next < lines.Length && indents[next] > indent ? indents[next] : -1;
            var collected = new List<string>();

            if (contentIndent > 0)
            {
                while (at < lines.Length && (indents[at] == -1 || indents[at] >= contentIndent))
                {
                    var text = lines[at];
                    collected.Add(indents[at] == -1 ? (text.Length > contentIndent ? text.Substring(contentIndent) : string.Empty) : text.Substring(contentIndent));
                    at++;
                }
            }

            // Trailing blank lines are chomped, then put back by the indicator.
            var trailing = 0;

            while (collected.Count > 0 && collected[collected.Count - 1].Trim().Length == 0)
            {
                collected.RemoveAt(collected.Count - 1);
                trailing++;
            }

            // A blank line ends the block only when no content line follows it, so
            // give back the blank lines the block did not keep.
            at -= trailing;
            var body = string.Join("\n", collected);

            if (collected.Count > 0)
            {
                body += chomp == '-' ? string.Empty : "\n";

                if (chomp == '+')
                {
                    body += new string('\n', trailing);
                }
            }

            return JsonText.Of(JsonKind.String, line + 1, body);
        }

        /// <summary>Parses a scalar written on the line after its key or dash.</summary>
        /// <param name="text">The scalar text.</param>
        /// <param name="line">The line index.</param>
        /// <returns>The scalar.</returns>
        private static JsonText ParseInline(string text, int line)
        {
            var first = text[0];

            if (first == '"')
            {
                var end = ClosingQuote(text, line);
                var tail = text.Substring(end + 1);

                if (tail.Trim().Length > 0)
                {
                    throw Fail(line, "nothing after the closing quote");
                }

                return JsonText.Of(JsonKind.String, line + 1, Unescape(text.Substring(1, end - 1), line));
            }

            if (first == '\'')
            {
                var built = new StringBuilder();
                var index = 1;

                while (true)
                {
                    if (index >= text.Length)
                    {
                        throw Fail(line, "a closing single quote on the same line");
                    }

                    if (text[index] == '\'')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '\'')
                        {
                            built.Append('\'');
                            index += 2;
                            continue;
                        }

                        break;
                    }

                    built.Append(text[index]);
                    index++;
                }

                if (text.Substring(index + 1).Trim().Length > 0)
                {
                    throw Fail(line, "nothing after the closing quote");
                }

                return JsonText.Of(JsonKind.String, line + 1, built.ToString());
            }

            var plain = text.TrimEnd(' ');

            if (plain == "[]")
            {
                return JsonText.Of(JsonKind.Array, line + 1, string.Empty);
            }

            if (plain == "{}")
            {
                return JsonText.Of(JsonKind.Object, line + 1, string.Empty);
            }

            if ("[{>&*!%@`#".IndexOf(first) >= 0)
            {
                throw Fail(line, first == '#'
                    ? "no comment"
                    : first == '>'
                        ? "a | block; folded > blocks are not allowed"
                        : first == '[' || first == '{'
                            ? "block style; flow collections other than [] and {} are not allowed"
                            : $"no '{first}': anchors, aliases, tags and directives are not allowed; quote the value");
            }

            if ((first == '-' || first == '?' || first == ':') && (plain.Length == 1 || plain[1] == ' '))
            {
                throw Fail(line, $"a quoted value; a plain value cannot start with '{first} '");
            }

            if (plain.IndexOf(" #", StringComparison.Ordinal) >= 0)
            {
                throw Fail(line, "no comment; quote a value that holds ' #'");
            }

            if (plain.IndexOf(": ", StringComparison.Ordinal) >= 0 || plain.EndsWith(":", StringComparison.Ordinal))
            {
                throw Fail(line, "a quoted value; a plain value cannot hold ': '");
            }

            switch (plain)
            {
                case "~":
                case "null":
                case "Null":
                case "NULL":
                    return JsonText.Of(JsonKind.Null, line + 1, string.Empty);
                case "true":
                case "True":
                case "TRUE":
                    return JsonText.Of(JsonKind.True, line + 1, string.Empty);
                case "false":
                case "False":
                case "FALSE":
                    return JsonText.Of(JsonKind.False, line + 1, string.Empty);
            }

            if (CoreNumber.IsMatch(plain))
            {
                if (!JsonNumber.IsMatch(plain))
                {
                    throw Fail(line, $"a JSON number or a quoted string, not '{plain}'");
                }

                return JsonText.Of(JsonKind.Number, line + 1, plain);
            }

            return JsonText.Of(JsonKind.String, line + 1, plain);
        }

        /// <summary>The index of the quote that closes the double-quoted text.</summary>
        /// <param name="text">The text, starting with the opening quote.</param>
        /// <param name="line">The line index.</param>
        /// <returns>The closing quote's index.</returns>
        private static int ClosingQuote(string text, int line)
        {
            for (var index = 1; index < text.Length; index++)
            {
                if (text[index] == '\\')
                {
                    index++;
                    continue;
                }

                if (text[index] == '"')
                {
                    return index;
                }
            }

            throw Fail(line, "a closing double quote on the same line");
        }

        /// <summary>Resolves the escapes of a double-quoted string.</summary>
        /// <param name="text">The text between the quotes.</param>
        /// <param name="line">The line index.</param>
        /// <returns>The string.</returns>
        private static string Unescape(string text, int line)
        {
            var built = new StringBuilder(text.Length);

            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];

                if (character != '\\')
                {
                    built.Append(character);
                    continue;
                }

                index++;

                if (index >= text.Length)
                {
                    throw Fail(line, "an escape after the backslash");
                }

                switch (text[index])
                {
                    case '"': built.Append('"'); break;
                    case '\\': built.Append('\\'); break;
                    case '/': built.Append('/'); break;
                    case 'b': built.Append('\b'); break;
                    case 'f': built.Append('\f'); break;
                    case 'n': built.Append('\n'); break;
                    case 'r': built.Append('\r'); break;
                    case 't': built.Append('\t'); break;
                    case 'u':
                        if (index + 4 >= text.Length)
                        {
                            throw Fail(line, "four hex digits after \\u");
                        }

                        if (!int.TryParse(text.Substring(index + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            throw Fail(line, "four hex digits after \\u");
                        }

                        built.Append((char)code);
                        index += 4;
                        break;
                    default:
                        throw Fail(line, $"a JSON escape, not '\\{text[index]}'");
                }
            }

            return built.ToString();
        }

        /// <summary>Reads a mapping key from the line's content.</summary>
        /// <param name="content">The line without its indentation.</param>
        /// <param name="key">The key.</param>
        /// <param name="rest">The text after the colon and its space.</param>
        /// <returns>Whether the content starts with a key.</returns>
        private bool TryKey(string content, out string key, out string rest)
        {
            var plain = PlainKey.Match(content);

            if (plain.Success)
            {
                key = plain.Groups[1].Value;
                rest = content.Substring(plain.Length).Trim(' ');
                return true;
            }

            var quoted = QuotedKey.Match(content);

            if (quoted.Success)
            {
                key = Unescape(quoted.Groups[1].Value, at);
                rest = content.Substring(quoted.Length).Trim(' ');
                return true;
            }

            key = string.Empty;
            rest = string.Empty;
            return false;
        }

        /// <summary>Refuses a document marker line.</summary>
        /// <param name="line">The line index.</param>
        /// <param name="content">The line's content.</param>
        private void CheckDocumentMarker(int line, string content)
        {
            if (indents[line] == 0 && (content.StartsWith("---", StringComparison.Ordinal) || content.StartsWith("...", StringComparison.Ordinal)))
            {
                throw Fail(line, "one document with no --- or ... markers");
            }

            if (content[0] == '#')
            {
                throw Fail(line, "no comment");
            }
        }

        /// <summary>Whether the content is a sequence item: a dash alone or a dash and a space.</summary>
        /// <param name="content">The line without its indentation.</param>
        /// <returns>True for an item.</returns>
        private static bool IsItem(string content) =>
            content.Length > 0 && content[0] == '-' && (content.Length == 1 || content[1] == ' ');

        /// <summary>The line's text after its indentation.</summary>
        /// <param name="line">The line index.</param>
        /// <returns>The content.</returns>
        private string Content(int line) => lines[line].Substring(Math.Max(indents[line], 0)).TrimEnd(' ');

        /// <summary>Moves past blank lines.</summary>
        private void SkipBlank()
        {
            while (at < lines.Length && indents[at] == -1)
            {
                at++;
            }
        }

        /// <summary>The index of the next non-blank line, without moving.</summary>
        /// <returns>The index, or the line count.</returns>
        private int NextContentLine()
        {
            var index = at;

            while (index < lines.Length && indents[index] == -1)
            {
                index++;
            }

            return index;
        }

        /// <summary>Builds the error for the line.</summary>
        /// <param name="line">The line index.</param>
        /// <param name="want">What was wanted.</param>
        /// <returns>The error.</returns>
        private static JsonTextException Fail(int line, string want) =>
            new JsonTextException($"Expected {want} at line {line + 1}.", line + 1);
    }
}
