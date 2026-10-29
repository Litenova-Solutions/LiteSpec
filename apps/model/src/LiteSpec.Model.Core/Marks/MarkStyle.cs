using System;

namespace LiteSpec.Model;

/// <summary>The one mark style a project declares in its project file.</summary>
public enum MarkStyle
{
    /// <summary>
    ///     A documentation tag: <c>&lt;implements&gt;</c> or <c>&lt;proves&gt;</c>.
    ///     This is the default when the project file names no style.
    /// </summary>
    Xml = 0,

    /// <summary>
    ///     A generated attribute: <c>Implements</c> or <c>Proves</c>.
    /// </summary>
    Attribute = 1,
}

/// <summary>Parses the <c>marks</c> value of <c>litespec.project.json</c>.</summary>
/// <follows>litespec/core/model#declare-one-mark-style</follows>
public static class MarkStyleParser
{
    /// <summary>
    ///     Parses a project file value. A missing or blank value reads as
    ///     <see cref="MarkStyle.Xml" />. Matching ignores case and trims whitespace.
    /// </summary>
    /// <param name="value">The project file value, or null when absent.</param>
    /// <returns>The declared style, defaulting to <see cref="MarkStyle.Xml" />.</returns>
    /// <exception cref="ArgumentException">The value names no known style.</exception>
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
    /// </summary>
    /// <param name="value">The project file value, or null when absent.</param>
    /// <param name="style">The parsed style, or <see cref="MarkStyle.Xml" /> when parsing fails.</param>
    /// <returns>True when the value is missing, blank, or names a known style.</returns>
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

/// <summary>Writes a <see cref="MarkStyle" /> back to its project file value.</summary>
/// <follows>litespec/core/model#declare-one-mark-style</follows>
public static class MarkStyleExtensions
{
    /// <summary>Returns <c>xml</c> or <c>attribute</c> for the style.</summary>
    /// <param name="style">The style to write.</param>
    /// <returns>The project file value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The style is not a known value.</exception>
    public static string ToProjectValue(this MarkStyle style)
    {
        switch (style)
        {
            case MarkStyle.Xml:
                return "xml";
            case MarkStyle.Attribute:
                return "attribute";
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown mark style.");
        }
    }
}
