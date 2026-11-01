using System;

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
    /// <summary>Initializes a new instance of the <see cref="ParsedMark"/> class.Creates a parsed mark.</summary>
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

    /// <summary>Gets the verb the mark was written with.</summary>
    public MarkVerb Verb { get; }

    /// <summary>Gets the record identifier, trimmed and non-empty.</summary>
    public string RecordId { get; }

    /// <summary>Gets the offset where the mark starts.</summary>
    public int Index { get; }

    /// <summary>Gets a value indicating whether true for an attribute, false for a tag.</summary>
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
