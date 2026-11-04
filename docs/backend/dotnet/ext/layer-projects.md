# Layer Projects

**Stack:** dotnet
**Layer:** application

## Intent

This extension packages each backend layer as its own .NET project: Domain, Application.Abstractions, Application, Infrastructure, and WebApi, with hosts beside them. It states the five-project form of the project, reference, package, and layout rules for backends already on five projects and for teams that want the compiler to hold Domain purity. The module layout is the default; this page holds the packaging that replaces it.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The consumer selects `layer-projects` when its backend already packages each layer as its own project, or when the team wants the compiler to hold Domain purity through project references. Without this extension, the module layout applies.

## Baseline Relationship

For a project selecting this extension, this extension replaces:

- `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`: one folder per module in every project replaces the one module tree.
- `litespec/backend/dotnet/architecture#use-one-application-project`: five application projects replace the one application project with its hosts.
- `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`: host endpoints without a handler live in the WebApi project instead of beside the one application project.
- `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`: one folder and namespace per module in every project replaces the one namespace tree.
- `litespec/workspace/dependencies#follow-the-declared-dependency-graph`: project references between the five projects replace references from the hosts to the one application project.
- `litespec/workspace/dependencies#keep-domain-dependency-free`: a Domain project with no package references replaces analyzer-checked purity of the Domain layer folders.
- `litespec/workspace/dependencies#keep-application-free-of-provider-packages`: an Application project with narrow references replaces the same boundary on the Application layer folders.
- `litespec/workspace/dependencies#assign-each-baseline-package-to-its-owning-layer`: package ownership by project replaces ownership by layer folder.
- `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-application-project-and-hosts`: the analyzers run in every application project instead of in the one application project and the hosts.
- `litespec/backend/dotnet/domain#use-the-domain-layer-folder-layout`: aggregate folders in the Domain project replace aggregate layer folders in the module tree.
- `litespec/backend/dotnet/application#use-the-use-case-folder-layout`: operation folders in the Application project replace operation files in the use case folders.
- `litespec/backend/dotnet/api#place-endpoints-in-their-use-case-folders`: endpoint folders in the WebApi project replace endpoint files in the use case folders.
- `litespec/backend/dotnet/persistence#use-the-persistence-layer-folder-layout`: the Infrastructure persistence folder replaces the product and module Infrastructure folders.
- `litespec/backend/testing#keep-the-test-suites-the-size-requires`: Domain.Tests and Application.Tests beside Architecture, Integration, and Acceptance replace the one unit test project beside them.
- `litespec/workspace/structure#keep-production-and-test-code-separate`: the five-project source tree replaces the one application project beside its hosts.

Every other baseline provision binds unchanged.

This extension switches on no optional record type.

## Standards

### Keep One Application Assembly

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-one-application-assembly`

**Strength:** rule

**Statement:** An application MUST place commands, queries, results, validators, handlers, event handler classes, and module-owned ports in one Application project, and cross-module contracts in Application.Abstractions, or in Application where the project merges them under `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects`.

**Rationale:** Splitting Application into Write, Read, Contracts, and event-handler assemblies would buy a deployment boundary the stack never uses, and every use case would pay for it with a project reference to cross. The separation this stack keeps is the one [Fowler describes as CQRS](https://www.martinfowler.com/bliki/CQRS.html): the command and query models differ, while one assembly holds them. Event handlers live in Application `On{Fact}` folders of the aggregate whose records they change; job host classes live in Worker or Infrastructure with the scheduling concerns, under `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`.

**Reviewed for:** Which command, query, result, validator, handler, or event handler files escape the Application project, or which cross-module contracts escape Application.Abstractions and the merged Application? Look at the Application project files beside the shared contracts.

### Mirror Modules in Folders and Namespaces

**ID:** `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`

**Strength:** rule

**Statement:** Every project MUST give each module one folder and namespace.

**Rationale:** The folder name derives from the module identifier, so `module/spending` owns the `Spending` folder in every project, and the namespace follows the folder under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. The same module names in every layer let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a map. This provision states the five-project form of the module organization that `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` states for the module layout.

**Example:** The `Spending` module owns one folder in every project, and each namespace follows its folder:

```text
Claimwick.Domain/Spending/Expenses/Expense.cs
Claimwick.Application/Spending/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler above lives in namespace `Claimwick.Application.Spending.Expenses.SubmitExpense`.

