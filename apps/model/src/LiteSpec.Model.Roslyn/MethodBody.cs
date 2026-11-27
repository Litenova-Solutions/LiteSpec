using System;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     One method or local-function body: its name, its containing type, its line, its
///     size in statements and tokens, and its normalized text. The normalized text
///     strips trivia and comments and replaces local variable and parameter names with
///     positional placeholders, while member names, types and literals stay as written,
///     so two bodies carrying the same logic normalize to one text whatever their locals
///     are called.
/// </summary>
/// <follows>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</follows>
public sealed class MethodBody : IEquatable<MethodBody>
{
    /// <summary>Initializes a new instance of the <see cref="MethodBody"/> class.Creates a method body.</summary>
    /// <param name="name">The simple name of the method or local function.</param>
    /// <param name="type">The simple name of the containing type, or empty when declared outside a type.</param>
    /// <param name="line">The 1-based line of the declaration name.</param>
    /// <param name="statements">The statements the body holds, nested statements counted.</param>
    /// <param name="tokens">The tokens the body holds, trivia excluded.</param>
    /// <param name="normalized">The normalized body text.</param>
    /// <exception cref="ArgumentNullException">The name, type or normalized text is null.</exception>
    public MethodBody(string name, string type, int line, int statements, int tokens, string normalized)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (normalized is null)
        {
            throw new ArgumentNullException(nameof(normalized));
        }

        Name = name;
        Type = type;
        Line = line;
        Statements = statements;
        Tokens = tokens;
        Normalized = normalized;
    }

    /// <summary>Gets the simple name of the method or local function.</summary>
    public string Name { get; }

    /// <summary>Gets the simple name of the containing type, or empty when declared outside a type.</summary>
    public string Type { get; }

    /// <summary>Gets the 1-based line of the declaration name.</summary>
    public int Line { get; }

    /// <summary>Gets the statements the body holds, nested statements counted.</summary>
    public int Statements { get; }

    /// <summary>Gets the tokens the body holds, trivia excluded.</summary>
    public int Tokens { get; }

    /// <summary>Gets the normalized body text.</summary>
    public string Normalized { get; }

    /// <inheritdoc />
    public bool Equals(MethodBody? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(Type, other.Type, StringComparison.Ordinal)
            && Line == other.Line
            && Statements == other.Statements
            && Tokens == other.Tokens
            && string.Equals(Normalized, other.Normalized, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MethodBody other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Name);
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Type);
            hash = hash * 31 + Line.GetHashCode();
            hash = hash * 31 + Statements.GetHashCode();
            hash = hash * 31 + Tokens.GetHashCode();
            hash = hash * 31 + StringComparer.Ordinal.GetHashCode(Normalized);
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Type + "." + Name + " (line " + Line + ")";
}
