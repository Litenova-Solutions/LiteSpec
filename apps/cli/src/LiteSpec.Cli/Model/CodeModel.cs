namespace LiteSpec.Model;

/// <summary>One mark as read from source.</summary>
sealed record CodeMark(string RecordId, MarkStyle Style, MarkVerb Verb, string File, int Line);

/// <summary>
///     One code element the marks table names: its kind, its type, its member for an
///     aggregate method or a test, its marks, whether it sits in a test project, and the
///     hand-written runtime value it declares (an event's Classification literal or a
///     rule exception's Code literal), when it declares one.
/// </summary>
sealed record MarkedElement(
    CodeKind Kind, string Class, string? Member, string File, int Line,
    IReadOnlyList<CodeMark> Marks, bool InTests, string? RuntimeValue = null);

/// <summary>
///     One marked type or method of no known kind: a reading method, a private
///     helper, or an unrecognized class carrying a mark, or a proves mark on a
///     method with no test attribute. Its marks are reported, never linked.
/// </summary>
sealed record StrayMark(
    string Class, string? Member, string File, int Line, IReadOnlyList<CodeMark> Marks);

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
    /// <summary>Static calls the handler makes on aggregate root types, with the root type name as receiver.</summary>
    public IReadOnlyList<CodeCall> FactoryCalls { get; init; } = [];
}

/// <summary>One reaction or job class.</summary>
sealed record ReactionInfo(
    string Class,
    string File,
    int Line,
    string Kind,
    string? UseCaseId,
    string? RecordId = null,
    IReadOnlyList<CodeRepository>? Repositories = null,
    IReadOnlyList<CodeRefusal>? Refusals = null,
    IReadOnlyList<CodeThrown>? Thrown = null,
    IReadOnlyList<CodeDispatch>? Dispatches = null,
    int DependencyCount = 0,
    IReadOnlyList<CodeCollaborator>? Collaborators = null,
    IReadOnlyList<CodeLoopRead>? LoopReads = null);

/// <summary>One domain event class.</summary>
sealed record DomainEventInfo(string Class, string File, int Line, string? EventId);

/// <summary>
///     One top-level type declared in the Domain project: its simple name, its namespace,
///     its file and line, and whether it carries a placing mark. A <c>follows</c> mark
///     never places a type, so a type carrying only <c>follows</c> marks reads as unmarked.
/// </summary>
/// <follows>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</follows>
sealed record DomainType(string Name, string Namespace, string File, int Line, bool HasMark);

/// <summary>
///     One refusal code literal in code. <paramref name="Declares" /> is true when the literal
///     sits where code declares a refusal code, so an unrecorded code is reported only there;
///     any literal still counts as evidence that a recorded code is thrown.
/// </summary>
sealed record CodeLiteral(string Value, string File, int Line, string? Class, bool Declares = true);

/// <summary>One comment in code, with its line.</summary>
sealed record CodeComment(string Text, string File, int Line);

/// <summary>One endpoint operation mapped to its use case.</summary>
sealed record EndpointOperation(string Operation, string UseCaseId, string Method, string Route, string File, int Line = 1);

/// <summary>One repository interface a handler, reaction or job class takes in its constructor.</summary>
sealed record CodeRepository(string Interface, string File, int Line);

/// <summary>One Infrastructure class implementing a domain repository interface, through its declared base list.</summary>
sealed record CodeRepositoryImplementation(string Interface, string Class, string File, int Line);

/// <summary>One refusal code a handler, reaction or job class produces in a declaring position.</summary>
sealed record CodeRefusal(string Code, string File, int Line);

/// <summary>One exception or failure type a handler, reaction or job class constructs.</summary>
sealed record CodeThrown(string Type, string File, int Line);

/// <summary>
///     One dispatch call a handler, reaction or job class makes. <paramref name="Verb" /> is
///     <c>issues</c>, <c>publishes</c>, <c>sends</c> or <c>calls</c>. <paramref name="Target" />
///     is the message type, the template key, or the port method. <paramref name="Detail" />
///     is the port type for <c>calls</c>, <c>type</c> or <c>template</c> for <c>sends</c>,
///     and empty otherwise. <paramref name="Via" /> names the helper <c>Type.Method</c> a
///     one-hop attribution found the dispatch through, or null for a direct dispatch.
/// </summary>
sealed record CodeDispatch(string Verb, string Target, string Detail, string File, int Line, string? Via = null);

/// <summary>One call a handler makes on an aggregate root it holds.</summary>
sealed record CodeCall(string Receiver, string Method, string File, int Line);

/// <summary>One public method on an aggregate root with the domain events it raises.</summary>
sealed record AggregateMethod(string Aggregate, string Method, IReadOnlyList<string> Events, string File, int Line)
{
    /// <summary>The use-case marks the method carries, in source order.</summary>
    public IReadOnlyList<string> UseCases { get; init; } = [];
}

/// <summary>One collaborator a handler, reaction or job class builds with <c>new</c> whose own constructor takes a repository.</summary>
sealed record CodeCollaborator(string Type, string File, int Line);

/// <summary>One repository read a handler, reaction or job class makes inside a loop body.</summary>
sealed record CodeLoopRead(string Repository, string Method, string File, int Line);

/// <summary>One LINQ filter a query handler applies to the result of a repository list call.</summary>
sealed record CodeMemoryFilter(string File, int Line);

/// <summary>One audit action literal a command message declares in its audit attribute.</summary>
sealed record CodeAuditAction(string Operation, string Action, string File, int Line);

/// <summary>What the backend source says.</summary>
sealed class CodeModel
{
    public List<HandlerInfo> Handlers { get; } = [];
    public List<ReactionInfo> Reactions { get; } = [];
    public List<DomainEventInfo> Events { get; } = [];
    public List<CodeLiteral> Literals { get; } = [];

    /// <summary>Every recognized element of the backend projects, sorted by file then line.</summary>
    public List<MarkedElement> Elements { get; } = [];

    /// <summary>Every marked element of no known kind, sorted by file then line.</summary>
    public List<StrayMark> StrayMarks { get; } = [];

    /// <summary>Every kebab-case string literal in the code, the keys a composed refusal code is built from.</summary>
    public HashSet<string> KeyLiterals { get; } = new(StringComparer.Ordinal);
    public List<CodeComment> Comments { get; } = [];
    public List<EndpointOperation> Endpoints { get; } = [];
    public List<AggregateMethod> AggregateMethods { get; } = [];
    public List<CodeAuditAction> AuditActions { get; } = [];

    /// <summary>Every top-level type declared in the Domain project, merged across partial parts.</summary>
    public List<DomainType> DomainTypes { get; } = [];

    /// <summary>Every Infrastructure class implementing a domain repository interface, sorted by file then line.</summary>
    public List<CodeRepositoryImplementation> RepositoryImplementations { get; } = [];
}
