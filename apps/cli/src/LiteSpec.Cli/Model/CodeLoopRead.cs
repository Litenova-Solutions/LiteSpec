namespace LiteSpec.Model;

/// <summary>One repository read a handler, reaction or job class makes inside a loop body.</summary>
sealed record CodeLoopRead(string Repository, string Method, string File, int Line);
