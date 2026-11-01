namespace LiteSpec.Authoring;

/// <summary>
///     One authoring finding: the repository-relative file, the 1-based line, the stable code, and the message.
/// </summary>
internal sealed record StandardsDiagnostic(string Relative, int Line, string Code, string Message);
