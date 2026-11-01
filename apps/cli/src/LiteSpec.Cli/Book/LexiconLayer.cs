namespace LiteSpec.Book;

/// <summary>One record layer's book words: its display label, writers and reviewers.</summary>
/// <param name="Label">The display label, such as Intent.</param>
/// <param name="Writes">Who writes the layer, in the four-layer table's words.</param>
/// <param name="Reviews">Who reviews the layer, in the four-layer table's words.</param>
sealed record LexiconLayer(string Label, string Writes, string Reviews);
