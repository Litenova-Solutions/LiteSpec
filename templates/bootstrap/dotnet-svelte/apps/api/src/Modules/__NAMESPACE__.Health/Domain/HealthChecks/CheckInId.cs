using System.Diagnostics.CodeAnalysis;
using __NAMESPACE__.Kernel.Values;

namespace __NAMESPACE__.Health.Domain.HealthChecks;

/// <summary>
/// The identifier of one recorded check-in. One check-in is one moment the
/// sample recorded, and the identifier is never the identifier of another aggregate.
/// </summary>
internal readonly record struct CheckInId : ITypedId, IParsable<CheckInId>
{
    /// <summary>Creates a check-in identifier from its GUID value.</summary>
    /// <param name="value">The GUID value.</param>
    public CheckInId(Guid value)
    {
        Value = value;
    }

    /// <summary>The identifier value.</summary>
    public Guid Value { get; init; }

    /// <summary>Creates a check-in identifier, turning away the empty value.</summary>
    /// <param name="value">The GUID value.</param>
    /// <exception cref="ArgumentException">The value is empty.</exception>
    public static CheckInId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("The check-in identifier is empty.", nameof(value));
        }

        return new CheckInId(value);
    }

    /// <summary>Parses a check-in identifier from its text, for route and query binding.</summary>
    /// <param name="s">The identifier text.</param>
    /// <param name="provider">The format provider.</param>
    /// <exception cref="FormatException">The text is not a GUID.</exception>
    public static CheckInId Parse(string s, IFormatProvider? provider)
    {
        return From(Guid.Parse(s));
    }

    /// <summary>Tries to parse a check-in identifier from its text, for route and query binding.</summary>
    /// <param name="s">The identifier text.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">The identifier when the text is a non-empty GUID.</param>
    /// <returns>True when the text is a non-empty GUID.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out CheckInId result)
    {
        if (Guid.TryParse(s, out var value) && value != Guid.Empty)
        {
            result = new CheckInId(value);
            return true;
        }

        result = default;
        return false;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
