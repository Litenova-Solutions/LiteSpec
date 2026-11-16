namespace LiteSpec.Cli.Model.CompletionStages;

/// <summary>One parked child: a record a live waiver holds out of its parent's stage, with that waiver and its expiry.</summary>
/// <param name="Id">The parked record id.</param>
/// <param name="Waiver">The live waiver parking the record.</param>
/// <param name="ExpiresOn">The waiver expiry date, or null for a lasting waiver.</param>
sealed record ParkedEntry(string Id, string Waiver, string? ExpiresOn);
