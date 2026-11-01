namespace LiteSpec.Model;

/// <summary>One repository interface a handler, reaction or job class takes in its constructor.</summary>
sealed record CodeRepository(string Interface, string File, int Line);
