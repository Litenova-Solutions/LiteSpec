namespace LiteSpec.Book;

/// <summary>One finding code's book words: its tier and its meaning.</summary>
/// <param name="Tier">The tier the finding-code table states.</param>
/// <param name="Meaning">The meaning the finding-code table states, verbatim.</param>
sealed record LexiconFinding(string Tier, string Meaning);
