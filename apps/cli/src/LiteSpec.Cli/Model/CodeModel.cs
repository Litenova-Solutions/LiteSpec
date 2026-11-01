namespace LiteSpec.Model;

/// <summary>What the backend source says.</summary>
sealed class CodeModel
{
    /// <summary>Gets every use-case handler the backend read found, in read order.</summary>
    public List<HandlerInfo> Handlers { get; } = [];

    /// <summary>Gets every event handler the backend read found, in read order.</summary>
    public List<ReactionInfo> Reactions { get; } = [];

    /// <summary>Gets every job host the backend read found, in read order.</summary>
    public List<JobInfo> Jobs { get; } = [];

    /// <summary>Gets every domain event class the backend read found, in read order.</summary>
    public List<DomainEventInfo> Events { get; } = [];

    /// <summary>Gets every refusal code literal the backend read found, in read order.</summary>
    public List<CodeLiteral> Literals { get; } = [];

    /// <summary>Gets every recognized element of the backend projects, sorted by file then line.</summary>
    public List<MarkedElement> Elements { get; } = [];

    /// <summary>Gets every marked element of no known kind, sorted by file then line.</summary>
    public List<StrayMark> StrayMarks { get; } = [];

    /// <summary>Gets every kebab-case string literal in the code, the keys a composed refusal code is built from.</summary>
    public HashSet<string> KeyLiterals { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets every comment in the backend code, in read order.</summary>
    public List<CodeComment> Comments { get; } = [];

    /// <summary>Gets every endpoint operation mapped to its use case, in read order.</summary>
    public List<EndpointOperation> Endpoints { get; } = [];

    /// <summary>Gets every public aggregate method with the events it raises, in read order.</summary>
    public List<AggregateMethod> AggregateMethods { get; } = [];

    /// <summary>Gets every audit action literal a command message declares, in read order.</summary>
    public List<CodeAuditAction> AuditActions { get; } = [];

    /// <summary>Gets every top-level type declared in the Domain project, merged across partial parts.</summary>
    public List<DomainType> DomainTypes { get; } = [];

    /// <summary>Gets every Infrastructure class implementing a domain repository interface, sorted by file then line.</summary>
    public List<CodeRepositoryImplementation> RepositoryImplementations { get; } = [];
}
