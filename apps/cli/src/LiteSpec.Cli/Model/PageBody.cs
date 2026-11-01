namespace LiteSpec.Model;

/// <summary>
///     One Markdown text the reader keeps: an explanation file beside a record file, or a document.
/// </summary>
/// <param name="File">The absolute path.</param>
/// <param name="Body">The whole explanation text, or the document text after its frontmatter.</param>
/// <param name="BodyStartLine">The body's first line, 1-based.</param>
/// <param name="PageId">The page record's id, for an explanation beside a page record; null otherwise.</param>
sealed record PageBody(string File, string Body, int BodyStartLine, string? PageId);
