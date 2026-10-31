namespace LiteSpec.Model;

/// <summary>
///     One code element the marks table names: its kind, its type, its member for an
///     aggregate method or a test, its marks, whether it sits in a test project, and the
///     hand-written runtime value it declares (an event's Classification literal or a
///     rule exception's Code literal), when it declares one.
/// </summary>
sealed record MarkedElement(
    CodeKind Kind, string Class, string? Member, string File, int Line,
    IReadOnlyList<CodeMark> Marks, bool InTests, string? RuntimeValue = null);
