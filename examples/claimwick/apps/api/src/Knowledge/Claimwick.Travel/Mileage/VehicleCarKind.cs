namespace Claimwick.Travel.Mileage;

/// <summary>The car vehicle kind.</summary>
public sealed record VehicleCarKind : VehicleKind
{
    /// <summary>The stable kind code.</summary>
    public override string Code => "car";
}
