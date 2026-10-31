namespace LiteSpec.Book;

/// <summary>One navigation layer: a heading over a fixed list of record types.</summary>
/// <param name="Name">The layer heading.</param>
/// <param name="Types">The record types in the layer.</param>
sealed record BookLayer(string Name, IReadOnlyList<string> Types);
