using System;

namespace LiteSpec.Model;

/// <summary>Parses the C# <c>marks</c> value of <c>litespec.project.json</c>.</summary>
/// <follows>litespec/core/model#declare-one-mark-style</follows>
public static class MarkStyleParser
{
    /// <summary>
    ///     Parses a project file value. A missing or blank value reads as
    ///     <see cref="MarkStyle.Xml" />. Matching ignores case and trims whitespace.
    ///     Only the C# styles are declared; the typed constant and the comment
    ///     are the fixed styles of their languages, never a project choice.
    /// </summary>
    /// <param name="value">The project file value, or null when absent.</param>
    /// <returns>The declared style, defaulting to <see cref="MarkStyle.Xml" />.</returns>
    /// <exception cref="ArgumentException">The value names no declared style.</exception>
    public static MarkStyle Parse(string? value)
    {
        if (TryParse(value, out var style))
        {
            return style;
        }

        throw new ArgumentException(
            $"Unknown mark style \"{value}\". Expected \"xml\" or \"attribute\".", nameof(value));
    }

    /// <summary>
    ///     Tries to parse a project file value. A missing or blank value reads as
    ///     <see cref="MarkStyle.Xml" />. Matching ignores case and trims whitespace.
    ///     Only the C# styles are declared; the typed constant and the comment
    ///     are the fixed styles of their languages, never a project choice.
    /// </summary>
    /// <param name="value">The project file value, or null when absent.</param>
    /// <param name="style">The parsed style, or <see cref="MarkStyle.Xml" /> when parsing fails.</param>
    /// <returns>True when the value is missing, blank, or names a declared style.</returns>
    public static bool TryParse(string? value, out MarkStyle style)
    {
        if (value is null)
        {
            style = MarkStyle.Xml;
            return true;
        }

        var text = value.Trim();
        if (text.Length == 0)
        {
            style = MarkStyle.Xml;
            return true;
        }

        if (text.Equals("xml", StringComparison.OrdinalIgnoreCase))
        {
            style = MarkStyle.Xml;
            return true;
        }

        if (text.Equals("attribute", StringComparison.OrdinalIgnoreCase))
        {
            style = MarkStyle.Attribute;
            return true;
        }

        style = MarkStyle.Xml;
        return false;
    }
}
