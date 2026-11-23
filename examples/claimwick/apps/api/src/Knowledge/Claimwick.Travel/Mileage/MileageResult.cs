namespace Claimwick.Travel.Mileage;

/// <summary>One answered mileage lookup: the row in force with the set version that stated it.</summary>
public sealed record MileageResult(MileageRate Rate, string SetVersion);
