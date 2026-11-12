using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace LiteSpec.Model;

/// <summary>
///     Reads YAML into the same values <see cref="JsonText"/> holds, with the 1-based
///     line of every value. The reader accepts the YAML YamlDotNet reads, except it
///     refuses an empty document, a document that does not start at the first column,
///     document markers, tabs in indentation, folded block scalars and duplicate keys,
///     each with the line it sits on. Scalars resolve by the YAML 1.2 core schema; a
///     scalar the core schema reads as a number that JSON cannot hold is refused.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class YamlText
{
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
    /// <exception cref="JsonTextException">The text is no YAML; the message names the line.</exception>
    public static JsonText Parse(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
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

        var carriage = text.IndexOf('\r');

        if (carriage >= 0)
        {
            var carriageLine = 0;

            for (var scan = 0; scan < carriage; scan++)
            {
                if (text[scan] == '\n')
                {
                    carriageLine++;
                }
            }

            throw Fail(carriageLine, "no carriage return inside a line");
        }

        var lines = text.Split('\n');
        var first = -1;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (line.Trim().Length == 0)
            {
                continue;
            }

            var spaces = 0;

            while (spaces < line.Length && line[spaces] == ' ')
            {
                spaces++;
            }

            if (spaces < line.Length && line[spaces] == '\t')
            {
                throw Fail(index, "spaces, not tabs, for indentation");
            }

            if (first < 0)
            {
                first = index;
            }

            if (spaces == 0)
            {
                var content = line.TrimEnd(' ');

                if (content.StartsWith("---", StringComparison.Ordinal) || content.StartsWith("...", StringComparison.Ordinal))
                {
                    throw Fail(index, "one document with no --- or ... markers");
                }
            }
        }

        if (first < 0)
        {
            throw Fail(0, "a value; the document is empty");
        }

        var firstSpaces = 0;

        while (firstSpaces < lines[first].Length && lines[first][firstSpaces] == ' ')
        {
            firstSpaces++;
        }

        if (firstSpaces != 0)
        {
            throw Fail(first, "the document to start at the first column");
        }

        text = QuoteReservedIndicators(text);

        YamlDocument document;

        try
        {
            using var reader = new StringReader(text);
            var stream = new YamlStream();
            stream.Load(reader);

            if (stream.Documents.Count == 0)
            {
                throw Fail(0, "a value; the document is empty");
            }

            if (stream.Documents.Count > 1)
            {
                var second = (int)(stream.Documents[1].RootNode?.Start.Line ?? 1);

                throw Fail(second - 1, "one document with no --- or ... markers");
            }

            document = stream.Documents[0];
        }
        catch (YamlException exception)
        {
            if (DuplicateKeyOf(exception) is string duplicate)
            {
                throw Fail(YamlLine(exception), $"no second '{duplicate}' key in one mapping");
            }

            throw Fail(YamlLine(exception), "valid YAML");
        }

        if (document.RootNode is null)
        {
            throw Fail(0, "a value; the document is empty");
        }

        return ToValue(document.RootNode);
    }

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

    /// <summary>The 0-based line a YAML error sits on.</summary>
    /// <param name="exception">The YAML error.</param>
    /// <returns>The 0-based line, or 0 when the error names none.</returns>
    private static int YamlLine(YamlException exception)
    {
        var line = (int)exception.Start.Line - 1;

        return line < 0 ? 0 : line;
    }

    /// <summary>The duplicate key a YAML error reports, or null when the error reports none.</summary>
    /// <param name="exception">The YAML error.</param>
    /// <returns>The duplicate key, or null.</returns>
    private static string? DuplicateKeyOf(YamlException exception)
    {
        const string prefix = "Duplicate key ";
        var message = exception.Message ?? string.Empty;
        var at = message.IndexOf(prefix, StringComparison.Ordinal);

        if (at < 0)
        {
            return null;
        }

        var key = message.Substring(at + prefix.Length).Trim();

        return key.Length > 0 ? key : null;
    }

    /// <summary>Quotes every block value opening with a reserved indicator, so it reads as a string.</summary>
    /// <param name="text">The YAML text.</param>
    /// <returns>The text with reserved values quoted, on the same lines.</returns>
    private static string QuoteReservedIndicators(string text)
    {
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = QuoteReservedValue(lines[index]);
        }

        return string.Join("\n", lines);
    }

    /// <summary>Quotes the value when it opens with @ or a backtick, keeping any comment.</summary>
    /// <param name="line">The line.</param>
    /// <returns>The line with the value quoted, or the line unchanged.</returns>
    private static string QuoteReservedValue(string line)
    {
        var start = ValueStart(line);

        if (start < 0 || start >= line.Length)
        {
            return line;
        }

        var opener = line[start];

        if (opener != '@' && opener != '`')
        {
            return line;
        }

        var end = line.IndexOf(" #", start, StringComparison.Ordinal);
        string value;
        string comment;

        if (end >= 0)
        {
            value = line.Substring(start, end - start).TrimEnd(' ');
            comment = line.Substring(end);
        }
        else
        {
            value = line.Substring(start).TrimEnd(' ', '\t');
            comment = string.Empty;
        }

        if (value.Length == 0)
        {
            return line;
        }

        return line.Substring(0, start) + "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" + comment;
    }

    /// <summary>The offset where a block mapping or sequence value starts, or -1 when the line holds none.</summary>
    /// <param name="line">The line.</param>
    /// <returns>The value offset, or -1.</returns>
    private static int ValueStart(string line)
    {
        var index = 0;

        while (index < line.Length && line[index] == ' ')
        {
            index++;
        }

        if (index < line.Length && line[index] == '-' && index + 1 < line.Length && line[index + 1] == ' ')
        {
            var entry = index + 2;

            while (entry < line.Length && line[entry] == ' ')
            {
                entry++;
            }

            return entry < line.Length ? entry : -1;
        }

        var inDouble = false;
        var inSingle = false;

        for (var scan = index; scan < line.Length; scan++)
        {
            var character = line[scan];

            if (inDouble)
            {
                if (character == '\\' && scan + 1 < line.Length)
                {
                    scan++;
                }
                else if (character == '"')
                {
                    inDouble = false;
                }

                continue;
            }

            if (inSingle)
            {
                if (character == '\'' && scan + 1 < line.Length && line[scan + 1] == '\'')
                {
                    scan++;
                }
                else if (character == '\'')
                {
                    inSingle = false;
                }

                continue;
            }

            if (character == '"')
            {
                inDouble = true;
            }
            else if (character == '\'')
            {
                inSingle = true;
            }
            else if (character == ':' && scan + 1 < line.Length && line[scan + 1] == ' ')
            {
                var value = scan + 2;

                while (value < line.Length && line[value] == ' ')
                {
                    value++;
                }

                return value < line.Length ? value : -1;
            }
            else if (character == '#')
            {
                return -1;
            }
        }

        return -1;
    }

    /// <summary>Reads one YAML node as a JSON value with its line.</summary>
    /// <param name="node">The YAML node.</param>
    /// <returns>The JSON value.</returns>
    private static JsonText ToValue(YamlNode node)
    {
        switch (node.NodeType)
        {
            case YamlNodeType.Mapping:
                return ToObject((YamlMappingNode)node);
            case YamlNodeType.Sequence:
                return ToArray((YamlSequenceNode)node);
            case YamlNodeType.Scalar:
                return ToScalar((YamlScalarNode)node);
            default:
                throw Fail((int)node.Start.Line - 1, "valid YAML");
        }
    }

    /// <summary>Reads one YAML mapping as an object, refusing a second key.</summary>
    /// <param name="mapping">The YAML mapping.</param>
    /// <returns>The object.</returns>
    private static JsonText ToObject(YamlMappingNode mapping)
    {
        var properties = new List<KeyValuePair<string, JsonText>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pair in mapping.Children)
        {
            if (pair.Key.NodeType != YamlNodeType.Scalar)
            {
                throw Fail((int)pair.Key.Start.Line - 1, "a 'key: value' line");
            }

            var keyNode = (YamlScalarNode)pair.Key;

            if (keyNode.Style == ScalarStyle.SingleQuoted)
            {
                throw Fail((int)keyNode.Start.Line - 1, "a 'key: value' line");
            }

            var key = keyNode.Value ?? string.Empty;

            if (!seen.Add(key))
            {
                throw Fail((int)keyNode.Start.Line - 1, $"no second '{key}' key in one mapping");
            }

            properties.Add(new KeyValuePair<string, JsonText>(key, ToValue(pair.Value)));
        }

        return JsonText.Of(JsonKind.Object, (int)mapping.Start.Line, string.Empty, properties);
    }

    /// <summary>Reads one YAML sequence as an array.</summary>
    /// <param name="sequence">The YAML sequence.</param>
    /// <returns>The array.</returns>
    private static JsonText ToArray(YamlSequenceNode sequence)
    {
        var items = new List<JsonText>();

        foreach (var item in sequence.Children)
        {
            items.Add(ToValue(item));
        }

        return JsonText.Of(JsonKind.Array, (int)sequence.Start.Line, string.Empty, null, items);
    }

    /// <summary>Reads one YAML scalar by the core schema, refusing a folded block and a non-JSON number.</summary>
    /// <param name="scalar">The YAML scalar.</param>
    /// <returns>The scalar value.</returns>
    private static JsonText ToScalar(YamlScalarNode scalar)
    {
        var line = (int)scalar.Start.Line;

        if (scalar.Style == ScalarStyle.Folded)
        {
            throw Fail(line - 1, "a | block; folded > blocks are not allowed");
        }

        var value = scalar.Value ?? string.Empty;

        if (scalar.Style == ScalarStyle.SingleQuoted || scalar.Style == ScalarStyle.DoubleQuoted || scalar.Style == ScalarStyle.Literal)
        {
            return JsonText.Of(JsonKind.String, line, value);
        }

        switch (value)
        {
            case "":
            case "~":
            case "null":
            case "Null":
            case "NULL":
                return JsonText.Of(JsonKind.Null, line, string.Empty);
            case "true":
            case "True":
            case "TRUE":
                return JsonText.Of(JsonKind.True, line, string.Empty);
            case "false":
            case "False":
            case "FALSE":
                return JsonText.Of(JsonKind.False, line, string.Empty);
        }

        if (CoreNumber.IsMatch(value))
        {
            if (!JsonNumber.IsMatch(value))
            {
                throw Fail(line - 1, $"a JSON number or a quoted string, not '{value}'");
            }

            return JsonText.Of(JsonKind.Number, line, value);
        }

        return JsonText.Of(JsonKind.String, line, value);
    }

    /// <summary>Builds the error for the line.</summary>
    /// <param name="line">The line index.</param>
    /// <param name="want">What was wanted.</param>
    /// <returns>The error.</returns>
    private static JsonTextException Fail(int line, string want) =>
        new JsonTextException($"Expected {want} at line {line + 1}.", line + 1);
}
