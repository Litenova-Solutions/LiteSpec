using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteSpec.Model;

/// <summary>
///     Writes a value in the one canonical form of the strict YAML subset
///     <see cref="YamlText"/> reads: block style, two-space indentation, sequences
///     indented under their key, plain scalars where the core schema reads them back
///     as the same string, double quotes otherwise, and literal blocks for multi-line
///     text. Reading the output back with <see cref="YamlText.Parse"/> gives the same value.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
public static class YamlWriter
{
    /// <summary>A key that needs no quotes.</summary>
    private static readonly Regex PlainKey = new Regex("^[A-Za-z0-9_$][A-Za-z0-9_$.\\-/]*$", RegexOptions.CultureInvariant);

    /// <summary>
    ///     Writes the value as a YAML document ending in one line end.
    /// </summary>
    /// <param name="root">The value to write.</param>
    /// <param name="keyOrder">
    ///     The key order for each object, or null to keep written order. Keys the
    ///     function leaves out follow in written order.
    /// </param>
    /// <returns>The YAML text.</returns>
    public static string Write(JsonText root, Func<JsonText, IReadOnlyList<string>>? keyOrder = null)
    {
        var built = new StringBuilder();

        switch (root.Kind)
        {
            case JsonKind.Object when root.Properties.Count > 0:
                WriteObject(built, root, 0, keyOrder);
                break;
            case JsonKind.Array when root.Items.Count > 0:
                WriteArray(built, root, 0, keyOrder);
                break;
            default:
                built.Append(Inline(root) ?? Quote(root.GetString() ?? string.Empty)).Append('\n');
                break;
        }

        return built.ToString();
    }

