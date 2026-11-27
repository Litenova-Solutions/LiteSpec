namespace Claimwick.Travel.PerDiem;

/// <summary>The full-day per-diem role: the allowance for a whole travel day.</summary>
public sealed record PerDiemFullDayRole : PerDiemRole
{
    /// <summary>The stable role code.</summary>
    public override string Code => "full-day";
}
