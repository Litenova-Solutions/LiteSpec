using System;
using System.Collections.Generic;
using Claimwick.Kernel.Values;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel.Netherlands;

/// <summary>The Netherlands travel set: the example per-diem and mileage rows in force in the Netherlands.</summary>
/// <follows>litespec/core/knowledge#keep-knowledge-sets-free-of-logic</follows>
public sealed class Netherlands : ITravelSet
{
    /// <summary>The ISO 3166-1 alpha-2 country code this set covers.</summary>
    public string Country => "NL";

    /// <summary>The set version operational records keep with the values they read.</summary>
    public string Version => "2026.1";

    /// <summary>The per-diem rows in force in the Netherlands.</summary>
    public IReadOnlyList<PerDiemRate> PerDiemRates { get; } = new PerDiemRate[]
    {
        PerDiemRate.Create(
            country: "NL",
            key: PerDiemRateKey.From("nl-full-day-2026"),
            role: PerDiemRole.FullDay,
            dailyAmount: Money.Create(25m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.per-diem-rates"),
        PerDiemRate.Create(
            country: "NL",
            key: PerDiemRateKey.From("nl-partial-day-2026"),
            role: PerDiemRole.PartialDay,
            dailyAmount: Money.Create(12m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.per-diem-rates"),
    };

    /// <summary>The mileage rows in force in the Netherlands.</summary>
    public IReadOnlyList<MileageRate> MileageRates { get; } = new MileageRate[]
    {
        MileageRate.Create(
            country: "NL",
            key: MileageRateKey.From("nl-car-2026"),
            vehicle: VehicleKind.Car,
            amountPerKilometre: Money.Create(0.25m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
        MileageRate.Create(
            country: "NL",
            key: MileageRateKey.From("nl-motorcycle-2026"),
            vehicle: VehicleKind.Motorcycle,
            amountPerKilometre: Money.Create(0.15m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
        MileageRate.Create(
            country: "NL",
            key: MileageRateKey.From("nl-bicycle-2026"),
            vehicle: VehicleKind.Bicycle,
            amountPerKilometre: Money.Create(0.10m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
    };
}
