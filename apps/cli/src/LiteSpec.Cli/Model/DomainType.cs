namespace LiteSpec.Model;

/// <summary>
///     One top-level type declared in the Domain project: its simple name, its namespace,
///     its file and line, and whether it carries a placing mark. A <c>follows</c> mark
///     never places a type, so a type carrying only <c>follows</c> marks reads as unmarked.
/// </summary>
/// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
sealed record DomainType(string Name, string Namespace, string File, int Line, bool HasMark);
