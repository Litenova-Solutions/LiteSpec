namespace LiteSpec.Cli.Model.Code;

/// <summary>One refusal code a handler, reaction or job class produces in a declaring position.</summary>
sealed record CodeRefusal(string Code, string File, int Line);
