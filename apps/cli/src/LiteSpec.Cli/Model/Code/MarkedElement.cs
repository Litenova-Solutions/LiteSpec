using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Code;

/// <summary>
///     One code element the marks table names: its kind, its type, its member for an
///     aggregate method or a test, its marks, whether it sits in a test project, the
///     hand-written runtime value it declares (an event's Classification literal or a
///     rule exception's Code literal), when it declares one, and the namespace-qualified
///     name of its type, which merges the partial parts of one type into one element.
/// </summary>
sealed record MarkedElement(
    CodeKind Kind, string Class, string? Member, string File, int Line,
    IReadOnlyList<CodeMark> Marks, bool InTests, string? RuntimeValue = null, string QualifiedName = "");
