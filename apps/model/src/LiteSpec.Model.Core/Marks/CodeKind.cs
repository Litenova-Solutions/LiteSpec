namespace LiteSpec.Model;

/// <summary>
///     The kind of a code element that carries a mark: what the element is,
///     recognized from the code itself, never from the mark.
/// </summary>
public enum CodeKind
{
    /// <summary>An aggregate root class, marked with one <c>aggregate/</c> record.</summary>
    AggregateRoot = 0,

    /// <summary>
    ///     A public aggregate root method that changes state, marked with one or
    ///     more <c>use-case/</c> records.
    /// </summary>
    AggregateMethod = 1,

    /// <summary>A domain event, marked with one <c>event/</c> record.</summary>
    DomainEvent = 2,

    /// <summary>
    ///     A domain exception for one rejected rule, marked with one <c>rule/</c> record.
    /// </summary>
    RuleException = 3,

    /// <summary>
    ///     A command or query handler, marked with one <c>use-case/</c> record.
    /// </summary>
    Handler = 4,

    /// <summary>An event handler, marked with one <c>reaction/</c> record.</summary>
    EventHandler = 5,

    /// <summary>A job host, marked with one <c>job/</c> record.</summary>
    Job = 6,

    /// <summary>
    ///     A port for an external system, marked with one or more
    ///     <c>external-system/</c> records.
    /// </summary>
    Port = 7,

    /// <summary>
    ///     An adapter implementing a port, marked with one or more
    ///     <c>external-system/</c> records.
    /// </summary>
    Adapter = 8,

    /// <summary>An endpoint, marked with one <c>use-case/</c> record.</summary>
    Endpoint = 9,

    /// <summary>
    ///     A frontend use-case module folder, marked with one <c>use-case/</c> record.
    /// </summary>
    FrontendModule = 10,

    /// <summary>
    ///     A test method, marked with one or more <c>scenario/</c> or <c>rule/</c> records.
    /// </summary>
    Test = 11,

    /// <summary>A domain service holding one rule calculation, recognized by its <c>rule/</c> mark.</summary>
    DomainService = 12,
}
