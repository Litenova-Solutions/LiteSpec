namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     One file set <c>litespec sync</c> writes from the records, from an entry of the
///     <c>sync</c> list in the consumer declaration: the kind of facts, the stack to
///     write for, and the folder the files go in.
/// </summary>
/// <param name="Kind">The record facts the target writes: refusal-codes, permissions, event-decisions or closed-sets.</param>
/// <param name="Stack">The stack the target writes for: dotnet or android writes C#, svelte writes TypeScript.</param>
/// <param name="Path">The folder the target writes into, relative to the consumer root with forward slashes, with {module} or {Module} for the module name on a .NET stack.</param>
/// <param name="Namespace">The C# namespace of the synced files, with {module} or {Module} for the module name; empty when the declaration names none.</param>
sealed record SyncTarget(string Kind, string Stack, string Path, string Namespace);
