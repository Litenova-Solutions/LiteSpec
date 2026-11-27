namespace __NAMESPACE__.Infrastructure.Persistence;

/// <summary>The persisted shape of one health check-in, as the file store writes it under app-private storage.</summary>
/// <param name="Id">The check-in identifier the backend sample assigned.</param>
/// <param name="State">The lifecycle state of the check-in, as written.</param>
public sealed record StoredCheckIn(string Id, string State);
