using System;
using Claimwick.Travel.Mileage;
using Claimwick.Travel.PerDiem;

namespace Claimwick.Travel;

/// <summary>Answers travel lookups by role and date. Callers name a role, never a set or a data key.</summary>
public interface ITravelRules
{
    /// <summary>Reads the per-diem rate in force for one role on one date.</summary>
    PerDiemResult PerDiemFor(string country, PerDiemRole role, DateOnly date);

    /// <summary>Reads the mileage rate in force for one vehicle kind on one date.</summary>
    MileageResult MileageFor(string country, VehicleKind vehicle, DateOnly date);

    /// <summary>Reads the version of one country set.</summary>
    string SetVersion(string country);
}
