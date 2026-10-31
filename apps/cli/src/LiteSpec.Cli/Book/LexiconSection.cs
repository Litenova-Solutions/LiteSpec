namespace LiteSpec.Book;

/// <summary>One model-page section's book words: its first paragraph and its source.</summary>
/// <param name="Text">The section's first paragraph, verbatim.</param>
/// <param name="Source">The LiteSpec source, such as model.md, Statuses.</param>
sealed record LexiconSection(string Text, string Source);
