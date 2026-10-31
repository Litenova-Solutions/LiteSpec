namespace LiteSpec.Model;

/// <summary>One browser test and what it proves, reaches and asserts.</summary>
sealed record BrowserTestInfo(
    string Frontend,
    string Title,
    string File,
    int Line,
    IReadOnlyList<string> Proves,
    string? Journey,
    IReadOnlyList<string> Gotos,
    IReadOnlyList<string> Codes,
    IReadOnlyList<string> Texts,
    string Reader = Finding.TypeScriptReader);
