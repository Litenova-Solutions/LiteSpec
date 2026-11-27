namespace Claimwick.Travel.Mileage;

/// <summary>The motorcycle vehicle kind.</summary>
public sealed record VehicleMotorcycleKind : VehicleKind
{
    /// <summary>The stable kind code.</summary>
    public override string Code => "motorcycle";
}
