namespace LiteSpec.Model.Checks;

/// <summary>One Markdown table outside fenced code, with its header line offset in the body.</summary>
internal sealed record BodyTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, int Line);
