namespace LiteSpec.Book;

/// <summary>One record type's book words: its display label, plural, layer and question.</summary>
/// <param name="Label">The display label, such as Use case.</param>
/// <param name="Plural">The display plural, such as Use cases.</param>
/// <param name="Layer">The record layer the record-type table states, verbatim.</param>
/// <param name="Question">The question the record-type table states, verbatim.</param>
sealed record LexiconType(string Label, string Plural, string Layer, string Question);
