namespace LiteSpec.Model;

/// <summary>
///     One page's body, kept for citation and generated-region reads.
/// </summary>
/// <param name="File">The absolute path.</param>
/// <param name="Body">The text after the frontmatter.</param>
/// <param name="BodyStartLine">The body's first line, 1-based.</param>
/// <param name="PageId">The page record's id, when the page carries one.</param>
sealed record PageBody(string File, string Body, int BodyStartLine, string? PageId);