**Reviewed for:** Which projects miss one folder and namespace per module, or name one outside its module? Look at the module folders and namespaces in every project.

### Use the Project Reference Graph

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-project-reference-graph`

**Strength:** rule

**Statement:** A project reference MUST follow the graph in the table in this section and add no other edge.

**Rationale:** An undeclared reference lets a layer reach past its boundary, and review has no ground to refuse what no graph forbids. The table names every permitted edge, so composition stays in the hosts and each project names only the layers its job needs. A small project merged under `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects` has no Application.Abstractions row; its remaining rows still bind. This provision states the project form of the layer graph that `litespec/workspace/dependencies#follow-the-declared-dependency-graph` declares.

**Example:**

| Project | may reference |
|:---|:---|
| Domain | No application project |
| Application.Abstractions | Domain |
| Application | Domain, Application.Abstractions |
| Infrastructure | Domain, Application.Abstractions, Application |
| WebApi | Application.Abstractions, Application, Infrastructure |
| Worker | Application.Abstractions, Application, Infrastructure |
| AppHost | Deployable host projects through Aspire resource references |
| Test projects | Only production projects and test support needed by their test category |

`WebApi` references Infrastructure for composition in `Program.cs`. Endpoint implementations take their dependencies from Application abstractions, not from Infrastructure repositories, sessions, or provider clients.

**Reviewed for:** Which project files add a reference outside the permitted edges of the graph table? Look at each project file beside the graph table.

### Keep Domain Package-Free

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-domain-package-free`

**Strength:** rule

**Statement:** The Domain project MUST reference no package beyond the .NET base class library.

**Rationale:** A package in Domain makes business rules depend on a framework, a provider, or a messenger. Marten, EF Core, LiteBus, ASP.NET Core, injection, serialization, and logging packages all stay outside, so Domain builds on the base class library alone. This provision states the .NET form of `litespec/workspace/dependencies#keep-domain-dependency-free`.

**Reviewed for:** Which Domain project files reference a package beyond the base class library? Look at the Domain project file.

### Keep Application Dependencies Narrow

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-application-dependencies-narrow`

**Strength:** rule

**Statement:** Application MUST reference only LiteBus command and query abstractions, required read abstractions, and needed Microsoft abstractions, plus the `IQuerySession` read boundary.

**Rationale:** Application coordinates; Infrastructure hides technology. A provider package in Application makes the coordination layer depend on the technology it was meant to stay free of. The Statement names the one permitted read boundary; everything else provider-shaped stays out. This provision states the .NET form of `litespec/workspace/dependencies#keep-application-free-of-provider-packages`.

**Reviewed for:** Which Application project files reference beyond the permitted abstractions and the one read boundary? Look at the Application project file.

### Keep the Test Projects the Size Requires

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-the-test-projects-the-size-requires`

**Strength:** rule

**Statement:** A backend solution MUST hold exactly the test projects its size requires: Architecture and Integration for small, adding Acceptance for large.

**Rationale:** The required suites match the size: Architecture and Integration for small, adding Acceptance for large. Unit test projects stay optional in both sizes, and Acceptance stays optional for small. An Acceptance project without the executable BDD extension is an unnamed baseline project: select the extension, or name the project for what it actually is. This provision states the .NET form of `litespec/backend/testing#keep-the-test-suites-the-size-requires`.

**Example:** A test suite can contain:

```text
{ProjectName}.Architecture.Tests/
{ProjectName}.Integration.Tests/
{ProjectName}.Acceptance.Tests/
```

A small solution holds the first two; Acceptance appears when the executable BDD extension activates.

**Reviewed for:** Which solution trees miss Architecture or Integration test projects, or add Acceptance outside size large? Look at `apps/api/tests/` beside the declared size.

