namespace LiteSpec.Cli.Model.Code;

/// <summary>One exception or failure type a handler, reaction or job class constructs.</summary>
sealed record CodeThrown(string Type, string File, int Line);
