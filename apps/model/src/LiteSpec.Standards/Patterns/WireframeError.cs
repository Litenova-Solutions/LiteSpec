namespace LiteSpec.Standards;

/// <summary>One problem in a wireframe block, at a 1-based line inside the block.</summary>
/// <param name="Line">The 1-based line inside the block.</param>
/// <param name="Message">The problem text.</param>
/// <param name="Kind">The problem kind: the language itself, or one composition rule.</param>
public sealed record WireframeError(int Line, string Message, WireframeProblemKind Kind = WireframeProblemKind.Language);
