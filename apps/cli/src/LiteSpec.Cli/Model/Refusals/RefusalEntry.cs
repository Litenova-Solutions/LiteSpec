namespace LiteSpec.Cli.Model.Refusals;

/// <summary>One refusal an aggregate or a module declares in its <c>refusals.yaml</c>: its meaning.</summary>
/// <param name="Module">The module that declares the refusal.</param>
/// <param name="Code">The refusal code, <c>&lt;module&gt;.&lt;name&gt;</c>, derived from the refusal identifier.</param>
/// <param name="Meaning">What the refusal says to the caller.</param>
/// <param name="File">The refusal set file that declares the refusal.</param>
/// <param name="Aggregate">The aggregate folder that holds the refusal set, or null for a module-level refusal.</param>
/// <param name="Line">The 1-based line of the entry in its file.</param>
sealed record RefusalEntry(string Module, string Code, string Meaning, string File, string? Aggregate, int Line = 1);
