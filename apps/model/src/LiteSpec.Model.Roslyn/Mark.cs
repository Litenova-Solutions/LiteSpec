using System;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     A written link from one code element to one record: the record identifier,
///     the style and verb it was written in, the 1-based line of the mark, and the
///     text span of the identifier inside the source for code fixes and editor
///     navigation. A malformed mark carries an empty record identifier so hosts
///     report it as unresolved rather than ignoring it.
/// </summary>
public sealed class Mark : IEquatable<Mark>
{
    /// <summary>Creates a mark.</summary>
    /// <param name="recordId">The record identifier, or empty when malformed.</param>
    /// <param name="style">The style the mark was written in.</param>
    /// <param name="verb">The verb the mark was written with.</param>
    /// <param name="line">The 1-based line of the mark.</param>
    /// <param name="span">
    ///     The span of the identifier text when present, otherwise the span of the
    ///     mark that carries it.
    /// </param>
    /// <exception cref="ArgumentNullException">The record identifier is null.</exception>
    public Mark(string recordId, MarkStyle style, MarkVerb verb, int line, TextSpan span)
    {
        if (recordId is null)
        {
            throw new ArgumentNullException(nameof(recordId));
        }

        RecordId = recordId;
        Style = style;
        Verb = verb;
        Line = line;
        Span = span;
    }

    /// <summary>The record identifier, or empty when the mark is malformed.</summary>
    public string RecordId { get; }

    /// <summary>The style the mark was written in.</summary>
    public MarkStyle Style { get; }

    /// <summary>The verb the mark was written with.</summary>
    public MarkVerb Verb { get; }

    /// <summary>The 1-based line of the mark.</summary>
    public int Line { get; }

    /// <summary>
    ///     The span of the identifier text when present, otherwise the span of the
    ///     mark that carries it.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>Whether the mark names a record: false for a malformed mark.</summary>
    public bool HasRecordId => RecordId.Length != 0;

    /// <inheritdoc />
    public bool Equals(Mark? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(RecordId, other.RecordId, StringComparison.Ordinal)
            && Style == other.Style
            && Verb == other.Verb
            && Line == other.Line
            && Span.Equals(other.Span);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Mark other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(RecordId);
            hash = hash * 31 + Style.GetHashCode();
            hash = hash * 31 + Verb.GetHashCode();
            hash = hash * 31 + Line.GetHashCode();
            hash = hash * 31 + Span.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        Verb.ToTagName() + " " + RecordId + " (" + Style.ToProjectValue() + ", line " + Line + ")";

    /// <summary>Whether two marks hold the same record identifier, style, verb, and location.</summary>
    /// <param name="left">The first mark.</param>
    /// <param name="right">The second mark.</param>
    /// <returns>True when both are null or hold the same values.</returns>
    public static bool operator ==(Mark? left, Mark? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>Whether two marks differ.</summary>
    /// <param name="left">The first mark.</param>
    /// <param name="right">The second mark.</param>
    /// <returns>True when they hold different values.</returns>
    public static bool operator !=(Mark? left, Mark? right) => !(left == right);
}
