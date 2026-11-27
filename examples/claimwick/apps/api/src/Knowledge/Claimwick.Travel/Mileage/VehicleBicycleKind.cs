namespace Claimwick.Travel.Mileage;

/// <summary>The bicycle vehicle kind.</summary>
public sealed record VehicleBicycleKind : VehicleKind
{
    /// <summary>The stable kind code.</summary>
    public override string Code => "bicycle";
}
