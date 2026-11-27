using System;
using System.Collections.Generic;
using Claimwick.Kernel.Values;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel.Germany;

/// <summary>The Germany travel set: the example per-diem and mileage rows in force in Germany.</summary>
/// <follows>litespec/core/knowledge#keep-knowledge-sets-free-of-logic</follows>
public sealed class Germany : ITravelSet
{
    /// <summary>The ISO 3166-1 alpha-2 country code this set covers.</summary>
    public string Country => "DE";

    /// <summary>The set version operational records keep with the values they read.</summary>
    public string Version => "2026.1";

    /// <summary>The per-diem rows in force in Germany.</summary>
    public IReadOnlyList<PerDiemRate> PerDiemRates { get; } = new PerDiemRate[]
    {
        PerDiemRate.Create(
            country: "DE",
            key: PerDiemRateKey.From("de-full-day-2025"),
            role: PerDiemRole.FullDay,
            dailyAmount: Money.Create(28m, "EUR"),
            validFrom: new DateOnly(2025, 1, 1),
            validTo: new DateOnly(2025, 12, 31),
            source: "reference-data/travel.per-diem-rates"),
        PerDiemRate.Create(
            country: "DE",
            key: PerDiemRateKey.From("de-full-day-2026"),
            role: PerDiemRole.FullDay,
            dailyAmount: Money.Create(30m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.per-diem-rates"),
        PerDiemRate.Create(
            country: "DE",
            key: PerDiemRateKey.From("de-partial-day-2026"),
            role: PerDiemRole.PartialDay,
            dailyAmount: Money.Create(15m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.per-diem-rates"),
    };

    /// <summary>The mileage rows in force in Germany.</summary>
    public IReadOnlyList<MileageRate> MileageRates { get; } = new MileageRate[]
    {
        MileageRate.Create(
            country: "DE",
            key: MileageRateKey.From("de-car-2026"),
            vehicle: VehicleKind.Car,
            amountPerKilometre: Money.Create(0.30m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
        MileageRate.Create(
            country: "DE",
            key: MileageRateKey.From("de-motorcycle-2026"),
            vehicle: VehicleKind.Motorcycle,
            amountPerKilometre: Money.Create(0.20m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
        MileageRate.Create(
            country: "DE",
            key: MileageRateKey.From("de-bicycle-2026"),
            vehicle: VehicleKind.Bicycle,
            amountPerKilometre: Money.Create(0.10m, "EUR"),
            validFrom: new DateOnly(2026, 1, 1),
            validTo: null,
            source: "reference-data/travel.mileage-rates"),
    };
}
