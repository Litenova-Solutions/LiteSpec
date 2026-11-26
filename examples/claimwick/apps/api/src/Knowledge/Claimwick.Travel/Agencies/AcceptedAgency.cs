using System;

namespace Claimwick.Travel.Agencies;

/// <summary>One accepted agency: an agency the product accepts bookings from over its dates.</summary>
public sealed record AcceptedAgency
{
    /// <summary>Builds one accepted agency.</summary>
    public AcceptedAgency(string country, AcceptedAgencyKey key, string name, DateOnly validFrom, DateOnly? validTo, string source)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("An accepted agency must name its country.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(key.Value))
        {
            throw new ArgumentException("An accepted agency must carry its key.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An accepted agency must carry its name.", nameof(name));
        }

        if (validTo.HasValue && validTo.Value <= validFrom)
        {
            throw new ArgumentException("An accepted agency must end after it starts.", nameof(validTo));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("An accepted agency must name its source.", nameof(source));
        }

        Country = country;
        Key = key;
        Name = name;
        ValidFrom = validFrom;
        ValidTo = validTo;
        Source = source;
    }

    /// <summary>The ISO 3166-1 alpha-2 country code this agency belongs to.</summary>
    public string Country { get; }

    /// <summary>The stable key operational records keep with the agency they used.</summary>
    public AcceptedAgencyKey Key { get; }

    /// <summary>The agency name.</summary>
    public string Name { get; }

    /// <summary>The first date the product accepts the agency's bookings.</summary>
    public DateOnly ValidFrom { get; }

    /// <summary>The last date the product accepts the agency's bookings, left out while it stands.</summary>
    public DateOnly? ValidTo { get; }

    /// <summary>The record behind the acceptance.</summary>
    public string Source { get; }
}