### Keep .NET Production and Test Projects Separate

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-net-production-and-test-projects-separate`

**Strength:** rule

**Statement:** Production projects MUST live under `apps/api/src/` and test projects under `apps/api/tests/`, with the solution directly under `apps/api/`.

**Rationale:** Production under `apps/api/src/` and tests under `apps/api/tests/` make a test dependency reaching production visible in the path itself. The solution sits directly under `apps/api/`, beside both. A workspace AppHost is the one production project outside `src/`, placed by `litespec/backend/dotnet/workspace#place-a-workspace-apphost-beside-the-applications-it-runs`. This provision states the .NET form of `litespec/workspace/structure#keep-production-and-test-code-separate`.

**Example:** The baseline tree is:

```text
apps/api/
  {ProjectName}.slnx
  Directory.Build.props
  Directory.Packages.props
  src/
    {ProjectName}.Domain/
    {ProjectName}.Application.Abstractions/
    {ProjectName}.Application/
    {ProjectName}.Infrastructure/
    {ProjectName}.WebApi/
    {ProjectName}.Worker/             when background work needs a host
    {ProjectName}.AppHost/            when an orchestration host runs the API alone
    {ProjectName}.ServiceDefaults/    when a local orchestration host is used
  tests/
    {ProjectName}.Architecture.Tests/
    {ProjectName}.Integration.Tests/
    {ProjectName}.Acceptance.Tests/   when size is large
    {ProjectName}.Domain.Tests/       optional unit tests
    {ProjectName}.Application.Tests/  optional unit tests
```

Worker is conditional on the extension that runs background work. Acceptance.Tests is required when `size` is large and optional otherwise. Domain.Tests and Application.Tests are optional in both sizes. `AppHost` and `ServiceDefaults` are local hosting and diagnostics projects rather than application layers. An application that orchestrates its local dependencies another way ships neither and records no exception for their absence.

**Reviewed for:** Which production or test project files, or solution files, sit outside their fixed address? Look at `apps/api/`.

## Conventions

### Use Five Application Projects

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects`

**Strength:** default

**Default:** Use Domain, Application.Abstractions, Application, Infrastructure, and WebApi as the five baseline projects at every size, with a small project merging Application.Abstractions into Application and hosts and a Worker project as allowed extras.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** `Application.Abstractions` holds the contracts that more than one module names: the pipeline's own positions, the cross-module ports, the transport-neutral failures, and the published integration events. It references Domain and the required LiteBus module abstractions, and nothing else, so no type in it can name a use case, a handler, or a module.

That refusal is why it is a project rather than a folder. A folder inside Application cannot decline what a developer puts in it, and the folder that tries becomes the place every unplaced type goes. A small project with few cross-module contracts may merge them into Application, trading that protection for one less project.

An implementation never belongs there. The project name states the test, and a type that cannot pass it belongs to a module under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`.

Hosts support local hosting and diagnostics. They are hosts rather than application projects. A Worker project hosts background work under `litespec/backend/architecture#create-a-worker-project-when-background-work-needs-its-own-process`.

**Example:**

```text
apps/api/src/{ProjectName}.Domain/
apps/api/src/{ProjectName}.Application.Abstractions/
apps/api/src/{ProjectName}.Application/
apps/api/src/{ProjectName}.Infrastructure/
apps/api/src/{ProjectName}.WebApi/
```

**Reviewed for:** Which projects sit outside the five baseline projects, the merged small layout, or the allowed hosts and Worker? Look at the solution file beside the declared project size.

### Use This Baseline Package Ownership

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-this-baseline-package-ownership`

**Strength:** default

**Default:** Assign each baseline package to the owning layer named in the table in this section.

**Replacement:** A project names the owning layer of each baseline package in its project documentation.

**Rationale:** Without an owner, a package lands in the project that first needs it and spreads from there. The table assigns each baseline package to its one owning project, so review can refuse it anywhere else. This provision states the .NET form of `litespec/workspace/dependencies#assign-each-baseline-package-to-its-owning-layer`.

**Example:**

