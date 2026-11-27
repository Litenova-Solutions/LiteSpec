namespace LiteSpec.Cli.Book.Lexicon;

/// <summary>One model-page section's book words: its carried paragraph and its source.</summary>
/// <param name="Text">The section's carried paragraph, verbatim.</param>
/// <param name="Source">The LiteSpec source, such as model.md, Completion Stages.</param>
sealed record LexiconSection(string Text, string Source);
