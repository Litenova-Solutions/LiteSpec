namespace LiteSpec.Model;

/// <summary>One use-case handler, linked to its use case through its mark.</summary>
sealed record HandlerInfo(
    string UseCaseId,
    string Module,
    string Name,
    string Operation,
    string Kind,
    string File,
    string Class = "",
    int Line = 1,
    IReadOnlyList<CodeRepository>? Repositories = null,
    IReadOnlyList<CodeRefusal>? Refusals = null,
    IReadOnlyList<CodeThrown>? Thrown = null,
    IReadOnlyList<CodeDispatch>? Dispatches = null,
    string MessageType = "",
    int DependencyCount = 0,
    IReadOnlyList<CodeCall>? AggregateCalls = null,
    IReadOnlyList<CodeCollaborator>? Collaborators = null,
    IReadOnlyList<CodeLoopRead>? LoopReads = null,
    IReadOnlyList<CodeMemoryFilter>? MemoryFilters = null)
{
    /// <summary>Gets static calls the handler makes on aggregate root types, with the root type name as receiver.</summary>
    public IReadOnlyList<CodeCall> FactoryCalls { get; init; } = [];
}
