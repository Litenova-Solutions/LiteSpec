namespace LiteSpec.Model;

/// <summary>One counted name: a verb or a noun with its uses.</summary>
/// <param name="Name">The verb or noun.</param>
/// <param name="Count">How many use cases use it.</param>
sealed record NameCount(string Name, int Count);
