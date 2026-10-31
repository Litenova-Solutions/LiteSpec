namespace LiteSpec.Model.Readers.Dotnet;

/// <summary>One class's collected details: its repositories, refusals, thrown types, dispatches and handler shape.</summary>
/// <param name="Class">The class name.</param>
/// <param name="Line">The 1-based line of the class.</param>
/// <param name="Repositories">The repository interfaces the class takes.</param>
/// <param name="Refusals">The refusal codes the class produces.</param>
/// <param name="Thrown">The exception and failure types the class constructs.</param>
/// <param name="Dispatches">The dispatch calls the class makes.</param>
/// <param name="MessageType">The message type the handler handles.</param>
/// <param name="DependencyCount">The constructor dependencies the class declares.</param>
/// <param name="AggregateCalls">The calls the class makes on a held aggregate root.</param>
/// <param name="FactoryCalls">The static calls the class makes on aggregate root types.</param>
/// <param name="Collaborators">The collaborators the class builds whose constructor takes a repository.</param>
/// <param name="LoopReads">The repository reads the class makes inside a loop body.</param>
/// <param name="MemoryFilters">The filters the class applies to a repository list in memory.</param>
sealed record ClassDetails(
    string Class,
    int Line,
    List<CodeRepository> Repositories,
    List<CodeRefusal> Refusals,
    List<CodeThrown> Thrown,
    List<CodeDispatch> Dispatches,
    string MessageType,
    int DependencyCount,
    List<CodeCall> AggregateCalls,
    List<CodeCall> FactoryCalls,
    List<CodeCollaborator> Collaborators,
    List<CodeLoopRead> LoopReads,
    List<CodeMemoryFilter> MemoryFilters);
