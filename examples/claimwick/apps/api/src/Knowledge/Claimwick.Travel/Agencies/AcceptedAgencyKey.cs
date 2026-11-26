using System;

namespace Claimwick.Travel.Agencies;

/// <summary>One accepted-agency row key: the stable name operational records keep with the agency they used.</summary>
public readonly record struct AcceptedAgencyKey
{
    private AcceptedAgencyKey(string value)
    {
        Value = value;
    }

    /// <summary>The key value.</summary>
    public string Value { get; }

    /// <summary>Builds the key from its value, refusing an empty one.</summary>
    public static AcceptedAgencyKey From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An accepted agency key must not be empty.", nameof(value));
        }

        return new AcceptedAgencyKey(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
