namespace LiteSpec.Cli.Model.Code;

/// <summary>One comment in code, with its line.</summary>
sealed record CodeComment(string Text, string File, int Line);
