using System;
using System.Collections.Generic;
using System.Linq;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel;

/// <summary>The registered travel sets, keyed by country and validated before the host serves them. Only hosts add sets.</summary>
/// <follows>litespec/core/knowledge#check-shape-at-compile-time-and-dates-at-startup</follows>
public sealed class TravelRegistry
{
    private readonly Dictionary<string, ITravelSet> sets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The registered sets.</summary>
    public IReadOnlyCollection<ITravelSet> Sets => sets.Values;

    /// <summary>Adds one set. Each country registers one set.</summary>
    public TravelRegistry Add(ITravelSet set)
    {
        if (set is null)
        {
            throw new ArgumentNullException(nameof(set));
        }

        if (sets.ContainsKey(set.Country))
        {
            throw new InvalidOperationException($"The travel set for country '{set.Country}' is already registered.");
        }

        sets.Add(set.Country, set);
        return this;
    }

    /// <summary>Adds the set of type TSet.</summary>
    public TravelRegistry Add<TSet>()
        where TSet : ITravelSet, new()
    {
        return Add(new TSet());
    }

    /// <summary>Reads the set of one country, refusing with a reason when none is registered.</summary>
    public ITravelSet For(string country)
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

    /// <summary>Validates every registered set: each set carries a version, its rows match its country, no two rows of one series overlap, and every role and vehicle kind covers the current year.</summary>
    public void Validate()
    {
        if (sets.Count == 0)
        {
            throw new InvalidOperationException("No travel set is registered. Register one set per country with Add.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var set in sets.Values)
        {
            ValidateSet(set, today);
        }
    }

    private static void ValidateSet(ITravelSet set, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(set.Version))
        {
            throw new InvalidOperationException($"The travel set for country '{set.Country}' carries no version.");
        }

        var yearStart = new DateOnly(today.Year, 1, 1);
        var yearEnd = new DateOnly(today.Year, 12, 31);

        ValidateSeries(
            set.Country,
            "per-diem rate",
            new[] { PerDiemRole.FullDay.Code, PerDiemRole.PartialDay.Code },
            set.PerDiemRates.Select(row => (Code: row.Role.Code, Key: row.Key.Value, Country: row.Country, From: row.ValidFrom, To: row.ValidTo)).ToList(),
            yearStart,
            yearEnd);

        ValidateSeries(
            set.Country,
            "mileage rate",
            new[] { VehicleKind.Car.Code, VehicleKind.Motorcycle.Code, VehicleKind.Bicycle.Code },
            set.MileageRates.Select(row => (Code: row.Vehicle.Code, Key: row.Key.Value, Country: row.Country, From: row.ValidFrom, To: row.ValidTo)).ToList(),
            yearStart,
            yearEnd);
    }

    private static void ValidateSeries(
        string country,
        string subject,
        IReadOnlyList<string> required,
        IReadOnlyList<(string Code, string Key, string Country, DateOnly From, DateOnly? To)> rows,
        DateOnly yearStart,
        DateOnly yearEnd)
    {
        foreach (var row in rows)
        {
            if (!string.Equals(row.Country, country, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The {country} travel set holds row '{row.Key}' of country '{row.Country}'.");
            }
        }

        foreach (var code in required)
        {
            var series = rows.Where(row => string.Equals(row.Code, code, StringComparison.Ordinal)).OrderBy(row => row.From).ToList();
            if (series.Count == 0)
            {
                throw new InvalidOperationException($"The {country} travel set states no {subject} row for '{code}'.");
            }

            for (var index = 1; index < series.Count; index++)
            {
                var previous = series[index - 1];
                if (previous.To is null || series[index].From <= previous.To.Value)
                {
                    throw new InvalidOperationException($"The {country} travel set overlaps {subject} rows '{previous.Key}' and '{series[index].Key}' for '{code}'.");
                }
            }

            var neededFrom = yearStart;
            var covered = false;
            foreach (var row in series)
            {
                if (covered)
                {
                    break;
                }

                if (row.From > neededFrom)
                {
                    throw new InvalidOperationException($"The {country} travel set states no {subject} row for '{code}' covering {neededFrom:yyyy-MM-dd}.");
                }

                if (row.To is null || row.To.Value == DateOnly.MaxValue || row.To.Value >= yearEnd)
                {
                    covered = true;
                }
                else if (row.To.Value >= neededFrom)
                {
                    neededFrom = row.To.Value.AddDays(1);
                }
            }

            if (!covered)
            {
                throw new InvalidOperationException($"The {country} travel set states no {subject} row for '{code}' covering {neededFrom:yyyy-MM-dd}.");
            }
        }
    }
}
