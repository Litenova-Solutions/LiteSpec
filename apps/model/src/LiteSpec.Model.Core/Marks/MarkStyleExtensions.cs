using System;

namespace LiteSpec.Model;

/// <summary>Writes a <see cref="MarkStyle" /> back to its project file value.</summary>
/// <follows>litespec/core/model#declare-one-mark-style</follows>
public static class MarkStyleExtensions
{
    /// <summary>Returns <c>xml</c>, <c>attribute</c>, <c>typed</c>, or <c>comment</c> for the style.</summary>
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
            case MarkStyle.Typed:
                return "typed";
            case MarkStyle.Comment:
                return "comment";
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown mark style.");
        }
    }

    /// <summary>Whether the style is a C# style, the only styles the project file declares.</summary>
    /// <param name="style">The style the mark was written in.</param>
    /// <returns>True for <see cref="MarkStyle.Xml" /> and <see cref="MarkStyle.Attribute" />.</returns>
    public static bool IsCSharp(this MarkStyle style) =>
        style is MarkStyle.Xml or MarkStyle.Attribute;
}