| Project | Direct package groups |
|:---|:---|
| Domain | None |
| Application.Abstractions | Required LiteBus module abstractions only |
| Application | Required LiteBus module abstractions; `Marten` for the selected `IQuerySession` read boundary; Microsoft abstractions used by public ports |
| Infrastructure | `Marten`; LiteBus registration modules. Provider, resilience, service discovery, and configuration packages required by implemented adapters |
| WebApi | JWT Bearer [REDACTED], ASP.NET Core OpenAPI, build-time OpenAPI generation, Scalar, and required LiteBus mediator abstractions |
| Worker | LiteBus registration modules; the provider, resilience, and configuration packages its hosted operations need |
| ServiceDefaults | Health checks, service discovery, HTTP resilience, and OpenTelemetry registration and instrumentation |
| AppHost | Aspire AppHost and resource hosting packages |
| Domain.Tests | xUnit v3, assertions, and FsCheck |
| Application.Tests | xUnit v3, assertions, and NSubstitute |
| Integration.Tests | xUnit v3, assertions, test host, Testcontainers PostgreSQL, and the Microsoft.Testing.Platform report and coverage extensions |
| Architecture.Tests | xUnit v3, assertions, and ArchUnitNET |
| Acceptance.Tests | xUnit v3, assertions, the behavior runner its extension selects, test host, and the report and coverage extensions |

Test projects reference xUnit v3 and run on Microsoft.Testing.Platform. The consumer `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`, because the .NET 10 `dotnet test` command refuses the VSTest mode for these projects. Domain.Tests carries FsCheck at the 3.6.0 pin `litespec.manifest.json` states for the property tests that `litespec/backend/testing#prove-every-calculation-with-property-tests` names and the model-based tests that `litespec/backend/testing#prove-every-aggregate-with-a-model-based-test` names.

The example adds a package to the narrowest owning project. A central version entry does not authorize every project to reference that package. Conditional packages such as EF Core, Reqnroll, LightBDD, SignalR, and WireMock are referenced only after their extension activates.

**Reviewed for:** Which baseline package references sit outside the project the ownership table assigns? Look at each package reference beside the ownership table.

### Run LiteSpec.Analyzers in Every Application Project

**ID:** `litespec/backend/dotnet/ext/layer-projects#run-litespecanalyzers-in-every-application-project`

**Strength:** default

**Default:** Reference `LiteSpec.Analyzers` at the manifest pin, with `PrivateAssets="all"`, from every Domain, Application.Abstractions, Application, Infrastructure, and WebApi project.

**Replacement:** A project names the projects its analyzer run covers in its project documentation.

**Rationale:** The analyzers report a breach of a .NET provision in the editor and the build, at the line that breaks it, with a link to the provision. Without them, a breach surfaces only in review, if at all. The package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time. `LS2016` reports a pipeline-owned collaborator in a handler constructor; its default list covers the LiteBus unit of work, idempotency store, and outbox, and a project adds its uniqueness claim service. [Check .NET code with the analyzers](../../../analyzers/check-dotnet-code-with-the-analyzers.md) sets it up, and [Analyzer rules](../../../analyzers/README.md) lists what each rule reports.

**Example:**

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

**Reviewed for:** Which application project files miss `LiteSpec.Analyzers` at the manifest pin with `PrivateAssets="all"`? Look at each application project file.

### Use the Domain Module Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-domain-module-layout`

**Strength:** default

**Default:** Give each aggregate a folder named with its plural root name, holding its states, events, exceptions, and concepts in the files this provision names.

**Replacement:** A project names its Domain module layout in its Domain documentation.

**Rationale:** Each aggregate folder holds everything one aggregate owns in the files the layout names, so a reader finds the full aggregate in one place. Namespaces follow the folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`, and each file sits at the lowest folder holding its consumers under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. This provision states the .NET form of `litespec/backend/domain#organize-a-module-by-aggregate-and-concept`.

**Example:** The Domain module layout is:

