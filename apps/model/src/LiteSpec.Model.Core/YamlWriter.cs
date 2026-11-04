using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace LiteSpec.Model;

/// <summary>
///     Writes a value as a YAML document YamlDotNet emits: block style, plain scalars
///     where the core schema reads them back as the same string, double quotes
///     otherwise, and literal blocks for multi-line text. Reading the output back with
///     <see cref="YamlText.Parse"/> gives the same value.
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
        var stream = new YamlStream();
        stream.Documents.Add(new YamlDocument(ToNode(root, keyOrder, true)));

        using var buffered = new StringWriter();
        stream.Save(buffered, assignAnchors: false);

        var text = buffered.ToString().Replace("\r\n", "\n");

        if (text.StartsWith("---", StringComparison.Ordinal))
        {
            var end = text.IndexOf('\n');
            text = end < 0 ? string.Empty : text.Substring(end + 1);
        }

        if (text.EndsWith("...\n", StringComparison.Ordinal))
        {
            text = text.Substring(0, text.Length - 4);
        }

        if (!text.EndsWith("\n", StringComparison.Ordinal))
        {
            text += "\n";
        }

        return text;
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
            if (character < ' ' || character == '\u007f' || character == '\uFEFF')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Reads one JSON value as a YAML node with its style.</summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="keyOrder">The key order function.</param>
    /// <param name="isRoot">Whether the value is the document root.</param>
    /// <returns>The YAML node.</returns>
    private static YamlNode ToNode(JsonText value, Func<JsonText, IReadOnlyList<string>>? keyOrder, bool isRoot)
    {
        switch (value.Kind)
        {
            case JsonKind.Object:
                var mapping = new YamlMappingNode();

                foreach (var property in Ordered(value, keyOrder))
                {
                    mapping.Add(ToKey(property.Key), ToNode(property.Value, keyOrder, false));
                }

                return mapping;
            case JsonKind.Array:
                var sequence = new YamlSequenceNode();

                foreach (var item in value.Items)
                {
                    sequence.Add(ToNode(item, keyOrder, false));
                }

                return sequence;
            case JsonKind.String:
                var text = value.GetString() ?? string.Empty;

                if (isRoot)
                {
                    return new YamlScalarNode(text) { Style = ScalarStyle.DoubleQuoted };
                }

                if (text.IndexOf('\n') >= 0 && IsBlockSafe(text))
                {
                    return new YamlScalarNode(text) { Style = ScalarStyle.Literal };
                }

                return new YamlScalarNode(text) { Style = IsPlainSafe(text) ? ScalarStyle.Plain : ScalarStyle.DoubleQuoted };
            case JsonKind.True:
                return new YamlScalarNode("true") { Style = ScalarStyle.Plain };
            case JsonKind.False:
                return new YamlScalarNode("false") { Style = ScalarStyle.Plain };
            case JsonKind.Null:
                return new YamlScalarNode("null") { Style = ScalarStyle.Plain };
            default:
                return new YamlScalarNode(value.ToJson()) { Style = ScalarStyle.Plain };
        }
    }

    /// <summary>Reads one object key as a YAML scalar with its style.</summary>
    /// <param name="key">The object key.</param>
    /// <returns>The key node.</returns>
    private static YamlScalarNode ToKey(string key) =>
        new YamlScalarNode(key) { Style = PlainKey.IsMatch(key) && !YamlText.ResolvesToNonString(key) ? ScalarStyle.Plain : ScalarStyle.DoubleQuoted };

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
                if ((character < ' ' && character != '\t') || character == '\u007f' || character == '\uFEFF')
                {
                    return false;
                }
            }
        }

        return true;
    }

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
}
