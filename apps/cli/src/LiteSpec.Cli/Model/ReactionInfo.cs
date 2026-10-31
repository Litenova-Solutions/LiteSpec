namespace LiteSpec.Model;

/// <summary>One event handler, linked to its reaction through its mark.</summary>
sealed record ReactionInfo(
    string Class,
    string File,
    int Line,
    string? UseCaseId,
    string? RecordId = null,
    IReadOnlyList<CodeRepository>? Repositories = null,
    IReadOnlyList<CodeRefusal>? Refusals = null,
    IReadOnlyList<CodeThrown>? Thrown = null,
    IReadOnlyList<CodeDispatch>? Dispatches = null,
    int DependencyCount = 0,
    IReadOnlyList<CodeCollaborator>? Collaborators = null,
    IReadOnlyList<CodeLoopRead>? LoopReads = null);
