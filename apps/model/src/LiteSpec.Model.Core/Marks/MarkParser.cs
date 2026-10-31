using System;
using System.Collections.Generic;

namespace LiteSpec.Model;

/// <summary>
///     One mark read from text: the verb it was written with, the record
///     identifier it names, the offset where the mark starts, and whether it
///     was written as an attribute rather than a tag. The caller assigns the
///     style from the language it reads: a tag in C# is <see cref="MarkStyle.Xml" />,
///     a tag in an operations comment is <see cref="MarkStyle.Comment" />, and an
///     attribute is <see cref="MarkStyle.Attribute" />.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public sealed class ParsedMark : IEquatable<ParsedMark>
{
    /// <summary>Creates a parsed mark.</summary>
    /// <param name="verb">The verb the mark was written with.</param>
    /// <param name="recordId">The record identifier, trimmed and non-empty.</param>
    /// <param name="index">The offset where the mark starts.</param>
    /// <param name="fromAttribute">True for an attribute, false for a tag.</param>
    /// <exception cref="ArgumentNullException">The record identifier is null.</exception>
    public ParsedMark(MarkVerb verb, string recordId, int index, bool fromAttribute)
    {
        if (recordId is null)
        {
            throw new ArgumentNullException(nameof(recordId));
        }

        Verb = verb;
        RecordId = recordId;
        Index = index;
        FromAttribute = fromAttribute;
    }

    /// <summary>The verb the mark was written with.</summary>
    public MarkVerb Verb { get; }

    /// <summary>The record identifier, trimmed and non-empty.</summary>
    public string RecordId { get; }

    /// <summary>The offset where the mark starts.</summary>
    public int Index { get; }

    /// <summary>True for an attribute, false for a tag.</summary>
    public bool FromAttribute { get; }

    /// <inheritdoc />
    public bool Equals(ParsedMark? other)
    {
        if (other is null)
        {
            return false;
        }

        return Verb == other.Verb
            && string.Equals(RecordId, other.RecordId, StringComparison.Ordinal)
            && Index == other.Index
            && FromAttribute == other.FromAttribute;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ParsedMark other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Verb.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(RecordId);
            hash = hash * 31 + Index.GetHashCode();
            hash = hash * 31 + FromAttribute.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        Verb.ToTagName() + " " + RecordId + " (at " + Index + ")";
}

/// <summary>
///     Reads marks from text in every written form: the documentation tags and
///     the generated attributes. The Blazor and operations readers share this,
///     so all three read the same verbs with the same trimming. Matching is
///     exact and case sensitive. A malformed mark is left out rather than
///     returned with an empty identifier: text readers report what they find,
///     never what they cannot parse.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class MarkParser
{
    /// <summary>Reads every documentation tag mark in the text, in source order.</summary>
    /// <param name="text">The text to read, or null for no marks.</param>
    /// <returns>The well-formed tag marks, each with a non-empty identifier.</returns>
    public static IReadOnlyList<ParsedMark> ReadTags(string? text)
    {
        var found = new List<ParsedMark>();

        if (text is null || text.Length == 0)
        {
            return found;
        }

        var index = 0;

        while (index < text.Length)
        {
            var open = text.IndexOf('<', index);

            if (open < 0 || open + 1 >= text.Length)
            {
                return found;
            }

            if (TryReadOpenTag(text, open, out var verb, out var tagEnd))
            {
                var close = FindCloseTag(text, tagEnd + 1, verb);

                if (close is null)
                {
                    index = tagEnd + 1;
                    continue;
                }

                var id = text.Substring(tagEnd + 1, close.Start - tagEnd - 1).Trim();

                if (id.Length > 0 && id.IndexOf('<') < 0 && id.IndexOf('>') < 0)
                {
                    found.Add(new ParsedMark(verb, id, open, fromAttribute: false));
                }

                index = close.End;
                continue;
            }

            index = open + 1;
        }

        return found;
    }

    /// <summary>
    ///     Reads every attribute mark in the text, in source order: a verb name with
    ///     an optional namespace qualifier and an optional <c>Attribute</c> suffix,
    ///     followed by a double-quoted identifier. Only an unqualified verb or one
    ///     qualified with the generated namespace reads as a mark; any other
    ///     qualifier names a domain attribute, never a mark.
    /// </summary>
    /// <param name="text">The text to read, or null for no marks.</param>
    /// <returns>The well-formed attribute marks, each with a non-empty identifier.</returns>
    public static IReadOnlyList<ParsedMark> ReadAttributes(string? text)
    {
        var found = new List<ParsedMark>();

        if (text is null || text.Length == 0)
        {
            return found;
        }

        var index = 0;

        while (index < text.Length)
        {
            var bracket = text.IndexOf('[', index);

            if (bracket < 0)
            {
                return found;
            }

            var end = text.IndexOf(']', bracket + 1);
            var stop = end < 0 ? text.Length : end;
            var cursor = bracket + 1;

            while (cursor < stop)
            {
                if (TryReadAttribute(text, cursor, stop, out var mark, out var next))
                {
                    found.Add(mark);
                    cursor = next;
                    continue;
                }

                cursor++;
            }

            index = bracket + 1;
        }

        found.Sort(static (left, right) => left.Index.CompareTo(right.Index));

        return found;
    }

    /// <summary>Reads every tag and attribute mark in the text, in source order.</summary>
    /// <param name="text">The text to read, or null for no marks.</param>
    /// <returns>The well-formed marks of both forms, ordered by offset.</returns>
    public static IReadOnlyList<ParsedMark> Read(string? text)
    {
        var found = new List<ParsedMark>();
        found.AddRange(ReadTags(text));
        found.AddRange(ReadAttributes(text));
        found.Sort(static (left, right) => left.Index.CompareTo(right.Index));

        return found;
    }

    private static bool TryReadOpenTag(string text, int open, out MarkVerb verb, out int tagEnd)
    {
        verb = MarkVerb.Implements;
        tagEnd = -1;

        if (text[open] != '<' || open + 1 < text.Length && text[open + 1] == '/')
        {
            return false;
        }

        foreach (var candidate in new[] { MarkVerb.Implements, MarkVerb.Proves, MarkVerb.Follows })
        {
            var name = candidate.ToTagName();

            if (StartsAt(text, open + 1, name) && IsTagBoundary(text, open + 1 + name.Length))
            {
                var end = text.IndexOf('>', open + 1);

                if (end < 0)
                {
                    return false;
                }

                var head = text.Substring(open + 1, end - open - 1);

                if (head.IndexOf('<') >= 0 || head.TrimEnd().EndsWith("/", StringComparison.Ordinal))
                {
                    return false;
                }

                verb = candidate;
                tagEnd = end;

                return true;
            }
        }

        return false;
    }

    private sealed class CloseTag
    {
        public CloseTag(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }

        public int End { get; }
    }

    private static CloseTag? FindCloseTag(string text, int index, MarkVerb verb)
    {
        var name = verb.ToTagName();

        while (index < text.Length)
        {
            var next = text.IndexOf("</", index, StringComparison.Ordinal);

            if (next < 0 || !StartsAt(text, next + 2, name))
            {
                return null;
            }

            var end = next + 2 + name.Length;

            while (end < text.Length && char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            if (end < text.Length && text[end] == '>')
            {
                return new CloseTag(next, end + 1);
            }

            index = next + 2;
        }

        return null;
    }

    private static bool TryReadAttribute(string text, int cursor, int stop, out ParsedMark mark, out int next)
    {
        mark = new ParsedMark(MarkVerb.Implements, "x", cursor, fromAttribute: true);
        next = cursor + 1;

        var start = cursor;

        while (start < stop && (char.IsWhiteSpace(text[start]) || text[start] == ','))
        {
            start++;
        }

        if (start >= stop || !(char.IsLetter(text[start]) || text[start] == '_' || text[start] == '@'))
        {
            return false;
        }

        var before = start - 1;

        while (before >= 0 && char.IsWhiteSpace(text[before]))
        {
            before--;
        }

        if (before >= 0
            && (text[before] == '.' || text[before] == ':'
                || char.IsLetterOrDigit(text[before]) || text[before] == '_' || text[before] == '@'))
        {
            return false;
        }

        var wordEnd = start;

        if (text[wordEnd] == '@')
        {
            wordEnd++;
        }

        while (wordEnd < stop && (char.IsLetterOrDigit(text[wordEnd]) || text[wordEnd] == '_'))
        {
            wordEnd++;
        }

        if (wordEnd == start)
        {
            return false;
        }

        var qualifier = string.Empty;
        var nameStart = text[start] == '@' ? start + 1 : start;
        var nameEnd = wordEnd;
        var scan = wordEnd;

        while (true)
        {
            var probe = scan;

            while (probe < stop && char.IsWhiteSpace(text[probe]))
            {
                probe++;
            }

            if (probe + 1 < stop && text[probe] == ':' && text[probe + 1] == ':')
            {
                probe += 2;

                while (probe < stop && char.IsWhiteSpace(text[probe]))
                {
                    probe++;
                }

                qualifier += text.Substring(nameStart, nameEnd - nameStart) + "::";
                nameStart = probe;

                if (nameStart < stop && text[nameStart] == '@')
                {
                    nameStart++;
                }

                nameEnd = nameStart;

                while (nameEnd < stop && (char.IsLetterOrDigit(text[nameEnd]) || text[nameEnd] == '_'))
                {
                    nameEnd++;
                }

                if (nameEnd == nameStart)
                {
                    return false;
                }

                scan = nameEnd;
                continue;
            }

            if (probe < stop && text[probe] == '.')
            {
                probe++;

                while (probe < stop && char.IsWhiteSpace(text[probe]))
                {
                    probe++;
                }

                qualifier += text.Substring(nameStart, nameEnd - nameStart) + ".";
                nameStart = probe;

                if (nameStart < stop && text[nameStart] == '@')
                {
                    nameStart++;
                }

                nameEnd = nameStart;

                while (nameEnd < stop && (char.IsLetterOrDigit(text[nameEnd]) || text[nameEnd] == '_'))
                {
                    nameEnd++;
                }

                if (nameEnd == nameStart)
                {
                    return false;
                }

                scan = nameEnd;
                continue;
            }

            break;
        }

        var simple = text.Substring(nameStart, nameEnd - nameStart);

        if (!MarkVerbNames.TryParseAttributeName(simple, out var verb))
        {
            return false;
        }

        if (!IsGeneratedQualifier(qualifier))
        {
            return false;
        }

        var after = nameEnd;

        while (after < stop && char.IsWhiteSpace(text[after]))
        {
            after++;
        }

        if (after >= stop || text[after] != '(')
        {
            return false;
        }

        after++;

        while (after < stop && char.IsWhiteSpace(text[after]))
        {
            after++;
        }

        if (after >= stop || text[after] != '"')
        {
            return false;
        }

        var idStart = after + 1;
        var idEnd = text.IndexOf('"', idStart);

        if (idEnd < 0 || idEnd > stop)
        {
            return false;
        }

        var id = text.Substring(idStart, idEnd - idStart).Trim();

        if (id.Length == 0)
        {
            return false;
        }

        mark = new ParsedMark(verb, id, start, fromAttribute: true);
        next = idEnd + 1;

        return true;
    }

    private static bool IsGeneratedQualifier(string qualifier)
    {
        var cleaned = qualifier.Replace("@", string.Empty).Replace(" ", string.Empty).Replace("\t", string.Empty);

        if (cleaned.Length == 0)
        {
            return true;
        }

        return string.Equals(cleaned, "LiteSpec.", StringComparison.Ordinal)
            || string.Equals(cleaned, "LiteSpec::", StringComparison.Ordinal)
            || string.Equals(cleaned, "global::LiteSpec.", StringComparison.Ordinal)
            || string.Equals(cleaned, "global::LiteSpec::", StringComparison.Ordinal);
    }

    private static bool StartsAt(string text, int index, string value)
    {
        if (index + value.Length > text.Length)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (text[index + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTagBoundary(string text, int index) =>
        index >= text.Length || char.IsWhiteSpace(text[index]) || text[index] == '>' || text[index] == '/';
}
