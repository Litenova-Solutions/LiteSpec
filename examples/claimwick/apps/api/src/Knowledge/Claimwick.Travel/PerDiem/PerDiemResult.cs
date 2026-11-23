namespace Claimwick.Travel.PerDiem;

/// <summary>One answered per-diem lookup: the row in force with the set version that stated it.</summary>
public sealed record PerDiemResult(PerDiemRate Rate, string SetVersion);
