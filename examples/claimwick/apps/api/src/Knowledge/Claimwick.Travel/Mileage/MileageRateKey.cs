using System;

namespace Claimwick.Travel.Mileage;

/// <summary>One mileage row key: the stable name operational records keep with the value they used.</summary>
public readonly record struct MileageRateKey
{
    private MileageRateKey(string value)
    {
        Value = value;
    }

    /// <summary>The key value.</summary>
    public string Value { get; }

    /// <summary>Builds the key from its value, refusing an empty one.</summary>
    public static MileageRateKey From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A mileage rate key must not be empty.", nameof(value));
        }

        return new MileageRateKey(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
