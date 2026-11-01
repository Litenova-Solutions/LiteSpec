namespace LiteSpec.Book;

/// <summary>One glossary term's book words: its definition, layer and source.</summary>
/// <param name="Term">The term, in the glossary heading's words.</param>
/// <param name="Definition">The definition paragraphs flattened to one line.</param>
/// <param name="Layer">The layer the entry states, or null when it states none.</param>
/// <param name="Source">The source the entry states, or null when it states none.</param>
sealed record LexiconGlossaryEntry(string Term, string Definition, string? Layer, string? Source);
