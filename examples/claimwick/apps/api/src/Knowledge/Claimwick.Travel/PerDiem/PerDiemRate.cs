using System;
using Claimwick.Kernel.Values;

namespace Claimwick.Travel.PerDiem;

/// <summary>One per-diem row: the daily amount in force for one role over its dates.</summary>
public sealed record PerDiemRate
{
    private PerDiemRate(string country, PerDiemRateKey key, PerDiemRole role, Money dailyAmount, DateOnly validFrom, DateOnly? validTo, string source)
    {
        Country = country;
        Key = key;
        Role = role;
        DailyAmount = dailyAmount;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Source = source;
    }

    /// <summary>The ISO 3166-1 alpha-2 country code this row belongs to.</summary>
    public string Country { get; }

    /// <summary>The stable key operational records keep with the value they used.</summary>
    public PerDiemRateKey Key { get; }

    /// <summary>The role this row plays.</summary>
    public PerDiemRole Role { get; }

    /// <summary>The daily amount.</summary>
    public Money DailyAmount { get; }

    /// <summary>The first date the row is in force.</summary>
    public DateOnly ValidFrom { get; }

    /// <summary>The last date the row is in force, left out while the row stands.</summary>
    public DateOnly? ValidTo { get; }

    /// <summary>The record behind the value.</summary>
    public string Source { get; }

    /// <summary>Builds one per-diem row.</summary>
    public static PerDiemRate Create(string country, PerDiemRateKey key, PerDiemRole role, Money dailyAmount, DateOnly validFrom, DateOnly? validTo, string source)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("A per-diem rate must name its country.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(key.Value))
        {
            throw new ArgumentException("A per-diem rate must carry its key.", nameof(key));
        }

        if (role is null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        if (dailyAmount is null)
        {
            throw new ArgumentNullException(nameof(dailyAmount));
        }

        if (dailyAmount.Amount < 0)
        {
            throw new ArgumentException("A per-diem rate must not be negative.", nameof(dailyAmount));
        }

        if (validTo.HasValue && validTo.Value <= validFrom)
        {
            throw new ArgumentException("A per-diem rate must end after it starts.", nameof(validTo));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("A per-diem rate must name its source.", nameof(source));
        }

        return new PerDiemRate(country, key, role, dailyAmount, validFrom, validTo, source);
    }
}
