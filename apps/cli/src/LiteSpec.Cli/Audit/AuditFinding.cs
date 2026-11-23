namespace LiteSpec.Cli.Audit;

/// <summary>One merged audit finding: one code, subject and fact across the widths that showed it.</summary>
/// <param name="Code">The audit finding code.</param>
/// <param name="Subject">The subject the finding is about: the screen id, or the routes file name.</param>
/// <param name="Message">What is wrong, in one present-tense sentence.</param>
/// <param name="Route">The route path the finding was read on.</param>
/// <param name="Scheme">The color scheme the finding was read in.</param>
/// <param name="Widths">The viewport widths that showed the finding, ascending.</param>
/// <param name="Selector">The selector of the element the finding names.</param>
sealed record AuditFinding(
    string Code,
    string Subject,
    string Message,
    string Route,
    string Scheme,
    IReadOnlyList<int> Widths,
    string Selector);
