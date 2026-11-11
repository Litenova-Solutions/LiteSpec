namespace LiteSpec.Cli;

/// <summary>The output writers, working folder and variables of one command run in-process.</summary>
/// <param name="Out">The writer for standard output.</param>
/// <param name="Error">The writer for standard error.</param>
/// <param name="WorkingDirectory">The folder the command runs in.</param>
/// <param name="Variables">The variables the run sets; an empty value reads as unset.</param>
sealed record CliRun(TextWriter Out, TextWriter Error, string WorkingDirectory, IReadOnlyDictionary<string, string> Variables);