```text
{ProjectName}.Domain/
  Shared/
    AggregateRoot.cs
    Entity.cs
    IAggregateRoot.cs
    IDomainEvent.cs
    StronglyTypedIds/
      IStronglyTypedId.cs
    Exceptions/
      DomainException.cs
  Posts/                          one aggregate whose name matches the module: kept flat in the module folder
    Post.cs
    PostId.cs
    PostTitle.cs
    IPostRepository.cs
    States/
      PostState.cs
      PostDraftState.cs
      PostPublishedState.cs
      PostArchivedState.cs
    Events/
      PostCreatedEvent.cs
      PostPublishedEvent.cs
    Exceptions/
      PostIdentityRequiredException.cs
      PostAlreadyPublishedException.cs
  Shipping/                       one aggregate whose name differs from the module, with a domain union in a concept folder
    Shipments/
      Shipment.cs
      ShipmentId.cs
      IShipmentRepository.cs
      Outcomes/
        ShipmentOutcome.cs
        ShipmentDeliveredOutcome.cs
        ShipmentReturnedOutcome.cs
        ShipmentLostOutcome.cs
      States/
        ShipmentState.cs
        ShipmentPendingState.cs
        ShipmentDispatchedState.cs
        ShipmentDeliveredState.cs
      Events/
        ShipmentOutcomeRecordedEvent.cs
  Customers/                      more than one aggregate: one plural folder per aggregate
    CustomerAccounts/               namespace Shop.Domain.Customers.CustomerAccounts
      CustomerAccount.cs
      CustomerAccountId.cs
      ICustomerAccountRepository.cs
      States/
        CustomerAccountState.cs
        CustomerAccountClaimedState.cs
      Events/
        CustomerAccountRestrictedEvent.cs
    Consents/                       namespace Shop.Domain.Customers.Consents
      Consent.cs
      ConsentId.cs
      IConsentRepository.cs
      States/
        ConsentState.cs
        ConsentGrantedState.cs
      Events/
        ConsentGrantedEvent.cs
```

Each aggregate-specific repository interface stays with the aggregate it loads. Application, Infrastructure, and WebApi use the same module and per-aggregate folder names, per `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`.

**Reviewed for:** Which Domain module folders break the one plural folder per aggregate with states, events, exceptions, and concepts? Look at each module folder.

### Use the Operation Folder Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-operation-folder-layout`

**Strength:** default

**Default:** Place each operation in a folder named for its use case under its module and aggregate, with the files this provision names.

**Replacement:** A project names its operation layout in its Application documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Type names derive from the use-case identifier with the architectural suffix, so `use-case/spending.submit-expense` owns `SubmitExpenseCommand` under `litespec/workspace/naming#use-architectural-suffixes`. Only a use case or a handled fact gets a folder: any other type sits flat in the folder that owns it under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`, and a handled fact's folder takes the `On{Fact}` name with its marked handler classes. Each result type carries one case per result, and each refusal carries the refusal `code` from its result record. A query result type carries one member per declared `include` group, and each result part is named after its result under `litespec/workspace/naming#name-result-parts-after-their-result`.

**Example:** The operation layout is:

```text
{ProjectName}.Application.Abstractions/     contracts more than one module names
  Clock/
    IClock.cs
  Failures/
    ResourceNotFoundException.cs
    UseCaseForbiddenException.cs
    UseCaseConflictException.cs
  Validation/
    ValidationErrors.cs
{ProjectName}.Application/
  Pipeline/                         the pipeline's own stages, and nothing else
    ApplicationPipeline.cs
    AuthorizationGuard.cs
  Posts/                            single aggregate whose name matches the module: operations sit directly under the module
    IPostArchive.cs                 a port several Posts use cases name
    CreateDraft/
      CreateDraftCommand.cs
      CreateDraftCommandResult.cs
      CreateDraftCommandValidator.cs
      CreateDraftCommandHandler.cs
    GetPost/
      GetPostQuery.cs
      GetPostQueryResult.cs
      GetPostQueryValidator.cs
      GetPostQueryHandler.cs
    ListPosts/
      ListPostsQuery.cs
      ListPostsQueryResult.cs
      ListPostsQueryResultItem.cs
      ListPostsQueryValidator.cs
      ListPostsQueryHandler.cs
    OnPostPublished/   event handlers of one fact: one marked class per reaction
      NotifySubscribersOnPostPublishedHandler.cs
      IPostPublicationNotifier.cs   a port one event handler names
  Customers/                        two aggregates: operations nest under each aggregate
    CustomerAccounts/
      CustomerAccountScopeLookup.cs aggregate-owned, so it sits with the aggregate
      RestrictAccount/
        RestrictAccountCommand.cs
        RestrictAccountCommandHandler.cs
    Consents/
      GrantConsent/
        GrantConsentCommand.cs
        GrantConsentCommandHandler.cs
```

