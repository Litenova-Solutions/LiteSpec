namespace LiteSpec.Cli.Audit;

/// <summary>One script finding at one width: the code, selector and message the page yielded.</summary>
/// <param name="Code">The audit finding code.</param>
/// <param name="Selector">The selector of the element the finding names.</param>
/// <param name="Message">What is wrong, in one present-tense sentence.</param>
/// <param name="Route">The route path the finding was read on.</param>
/// <param name="Scheme">The color scheme the finding was read in.</param>
/// <param name="Width">The viewport width the finding was read at.</param>
sealed record AuditRawFinding(string Code, string Selector, string Message, string Route, string Scheme, int Width);
