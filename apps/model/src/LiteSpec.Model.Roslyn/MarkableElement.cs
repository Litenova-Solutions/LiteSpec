using System;
using System.Collections.Generic;
using System.Linq;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     One type or method from a syntax tree that is marked or of a markable
///     kind: its name, its kind, its marks, and the location of its declaration
///     name. The kind is null when the element carries marks but is of no known
///     kind.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public sealed class MarkableElement : IEquatable<MarkableElement>
{
    /// <summary>Creates a marked or markable element.</summary>
    /// <param name="name">The simple name of the type or method.</param>
    /// <param name="isType">True for a type, false for a method.</param>
    /// <param name="kind">The kind, or null when the element is of no known kind.</param>
    /// <param name="marks">The marks on the declaration, in source order.</param>
    /// <param name="line">The 1-based line of the declaration name.</param>
    /// <param name="span">The span of the declaration name.</param>
    /// <exception cref="ArgumentNullException">The name or marks are null.</exception>
    public MarkableElement(
        string name,
        bool isType,
        CodeKind? kind,
        IReadOnlyList<Mark> marks,
        int line,
        TextSpan span)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (marks is null)
        {
            throw new ArgumentNullException(nameof(marks));
        }

        Name = name;
        IsType = isType;
        Kind = kind;
        Marks = marks;
        Line = line;
        Span = span;
    }

    /// <summary>The simple name of the type or method.</summary>
    public string Name { get; }

    /// <summary>True for a type, false for a method.</summary>
    public bool IsType { get; }

    /// <summary>The kind, or null when the element is of no known kind.</summary>
    public CodeKind? Kind { get; }

    /// <summary>The marks on the declaration, in source order.</summary>
    public IReadOnlyList<Mark> Marks { get; }

    /// <summary>The 1-based line of the declaration name.</summary>
    public int Line { get; }

    /// <summary>The span of the declaration name.</summary>
    public TextSpan Span { get; }

    /// <inheritdoc />
    public bool Equals(MarkableElement? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && IsType == other.IsType
            && Kind == other.Kind
            && Marks.SequenceEqual(other.Marks)
            && Line == other.Line
            && Span.Equals(other.Span);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MarkableElement other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name);
            hash = hash * 31 + IsType.GetHashCode();
            hash = hash * 31 + (Kind?.GetHashCode() ?? 0);
            foreach (var mark in Marks)
            {
                hash = hash * 31 + mark.GetHashCode();
            }

            hash = hash * 31 + Line.GetHashCode();
            hash = hash * 31 + Span.GetHashCode();
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        (IsType ? "type " : "method ") + Name + ": " + (Kind?.ToString() ?? "none")
        + " (" + Marks.Count + (Marks.Count == 1 ? " mark)" : " marks)");
}