Module and aggregate order, including the single-aggregate flat form, follow `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`.

**Reviewed for:** Which operation folders miss message, result, validator, or handler files named for the use case, or which event handlers escape their `On{Fact}` folder? Look at each operation folder.

### Use the Endpoint Folder Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-endpoint-folder-layout`

**Strength:** default

**Default:** Place each endpoint in a folder named for its use case under its module and aggregate, with the files this provision names.

**Replacement:** A project names its endpoint layout in its WebApi documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Transport types take the names their boundary roles assign under `litespec/backend/api#name-transport-types-by-their-boundary-role`, and transport mapping lives in WebApi under `litespec/backend/application#keep-mappings-at-the-owning-boundary`. Response models carry the query's declared `include` groups. Module nesting, including the single-aggregate flat form, follows `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`.

**Example:** The endpoint layout is:

```text
{ProjectName}.WebApi/
  Endpoints/
    Posts/                          single aggregate whose name matches the module
      CreateDraft/
        CreateDraftEndpoint.cs
        CreateDraftRequestModel.cs
        CreateDraftResponseModel.cs
        CreateDraftApiMappings.cs
      GetPost/
        GetPostEndpoint.cs
        GetPostResponseModel.cs
        GetPostApiMappings.cs
    Customers/                      two aggregates: operations nest under each aggregate
      CustomerAccounts/
        RestrictAccount/
          RestrictAccountEndpoint.cs
          RestrictAccountRequestModel.cs
          RestrictAccountApiMappings.cs
      Consents/
        GrantConsent/
          GrantConsentEndpoint.cs
          GrantConsentRequestModel.cs
          GrantConsentApiMappings.cs
  Errors/
    GlobalExceptionHandler.cs
    ProblemDetailsApiMappings.cs
  Security/
    CurrentActor.cs
  OpenApi/
  Program.cs
```

**Reviewed for:** Which endpoint folders miss the endpoint, request and response models, or mappings named for the use case? Look at each endpoint folder.

### Use This Infrastructure Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-this-infrastructure-layout`

**Strength:** default

**Default:** Place Marten configuration, repositories, commit behavior, and serialization under one Infrastructure persistence folder.

**Replacement:** A project names the folder holding its persistence types in its project documentation.

**Rationale:** One persistence folder holds Marten configuration, repositories, commit behavior, and serialization, so every persistence type has one discoverable home. Scattered configuration hides the provider setup across the project. This provision states the .NET form of `litespec/backend/persistence#use-one-persistence-folder`.

**Example:**

```text
{ProjectName}.Infrastructure/
  Persistence/
    Marten/
      MartenServiceRegistration.cs
      MartenStoreConfiguration.cs
      Commands/
        SaveChangesCommandPostHandler.cs
      Events/
        DomainEventBuffer.cs
      Posts/                        single aggregate whose name matches the module: files directly under the module
        PostRepository.cs
        PostMartenConfiguration.cs
      Customers/                    two aggregates: one folder per aggregate
        CustomerAccounts/
          CustomerAccountRepository.cs
          CustomerAccountMartenConfiguration.cs
        Consents/
          ConsentRepository.cs
          ConsentMartenConfiguration.cs
```

The module folders follow `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`. A single-aggregate module stays flat when its aggregate root's plural name equals the module name. For example, `Post` stays flat under `Posts`. Otherwise, each aggregate takes its own folder.

Aggregate-specific configuration stays beside its aggregate, while session and commit plumbing stay under the Marten root.

**Reviewed for:** Which Marten configuration, repository, commit, or serialization files sit outside the one Infrastructure persistence folder? Look at the persistence files.

## Dependencies

Packages: None.

Related extensions: the [EF Core Persistence](efcore.md), for aggregates needing relational storage.

Guides: None.
