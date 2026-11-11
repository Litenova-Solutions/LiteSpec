namespace LiteSpec.Cli.Model.Code;

/// <summary>One collaborator a handler, reaction or job class builds with <c>new</c> whose own constructor takes a repository.</summary>
sealed record CodeCollaborator(string Type, string File, int Line);
