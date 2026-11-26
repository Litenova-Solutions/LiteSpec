using System.Collections.Generic;
using Claimwick.Travel.Agencies;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel;

/// <summary>One travel knowledge set: the per-diem, mileage and accepted-agency rows in force in one country.</summary>
/// <follows>litespec/backend/knowledge#give-each-knowledge-area-one-model</follows>
public interface ITravelSet
{
    /// <summary>The ISO 3166-1 alpha-2 country code this set covers.</summary>
    string Country { get; }

    /// <summary>The set version operational records keep with the values they read.</summary>
    string Version { get; }

    /// <summary>The per-diem rows of this set.</summary>
    IReadOnlyList<PerDiemRate> PerDiemRates { get; }

    /// <summary>The mileage rows of this set.</summary>
    IReadOnlyList<MileageRate> MileageRates { get; }

    /// <summary>The accepted agencies of this set.</summary>
    IReadOnlyList<AcceptedAgency> AcceptedAgencies { get; }
}
