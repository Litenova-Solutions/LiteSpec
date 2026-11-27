using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Rules;

/// <summary>
///     Every diagnostic the package reports. LS1xxx check Domain, LS2xxx the outer layers,
///     LS3xxx code style in every production layer, LS4xxx test projects. Each one
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

    /// <summary>The provision the related descriptors enforce: keep domain free of outer layer concerns (litespec/packs/dotnet/domain#keep-domain-free-of-outer-layer-concerns).</summary>
    private static readonly Provision DomainOuterLayers = Provision.Of("packs/dotnet/domain", "keep-domain-free-of-outer-layer-concerns");
    /// <summary>The provision the related descriptors enforce: model every closed set of domain values without enums (litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums).</summary>
    private static readonly Provision ClosedSets = Provision.Of("packs/dotnet/domain", "model-every-closed-set-of-domain-values-without-enums");
    /// <summary>The provision the related descriptors enforce: model every aggregate lifecycle with state records (litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records).</summary>
    private static readonly Provision StateRecords = Provision.Of("packs/dotnet/domain", "model-every-aggregate-lifecycle-with-state-records");
    /// <summary>The provision the related descriptors enforce: reject business violations with domain exceptions (litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions).</summary>
    private static readonly Provision DomainExceptions = Provision.Of("packs/dotnet/domain", "reject-business-violations-with-domain-exceptions");
    /// <summary>The provision the related descriptors enforce: raise immutable domain facts (litespec/packs/dotnet/domain#raise-immutable-domain-facts).</summary>
    private static readonly Provision DomainEvents = Provision.Of("packs/dotnet/domain", "raise-immutable-domain-facts");
    /// <summary>The provision the related descriptors enforce: keep binary floating point out of exact quantities (litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities).</summary>
    private static readonly Provision ExactQuantities = Provision.Of("core/backend-domain", "keep-binary-floating-point-out-of-exact-quantities");
    /// <summary>The provision the related descriptors enforce: pass nondeterministic values into domain (litespec/core/backend-domain#pass-nondeterministic-values-into-domain).</summary>
    private static readonly Provision Nondeterminism = Provision.Of("core/backend-domain", "pass-nondeterministic-values-into-domain");
    /// <summary>The provision the related descriptors enforce: keep repository interfaces in domain (litespec/packs/dotnet/domain#keep-repository-interfaces-in-domain).</summary>
    private static readonly Provision Repositories = Provision.Of("packs/dotnet/domain", "keep-repository-interfaces-in-domain");
    /// <summary>The provision the related descriptors enforce: document every public domain contract (litespec/core/backend-domain#document-every-public-domain-contract).</summary>
    private static readonly Provision DomainDocumentation = Provision.Of("core/backend-domain", "document-every-public-domain-contract");
    /// <summary>The provision the related descriptors enforce: use strongly typed version 7 identifiers (litespec/packs/dotnet/domain#use-strongly-typed-version-7-identifiers).</summary>
    private static readonly Provision TypedIdentifiers = Provision.Of("packs/dotnet/domain", "use-strongly-typed-version-7-identifiers");
    /// <summary>The provision the related descriptors enforce: offer a non-throwing parse on validated value types (litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types).</summary>
    private static readonly Provision NonThrowingParse = Provision.Of("packs/dotnet/domain", "offer-a-non-throwing-parse-on-validated-value-types");
    /// <summary>The provision the development shortcut descriptor enforces: keep development shortcuts out of release builds (litespec/quality/development#keep-development-shortcuts-out-of-release-builds).</summary>
    private static readonly Provision DevelopmentShortcuts = Provision.Of("quality/development", "keep-development-shortcuts-out-of-release-builds");
    /// <summary>The provision the related descriptors enforce: keep implementation types internal (litespec/packs/dotnet/architecture#keep-implementation-types-internal).</summary>
    private static readonly Provision InternalImplementations = Provision.Of("packs/dotnet/architecture", "keep-implementation-types-internal");
    /// <summary>The provision the related descriptors enforce: follow layer direction in every file (litespec/packs/dotnet/architecture#follow-layer-direction-in-every-file).</summary>
    private static readonly Provision RespectLayerDirection = Provision.Of("packs/dotnet/architecture", "follow-layer-direction-in-every-file");
    /// <summary>The provision the related descriptors enforce: keep use case folders isolated (litespec/packs/dotnet/architecture#keep-use-case-folders-isolated).</summary>
    private static readonly Provision UseCaseIsolation = Provision.Of("packs/dotnet/architecture", "keep-use-case-folders-isolated");
    /// <summary>The provision the related descriptors enforce: reach another module only through contracts (litespec/packs/dotnet/architecture#reach-another-module-only-through-contracts).</summary>
    private static readonly Provision ModuleContracts = Provision.Of("packs/dotnet/architecture", "reach-another-module-only-through-contracts");
    /// <summary>The provision the related descriptors enforce: publish only data from each contracts project (litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project).</summary>
    private static readonly Provision ContractsData = Provision.Of("packs/dotnet/architecture", "publish-only-data-from-each-contracts-project");
    /// <summary>The provision the related descriptors enforce: exclude mvc controllers from the stack (litespec/packs/dotnet/api#exclude-mvc-controllers-from-the-stack).</summary>
    private static readonly Provision NoControllers = Provision.Of("packs/dotnet/api", "exclude-mvc-controllers-from-the-stack");
    /// <summary>The provision the related descriptors enforce: stage aggregate writes through repositories (litespec/packs/dotnet/persistence#stage-aggregate-writes-through-repositories).</summary>
    private static readonly Provision RepositoryWrites = Provision.Of("packs/dotnet/persistence", "stage-aggregate-writes-through-repositories");
    /// <summary>The provision the related descriptors enforce: name asynchronous operations completely (litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names).</summary>
    private static readonly Provision AsyncNames = Provision.Of("workspace/naming", "reveal-asynchrony-and-cancellation-in-operation-names");
    /// <summary>The provision the related descriptors enforce: keep implementation style consistent (litespec/packs/dotnet/naming#keep-implementation-style-consistent).</summary>
    private static readonly Provision ImplementationStyle = Provision.Of("packs/dotnet/naming", "keep-implementation-style-consistent");
    /// <summary>The provision the related descriptors enforce: put the cancellation token last (litespec/packs/dotnet/naming#put-the-cancellation-token-last).</summary>
    private static readonly Provision CancellationTokenLast = Provision.Of("packs/dotnet/naming", "put-the-cancellation-token-last");
    /// <summary>The provision the related descriptors enforce: match C# files and primary types (litespec/packs/dotnet/naming#match-c-files-and-primary-types).</summary>
    private static readonly Provision FileTypeNames = Provision.Of("packs/dotnet/naming", "match-c-files-and-primary-types");
    /// <summary>The provision the related descriptors enforce: use architectural suffixes (litespec/workspace/naming#use-architectural-suffixes).</summary>
    private static readonly Provision RoleSuffixes = Provision.Of("workspace/naming", "use-architectural-suffixes");
    /// <summary>The provision the related descriptors enforce: name every event type by its kind (litespec/packs/dotnet/naming#name-every-event-type-by-its-kind).</summary>
    private static readonly Provision EventKindSuffixes = Provision.Of("packs/dotnet/naming", "name-every-event-type-by-its-kind");
    /// <summary>The provision the related descriptors enforce: name result parts after their result (litespec/workspace/naming#name-result-parts-after-their-result).</summary>
    private static readonly Provision ResultParts = Provision.Of("workspace/naming", "name-result-parts-after-their-result");
    /// <summary>The provision the related descriptors enforce: name repository variables after their interface (litespec/workspace/naming#name-repository-variables-after-their-interface).</summary>
    private static readonly Provision RepositoryVariables = Provision.Of("workspace/naming", "name-repository-variables-after-their-interface");
    /// <summary>The provision the related descriptors enforce: project queries directly (litespec/core/backend-application#project-queries-directly).</summary>
    private static readonly Provision QueryProjection = Provision.Of("core/backend-application", "project-queries-directly");
    /// <summary>The provision the related descriptors enforce: enforce target authorization in the pipeline (litespec/core/backend-application#enforce-target-authorization-in-the-pipeline).</summary>
    private static readonly Provision TargetAuthorization = Provision.Of("core/backend-application", "enforce-target-authorization-in-the-pipeline");
    /// <summary>The provision the related descriptors enforce: declare audit values beside the message (litespec/core/backend-application#declare-the-audit-target-and-reason-beside-the-message).</summary>
    private static readonly Provision AuditValues = Provision.Of("core/backend-application", "declare-the-audit-target-and-reason-beside-the-message");
    /// <summary>The provision the related descriptors enforce: implement one outcome path per command (litespec/core/backend-application#implement-one-outcome-path-per-command).</summary>
    private static readonly Provision OutcomePath = Provision.Of("core/backend-application", "implement-one-outcome-path-per-command");
    /// <summary>The provision the related descriptors enforce: issue per record work from a job (litespec/core/backend-application#issue-per-record-work-from-a-job).</summary>
    private static readonly Provision PerRecordWork = Provision.Of("core/backend-application", "issue-per-record-work-from-a-job");
    /// <summary>The provision the related descriptors enforce: keep uniqueness out of handlers (litespec/core/backend-persistence#keep-uniqueness-out-of-handlers).</summary>
    private static readonly Provision UniquenessOutOfHandlers = Provision.Of("core/backend-persistence", "keep-uniqueness-out-of-handlers");
    /// <summary>The provision the related descriptors enforce: compare typed identifiers by their underlying values in queries (litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries).</summary>
    private static readonly Provision TypedIdentifierQueryValues = Provision.Of("packs/dotnet/persistence", "compare-typed-identifiers-by-their-underlying-values-in-queries");
    /// <summary>The provision the related descriptors enforce: compare typed identifiers by their underlying values in queries (litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries).</summary>
    private static readonly Provision TypedIdentifierUnderlyingValues = Provision.Of("packs/dotnet/persistence", "compare-typed-identifiers-by-their-underlying-values-in-queries");
    /// <summary>The provision the related descriptors enforce: read knowledge through its model (litespec/core/knowledge#read-knowledge-through-its-model).</summary>
    private static readonly Provision KnowledgeThroughModel = Provision.Of("core/knowledge", "read-knowledge-through-its-model");
    /// <summary>The provision the related descriptors enforce: keep knowledge sets free of logic (litespec/core/knowledge#keep-knowledge-sets-free-of-logic).</summary>
    private static readonly Provision KnowledgeSetData = Provision.Of("core/knowledge", "keep-knowledge-sets-free-of-logic");
    /// <summary>The provision the related descriptors enforce: read knowledge by role and date (litespec/core/knowledge#read-knowledge-by-role-and-date).</summary>
    private static readonly Provision KnowledgeByRole = Provision.Of("core/knowledge", "read-knowledge-by-role-and-date");
    /// <summary>The provision the related descriptors enforce: load documents keyed by typed identifiers one at a time (litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time).</summary>
    private static readonly Provision TypedIdentifierDocumentLoads = Provision.Of("packs/dotnet/persistence", "load-documents-keyed-by-typed-identifiers-one-at-a-time");
    /// <summary>The provision the related descriptors enforce: mark the test that proves each scenario (litespec/quality/testing#prove-each-scenario-through-the-public-boundary).</summary>
    private static readonly Provision ScenarioProof = Provision.Of("quality/testing", "prove-each-scenario-through-the-public-boundary");
    /// <summary>The provision the related descriptors enforce: keep command handlers to load, call and stage (litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage).</summary>
    private static readonly Provision CommandHandlerStages = Provision.Of("core/backend-application", "keep-command-handlers-to-load-call-and-stage");
    /// <summary>The provision the related descriptors enforce: emit declared integration events after commit (litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit).</summary>
    private static readonly Provision EmitAfterCommit = Provision.Of("core/backend-application", "stage-declared-module-and-integration-events-in-the-commit");
    /// <summary>The provision the related descriptors enforce: pass explicit mediation settings to nested use cases (litespec/packs/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases).</summary>
    private static readonly Provision HandlerMediationSettings = Provision.Of("packs/dotnet/application", "pass-explicit-mediation-settings-to-nested-use-cases");
    /// <summary>The provision the related descriptors enforce: map failures to statuses in the global exception handler (litespec/packs/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler).</summary>
    private static readonly Provision UnknownRecordRefusals = Provision.Of("packs/dotnet/api", "map-failures-to-statuses-in-the-global-exception-handler");
    /// <summary>The provision the related descriptors enforce: read time from the scoped clock (litespec/core/backend-application#read-time-from-the-scoped-clock).</summary>
    private static readonly Provision ProductionClockReads = Provision.Of("core/backend-application", "read-time-from-the-scoped-clock");
    /// <summary>The provision the related descriptors enforce: point code and tests up to records (litespec/core/system#point-code-and-tests-up-to-records).</summary>
    private static readonly Provision PointToRecords = Provision.Of("core/system", "point-code-and-tests-up-to-records");
    /// <summary>The provision the related descriptors enforce: construct aggregate parts only inside the root (litespec/packs/dotnet/domain#construct-aggregate-parts-only-inside-the-root).</summary>
    private static readonly Provision AggregateConstruction = Provision.Of("packs/dotnet/domain", "construct-aggregate-parts-only-inside-the-root");
    /// <summary>The provision the related descriptors enforce: model child entities as sealed classes (litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes).</summary>
    private static readonly Provision ChildEntities = Provision.Of("packs/dotnet/domain", "model-child-entities-as-sealed-classes");
    /// <summary>The provision the related descriptors enforce: reference other aggregates by typed identifier (litespec/packs/dotnet/domain#reference-other-aggregates-by-typed-identifier).</summary>
    private static readonly Provision TypedAggregateRefs = Provision.Of("packs/dotnet/domain", "reference-other-aggregates-by-typed-identifier");
    /// <summary>The provision the related descriptors enforce: declare each pipeline concern in its own class (litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class).</summary>
    private static readonly Provision PipelineConcern = Provision.Of("workspace/design", "declare-each-pipeline-concern-in-its-own-class");
    /// <summary>The provision the related descriptors enforce: plug modules into the platform through contributions (litespec/packs/dotnet/architecture#plug-modules-into-the-platform-through-contributions).</summary>
    private static readonly Provision PlatformContributions = Provision.Of("packs/dotnet/architecture", "plug-modules-into-the-platform-through-contributions");
    /// <summary>The provision the related descriptors enforce: expose only contracts and the entry point (litespec/packs/dotnet/architecture#expose-only-contracts-and-the-entry-point).</summary>
    private static readonly Provision ModuleSurface = Provision.Of("packs/dotnet/architecture", "expose-only-contracts-and-the-entry-point");
    /// <summary>The provision the related descriptors enforce: produce file formats in infrastructure (litespec/core/backend-architecture#produce-file-formats-in-infrastructure).</summary>
    private static readonly Provision FileFormats = Provision.Of("core/backend-architecture", "produce-file-formats-in-infrastructure");
    /// <summary>The provision the related descriptors enforce: avoid primary constructors on classes and structs (litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs).</summary>
    private static readonly Provision NoPrimaryConstructors = Provision.Of("workspace/naming", "avoid-primary-constructors-on-classes-and-structs");
    /// <summary>The provision the related descriptors enforce: write each mark in the declared style (litespec/packs/dotnet/stack#write-each-mark-in-the-declared-style).</summary>
    private static readonly Provision DeclaredStyle = Provision.Of("packs/dotnet/stack", "write-each-mark-in-the-declared-style");
    /// <summary>The provision the related descriptors enforce: generate runtime values from the records (litespec/packs/dotnet/stack#generate-runtime-values-from-the-records).</summary>
    private static readonly Provision RuntimeValues = Provision.Of("packs/dotnet/stack", "generate-runtime-values-from-the-records");
    /// <summary>The provision the related descriptors enforce: declare marked events and exceptions as partial types (litespec/packs/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types).</summary>
    private static readonly Provision PartialTypes = Provision.Of("packs/dotnet/stack", "declare-marked-events-and-exceptions-as-partial-types");
    /// <summary>The LS1001 descriptor: Domain references an outer-layer concern.</summary>
    public static readonly DiagnosticDescriptor LS1001 = Create("LS1001", DomainCategory, DomainOuterLayers,
        "Domain references an outer-layer concern",
        "Domain uses '{0}' from '{1}', an outer-layer concern",
        "Domain references only the .NET base class library and project-owned Domain types: no persistence, web, messaging, mediator, logging, configuration, dependency injection, serialization, or Application, Ports, Infrastructure or WebApi type.");

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
        "Domain event '{0}' must be a record named '{{Aggregate}}{{PastFact}}DomainEvent' with no settable members",
        "A domain event is a public immutable record named {Aggregate}{PastFact}DomainEvent implementing IDomainEvent and carrying business data only.");

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

    /// <summary>The LS1014 descriptor: Aggregate part constructed outside its Domain folder.</summary>
    public static readonly DiagnosticDescriptor LS1014 = Create("LS1014", DomainCategory, AggregateConstruction,
        "Aggregate part constructed outside its Domain folder",
        "'{0}' belongs to the '{1}' aggregate; construct it through the root instead of here",
        "Application, WebApi, reactions and jobs call only the root's named factories and methods with plain inputs. They never construct an entity or value object that belongs to an aggregate.");

    /// <summary>The LS1015 descriptor: Non-root type exposes public mutation.</summary>
    public static readonly DiagnosticDescriptor LS1015 = Create("LS1015", DomainCategory, ChildEntities,
        "Non-root type exposes public mutation",
        "'{0}' is a non-root type of the '{1}' aggregate; {2}",
        "Only the root changes its aggregate: entities and value objects inside an aggregate have no public setters, no public constructors and no public mutable collections.");

    /// <summary>The LS1016 descriptor: Aggregate references another aggregate by object.</summary>
    public static readonly DiagnosticDescriptor LS1016 = Create("LS1016", DomainCategory, TypedAggregateRefs,
        "Aggregate references another aggregate by object",
        "'{0}' is typed as '{1}' of the '{2}' aggregate; reference it by its typed id",
        "An aggregate references another aggregate by its typed id only, never by object reference.");

    /// <summary>The LS1017 descriptor: Validated value type offers no non-throwing parse.</summary>
    public static readonly DiagnosticDescriptor LS1017 = Create("LS1017", DomainCategory, NonThrowingParse,
        "Validated value type offers no non-throwing parse",
        "'{0}' validates in its constructor and offers no non-throwing parse; add a TryParse the serializer and the API boundary can call",
        "A typed identifier or value type whose constructor throws on invalid input offers a non-throwing parse, so the serializer and the API boundary read invalid input without throwing.");

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

    /// <summary>The LS2018 descriptor: Application, Ports or WebApi names a type outside its layer.</summary>
    public static readonly DiagnosticDescriptor LS2018 = Create("LS2018", LayersCategory, RespectLayerDirection,
        "File names a type outside its layer",
        "{0} uses '{1}', {2}",
        "An Application file names no Infrastructure or WebApi type, no ASP.NET Core type, and no HttpContext. A Ports file names no Application, Infrastructure or WebApi type, no ASP.NET Core type, and no HttpContext. A WebApi file names no Infrastructure or Ports type, no repository, and no Domain type other than typed identifiers and shared-kernel values.");

    /// <summary>The LS2019 descriptor: Use case names a type of another use case.</summary>
    public static readonly DiagnosticDescriptor LS2019 = Create("LS2019", LayersCategory, UseCaseIsolation,
        "Use case names a type of another use case",
        "'{0}' lives in use case folder '{1}', which this use case may not name",
        "A use case folder names no type declared in another use case folder, the message and result of another use case included. A reaction or job sits in the folder of the use case it runs, so it names no other use case's type either.");

    /// <summary>The LS2020 descriptor: Module names a type outside another module's Contracts.</summary>
    public static readonly DiagnosticDescriptor LS2020 = Create("LS2020", LayersCategory, ModuleContracts,
        "Module names a type outside another module's Contracts",
        "'{0}' lives in module '{1}' outside its Contracts folder",
        "A module names another module's types only through its Contracts folder, including through global using directives.");

    /// <summary>The LS2021 descriptor: A type holds a mediator its role may not hold.</summary>
    public static readonly DiagnosticDescriptor LS2021 = Create("LS2021", LayersCategory, CommandHandlerStages,
        "Message Sent Outside an Entry Point",
        "'{0}' holds '{1}', {2}",
        "A command or query handler holds the query mediator to send another module's public query. A reaction or a job holds the command and query mediators. No module type holds the event mediator: only the pipeline publishes events. A use case handler loads, calls one act, and stages; follow-on work runs from a reaction or a job after the commit.");

    /// <summary>The LS2022 descriptor: User code publishes an event.</summary>
    public static readonly DiagnosticDescriptor LS2022 = Create("LS2022", LayersCategory, EmitAfterCommit,
        "Event Published by User Code",
        "'{0}' publishes events from user code; the pipeline publishes the integration event a raised domain event translates",
        "Only the pipeline publishes an outbound integration event. A use case raises a domain event, and the pipeline stages each integration event that translates it in the same commit. User code holds no event mediator and no event publisher.");

    /// <summary>The LS2023 descriptor: Class implements two message definitions.</summary>
    public static readonly DiagnosticDescriptor LS2023 = Create("LS2023", LayersCategory, PipelineConcern,
        "Class implements two message definitions",
        "'{0}' implements {1} message definitions; {2}",
        "Each pipeline declaration sits in its own class: one class implements exactly one LiteBus message definition contract, never two concerns such as authorization and audit.");

    /// <summary>The LS2024 descriptor: Module holds platform-owned capability code.</summary>
    public static readonly DiagnosticDescriptor LS2024 = Create("LS2024", LayersCategory, PlatformContributions,
        "Module holds platform-owned capability code",
        "'{0}' {1}",
        "A module plugs into the platform by contributing data, never by copying platform code: store and serializer setup, guard evaluation and converters live in the platform.");

    /// <summary>The LS2025 descriptor: An adapter names a type outside its layer.</summary>
    public static readonly DiagnosticDescriptor LS2025 = Create("LS2025", LayersCategory, RespectLayerDirection,
        "Adapter names a type outside its layer",
        "Adapter uses '{0}', {1}",
        "An adapter in Infrastructure/{Port}/ implements its port from platform and library types. It names no Domain type outside typed identifiers, shared-kernel values and the types the interfaces of the ports it implements name in their member signatures, and no WebApi type.");

    /// <summary>The LS2026 descriptor: Model duplicated in two use case folders.</summary>
    public static readonly DiagnosticDescriptor LS2026 = Create("LS2026", LayersCategory, UseCaseIsolation,
        "Model duplicated in two use case folders",
        "'{0}' in use case '{1}' duplicates the model in use case '{2}'{3}",
        "A shape two use cases need is declared once beside the aggregate they share, or referenced from the Kernel when it is a shared value. Copying it into each use case folder breaks isolation.",
        compilationEnd: true);

    /// <summary>The LS2027 descriptor: Application does infrastructure work.</summary>
    public static readonly DiagnosticDescriptor LS2027 = Create("LS2027", LayersCategory, FileFormats,
        "Application does infrastructure work",
        "'{0}' from '{1}' does file, archive or document work in Application; move it behind a port into Infrastructure",
        "Producing a file format, rendering and parsing are Infrastructure behind an Application port, even when they use only the base class library.");

    /// <summary>The LS2028 descriptor: Module exposes a public type outside Contracts.</summary>
    public static readonly DiagnosticDescriptor LS2028 = Create("LS2028", LayersCategory, ModuleSurface,
        "Module exposes a public type outside Contracts",
        "'{0}' is public outside Contracts; keep module types internal behind the '{1}' entry type",
        "In one project per module only the Contracts project and the module entry type are public. Every other module type stays internal, so the compiler enforces the module boundary.");

    /// <summary>The LS2029 descriptor: File names a contract type outside its layer.</summary>
    public static readonly DiagnosticDescriptor LS2029 = Create("LS2029", LayersCategory, RespectLayerDirection,
        "File names a contract type outside its layer",
        "{0} uses '{1}', {2}",
        "A handler, a reaction, or a job names another module's public query or query result to send it through the query mediator, and only a reaction or a job names another module's public command or command result. Every other module file names no public query, public command or result of another module's Contracts project. A Platform file names no module identifier.");

    /// <summary>The LS2030 descriptor: Contracts project declares an interface.</summary>
    public static readonly DiagnosticDescriptor LS2030 = Create("LS2030", LayersCategory, ContractsData,
        "Contracts project declares an interface",
        "'{0}' is an interface in a Contracts project; a Contracts project publishes data only",
        "A Contracts project publishes data only: identifiers, facts, and public queries grouped by aggregate. It declares no interface.");

    /// <summary>The LS2031 descriptor: Event name lacks its kind suffix.</summary>
    public static readonly DiagnosticDescriptor LS2031 = Create("LS2031", LayersCategory, EventKindSuffixes,
        "Event name lacks its kind suffix",
        "'{0}' is a {1} and must end in '{2}'",
        "A domain event ends in DomainEvent, a module event in ModuleEvent, and an integration event in IntegrationEvent, so the name states which contract the type carries.");

    /// <summary>The LS2032 descriptor: Queryable Contains over typed identifiers.</summary>
    public static readonly DiagnosticDescriptor LS2032 = Create("LS2032", LayersCategory, TypedIdentifierQueryValues,
        "Queryable Contains over typed identifiers",
        "'{0}' is a collection of typed identifier '{1}'; compare the underlying value in the query instead",
        "A Contains inside a queryable expression compares the underlying value of each typed identifier, never the identifier itself, because the database driver cannot send a typed identifier collection as a parameter.");

    /// <summary>The LS2033 descriptor: LoadMany on a typed-identifier document.</summary>
    public static readonly DiagnosticDescriptor LS2033 = Create("LS2033", LayersCategory, TypedIdentifierDocumentLoads,
        "LoadMany on a typed-identifier document",
        "'{0}' cannot load many '{1}' documents identified by a typed identifier; load one at a time or query by the underlying value",
        "Marten refuses a multi-load of documents whose id member is a typed identifier, so the read loads one document at a time or queries by the underlying value.");

    /// <summary>The LS2034 descriptor: Mediator call without explicit mediation settings.</summary>
    public static readonly DiagnosticDescriptor LS2034 = Create("LS2034", LayersCategory, HandlerMediationSettings,
        "Mediator call without explicit mediation settings",
        "'{0}' calls '{1}' with no explicit mediation settings; the handler runs with no caller, so name the identity the nested use case runs under",
        "An event handler runs with no caller, so each command or query it dispatches names the mediation settings carrying the identity the nested use case runs under.");

    /// <summary>The LS2035 descriptor: Unknown record thrown as a generic exception.</summary>
    public static readonly DiagnosticDescriptor LS2035 = Create("LS2035", LayersCategory, UnknownRecordRefusals,
        "Unknown record thrown as a generic exception",
        "'{0}' returned no record; refuse the unknown record instead of throwing '{1}'",
        "A null check on a value returned by a load, find or lookup call on a repository or session refuses the unknown record with its not-found refusal, never with a generic exception.");

    /// <summary>The LS2036 descriptor: Production code reads the clock directly.</summary>
    public static readonly DiagnosticDescriptor LS2036 = Create("LS2036", LayersCategory, ProductionClockReads,
        "Production code reads the clock directly",
        "'{0}' reads the clock directly; read the pipeline timestamp instead",
        "Production code outside Domain reads time from the pipeline timestamp or an injected clock, never from the static clock, so one message answers to one time.");

    /// <summary>The LS2037 descriptor: Identity value read in a queryable expression.</summary>
    public static readonly DiagnosticDescriptor LS2037 = Create("LS2037", LayersCategory, TypedIdentifierUnderlyingValues,
        "Identity value read in a queryable expression",
        "'{0}' reads its identity '{1}' by value inside a queryable expression; compare the identifier itself or page in memory",
        "A queryable expression never reads the underlying value of the document's own typed identifier, because the document store cannot translate the member chain on its identity member.");

    /// <summary>The LS2038 descriptor: Module references a knowledge set.</summary>
    public static readonly DiagnosticDescriptor LS2038 = Create("LS2038", LayersCategory, KnowledgeThroughModel,
        "Module references a knowledge set",
        "'{0}' lives in knowledge set '{1}'; read knowledge through its model",
        "A module reads knowledge through the knowledge model, never by naming a type of a knowledge set project. Only hosts reference a set, to register it.");

    /// <summary>The LS2039 descriptor: Knowledge set declares logic.</summary>
    public static readonly DiagnosticDescriptor LS2039 = Create("LS2039", LayersCategory, KnowledgeSetData,
        "Knowledge set declares logic",
        "'{0}' declares logic; a knowledge set holds only rows built through its model's constructors",
        "A knowledge set holds only rows built through its model's constructors: no branches, loops, switches, calls, or throws. A variant that needs behavior selects a strategy the model declares.");

    /// <summary>The LS2040 descriptor: Knowledge key built from a string literal.</summary>
    public static readonly DiagnosticDescriptor LS2040 = Create("LS2040", LayersCategory, KnowledgeByRole,
        "Knowledge key built from a string literal",
        "'{0}' builds a knowledge key from a string literal; read knowledge by role and date",
        "Code outside a knowledge set reads knowledge by role and date through the model; it never builds a knowledge key from a string literal.");

    /// <summary>The LS2041 descriptor: Closed-set Code compared or carried as a string literal.</summary>
    public static readonly DiagnosticDescriptor LS2041 = Create("LS2041", LayersCategory, ClosedSets,
        "Closed-set Code compared or carried as a string literal",
        "'{0}' writes the Code of '{1}' as a string literal; {2}",
        "Code outside a closed set names the typed case or a synced constant, never a string literal: a comparison against Code branches on the case type or compares a CodeValue constant, and a refusal carries its code from the synced refusal constant, so a renamed code fails the build instead of reaching production.");

    /// <summary>The LS3001 descriptor: Asynchronous method name lacks Async.</summary>
    public static readonly DiagnosticDescriptor LS3001 = Create("LS3001", StyleCategory, AsyncNames,
        "Asynchronous method name lacks Async",
        "Method '{0}' returns '{1}' and must end in 'Async'",
        "A method returning Task or ValueTask ends in Async.");

    /// <summary>The LS3002 descriptor: CancellationToken is not the last parameter.</summary>
    public static readonly DiagnosticDescriptor LS3002 = Create("LS3002", StyleCategory, CancellationTokenLast,
        "CancellationToken is not the last parameter",
        "Method '{0}' must take 'CancellationToken cancellationToken' as its last parameter",
        "A CancellationToken parameter sits last, or second to last before a params array, named cancellationToken.");

    /// <summary>The LS3003 descriptor: File does not match its primary type.</summary>
    public static readonly DiagnosticDescriptor LS3003 = Create("LS3003", StyleCategory, FileTypeNames,
        "File does not match its primary type",
        "Type '{0}' is not the primary type of file '{1}'",
        "A C# file contains one primary top-level type and takes that type's exact name.");

    /// <summary>The LS3004 descriptor: Concrete class is not sealed.</summary>
    public static readonly DiagnosticDescriptor LS3004 = Create("LS3004", StyleCategory, ImplementationStyle,
        "Concrete class is not sealed",
        "Class '{0}' has no derived type and must be sealed",
        "Production C# seals every concrete class that nothing derives from.",
        compilationEnd: true);

    /// <summary>The LS3005 descriptor: Class or struct declares a primary constructor.</summary>
    public static readonly DiagnosticDescriptor LS3005 = Create("LS3005", StyleCategory, NoPrimaryConstructors,
        "Class or struct declares a primary constructor",
        "'{0}' declares a primary constructor; declare private readonly fields and an explicit constructor",
        "Classes and structs state their state with private readonly fields and an explicit constructor. Positional records stay allowed for immutable data.",
        severity: DiagnosticSeverity.Info);

    /// <summary>The LS4001 descriptor: Test contains no assertion.</summary>
    public static readonly DiagnosticDescriptor LS4001 = Create("LS4001", TestsCategory, ScenarioProof,
        "Test contains no assertion",
        "Test '{0}' contains no assertion",
        "A test calls the behavior it proves and asserts the result, so a passing test proves something.");

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

    /// <summary>The LS5011 descriptor: Message maps to two use cases.</summary>
    public static readonly DiagnosticDescriptor LS5011 = Create("LS5011", TraceabilityCategory, PointToRecords,
        "Message maps to two use cases",
        "Message '{0}' is handled by both '{1}' and '{2}' with different use cases",
        "One message maps to one use case, so the pipeline reads the permission and the audit identity from the message; a message two handlers serve with different use cases maps to neither.");

    /// <summary>The message fragment LS5014 uses for a development shortcut compiled into a build without DEBUG.</summary>
    public static readonly string DevelopmentShortcutWithoutDebug =
        "compiled into a build without DEBUG; reference its project only in Debug builds and register it behind #if DEBUG (" + DevelopmentShortcuts.HelpLink + ")";

    /// <summary>The message fragment LS5014 uses for a development shortcut whose tag states no reason.</summary>
    public static readonly string DevelopmentShortcutWithoutReason =
        "with no reason in its development-shortcut tag; state why the shortcut exists there (" + DevelopmentShortcuts.HelpLink + ")";

    /// <summary>The LS5014 descriptor: Development shortcut compiled without DEBUG, or with no reason.</summary>
    public static readonly DiagnosticDescriptor LS5014 = Create("LS5014", TraceabilityCategory, DevelopmentShortcuts,
        "Development shortcut compiled into a build without DEBUG",
        "'{0}' is a development shortcut {1}",
        "A type tagged as a development shortcut compiles only into builds that define DEBUG, and its development-shortcut tag states the reason it exists.");

    /// <summary>Creates a diagnostic descriptor.</summary>
    /// <param name="id">The diagnostic identifier, such as LS1001.</param>
    /// <param name="category">The diagnostic category name.</param>
    /// <param name="provision">The provision the diagnostic enforces, which supplies the help link.</param>
    /// <param name="title">The short title shown in diagnostic listings.</param>
    /// <param name="message">The message format, with placeholders for the reported names.</param>
    /// <param name="description">The full description of the rule the diagnostic enforces.</param>
    /// <param name="compilationEnd">Whether the diagnostic reports at compilation end instead of per element.</param>
    /// <param name="severity">The severity the diagnostic reports with.</param>
    /// <returns>The diagnostic descriptor.</returns>
    private static DiagnosticDescriptor Create(
        string id, string category, Provision provision, string title, string message, string description, bool compilationEnd = false, DiagnosticSeverity severity = DiagnosticSeverity.Error) =>
        new(
            id,
            title,
            message,
            category,
            severity,
            isEnabledByDefault: true,
            description: $"{description} Provision: {provision.Id}",
            helpLinkUri: provision.HelpLink,
            customTags: compilationEnd ? [provision.Id, WellKnownDiagnosticTags.CompilationEnd] : [provision.Id]);
}
