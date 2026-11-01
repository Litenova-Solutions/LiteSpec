namespace LiteSpec.Model;

/// <summary>
///     One marked type or method of no known kind: a reading method, a private
///     helper, or an unrecognized class carrying a mark, or a proves mark on a
///     method with no test attribute. Its marks are reported, never linked.
/// </summary>
sealed record StrayMark(
    string Class, string? Member, string File, int Line, IReadOnlyList<CodeMark> Marks);
