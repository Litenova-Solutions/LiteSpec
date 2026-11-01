namespace LiteSpec.Model;

/// <summary>
///     Where one lookalike form first occurs.
/// </summary>
/// <param name="Form">The form in lower kebab case.</param>
/// <param name="Seen">The first record id, or the relative path when no record holds it.</param>
/// <param name="Subject">The first record id, or null when no record holds the form.</param>
/// <param name="File">The absolute path of the first occurrence.</param>
/// <param name="Line">The 1-based line of the first occurrence.</param>
sealed record LookalikeHit(string Form, string Seen, string? Subject, string File, int Line);
