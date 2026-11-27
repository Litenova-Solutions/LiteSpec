using System;
using Claimwick.Kernel.Values;

namespace Claimwick.Travel.Mileage;

/// <summary>One mileage row: the amount per kilometre in force for one vehicle kind over its dates.</summary>
public sealed record MileageRate
{
    private MileageRate(string country, MileageRateKey key, VehicleKind vehicle, Money amountPerKilometre, DateOnly validFrom, DateOnly? validTo, string source)
    {
        Country = country;
        Key = key;
        Vehicle = vehicle;
        AmountPerKilometre = amountPerKilometre;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Source = source;
    }

    /// <summary>The ISO 3166-1 alpha-2 country code this row belongs to.</summary>
    public string Country { get; }

    /// <summary>The stable key operational records keep with the value they used.</summary>
    public MileageRateKey Key { get; }

    /// <summary>The vehicle kind this row pays for.</summary>
    public VehicleKind Vehicle { get; }

    /// <summary>The amount per kilometre.</summary>
    public Money AmountPerKilometre { get; }

    /// <summary>The first date the row is in force.</summary>
    public DateOnly ValidFrom { get; }

    /// <summary>The last date the row is in force, left out while the row stands.</summary>
    public DateOnly? ValidTo { get; }

    /// <summary>The record behind the value.</summary>
    public string Source { get; }

    /// <summary>Builds one mileage row.</summary>
    public static MileageRate Create(string country, MileageRateKey key, VehicleKind vehicle, Money amountPerKilometre, DateOnly validFrom, DateOnly? validTo, string source)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("A mileage rate must name its country.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(key.Value))
        {
            throw new ArgumentException("A mileage rate must carry its key.", nameof(key));
        }

        if (vehicle is null)
        {
            throw new ArgumentNullException(nameof(vehicle));
        }

        if (amountPerKilometre is null)
        {
            throw new ArgumentNullException(nameof(amountPerKilometre));
        }

        if (amountPerKilometre.Amount < 0)
        {
            throw new ArgumentException("A mileage rate must not be negative.", nameof(amountPerKilometre));
        }

        if (validTo.HasValue && validTo.Value <= validFrom)
        {
            throw new ArgumentException("A mileage rate must end after it starts.", nameof(validTo));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("A mileage rate must name its source.", nameof(source));
        }

        return new MileageRate(country, key, vehicle, amountPerKilometre, validFrom, validTo, source);
    }
}
