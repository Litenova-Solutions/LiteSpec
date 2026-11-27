namespace Claimwick.Travel.PerDiem;

/// <summary>The partial-day per-diem role: the allowance for a travel day under eight hours.</summary>
public sealed record PerDiemPartialDayRole : PerDiemRole
{
    /// <summary>The stable role code.</summary>
    public override string Code => "partial-day";
}
