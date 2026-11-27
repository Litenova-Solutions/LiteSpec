namespace LiteSpec.Cli.Model.Configuration;

/// <summary>The consumer harness the persistence check runs: a command LiteSpec appends a mode and a snapshot folder to.</summary>
/// <param name="Command">The command line, run from the consumer root.</param>
sealed record VerifyPersistence(string Command);
