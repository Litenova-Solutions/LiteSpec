namespace LiteSpec.Book;

/// <summary>A record id split into its type, home module and name.</summary>
/// <param name="Type">The part before the slash.</param>
/// <param name="Home">The part between the slash and the dot, or null when the id has no home.</param>
/// <param name="Name">The part after the dot, or after the slash when the id has no home.</param>
sealed record SplitId(string Type, string? Home, string Name);
