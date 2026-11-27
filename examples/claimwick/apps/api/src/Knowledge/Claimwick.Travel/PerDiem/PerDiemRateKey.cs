using System;

namespace Claimwick.Travel.PerDiem;

/// <summary>One per-diem row key: the stable name operational records keep with the value they used.</summary>
public readonly record struct PerDiemRateKey
{
    private PerDiemRateKey(string value)
    {
        Value = value;
    }

    /// <summary>The key value.</summary>
    public string Value { get; }

    /// <summary>Builds the key from its value, refusing an empty one.</summary>
    public static PerDiemRateKey From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A per-diem rate key must not be empty.", nameof(value));
        }

        return new PerDiemRateKey(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
