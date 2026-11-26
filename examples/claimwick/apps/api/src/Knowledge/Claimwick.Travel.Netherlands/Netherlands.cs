using System.Collections.Generic;
using Claimwick.Travel.Agencies;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel.Netherlands;

/// <summary>The Netherlands travel set: the example per-diem and mileage rows in force in the Netherlands.</summary>
/// <follows>litespec/backend/knowledge#keep-knowledge-sets-free-of-logic</follows>
public sealed class Netherlands : ITravelSet
{
    /// <summary>The ISO 3166-1 alpha-2 country code this set covers.</summary>
    public string Country => "NL";

    /// <summary>The set version operational records keep with the values they read.</summary>
    public string Version => "2026.1";

    /// <summary>The per-diem rows in force in the Netherlands.</summary>
    public IReadOnlyList<PerDiemRate> PerDiemRates => NetherlandsPerDiemRates.Rows;

    /// <summary>The mileage rows in force in the Netherlands.</summary>
    public IReadOnlyList<MileageRate> MileageRates => NetherlandsMileageRates.Rows;

    /// <summary>The accepted agencies in force in the Netherlands.</summary>
    public IReadOnlyList<AcceptedAgency> AcceptedAgencies => NetherlandsAcceptedAgencies.Rows;
}
