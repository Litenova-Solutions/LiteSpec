using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Rules;

/// <summary>
///     Every diagnostic the package reports. LS1xxx check Domain, LS2xxx the outer layers,
///     LS3xxx code style in the five production projects, LS4xxx test projects. Each one
///     names the provision it enforces, and its help link opens that provision.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class Descriptors
{
    private const string DomainCategory = "LiteSpec.Domain";
    private const string LayersCategory = "LiteSpec.Layers";
    private const string StyleCategory = "LiteSpec.Style";
    private const string TestsCategory = "LiteSpec.Tests";
    private const string TraceabilityCategory = "LiteSpec.Traceability";

    private static readonly Provision DomainOuterLayers = Provision.Of("backend/dotnet/domain", "keep-domain-free-of-outer-layer-concerns");
    private static readonly Provision ClosedSets = Provision.Of("backend/dotnet/domain", "model-every-closed-set-of-domain-values-without-enums");
    private static readonly Provision StateRecords = Provision.Of("backend/dotnet/domain", "model-every-aggregate-lifecycle-with-state-records");
    private static readonly Provision DomainExceptions = Provision.Of("backend/dotnet/domain", "reject-business-violations-with-domain-exceptions");
    private static readonly Provision DomainEvents = Provision.Of("backend/dotnet/domain", "raise-immutable-domain-facts");
    private static readonly Provision ExactQuantities = Provision.Of("backend/domain", "keep-binary-floating-point-out-of-exact-quantities");
    private static readonly Provision Nondeterminism = Provision.Of("backend/domain", "pass-nondeterministic-values-into-domain");
    private static readonly Provision Repositories = Provision.Of("backend/dotnet/domain", "keep-repository-interfaces-in-domain");
    private static readonly Provision DomainDocumentation = Provision.Of("backend/dotnet/domain", "document-every-public-domain-contract");
    private static readonly Provision TypedIdentifiers = Provision.Of("backend/dotnet/domain", "use-strongly-typed-version-7-identifiers");
    private static readonly Provision InternalImplementations = Provision.Of("backend/dotnet/architecture", "keep-implementation-types-internal");
    private static readonly Provision ApplicationProjects = Provision.Of("backend/dotnet/architecture", "use-five-application-projects");
    private static readonly Provision NoControllers = Provision.Of("backend/dotnet/api", "exclude-mvc-controllers-from-the-stack");
    private static readonly Provision RepositoryWrites = Provision.Of("backend/dotnet/persistence", "stage-aggregate-writes-through-repositories");
    private static readonly Provision AsyncNames = Provision.Of("backend/dotnet/naming", "name-asynchronous-methods-completely");
    private static readonly Provision FileNames = Provision.Of("backend/dotnet/naming", "match-c-files-and-primary-types");
    private static readonly Provision ImplementationStyle = Provision.Of("backend/dotnet/naming", "keep-implementation-style-consistent");
    private static readonly Provision ModuleFolders = Provision.Of("backend/domain", "organize-a-module-by-aggregate-and-concept");
    private static readonly Provision RoleSuffixes = Provision.Of("workspace/naming", "use-architectural-suffixes");
    private static readonly Provision EventHandlerFolders = Provision.Of("workspace/naming", "group-event-handlers-by-the-fact-they-handle");
    private static readonly Provision ResultParts = Provision.Of("workspace/naming", "name-result-parts-after-their-result");
    private static readonly Provision RepositoryVariables = Provision.Of("workspace/naming", "name-repository-variables-after-their-interface");
    private static readonly Provision QueryProjection = Provision.Of("backend/application", "project-queries-directly");
    private static readonly Provision TargetAuthorization = Provision.Of("backend/application", "enforce-target-authorization-in-the-pipeline");
    private static readonly Provision AuditValues = Provision.Of("backend/application", "declare-audit-values-beside-the-message");
    private static readonly Provision OutcomePath = Provision.Of("backend/application", "implement-one-outcome-path-per-command");
    private static readonly Provision PerRecordWork = Provision.Of("backend/application", "issue-per-record-work-from-a-job");
    private static readonly Provision UniquenessOutOfHandlers = Provision.Of("backend/persistence", "keep-uniqueness-out-of-handlers");
    private static readonly Provision ScenarioProof = Provision.Of("backend/dotnet/testing", "prove-each-scenario-with-a-test-that-calls-its-use-case");
    private static readonly Provision PointToRecords = Provision.Of("core/system", "point-code-and-tests-up-to-records");
    private static readonly Provision DeclaredStyle = Provision.Of("backend/dotnet/stack", "write-each-mark-in-the-declared-style");
    private static readonly Provision RuntimeValues = Provision.Of("backend/dotnet/stack", "generate-runtime-values-from-the-records");
    private static readonly Provision PartialTypes = Provision.Of("backend/dotnet/stack", "declare-marked-events-and-exceptions-as-partial-types");
    private static readonly Provision MarkedAct = Provision.Of("backend/domain", "raise-the-declared-events-from-the-marked-act");
    private static readonly Provision PermissionScope = Provision.Of("core/access", "state-the-scope-of-every-permission");

    public static readonly DiagnosticDescriptor LS1001 = Create("LS1001", DomainCategory, DomainOuterLayers,
        "Domain references an outer-layer concern",
        "Domain uses '{0}' from '{1}', an outer-layer concern",
        "Domain references only the .NET base class library and project-owned Domain types: no persistence, web, messaging, mediator, logging, configuration, dependency injection or serialization.");

    public static readonly DiagnosticDescriptor LS1002 = Create("LS1002", DomainCategory, ClosedSets,
        "Domain declares an enum",
        "Enum '{0}' models a closed set; use an abstract record with sealed cases and a stable Code",
        "A closed set of domain values is an abstract record base with one sealed record per case, exposing a stable Code and converting labels through FromCode.");

    public static readonly DiagnosticDescriptor LS1003 = Create("LS1003", DomainCategory, StateRecords,
        "Aggregate has no State record",
        "Aggregate '{0}' exposes no State property typed as an abstract record",
        "An aggregate exposes its lifecycle only through one State property backed by an abstract {Aggregate}State record.",
        compilationEnd: true);

    public static readonly DiagnosticDescriptor LS1004 = Create("LS1004", DomainCategory, StateRecords,
        "State case is not sealed",
        "State case '{0}' derives from '{1}' but is not sealed",
        "Each per-state record is sealed, so the set of states is closed and switch expressions are exhaustive.",
        compilationEnd: true);

    public static readonly DiagnosticDescriptor LS1005 = Create("LS1005", DomainCategory, DomainExceptions,
        "Domain throws a non-Domain exception",
        "Domain throws '{0}'; reject a business violation with a DomainException subclass",
        "A rejected business rule throws a DomainException subclass named {DomainType}{Reason}Exception with a stable code. Argument guards may throw ArgumentException and its subclasses.");

    public static readonly DiagnosticDescriptor LS1006 = Create("LS1006", DomainCategory, DomainExceptions,
        "Domain exception is misnamed or open",
        "Domain exception '{0}' must be sealed and end in 'Exception'",
        "A DomainException subclass is sealed and named {DomainType}{Reason}Exception.");

    public static readonly DiagnosticDescriptor LS1007 = Create("LS1007", DomainCategory, DomainEvents,
        "Domain event is not an immutable record",
        "Domain event '{0}' must be a record named '{{Aggregate}}{{PastFact}}Event' with no settable members",
        "A domain event is a public immutable record named {Aggregate}{PastFact}Event implementing IDomainEvent and carrying business data only.");

    public static readonly DiagnosticDescriptor LS1008 = Create("LS1008", DomainCategory, ExactQuantities,
        "Domain uses binary floating point",
        "'{0}' is typed '{1}'; use decimal or a value object for exact quantities",
        "A Domain type keeps double and float out of monetary amounts, rates applied to them, and quantities compared for exact equality.");

    public static readonly DiagnosticDescriptor LS1009 = Create("LS1009", DomainCategory, Nondeterminism,
        "Domain reads a nondeterministic value",
        "Domain reads '{0}'; pass time, randomness and new identifiers in as arguments",
        "Domain behavior receives time, randomness and new identifiers as arguments and reads no clock.");

    public static readonly DiagnosticDescriptor LS1010 = Create("LS1010", DomainCategory, Repositories,
        "Repository exposes a query surface",
        "Repository '{0}' exposes '{1}'; return aggregates and Domain types only",
        "A repository interface exposes only aggregate and Domain types for one aggregate root, without IQueryable, a session, or a generic CRUD surface.");

    public static readonly DiagnosticDescriptor LS1011 = Create("LS1011", DomainCategory, DomainDocumentation,
        "Public Domain contract is undocumented",
        "Public Domain member '{0}' has no XML documentation",
        "Every public Domain type and member carries XML documentation stating the business constraint, result, or failure it represents.");

    public static readonly DiagnosticDescriptor LS1012 = Create("LS1012", DomainCategory, TypedIdentifiers,
        "Identifier is not a strongly typed value",
        "Identifier '{0}' must be a readonly record struct wrapping one Guid with no implicit conversion",
        "An aggregate identity is a readonly record struct wrapping one version 7 Guid, with no implicit conversion, primitive alias, or public parameterless creation path.");

    public static readonly DiagnosticDescriptor LS1013 = Create("LS1013", DomainCategory, ModuleFolders,
        "Domain event sits outside the Events folder",
        "Domain event '{0}' lives in '{1}', outside the Events folder the model reads",
        "A domain event is declared in a file under its aggregate's Events folder, where the model builder discovers it.");

    public static readonly DiagnosticDescriptor LS2001 = Create("LS2001", LayersCategory, InternalImplementations,
        "Implementation type is not internal sealed",
        "'{0}' is an implementation type and must be declared internal sealed",
        "A handler, validator, definition, persistence implementation, or endpoint implementation is declared internal sealed.");

    public static readonly DiagnosticDescriptor LS2003 = Create("LS2003", LayersCategory, NoControllers,
        "WebApi uses an MVC controller",
        "'{0}' derives from '{1}'; map one IEndpoint per operation instead",
        "A WebApi project never routes requests through MVC controllers or ControllerBase.");

    public static readonly DiagnosticDescriptor LS2004 = Create("LS2004", LayersCategory, RepositoryWrites,
        "Command handler writes through a document session",
        "Command handler '{0}' uses '{1}'; stage aggregate writes through a Domain repository",
        "A command handler reaches persistence only through a Domain repository, never through IDocumentSession.");

    public static readonly DiagnosticDescriptor LS2005 = Create("LS2005", LayersCategory, RoleSuffixes,
        "Handler name lacks its command or query suffix",
        "'{0}' implements '{1}' and must end in '{2}'",
        "A command handler is named {UseCase}CommandHandler and a query handler {UseCase}QueryHandler, so the model builder discovers each handler from its file name.");

    public static readonly DiagnosticDescriptor LS2006 = Create("LS2006", LayersCategory, RoleSuffixes,
        "Endpoint name lacks its suffix",
        "'{0}' implements 'IEndpoint' and must end in 'Endpoint'",
        "An endpoint is named {UseCase}Endpoint, so the model builder discovers each endpoint from its file name.");

    public static readonly DiagnosticDescriptor LS2007 = Create("LS2007", LayersCategory, RoleSuffixes,
        "Event handler or job name hides its kind",
        "'{0}' is a {1} and must be named '{2}'",
        "An event handler is named {Action}On{Fact}Handler, and a job *Job, *Worker, or *Recurring*, so the model builder discovers each event handler and job by name.");

    public static readonly DiagnosticDescriptor LS2008 = Create("LS2008", LayersCategory, EventHandlerFolders,
        "Event handler sits outside its fact folder",
        "'{0}' handles '{1}' and must live in the 'On{2}' folder",
        "An event handler lives in the On{Fact} folder inside its subscriber's aggregate folder, where {Fact} is the handled event name without its Event suffix. A handler of two facts reports nothing.");

    public static readonly DiagnosticDescriptor LS2009 = Create("LS2009", LayersCategory, ResultParts,
        "Result part not named after its result",
        "'{0}' belongs to '{1}' and must start with '{1}'",
        "A type declared in the same namespace as a {UseCase}QueryResult or {UseCase}CommandResult type is named after that result, as {UseCase}QueryResult{Part} or {UseCase}CommandResult{Part}. The message, handler, validator, definition, and result types keep their own names.");

    public static readonly DiagnosticDescriptor LS2010 = Create("LS2010", LayersCategory, RepositoryVariables,
        "Repository variable not named after its interface",
        "'{0}' is typed '{1}' and must be named '{2}'",
        "A repository parameter, field, or local is named after its interface without the I prefix, ending in Repository, such as loanRepository for ILoanRepository. A field may carry a leading underscore.");

    public static readonly DiagnosticDescriptor LS2011 = Create("LS2011", LayersCategory, QueryProjection,
        "Query handler takes a repository",
        "Query handler '{0}' takes repository '{1}'; project through the read session instead",
        "A query handler projects directly through the read session and takes no domain repository.");

    public static readonly DiagnosticDescriptor LS2012 = Create("LS2012", LayersCategory, TargetAuthorization,
        "Handler authorizes itself",
        "Handler '{0}' throws '{1}'; the authorization guard authorizes every message",
        "A command or query handler never throws the forbidden refusal itself; the authorization guard authorizes every message from the access its record declares.");

    public static readonly DiagnosticDescriptor LS2013 = Create("LS2013", LayersCategory, AuditValues,
        "Audit value read from the message in the handler",
        "'{0}' reads only the message; declare the audit value beside the message",
        "A command handler adds only the audit values it learns while running; a value read from the message is declared beside the message, so the pipeline records it on every outcome.");

    public static readonly DiagnosticDescriptor LS2014 = Create("LS2014", LayersCategory, OutcomePath,
        "Command handler finds or creates",
        "Command handler '{0}' finds and creates '{1}'; run a query, then send the command that fits",
        "Each command implements one use case with one success path; a flow that either finds or creates a record is two use cases that the boundary owning the flow chooses between.");

    public static readonly DiagnosticDescriptor LS2015 = Create("LS2015", LayersCategory, PerRecordWork,
        "Command handler changes records in a loop",
        "Command handler '{0}' calls '{1}' in a loop; a job issues one command per record",
        "A command handler never changes records in a loop; work over many records is a job that issues one command per record, each in its own unit of work.");

    public static readonly DiagnosticDescriptor LS2016 = Create("LS2016", LayersCategory, UniquenessOutOfHandlers,
        "Handler takes a pipeline-owned collaborator",
        "Handler '{0}' takes '{1}', which the pipeline owns",
        "A command or query handler takes no pipeline-owned collaborator through its constructor; the unit of work, idempotency store, outbox, and uniqueness claim service belong to the pipeline.");

    public static readonly DiagnosticDescriptor LS2017 = Create("LS2017", LayersCategory, ApplicationProjects,
        "LiteSpecLayer names no known layer",
        "'{0}' names no known layer; use Domain, Application.Abstractions, Application, Infrastructure, WebApi, Worker, Test, or None",
        "The LiteSpecLayer property names one of the known application projects, Test, or None to opt out.");

    public static readonly DiagnosticDescriptor LS3001 = Create("LS3001", StyleCategory, AsyncNames,
        "Asynchronous method name lacks Async",
        "Method '{0}' returns '{1}' and must end in 'Async'",
        "A method returning Task or ValueTask ends in Async.");

    public static readonly DiagnosticDescriptor LS3002 = Create("LS3002", StyleCategory, AsyncNames,
        "CancellationToken is not the last parameter",
        "Method '{0}' must take 'CancellationToken cancellationToken' as its last parameter",
        "A cancelable asynchronous method takes CancellationToken cancellationToken last.");

    public static readonly DiagnosticDescriptor LS3003 = Create("LS3003", StyleCategory, FileNames,
        "File does not match its primary type",
        "File '{0}' must hold one primary top-level type named '{0}'",
        "A C# file contains one primary top-level type and takes that type's exact name.");

    public static readonly DiagnosticDescriptor LS3004 = Create("LS3004", StyleCategory, ImplementationStyle,
        "Concrete class is not sealed",
        "Class '{0}' has no derived type and must be sealed",
        "Production C# seals every concrete class that nothing derives from.",
        compilationEnd: true);

    public static readonly DiagnosticDescriptor LS4001 = Create("LS4001", TestsCategory, ScenarioProof,
        "Test contains no assertion",
        "Test '{0}' contains no assertion",
        "A test calls the behavior it proves and asserts the result, so a passing test proves something.");

    public static readonly DiagnosticDescriptor LS5001 = Create("LS5001", TraceabilityCategory, PointToRecords,
        "Code element carries no mark",
        "'{0}' carries no mark naming the {1} record it serves",
        "A code element of a recognized kind carries a mark naming the record it serves, in the style the project declares.");

    public static readonly DiagnosticDescriptor LS5002 = Create("LS5002", TraceabilityCategory, PointToRecords,
        "Mark names no usable record",
        "'{0}' marks '{1}', which {2}",
        "A mark names a record of a type its code element takes, spelled as the record states it.");

    public static readonly DiagnosticDescriptor LS5003 = Create("LS5003", TraceabilityCategory, PointToRecords,
        "Code element carries the wrong number of marks",
        "'{0}' carries {1} marks but takes exactly one",
        "A code element carries exactly the marks its kind takes: one mark for most kinds, one or more for ports, adapters and tests.");

    public static readonly DiagnosticDescriptor LS5004 = Create("LS5004", TraceabilityCategory, DeclaredStyle,
        "Mark uses the undeclared style",
        "Mark on '{0}' naming '{1}' uses the {2} style, but the project declares {3}",
        "Every mark is written in the one style the project declares: a documentation tag for xml, an attribute for attribute.");

    public static readonly DiagnosticDescriptor LS5005 = Create("LS5005", TraceabilityCategory, RuntimeValues,
        "Runtime value differs from its record",
        "'{0}' declares '{1}' as '{2}', but its record states '{3}'",
        "A domain event declares Classification only with its event identifier, and a rule exception declares Code only with its rule's refusal code; otherwise the generator writes the value from the record.");

    public static readonly DiagnosticDescriptor LS5006 = Create("LS5006", TraceabilityCategory, PartialTypes,
        "Marked event or exception is not partial",
        "'{0}' carries a mark and must be declared partial",
        "A marked domain event or rule exception is declared partial, so the generator can add the runtime value as a second part.");

    public static readonly DiagnosticDescriptor LS5007 = Create("LS5007", TraceabilityCategory, MarkedAct,
        "Marked act raises an undeclared event",
        "'{0}' marked with '{1}' raises '{2}', which the use case does not list in 'raises'",
        "An aggregate method marked with a use case raises only the events the use case lists in raises.");

    public static readonly DiagnosticDescriptor LS5008 = Create("LS5008", TraceabilityCategory, MarkedAct,
        "Declared event is raised by no marked act",
        "'{0}' marked with '{1}' raises no '{2}', which the use case lists in 'raises'",
        "Every event a use case lists in raises is raised by a root method marked with the use case.");

    public static readonly DiagnosticDescriptor LS5010 = Create("LS5010", TraceabilityCategory, PermissionScope,
        "Permission opens with an undeclared verb",
        "Permission '{0}' starts with '{1}', which the use-case vocabulary does not declare",
        "A permission opens with a verb from the use-case vocabulary, so whether it only reads follows from the verb's kind; a project verb is declared in the vocabulary of litespec.project.json.");

    public static readonly DiagnosticDescriptor LS5011 = Create("LS5011", TraceabilityCategory, PointToRecords,
        "Message maps to two use cases",
        "Message '{0}' is handled by both '{1}' and '{2}' with different use cases",
        "One message maps to one use case, so the pipeline reads the permission and the audit identity from the message; a message two handlers serve with different use cases maps to neither.");

    private static DiagnosticDescriptor Create(
        string id, string category, Provision provision, string title, string message, string description, bool compilationEnd = false) =>
        new(
            id,
            title,
            message,
            category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{description} Provision: {provision.Id}",
            helpLinkUri: provision.HelpLink,
            customTags: compilationEnd ? [provision.Id, WellKnownDiagnosticTags.CompilationEnd] : [provision.Id]);
}
