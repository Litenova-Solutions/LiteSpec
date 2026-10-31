namespace LiteSpec.Model.Readers.Operations;

/// <summary>One follows mark in an operations file.</summary>
/// <param name="File">The absolute file path.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Id">The decision, exception or provision the mark names.</param>
/// <param name="Style">The style the mark was written in: always the comment.</param>
sealed record OperationsMark(string File, int Line, string Id, MarkStyle Style = MarkStyle.Comment);
