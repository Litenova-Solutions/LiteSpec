namespace __NAMESPACE__.Health.Application.CheckIns.ReadCheckIns;

/// <summary>One listed check-in: its identifier and its state code.</summary>
/// <param name="CheckInId">The listed check-in.</param>
/// <param name="State">The state code the check-in is in.</param>
public sealed record CheckInRow(string CheckInId, string State);