    /// <summary>Whether a string can be written plain and read back as the same string.</summary>
    /// <param name="text">The string.</param>
    /// <returns>True when no quotes are needed.</returns>
    public static bool IsPlainSafe(string text)
    {
        if (text.Length == 0 || text[0] == ' ' || text[text.Length - 1] == ' ' || YamlText.ResolvesToNonString(text))
        {
            return false;
        }

        if ("-?:,[]{}#&*!|>'\"%@`".IndexOf(text[0]) >= 0)
        {
            return false;
        }

        if (text.IndexOf(": ", StringComparison.Ordinal) >= 0
            || text.IndexOf(" #", StringComparison.Ordinal) >= 0
            || text.EndsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var character in text)
        {
            if (character < ' ' || character == '\u007f' || character == '﻿')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Writes an object's properties, one per line, at the indentation.</summary>
    /// <param name="built">The text built so far.</param>
    /// <param name="value">The object.</param>
    /// <param name="indent">The indentation of its keys.</param>
    /// <param name="keyOrder">The key order function.</param>
    /// <param name="firstPrefix">The text that replaces the first key's indentation, for an object that starts on a dash line.</param>
    private static void WriteObject(StringBuilder built, JsonText value, int indent, Func<JsonText, IReadOnlyList<string>>? keyOrder, string? firstPrefix = null)
    {
        var first = true;

        foreach (var property in Ordered(value, keyOrder))
        {
            built.Append(first && firstPrefix is not null ? firstPrefix : new string(' ', indent));
            first = false;
            built.Append(PlainKey.IsMatch(property.Key) && !YamlText.ResolvesToNonString(property.Key) ? property.Key : Quote(property.Key));
            built.Append(':');
            WriteValue(built, property.Value, indent, keyOrder);
        }
    }

    /// <summary>Writes an array's items, one dash each, at the indentation.</summary>
    /// <param name="built">The text built so far.</param>
    /// <param name="value">The array.</param>
    /// <param name="indent">The indentation of its dashes.</param>
    /// <param name="keyOrder">The key order function.</param>
    private static void WriteArray(StringBuilder built, JsonText value, int indent, Func<JsonText, IReadOnlyList<string>>? keyOrder)
    {
        foreach (var item in value.Items)
        {
            if (item.Kind == JsonKind.Object && item.Properties.Count > 0)
            {
                WriteObject(built, item, indent + 2, keyOrder, new string(' ', indent) + "- ");
                continue;
            }

            built.Append(' ', indent).Append('-');

            if (item.Kind == JsonKind.Array && item.Items.Count > 0)
            {
                built.Append('\n');
                WriteArray(built, item, indent + 2, keyOrder);
                continue;
            }

            WriteValue(built, item, indent, keyOrder);
        }
    }

    /// <summary>Writes the value after a key's colon or a dash, ending the line.</summary>
    /// <param name="built">The text built so far.</param>
    /// <param name="value">The value.</param>
    /// <param name="indent">The indentation of the key or dash.</param>
    /// <param name="keyOrder">The key order function.</param>
    private static void WriteValue(StringBuilder built, JsonText value, int indent, Func<JsonText, IReadOnlyList<string>>? keyOrder)
    {
        switch (value.Kind)
        {
            case JsonKind.Object when value.Properties.Count > 0:
                built.Append('\n');
                WriteObject(built, value, indent + 2, keyOrder);
                return;
            case JsonKind.Array when value.Items.Count > 0:
                built.Append('\n');
                WriteArray(built, value, indent + 2, keyOrder);
                return;
            case JsonKind.String:
                var text = value.GetString() ?? string.Empty;

                if (text.IndexOf('\n') >= 0 && IsBlockSafe(text))
                {
                    WriteLiteral(built, text, indent + 2);
                    return;
                }

                built.Append(' ').Append(IsPlainSafe(text) ? text : Quote(text)).Append('\n');
                return;
            default:
                built.Append(' ').Append(Inline(value)).Append('\n');
                return;
        }
    }

    /// <summary>Writes multi-line text as a literal block.</summary>
    /// <param name="built">The text built so far.</param>
    /// <param name="text">The text.</param>
    /// <param name="indent">The indentation of the block's lines.</param>
    private static void WriteLiteral(StringBuilder built, string text, int indent)
    {
        var trailing = 0;

        while (trailing < text.Length && text[text.Length - 1 - trailing] == '\n')
        {
            trailing++;
        }

        var header = trailing == 0 ? "|-" : trailing == 1 ? "|" : "|+";
        built.Append(' ').Append(header).Append('\n');

        foreach (var line in text.Substring(0, text.Length - trailing).Split('\n'))
        {
            if (line.Length > 0)
            {
                built.Append(' ', indent).Append(line);
            }

            built.Append('\n');
        }

        for (var extra = 1; extra < trailing; extra++)
        {
            built.Append('\n');
        }
    }

    /// <summary>Whether multi-line text reads back unchanged from a literal block.</summary>
    /// <param name="text">The text.</param>
    /// <returns>True when a literal block holds it.</returns>
    private static bool IsBlockSafe(string text)
    {
        var content = text.TrimEnd('\n');

        if (content.Length == 0 || content[0] == ' ' || content[0] == '\n')
        {
            return false;
        }

        foreach (var line in content.Split('\n'))
        {
            if (line.Length > 0 && line.Trim().Length == 0)
            {
                return false;
            }

            if (line.EndsWith(" ", StringComparison.Ordinal) || line.EndsWith("\t", StringComparison.Ordinal))
            {
                return false;
            }

            foreach (var character in line)
            {
                if ((character < ' ' && character != '\t') || character == '\u007f' || character == '﻿')
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>The inline text of a scalar or an empty collection, or null for a string.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The inline text.</returns>
    private static string? Inline(JsonText value) => value.Kind switch
    {
        JsonKind.Object => "{}",
        JsonKind.Array => "[]",
        JsonKind.True => "true",
        JsonKind.False => "false",
        JsonKind.Null => "null",
        JsonKind.Number => value.ToJson(),
        _ => null,
    };

    /// <summary>The properties in the key order, then the rest in written order.</summary>
    /// <param name="value">The object.</param>
    /// <param name="keyOrder">The key order function.</param>
    /// <returns>The ordered properties.</returns>
    private static IEnumerable<KeyValuePair<string, JsonText>> Ordered(JsonText value, Func<JsonText, IReadOnlyList<string>>? keyOrder)
    {
        if (keyOrder is null)
        {
            return value.Properties;
        }

        var ordered = new List<KeyValuePair<string, JsonText>>(value.Properties.Count);
        var taken = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in keyOrder(value))
        {
            if (taken.Contains(key))
            {
                continue;
            }

            foreach (var property in value.Properties)
            {
                if (property.Key == key)
                {
                    ordered.Add(property);
                    taken.Add(key);
                    break;
                }
            }
        }

        foreach (var property in value.Properties)
        {
            if (taken.Add(property.Key))
            {
                ordered.Add(property);
            }
        }

        return ordered;
    }

    /// <summary>A double-quoted string with JSON escapes.</summary>
    /// <param name="text">The string.</param>
    /// <returns>The quoted string.</returns>
    private static string Quote(string text) => JsonText.Of(JsonKind.String, 1, text).ToJson();
}
