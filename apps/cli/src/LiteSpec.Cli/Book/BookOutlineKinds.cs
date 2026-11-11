namespace LiteSpec.Cli.Book;

/// <summary>
///     The closed set of outline node kinds the book writes in <c>kind</c>:
///     record, group, code, test and finding. A leaf type is always its own
///     kind. The client types them as one union.
/// </summary>
static class BookOutlineKinds
{
    /// <summary>Gets the <c>record</c> outline kind: a placed record.</summary>
    public const string Record = "record";

    /// <summary>Gets the <c>group</c> outline kind: a layer or type group.</summary>
    public const string Group = "group";

    /// <summary>Gets the <c>code</c> outline kind: an implementing file under its record.</summary>
    public const string Code = "code";

    /// <summary>Gets the <c>test</c> outline kind: a proving test under its record.</summary>
    public const string Test = "test";

    /// <summary>Gets the <c>finding</c> outline kind: a finding under its subject record.</summary>
    public const string Finding = "finding";

    /// <summary>Every outline kind the writer emits.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Record, Group, Code, Test, Finding,
    };
}
