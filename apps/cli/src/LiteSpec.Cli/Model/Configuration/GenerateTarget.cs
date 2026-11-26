namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     One file set <c>litespec generate</c> writes from the records, from an entry of the
///     <c>generate</c> list in the consumer declaration: the kind of facts, the stack to
///     write for, and the folder the files go in.
/// </summary>
/// <param name="Kind">The record facts the target writes: refusal-codes, permissions, event-decisions or closed-sets.</param>
/// <param name="Stack">The stack the target writes for: dotnet, blazor or android write C#, react or svelte write TypeScript.</param>
/// <param name="Path">The folder the target writes into, relative to the consumer root with forward slashes, with {module} or {Module} for the module name on a .NET stack.</param>
/// <param name="Namespace">The C# namespace of the generated files, with {module} or {Module} for the module name; empty when the declaration names none.</param>
sealed record GenerateTarget(string Kind, string Stack, string Path, string Namespace);
