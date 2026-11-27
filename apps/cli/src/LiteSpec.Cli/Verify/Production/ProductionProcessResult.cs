namespace LiteSpec.Cli.Verify.Production;

/// <summary>The result of one external process run: its exit status and its error text.</summary>
/// <param name="Status">The exit status; 0 for success.</param>
/// <param name="Error">The text the process wrote to standard error, or the runner's own message.</param>
sealed record ProductionProcessResult(int Status, string Error);
