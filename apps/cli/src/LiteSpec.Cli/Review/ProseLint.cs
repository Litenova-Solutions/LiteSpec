namespace LiteSpec.Cli.Review;

/// <summary>A prose finding on one line of a changed documentation or record file.</summary>
/// <param name="File">The file path relative to the repository root.</param>
/// <param name="Line">The one-based line number.</param>
/// <param name="Code">The lint code: paragraph-too-long, litespec-ese, promotional-language, refusal-code-raw, or file-unreadable.</param>
/// <param name="Message">The present-tense fact the line shows, with the fix.</param>
sealed record ProseLint(string File, int Line, string Code, string Message);
