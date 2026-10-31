namespace LiteSpec.Book;

/// <summary>One parent rule: the record type, the parent types it may sit under, and how the parent is found.</summary>
/// <param name="Type">The record type the rule covers.</param>
/// <param name="ParentTypes">The parent record types in precedence order; empty for the product root.</param>
/// <param name="FoundThrough">The phrase the Parents table uses, copied exactly.</param>
sealed record BookParentRule(string Type, IReadOnlyList<string> ParentTypes, string FoundThrough);
