using System;

namespace Claimwick.Travel.Mileage;

/// <summary>One vehicle kind a mileage row pays for: the car, the motorcycle, or the bicycle.</summary>
public abstract record VehicleKind
{
    /// <summary>The car kind.</summary>
    public static VehicleCarKind Car { get; } = new();

    /// <summary>The motorcycle kind.</summary>
    public static VehicleMotorcycleKind Motorcycle { get; } = new();

    /// <summary>The bicycle kind.</summary>
    public static VehicleBicycleKind Bicycle { get; } = new();

    /// <summary>The stable kind code.</summary>
    public abstract string Code { get; }

    /// <summary>Reads the kind of one code, refusing an unknown one.</summary>
    public static VehicleKind FromCode(string code)
    {
        if (code == Car.Code)
        {
            return Car;
        }

        if (code == Motorcycle.Code)
        {
            return Motorcycle;
        }

        if (code == Bicycle.Code)
        {
            return Bicycle;
        }

        throw new ArgumentException($"Unknown vehicle kind code '{code}'.", nameof(code));
    }
}
