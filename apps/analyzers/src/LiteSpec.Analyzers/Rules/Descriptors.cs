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
    /// <summary>The diagnostic category name for domain diagnostics.</summary>
    private const string DomainCategory = "LiteSpec.Domain";
    /// <summary>The diagnostic category name for layers diagnostics.</summary>
    private const string LayersCategory = "LiteSpec.Layers";
    /// <summary>The diagnostic category name for style diagnostics.</summary>
    private const string StyleCategory = "LiteSpec.Style";
    /// <summary>The diagnostic category name for tests diagnostics.</summary>
    private const string TestsCategory = "LiteSpec.Tests";
    /// <summary>The diagnostic category name for traceability diagnostics.</summary>
    private const string TraceabilityCategory = "LiteSpec.Traceability";

    /// <summary>The provision the related descriptors enforce: keep domain free of outer layer concerns (litespec/backend/dotnet/domain#keep-domain-free-of-outer-layer-concerns).</summary>
    private static readonly Provision DomainOuterLayers = Provision.Of("backend/dotnet/domain", "keep-domain-free-of-outer-layer-concerns");
    /// <summary>The provision the related descriptors enforce: model every closed set of domain values without enums (litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums).</summary>
    private static readonly Provision ClosedSets = Provision.Of("backend/dotnet/domain", "model-every-closed-set-of-domain-values-without-enums");
    /// <summary>The provision the related descriptors enforce: model every aggregate lifecycle with state records (litespec/backend/dotnet/domain#model-every-aggregate-lifecycle-with-state-records).</summary>
    private static readonly Provision StateRecords = Provision.Of("backend/dotnet/domain", "model-every-aggregate-lifecycle-with-state-records");
    /// <summary>The provision the related descriptors enforce: reject business violations with domain exceptions (litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions).</summary>
    private static readonly Provision DomainExceptions = Provision.Of("backend/dotnet/domain", "reject-business-violations-with-domain-exceptions");
    /// <summary>The provision the related descriptors enforce: raise immutable domain facts (litespec/backend/dotnet/domain#raise-immutable-domain-facts).</summary>
    private static readonly Provision DomainEvents = Provision.Of("backend/dotnet/domain", "raise-immutable-domain-facts");
    /// <summary>The provision the related descriptors enforce: keep binary floating point out of exact quantities (litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities).</summary>
    private static readonly Provision ExactQuantities = Provision.Of("backend/domain", "keep-binary-floating-point-out-of-exact-quantities");
    /// <summary>The provision the related descriptors enforce: pass nondeterministic values into domain (litespec/backend/domain#pass-nondeterministic-values-into-domain).</summary>
    private static readonly Provision Nondeterminism = Provision.Of("backend/domain", "pass-nondeterministic-values-into-domain");
    /// <summary>The provision the related descriptors enforce: keep repository interfaces in domain (litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain).</summary>
    private static readonly Provision Repositories = Provision.Of("backend/dotnet/domain", "keep-repository-interfaces-in-domain");
    /// <summary>The provision the related descriptors enforce: document every public domain contract (litespec/backend/dotnet/domain#document-every-public-domain-contract).</summary>
    private static readonly Provision DomainDocumentation = Provision.Of("backend/dotnet/domain", "document-every-public-domain-contract");
    /// <summary>The provision the related descriptors enforce: use strongly typed version 7 identifiers (litespec/backend/dotnet/domain#use-strongly-typed-version-7-identifiers).</summary>
    private static readonly Provision TypedIdentifiers = Provision.Of("backend/dotnet/domain", "use-strongly-typed-version-7-identifiers");
    /// <summary>The provision the related descriptors enforce: keep implementation types internal (litespec/backend/dotnet/architecture#keep-implementation-types-internal).</summary>
    private static readonly Provision InternalImplementations = Provision.Of("backend/dotnet/architecture", "keep-implementation-types-internal");
    /// <summary>The provision the related descriptors enforce: use five application projects (litespec/backend/dotnet/ext/layer-projects#use-five-application-projects).</summary>
    private static readonly Provision ApplicationProjects = Provision.Of("backend/dotnet/ext/layer-projects", "use-five-application-projects");
    /// <summary>The provision the related descriptors enforce: respect layer direction in every file (litespec/backend/dotnet/architecture#respect-layer-direction-in-every-file).</summary>
    private static readonly Provision RespectLayerDirection = Provision.Of("backend/dotnet/architecture", "respect-layer-direction-in-every-file");
    /// <summary>The provision the related descriptors enforce: keep use case folders isolated (litespec/backend/dotnet/architecture#keep-use-case-folders-isolated).</summary>
    private static readonly Provision UseCaseIsolation = Provision.Of("backend/dotnet/architecture", "keep-use-case-folders-isolated");
    /// <summary>The provision the related descriptors enforce: reach another module only through contracts (litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts).</summary>
    private static readonly Provision ModuleContracts = Provision.Of("backend/dotnet/architecture", "reach-another-module-only-through-contracts");
    /// <summary>The provision the related descriptors enforce: exclude mvc controllers from the stack (litespec/backend/dotnet/api#exclude-mvc-controllers-from-the-stack).</summary>
    private static readonly Provision NoControllers = Provision.Of("backend/dotnet/api", "exclude-mvc-controllers-from-the-stack");
    /// <summary>The provision the related descriptors enforce: stage aggregate writes through repositories (litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories).</summary>
    private static readonly Provision RepositoryWrites = Provision.Of("backend/dotnet/persistence", "stage-aggregate-writes-through-repositories");
    /// <summary>The provision the related descriptors enforce: name asynchronous operations completely (litespec/workspace/naming#name-asynchronous-operations-completely).</summary>
    private static readonly Provision AsyncNames = Provision.Of("workspace/naming", "name-asynchronous-operations-completely");
    /// <summary>The provision the related descriptors enforce: keep implementation style consistent (litespec/backend/dotnet/naming#keep-implementation-style-consistent).</summary>
    private static readonly Provision ImplementationStyle = Provision.Of("backend/dotnet/naming", "keep-implementation-style-consistent");
    /// <summary>The provision the related descriptors enforce: use architectural suffixes (litespec/workspace/naming#use-architectural-suffixes).</summary>
    private static readonly Provision RoleSuffixes = Provision.Of("workspace/naming", "use-architectural-suffixes");
    /// <summary>The provision the related descriptors enforce: name result parts after their result (litespec/workspace/naming#name-result-parts-after-their-result).</summary>
    private static readonly Provision ResultParts = Provision.Of("workspace/naming", "name-result-parts-after-their-result");
    /// <summary>The provision the related descriptors enforce: name repository variables after their interface (litespec/workspace/naming#name-repository-variables-after-their-interface).</summary>
    private static readonly Provision RepositoryVariables = Provision.Of("workspace/naming", "name-repository-variables-after-their-interface");
    /// <summary>The provision the related descriptors enforce: project queries directly (litespec/backend/application#project-queries-directly).</summary>
    private static readonly Provision QueryProjection = Provision.Of("backend/application", "project-queries-directly");
    /// <summary>The provision the related descriptors enforce: enforce target authorization in the pipeline (litespec/backend/application#enforce-target-authorization-in-the-pipeline).</summary>
    private static readonly Provision TargetAuthorization = Provision.Of("backend/application", "enforce-target-authorization-in-the-pipeline");
    /// <summary>The provision the related descriptors enforce: declare audit values beside the message (litespec/backend/application#declare-audit-values-beside-the-message).</summary>
    private static readonly Provision AuditValues = Provision.Of("backend/application", "declare-audit-values-beside-the-message");
    /// <summary>The provision the related descriptors enforce: implement one outcome path per command (litespec/backend/application#implement-one-outcome-path-per-command).</summary>
    private static readonly Provision OutcomePath = Provision.Of("backend/application", "implement-one-outcome-path-per-command");
    /// <summary>The provision the related descriptors enforce: issue per record work from a job (litespec/backend/application#issue-per-record-work-from-a-job).</summary>
    private static readonly Provision PerRecordWork = Provision.Of("backend/application", "issue-per-record-work-from-a-job");
    /// <summary>The provision the related descriptors enforce: keep uniqueness out of handlers (litespec/backend/persistence#keep-uniqueness-out-of-handlers).</summary>
    private static readonly Provision UniquenessOutOfHandlers = Provision.Of("backend/persistence", "keep-uniqueness-out-of-handlers");
    /// <summary>The provision the related descriptors enforce: mark the test that proves each scenario (litespec/quality/testing#prove-each-scenario-through-the-public-boundary).</summary>
    private static readonly Provision ScenarioProof = Provision.Of("quality/testing", "prove-each-scenario-through-the-public-boundary");
    /// <summary>The provision the related descriptors enforce: point code and tests up to records (litespec/core/system#point-code-and-tests-up-to-records).</summary>
    private static readonly Provision PointToRecords = Provision.Of("core/system", "point-code-and-tests-up-to-records");
    /// <summary>The provision the related descriptors enforce: write each mark in the declared style (litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style).</summary>
    private static readonly Provision DeclaredStyle = Provision.Of("backend/dotnet/stack", "write-each-mark-in-the-declared-style");
    /// <summary>The provision the related descriptors enforce: generate runtime values from the records (litespec/backend/dotnet/stack#generate-runtime-values-from-the-records).</summary>
    private static readonly Provision RuntimeValues = Provision.Of("backend/dotnet/stack", "generate-runtime-values-from-the-records");
    /// <summary>The provision the related descriptors enforce: declare marked events and exceptions as partial types (litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types).</summary>
    private static readonly Provision PartialTypes = Provision.Of("backend/dotnet/stack", "declare-marked-events-and-exceptions-as-partial-types");
    /// <summary>The provision the related descriptors enforce: raise the declared events from the marked act (litespec/backend/domain#raise-the-declared-events-from-the-marked-act).</summary>
    private static readonly Provision MarkedAct = Provision.Of("backend/domain", "raise-the-declared-events-from-the-marked-act");
    /// <summary>The LS1001 descriptor: Domain references an outer-layer concern.</summary>
    public static readonly DiagnosticDescriptor LS1001 = Create("LS1001", DomainCategory, DomainOuterLayers,
        "Domain references an outer-layer concern",
        "Domain uses '{0}' from '{1}', an outer-layer concern",
        "Domain references only the .NET base class library and project-owned Domain types: no persistence, web, messaging, mediator, logging, configuration, dependency injection, serialization, or Application, Infrastructure or WebApi type.");

    /// <summary>The LS1002 descriptor: Domain declares an enum.</summary>
    public static readonly DiagnosticDescriptor LS1002 = Create("LS1002", DomainCategory, ClosedSets,
        "Domain declares an enum",
        "Enum '{0}' models a closed set; use an abstract record with sealed cases and a stable Code",
        "A closed set of domain values is an abstract record base with one sealed record per case, exposing a stable Code and converting labels through FromCode.");

    /// <summary>The LS1003 descriptor: Aggregate has no State record.</summary>
    public static readonly DiagnosticDescriptor LS1003 = Create("LS1003", DomainCategory, StateRecords,
        "Aggregate has no State record",
        "Aggregate '{0}' exposes no State property typed as an abstract record",
        "An aggregate exposes its lifecycle only through one State property backed by an abstract {Aggregate}State record.",
        compilationEnd: true);

    /// <summary>The LS1004 descriptor: State case is not sealed.</summary>
    public static readonly DiagnosticDescriptor LS1004 = Create("LS1004", DomainCategory, StateRecords,
        "State case is not sealed",
        "State case '{0}' derives from '{1}' but is not sealed",
        "Each per-state record is sealed, so the set of states is closed; a switch over the states keeps a discard arm because the compiler does not check that it covers every sealed case.",
        compilationEnd: true);

    /// <summary>The LS1005 descriptor: Domain throws a non-Domain exception.</summary>
    public static readonly DiagnosticDescriptor LS1005 = Create("LS1005", DomainCategory, DomainExceptions,
        "Domain throws a non-Domain exception",
        "Domain throws '{0}'; reject a business violation with a DomainException subclass",
        "A rejected business rule throws a DomainException subclass named {DomainType}{Reason}Exception with a stable code. Argument guards may throw ArgumentException and its subclasses.");

    /// <summary>The LS1006 descriptor: Domain exception is misnamed or open.</summary>
    public static readonly DiagnosticDescriptor LS1006 = Create("LS1006", DomainCategory, DomainExceptions,
        "Domain exception is misnamed or open",
        "Domain exception '{0}' must be sealed and end in 'Exception'",
        "A DomainException subclass is sealed and named {DomainType}{Reason}Exception.");

    /// <summary>The LS1007 descriptor: Domain event is not an immutable record.</summary>
    public static readonly DiagnosticDescriptor LS1007 = Create("LS1007", DomainCategory, DomainEvents,
        "Domain event is not an immutable record",
        "Domain event '{0}' must be a record named '{{Aggregate}}{{PastFact}}Event' with no settable members",
        "A domain event is a public immutable record named {Aggregate}{PastFact}Event implementing IDomainEvent and carrying business data only.");

    /// <summary>The LS1008 descriptor: Domain uses binary floating point.</summary>
    public static readonly DiagnosticDescriptor LS1008 = Create("LS1008", DomainCategory, ExactQuantities,
        "Domain uses binary floating point",
        "'{0}' is typed '{1}'; use decimal or a value object for exact quantities",
        "A Domain type keeps double and float out of monetary amounts, rates applied to them, and quantities compared for exact equality.");

    /// <summary>The LS1009 descriptor: Domain reads a nondeterministic value.</summary>
    public static readonly DiagnosticDescriptor LS1009 = Create("LS1009", DomainCategory, Nondeterminism,
        "Domain reads a nondeterministic value",
        "Domain reads '{0}'; pass time, randomness and new identifiers in as arguments",
        "Domain behavior receives time, randomness and new identifiers as arguments and reads no clock.");

    /// <summary>The LS1010 descriptor: Repository exposes a query surface.</summary>
    public static readonly DiagnosticDescriptor LS1010 = Create("LS1010", DomainCategory, Repositories,
        "Repository exposes a query surface",
        "Repository '{0}' exposes '{1}'; return aggregates and Domain types only",
        "A repository interface exposes only aggregate and Domain types for one aggregate root, without IQueryable, a session, or a generic CRUD surface.");

    /// <summary>The LS1011 descriptor: Public Domain contract is undocumented.</summary>
    public static readonly DiagnosticDescriptor LS1011 = Create("LS1011", DomainCategory, DomainDocumentation,
        "Public Domain contract is undocumented",
        "Public Domain member '{0}' has no XML documentation",
        "Every public Domain type and member carries XML documentation stating the business constraint, result, or failure it represents.");

    /// <summary>The LS1012 descriptor: Identifier is not a strongly typed value.</summary>
    public static readonly DiagnosticDescriptor LS1012 = Create("LS1012", DomainCategory, TypedIdentifiers,
        "Identifier is not a strongly typed value",
        "Identifier '{0}' must be a readonly record struct wrapping one Guid with no implicit conversion",
        "An aggregate identity is a readonly record struct wrapping one version 7 Guid, with no implicit conversion, primitive alias, or public parameterless creation path.");

    /// <summary>The LS1013 descriptor: Switch leaves a sealed case to its discard arm.</summary>
    public static readonly DiagnosticDescriptor LS1013 = Create("LS1013", DomainCategory, StateRecords,
        "Switch leaves a sealed case to its discard arm",
        "Switch on '{0}' leaves '{1}' to its discard arm; handle every sealed case explicitly",
        "A switch over an abstract state or closed-set record gives every sealed case its own arm and keeps the discard arm for values outside the sealed set. The compiler does not check the switch, so a case left to the discard arm reaches it at run time.");

    /// <summary>The LS2001 descriptor: Implementation type is not internal sealed.</summary>
    public static readonly DiagnosticDescriptor LS2001 = Create("LS2001", LayersCategory, InternalImplementations,
        "Implementation type is not internal sealed",
        "'{0}' is an implementation type and must be declared internal sealed",
        "A handler, validator, definition, persistence implementation, or endpoint implementation is declared internal sealed.");

    /// <summary>The LS2003 descriptor: WebApi uses an MVC controller.</summary>
    public static readonly DiagnosticDescriptor LS2003 = Create("LS2003", LayersCategory, NoControllers,
        "WebApi uses an MVC controller",
        "'{0}' derives from '{1}'; map one IEndpoint per operation instead",
        "A WebApi project never routes requests through MVC controllers or ControllerBase.");

    /// <summary>The LS2004 descriptor: Command handler writes through a document session.</summary>
    public static readonly DiagnosticDescriptor LS2004 = Create("LS2004", LayersCategory, RepositoryWrites,
        "Command handler writes through a document session",
        "Command handler '{0}' uses '{1}'; stage aggregate writes through a Domain repository",
        "A command handler reaches persistence only through a Domain repository, never through IDocumentSession.");

    /// <summary>The LS2005 descriptor: Handler name lacks its command or query suffix.</summary>
    public static readonly DiagnosticDescriptor LS2005 = Create("LS2005", LayersCategory, RoleSuffixes,
        "Handler name lacks its command or query suffix",
        "'{0}' implements '{1}' and must end in '{2}'",
        "A command handler is named {UseCase}CommandHandler and a query handler {UseCase}QueryHandler, so the model builder discovers each handler from its file name.");

    /// <summary>The LS2006 descriptor: Endpoint name lacks its suffix.</summary>
    public static readonly DiagnosticDescriptor LS2006 = Create("LS2006", LayersCategory, RoleSuffixes,
        "Endpoint name lacks its suffix",
        "'{0}' implements 'IEndpoint' and must end in 'Endpoint'",
        "An endpoint is named {UseCase}Endpoint, so the model builder discovers each endpoint from its file name.");

    /// <summary>The LS2007 descriptor: Reaction or job name hides its kind.</summary>
    public static readonly DiagnosticDescriptor LS2007 = Create("LS2007", LayersCategory, RoleSuffixes,
        "Reaction or job name hides its kind",
        "'{0}' is a {1} and must be named '{2}'",
        "A reaction is named {Action}On{Fact}Handler, and a job *Job, *Worker, or *Recurring*, so the model builder discovers each reaction or job by name.");

    /// <summary>The LS2009 descriptor: Result part not named after its result.</summary>
    public static readonly DiagnosticDescriptor LS2009 = Create("LS2009", LayersCategory, ResultParts,
        "Result part not named after its result",
        "'{0}' belongs to '{1}' and must start with '{1}'",
        "A type declared in the same namespace as a {UseCase}QueryResult or {UseCase}CommandResult type is named after that result, as {UseCase}QueryResult{Part} or {UseCase}CommandResult{Part}. The message, handler, validator, definition, and result types keep their own names.");

    /// <summary>The LS2010 descriptor: Repository variable not named after its interface.</summary>
    public static readonly DiagnosticDescriptor LS2010 = Create("LS2010", LayersCategory, RepositoryVariables,
        "Repository variable not named after its interface",
        "'{0}' is typed '{1}' and must be named '{2}'",
        "A repository parameter, field, or local is named after its interface without the I prefix, ending in Repository, such as loanRepository for ILoanRepository. A field may carry a leading underscore.");

    /// <summary>The LS2011 descriptor: Query handler takes a repository.</summary>
    public static readonly DiagnosticDescriptor LS2011 = Create("LS2011", LayersCategory, QueryProjection,
        "Query handler takes a repository",
        "Query handler '{0}' takes repository '{1}'; project through the read session instead",
        "A query handler projects directly through the read session and takes no domain repository.");

    /// <summary>The LS2012 descriptor: Handler authorizes itself.</summary>
    public static readonly DiagnosticDescriptor LS2012 = Create("LS2012", LayersCategory, TargetAuthorization,
        "Handler authorizes itself",
        "Handler '{0}' authorizes itself with '{1}'; the authorization guard authorizes every message",
        "A command or query handler never throws the forbidden refusal itself and never takes the authorization guard; the authorization guard authorizes every message from the access its record declares.");

    /// <summary>The LS2013 descriptor: Audit value read from the message in the handler.</summary>
    public static readonly DiagnosticDescriptor LS2013 = Create("LS2013", LayersCategory, AuditValues,
        "Audit value read from the message in the handler",
        "'{0}' reads only the message; declare the audit value beside the message",
        "A command handler adds only the audit values it learns while running; a value read from the message is declared beside the message, so the pipeline records it on every outcome.");

    /// <summary>The LS2014 descriptor: Command handler finds or creates.</summary>
    public static readonly DiagnosticDescriptor LS2014 = Create("LS2014", LayersCategory, OutcomePath,
        "Command handler finds or creates",
        "Command handler '{0}' finds and creates '{1}'; run a query, then send the command that fits",
        "Each command implements one use case with one success path; a flow that either finds or creates a record is two use cases that the boundary owning the flow chooses between.");

    /// <summary>The LS2015 descriptor: Command handler changes records in a loop.</summary>
    public static readonly DiagnosticDescriptor LS2015 = Create("LS2015", LayersCategory, PerRecordWork,
        "Command handler changes records in a loop",
        "Command handler '{0}' calls '{1}' in a loop; a job runs one command per record",
        "A command handler never changes records in a loop; work over many records is a job that runs one command per record, each in its own unit of work.");

    /// <summary>The LS2016 descriptor: Handler takes a pipeline-owned collaborator.</summary>
    public static readonly DiagnosticDescriptor LS2016 = Create("LS2016", LayersCategory, UniquenessOutOfHandlers,
        "Handler takes a pipeline-owned collaborator",
        "Handler '{0}' takes '{1}', which the pipeline owns",
        "A command or query handler takes no pipeline-owned collaborator through its constructor; the unit of work, idempotency store, outbox, and uniqueness claim service belong to the pipeline.");

    /// <summary>The LS2017 descriptor: LiteSpecLayer names no known layer.</summary>
    public static readonly DiagnosticDescriptor LS2017 = Create("LS2017", LayersCategory, ApplicationProjects,
        "LiteSpecLayer names no known layer",
        "'{0}' names no known layer; use Domain, Application.Abstractions, Application, Infrastructure, WebApi, Worker, Test, or None",
        "The LiteSpecLayer property names one of the known application projects, Test, or None to opt out.");

    /// <summary>The LS2018 descriptor: Application or WebApi names a type outside its layer.</summary>
    public static readonly DiagnosticDescriptor LS2018 = Create("LS2018", LayersCategory, RespectLayerDirection,
        "File names a type outside its layer",
        "{0} uses '{1}', {2}",
        "An Application file names no Infrastructure or WebApi type, no ASP.NET Core type, and no HttpContext. A WebApi file names no Infrastructure type, no repository, and no Domain type other than typed identifiers and shared-kernel values.");

    /// <summary>The LS2019 descriptor: Use case names a type of another use case.</summary>
    public static readonly DiagnosticDescriptor LS2019 = Create("LS2019", LayersCategory, UseCaseIsolation,
        "Use case names a type of another use case",
        "'{0}' lives in use case folder '{1}', which this use case may not name",
        "A use case folder names no type declared in another use case folder. A reaction folder keeps the same boundary.");

    /// <summary>The LS2020 descriptor: Module names a type outside another module's Contracts.</summary>
    public static readonly DiagnosticDescriptor LS2020 = Create("LS2020", LayersCategory, ModuleContracts,
        "Module names a type outside another module's Contracts",
        "'{0}' lives in module '{1}' outside its Contracts folder",
        "A module names another module's types only through its Contracts folder, including through global using directives.");

    /// <summary>The LS3001 descriptor: Asynchronous method name lacks Async.</summary>
    public static readonly DiagnosticDescriptor LS3001 = Create("LS3001", StyleCategory, AsyncNames,
        "Asynchronous method name lacks Async",
        "Method '{0}' returns '{1}' and must end in 'Async'",
        "A method returning Task or ValueTask ends in Async.");

    /// <summary>The LS3004 descriptor: Concrete class is not sealed.</summary>
    public static readonly DiagnosticDescriptor LS3004 = Create("LS3004", StyleCategory, ImplementationStyle,
        "Concrete class is not sealed",
        "Class '{0}' has no derived type and must be sealed",
        "Production C# seals every concrete class that nothing derives from.",
        compilationEnd: true);

    /// <summary>The LS4001 descriptor: Test contains no assertion.</summary>
    public static readonly DiagnosticDescriptor LS4001 = Create("LS4001", TestsCategory, ScenarioProof,
        "Test contains no assertion",
        "Test '{0}' contains no assertion",
        "A test calls the behavior it proves and asserts the result, so a passing test proves something.");

    /// <summary>The LS5001 descriptor: Code element carries no mark.</summary>
    public static readonly DiagnosticDescriptor LS5001 = Create("LS5001", TraceabilityCategory, PointToRecords,
        "Code element carries no mark",
        "'{0}' carries no mark naming the {1} record it serves",
        "A code element of a recognized kind carries a mark naming the record it serves, in the style the project declares.");

    /// <summary>The LS5002 descriptor: Mark names no usable record.</summary>
    public static readonly DiagnosticDescriptor LS5002 = Create("LS5002", TraceabilityCategory, PointToRecords,
        "Mark names no usable record",
        "'{0}' marks '{1}', which {2}",
        "A mark names a record of a type its code element takes, spelled as the record states it.");

    /// <summary>The LS5003 descriptor: Code element carries the wrong number of marks.</summary>
    public static readonly DiagnosticDescriptor LS5003 = Create("LS5003", TraceabilityCategory, PointToRecords,
        "Code element carries the wrong number of marks",
        "'{0}' carries {1} marks but takes exactly one",
        "A code element carries exactly the marks its kind takes: one mark for most kinds, one or more for ports, adapters and tests.");

    /// <summary>The LS5004 descriptor: Mark uses the undeclared style.</summary>
    public static readonly DiagnosticDescriptor LS5004 = Create("LS5004", TraceabilityCategory, DeclaredStyle,
        "Mark uses the undeclared style",
        "Mark on '{0}' naming '{1}' uses the {2} style, but the project declares {3}",
        "Every mark is written in the one style the project declares: a documentation tag for xml, an attribute for attribute.");

    /// <summary>The LS5005 descriptor: Runtime value differs from its record.</summary>
    public static readonly DiagnosticDescriptor LS5005 = Create("LS5005", TraceabilityCategory, RuntimeValues,
        "Runtime value differs from its record",
        "'{0}' declares '{1}' as '{2}', but its record states '{3}'",
        "A domain event declares Classification only with its event identifier, and a rule exception declares Code only with its rule's refusal code; otherwise the generator writes the value from the record.");

    /// <summary>The LS5006 descriptor: Marked event or exception is not partial.</summary>
    public static readonly DiagnosticDescriptor LS5006 = Create("LS5006", TraceabilityCategory, PartialTypes,
        "Marked event or exception is not partial",
        "'{0}' carries a mark and must be declared partial",
        "A marked domain event or rule exception is declared partial, so the generator can add the runtime value as a second part.");

    /// <summary>The LS5007 descriptor: Marked act raises an undeclared event.</summary>
    public static readonly DiagnosticDescriptor LS5007 = Create("LS5007", TraceabilityCategory, MarkedAct,
        "Marked act raises an undeclared event",
        "'{0}' marked with '{1}' raises '{2}', which the use case does not list in 'raises'",
        "An aggregate method marked with a use case raises only the events the use case lists in raises.");

    /// <summary>The LS5008 descriptor: Declared event is raised by no marked act.</summary>
    public static readonly DiagnosticDescriptor LS5008 = Create("LS5008", TraceabilityCategory, MarkedAct,
        "Declared event is raised by no marked act",
        "'{0}' marked with '{1}' raises no '{2}', which the use case lists in 'raises'",
        "Every event a use case lists in raises is raised by a root method marked with the use case.");

    /// <summary>The LS5011 descriptor: Message maps to two use cases.</summary>
    public static readonly DiagnosticDescriptor LS5011 = Create("LS5011", TraceabilityCategory, PointToRecords,
        "Message maps to two use cases",
        "Message '{0}' is handled by both '{1}' and '{2}' with different use cases",
        "One message maps to one use case, so the pipeline reads the permission and the audit identity from the message; a message two handlers serve with different use cases maps to neither.");

    /// <summary>Creates a diagnostic descriptor.</summary>
    /// <param name="id">The diagnostic identifier, such as LS1001.</param>
    /// <param name="category">The diagnostic category name.</param>
    /// <param name="provision">The provision the diagnostic enforces, which supplies the help link.</param>
    /// <param name="title">The short title shown in diagnostic listings.</param>
    /// <param name="message">The message format, with placeholders for the reported names.</param>
    /// <param name="description">The full description of the rule the diagnostic enforces.</param>
    /// <param name="compilationEnd">Whether the diagnostic reports at compilation end instead of per element.</param>
    /// <returns>The diagnostic descriptor.</returns>
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
