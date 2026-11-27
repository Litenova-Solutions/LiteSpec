using System;

namespace Claimwick.Travel.PerDiem;

/// <summary>One per-diem role a caller asks for: the full or the partial day.</summary>
public abstract record PerDiemRole
{
    /// <summary>The full-day role.</summary>
    public static PerDiemFullDayRole FullDay { get; } = new();

    /// <summary>The partial-day role.</summary>
    public static PerDiemPartialDayRole PartialDay { get; } = new();

    /// <summary>The stable role code.</summary>
    public abstract string Code { get; }

    /// <summary>Reads the role of one code, refusing an unknown one.</summary>
    public static PerDiemRole FromCode(string code)
    {
        if (code == FullDay.Code)
        {
            return FullDay;
        }

        if (code == PartialDay.Code)
        {
            return PartialDay;
        }

        throw new ArgumentException($"Unknown per-diem role code '{code}'.", nameof(code));
    }
}
