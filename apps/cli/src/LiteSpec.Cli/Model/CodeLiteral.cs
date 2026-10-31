namespace LiteSpec.Model;

/// <summary>
///     One refusal code literal in code. <paramref name="Declares" /> is true when the literal
///     sits where code declares a refusal code, so an unrecorded code is reported only there;
///     any literal still counts as evidence that a recorded code is thrown.
/// </summary>
sealed record CodeLiteral(string Value, string File, int Line, string? Class, bool Declares = true);
