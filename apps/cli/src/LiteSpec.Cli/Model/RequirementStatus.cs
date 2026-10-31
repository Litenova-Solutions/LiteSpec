namespace LiteSpec.Model;

/// <summary>
///     One requirement's computed status: verified when everything fulfilling it is verified,
///     and, for a measured requirement, when its measure has a passing check.
///     <see cref="MeasurePassed"/> is null for a requirement with no measure or no result yet.
/// </summary>
sealed record RequirementStatus(bool Verified, bool? MeasurePassed);
