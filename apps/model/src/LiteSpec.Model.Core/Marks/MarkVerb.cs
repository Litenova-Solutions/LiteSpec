using System;

namespace LiteSpec.Model;

/// <summary>The verb of a mark: what the marked element does for its record.</summary>
public enum MarkVerb
{
    /// <summary>
    ///     The element implements the record: the verb of every code mark.
    /// </summary>
    Implements = 0,

    /// <summary>
    ///     The element proves the record: the verb of every test mark.
    /// </summary>
    Proves = 1,
}

/// <summary>The written names of each <see cref="MarkVerb" /> in both mark styles.</summary>
public static class MarkVerbNames
{
    /// <summary>Returns the documentation tag name: <c>implements</c> or <c>proves</c>.</summary>
    /// <param name="verb">The verb to write.</param>
    /// <returns>The lowercase tag name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The verb is not a known value.</exception>
    public static string ToTagName(this MarkVerb verb)
    {
        switch (verb)
        {
            case MarkVerb.Implements:
                return "implements";
            case MarkVerb.Proves:
                return "proves";
            default:
                throw new ArgumentOutOfRangeException(nameof(verb), verb, "Unknown mark verb.");
        }
    }

    /// <summary>Returns the attribute name: <c>Implements</c> or <c>Proves</c>.</summary>
    /// <param name="verb">The verb to write.</param>
    /// <returns>The attribute name without the <c>Attribute</c> suffix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The verb is not a known value.</exception>
    public static string ToAttributeName(this MarkVerb verb)
    {
        switch (verb)
        {
            case MarkVerb.Implements:
                return "Implements";
            case MarkVerb.Proves:
                return "Proves";
            default:
                throw new ArgumentOutOfRangeException(nameof(verb), verb, "Unknown mark verb.");
        }
    }

    /// <summary>
    ///     Reads a documentation tag name. Matching is exact and case sensitive:
    ///     only <c>implements</c> and <c>proves</c> are marks.
    /// </summary>
    /// <param name="name">The element name, or null.</param>
    /// <param name="verb">The parsed verb, or <see cref="MarkVerb.Implements" /> when parsing fails.</param>
    /// <returns>True when the name is a mark tag.</returns>
    public static bool TryParseTag(string? name, out MarkVerb verb)
    {
        if (string.Equals(name, "implements", StringComparison.Ordinal))
        {
            verb = MarkVerb.Implements;
            return true;
        }

        if (string.Equals(name, "proves", StringComparison.Ordinal))
        {
            verb = MarkVerb.Proves;
            return true;
        }

        verb = MarkVerb.Implements;
        return false;
    }

    /// <summary>
    ///     Reads a simple attribute name, with or without the <c>Attribute</c> suffix.
    ///     The caller strips any namespace qualifier first. Matching is exact and
    ///     case sensitive.
    /// </summary>
    /// <param name="name">The simple attribute name, or null.</param>
    /// <param name="verb">The parsed verb, or <see cref="MarkVerb.Implements" /> when parsing fails.</param>
    /// <returns>True when the name is a mark attribute.</returns>
    public static bool TryParseAttributeName(string? name, out MarkVerb verb)
    {
        if (name is not null && name.Length != 0)
        {
            const string suffix = "Attribute";
            var text = name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - suffix.Length)
                : name;

            if (string.Equals(text, "Implements", StringComparison.Ordinal))
            {
                verb = MarkVerb.Implements;
                return true;
            }

            if (string.Equals(text, "Proves", StringComparison.Ordinal))
            {
                verb = MarkVerb.Proves;
                return true;
            }
        }

        verb = MarkVerb.Implements;
        return false;
    }
}
