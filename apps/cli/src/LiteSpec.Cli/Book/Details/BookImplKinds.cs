namespace LiteSpec.Cli.Book.Details;

/// <summary>
///     The closed set of implementation entry kinds the book writes in
///     <c>impl[].kind</c>. The writer adds <c>route</c> itself from the
///     screen's route entry; every other kind arrives on an implements link.
///     The client types them as one union.
/// </summary>
static class BookImplKinds
{
    /// <summary>Gets the <c>handler</c> implementation kind: a command or query handler.</summary>
    public const string Handler = "handler";

    /// <summary>Gets the <c>endpoint</c> implementation kind: an endpoint serving a use case.</summary>
    public const string Endpoint = "endpoint";

    /// <summary>Gets the <c>aggregate-root</c> implementation kind: an aggregate root class.</summary>
    public const string AggregateRoot = "aggregate-root";

    /// <summary>Gets the <c>aggregate-method</c> implementation kind: an aggregate state-changing method.</summary>
    public const string AggregateMethod = "aggregate-method";

    /// <summary>Gets the <c>automation</c> implementation kind: a frontend automation.</summary>
    public const string Automation = "automation";

    /// <summary>Gets the <c>reaction</c> implementation kind: an event handler.</summary>
    public const string Reaction = "reaction";

    /// <summary>Gets the <c>job</c> implementation kind: a job host.</summary>
    public const string Job = "job";

    /// <summary>Gets the <c>port</c> implementation kind: a port for an external system.</summary>
    public const string Port = "port";

    /// <summary>Gets the <c>adapter</c> implementation kind: an adapter implementing a port.</summary>
    public const string Adapter = "adapter";

    /// <summary>Gets the <c>event</c> implementation kind: a domain event class.</summary>
    public const string Event = "event";

    /// <summary>Gets the <c>rule-exception</c> implementation kind: a domain exception for a rejected rule.</summary>
    public const string RuleException = "rule-exception";

    /// <summary>Gets the <c>result</c> implementation kind: a result code literal.</summary>
    public const string Result = "result";

    /// <summary>Gets the <c>domain-service</c> implementation kind: a domain service holding a rule calculation.</summary>
    public const string DomainService = "domain-service";

    /// <summary>Gets the <c>route</c> implementation kind: a screen's route entry.</summary>
    public const string Route = "route";

    /// <summary>Every implementation kind the writer emits.</summary>
    public static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Handler, Endpoint, AggregateRoot, AggregateMethod, Automation, Reaction, Job,
        Port, Adapter, Event, RuleException, Result, DomainService, Route,
    };
}
