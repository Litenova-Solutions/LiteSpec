namespace LiteSpec.Tests.Support;

/// <summary>The exit code and the two output streams of one CLI run.</summary>
/// <param name="Status">The exit code.</param>
/// <param name="Stdout">Everything written to standard output.</param>
/// <param name="Stderr">Everything written to standard error.</param>
sealed record CliResult(int Status, string Stdout, string Stderr)
{
    /// <summary>Gets both streams, standard output first.</summary>
    public string Output => Stdout + Stderr;
}
