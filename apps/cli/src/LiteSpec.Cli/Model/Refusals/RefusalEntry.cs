namespace LiteSpec.Cli.Model.Refusals;

/// <summary>One refusal code a module declares in its <c>refusals.yaml</c>: its meaning.</summary>
/// <param name="Module">The module that declares the code.</param>
/// <param name="Code">The refusal code, <c>&lt;module&gt;.&lt;name&gt;</c>.</param>
/// <param name="Meaning">What the refusal says to the caller.</param>
/// <param name="File">The refusal set file that declares the code.</param>
sealed record RefusalEntry(string Module, string Code, string Meaning, string File);
