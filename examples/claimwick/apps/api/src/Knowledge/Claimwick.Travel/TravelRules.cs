using System;
using System.Collections.Generic;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel;

/// <summary>Answers travel lookups from the registered sets by role and date.</summary>
/// <follows>litespec/core/knowledge#read-knowledge-by-role-and-date</follows>
public sealed class TravelRules : ITravelRules
{
    private readonly Dictionary<string, ITravelSet> sets;

    /// <summary>Indexes the sets of the registry by country.</summary>
    public TravelRules(TravelRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        sets = new Dictionary<string, ITravelSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var set in registry.Sets)
        {
            sets.Add(set.Country, set);
        }
    }

    /// <summary>Reads the per-diem rate in force for one role on one date, refusing with a reason when no row is stated.</summary>
    public PerDiemResult PerDiemFor(string country, PerDiemRole role, DateOnly date)
    {
        var set = SetFor(country);
        if (role is null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        foreach (var row in set.PerDiemRates)
        {
            if (row.Role == role && row.ValidFrom <= date && (row.ValidTo is null || date <= row.ValidTo.Value))
            {
                return new PerDiemResult(row, set.Version);
            }
        }

        throw new TravelRuleMissingException($"No per-diem rate is stated for '{role.Code}' in country '{country}' on {date:yyyy-MM-dd}.");
    }

    /// <summary>Reads the mileage rate in force for one vehicle kind on one date, refusing with a reason when no row is stated.</summary>
    public MileageResult MileageFor(string country, VehicleKind vehicle, DateOnly date)
    {
        var set = SetFor(country);
        if (vehicle is null)
        {
            throw new ArgumentNullException(nameof(vehicle));
        }

        foreach (var row in set.MileageRates)
        {
            if (row.Vehicle == vehicle && row.ValidFrom <= date && (row.ValidTo is null || date <= row.ValidTo.Value))
            {
                return new MileageResult(row, set.Version);
            }
        }

        throw new TravelRuleMissingException($"No mileage rate is stated for '{vehicle.Code}' in country '{country}' on {date:yyyy-MM-dd}.");
    }

    /// <summary>Reads the version of one country set, which operational records keep with the values they read.</summary>
    public string SetVersion(string country)
    {
        return SetFor(country).Version;
    }

    private ITravelSet SetFor(string country)
    {
        if (country is null)
        {
            throw new ArgumentNullException(nameof(country));
        }

        if (sets.TryGetValue(country, out var set))
        {
            return set;
        }

        throw new TravelRuleMissingException($"No travel set is registered for country '{country}'.");
    }
}
