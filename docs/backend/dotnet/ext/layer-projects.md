# Layer Projects

**Stack:** dotnet

**Concern:** application

This extension packages each backend layer as its own .NET project: Domain, Application.Abstractions, Application, Infrastructure, and WebApi, with hosts beside them. It states the project, reference, package, and layout rules for that five-project form. It fits backends already on five projects and teams that want the compiler to enforce Domain purity through project references. Without it, the module layout applies.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The consumer selects `layer-projects` when its backend already packages each layer as its own project, or when the team wants the compiler to enforce Domain purity through project references. Without this extension, the module layout applies.

## Baseline Relationship

For a project selecting this extension, this extension replaces:

- `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`: one folder per module in every project replaces the one module tree.
- `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`: five layer projects replace the Kernel, Platform, and module projects with their hosts.
- `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`: host endpoints without a handler live in the WebApi project instead of beside the Kernel, Platform, and module projects.
- `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`: one folder and namespace per module in every project replaces the one namespace tree.
- `litespec/workspace/dependencies#follow-the-declared-dependency-graph`: project references between the five projects replace references from the hosts to the Kernel, Platform, and module projects.
- `litespec/workspace/dependencies#keep-domain-dependency-free`: a Domain project with no package references replaces analyzer-checked purity of the Domain layer folders.
- `litespec/workspace/dependencies#keep-application-free-of-provider-packages`: an Application project with narrow references replaces the same boundary on the Application layer folders.
- `litespec/workspace/dependencies#assign-each-baseline-package-to-its-owning-layer`: package ownership by project replaces ownership by layer folder.
- `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`: the analyzers run in every layer project instead of in the Kernel, Platform, and module projects and the hosts.
- `litespec/backend/domain#organize-a-module-by-aggregate-and-concept`: aggregate folders in the Domain project replace aggregate layer folders in the module tree.
- `litespec/backend/application#use-this-use-case-layout`: operation folders in the Application project replace operation files in the `UseCases/` use case folders.
- `litespec/backend/api#use-this-endpoint-layout`: endpoint folders in the WebApi project replace endpoint files in the use case folders.
- `litespec/backend/persistence#use-one-persistence-folder`: the Infrastructure persistence folder replaces the platform and module Infrastructure folders.
- `litespec/backend/testing#keep-the-test-suites-the-size-requires`: Domain.Tests and Application.Tests beside Architecture, Integration, and Acceptance replace the one unit test project beside those suites.
- `litespec/workspace/structure#keep-production-and-test-code-separate`: the five-project source tree replaces the Kernel, Platform, and module projects beside their hosts.

Every other baseline provision binds unchanged.

This extension switches on no optional record type.

## Standards

### Keep One Application Assembly

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-one-application-assembly`

**Strength:** required

**Statement:** An application MUST place commands, queries, results, validators, handlers, event handler classes, job classes, and module-owned ports in one Application project, and cross-module contracts in Application.Abstractions, or in Application where the project merges them under `litespec/backend/dotnet/ext/layer-projects#package-each-layer-as-its-own-project`.

**Rationale:** One assembly holds commands, queries, and their handlers because the stack never deploys them separately. Splitting Application into Write, Read, Contracts, and event-handler assemblies would add a deployment boundary the stack never uses, and every use case would need a project reference to cross it. The separation this stack keeps is the one [Fowler describes as CQRS](https://www.martinfowler.com/bliki/CQRS.html). The command and query models differ, while one assembly holds them. Event handlers and jobs live with the use case they run inside the Application project, in that use case's operation folder. One that runs none lives in the module `Notifications/` folder of the notification it sends or the `ExternalSystems/` folder of the system it calls. Job scheduling stays in Worker or Infrastructure with the scheduling concerns, under `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request`.

**Reviewed for:** Does every command, query, result, validator, handler, event handler, and job file sit in the Application project, with cross-module contracts in Application.Abstractions or the merged Application? Look at the Application project files beside the shared contracts.

### Mirror Modules in Folders and Namespaces

**ID:** `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`

**Strength:** required

**Statement:** Every project MUST give each module one folder and namespace.

**Rationale:** The folder name derives from the module identifier, so `module/spending` owns the `Spending` folder in every project. The namespace follows the folder under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. The same module names in every layer let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a separate lookup. This provision states the five-project form of the module organization that `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` states for the module layout.

**Example:** The `Spending` module owns one folder in every project, and each namespace follows its folder:

```text
Claimwick.Domain/Spending/Expenses/Expense.cs
Claimwick.Application/Spending/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler above lives in namespace `Claimwick.Application.Spending.Expenses.SubmitExpense`.

**Reviewed for:** Does every project give each module one folder and namespace under its module name? Look at the module folders and namespaces in every project.

### Use the Project Reference Graph

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-project-reference-graph`

**Strength:** required

**Statement:** A project reference MUST follow the graph in the table in this section and add no other edge.

**Rationale:** An undeclared reference lets a layer reach past its boundary. The table names every permitted edge, so composition stays in the hosts, each project names only the layers its job needs, and review has a written graph to hold new references against. A small project merged under `litespec/backend/dotnet/ext/layer-projects#package-each-layer-as-its-own-project` has no Application.Abstractions row. Its remaining rows still bind. This provision states the project form of the layer graph that `litespec/workspace/dependencies#follow-the-declared-dependency-graph` declares.

**Example:**

| Project | may reference |
|:---|:---|
| Domain | None |
| Application.Abstractions | Domain |
| Application | Domain, Application.Abstractions |
| Infrastructure | Domain, Application.Abstractions, Application |
| WebApi | Application.Abstractions, Application, Infrastructure |
| Worker | Application.Abstractions, Application, Infrastructure |
| AppHost | Deployable host projects through Aspire resource references |
| Test projects | Only production projects and test support needed by their test category |

`WebApi` references Infrastructure for composition in `Program.cs`. Endpoint implementations take their dependencies from Application abstractions, not from Infrastructure repositories, sessions, or provider clients.

**Reviewed for:** Does every project file reference only the permitted edges of the graph table? Look at each project file beside the graph table.

### Keep Domain Package-Free

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-domain-package-free`

**Strength:** required

**Statement:** The Domain project MUST reference no package beyond the .NET base class library.

**Rationale:** A package in Domain makes business rules depend on a framework, a provider, or a messenger. Marten, LiteBus, ASP.NET Core, injection, serialization, and logging packages all stay outside, so Domain builds on the base class library alone. This provision states the .NET form of `litespec/workspace/dependencies#keep-domain-dependency-free`.

**Reviewed for:** Does the Domain project file reference any package beyond the base class library? Look at the Domain project file.

### Keep Application Dependencies Narrow

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-application-dependencies-narrow`

**Strength:** required

**Statement:** Application MUST reference only LiteBus command and query abstractions, required read abstractions, and needed Microsoft abstractions, plus the `IQuerySession` read boundary.

**Rationale:** Application coordinates work. Infrastructure hides technology. A provider package in Application makes the coordination layer depend on the technology it was meant to stay free of. The Statement names the one permitted read boundary. Everything else provider-shaped stays out. This provision states the .NET form of `litespec/workspace/dependencies#keep-application-free-of-provider-packages`.

**Reviewed for:** Does the Application project file reference anything beyond the permitted abstractions and the one read boundary? Look at the Application project file.

### Keep the Test Projects the Size Requires

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-the-test-projects-the-size-requires`

**Strength:** required

**Statement:** A backend solution MUST hold exactly the test projects its size requires: Architecture and Integration for small, adding Acceptance for large.

**Rationale:** The required suites match the size: Architecture and Integration for small, adding Acceptance for large. Unit test projects stay optional in both sizes, and Acceptance stays optional for small. An Acceptance project without the executable BDD extension is an unnamed baseline project. Select the extension, or name the project for what it actually is. This provision states the .NET form of `litespec/backend/testing#keep-the-test-suites-the-size-requires`.

**Example:** A test suite can contain:

```text
{ProjectName}.Architecture.Tests/
{ProjectName}.Integration.Tests/
{ProjectName}.Acceptance.Tests/
```

A small solution holds the first two. Acceptance appears when the executable BDD extension activates.

**Reviewed for:** Does the solution tree hold Architecture and Integration test projects, with Acceptance only where size large or the BDD extension requires it? Look at `apps/api/tests/` beside the declared size.

### Keep .NET Production and Test Projects Separate

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-net-production-and-test-projects-separate`

**Strength:** required

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

Worker is conditional on the extension that runs background work. Acceptance.Tests is required when `size` is large and optional otherwise. Domain.Tests and Application.Tests are optional in both sizes. `AppHost` and `ServiceDefaults` are local hosting and diagnostics projects rather than application layers. An application that orchestrates its local dependencies another way ships neither and records no waiver for their absence.

**Reviewed for:** Does every production project, test project, and solution file sit at its fixed address? Look at `apps/api/`.

## Conventions

### Package Each Layer as Its Own Project

**ID:** `litespec/backend/dotnet/ext/layer-projects#package-each-layer-as-its-own-project`

**Strength:** default

**Default:** Use Domain, Application.Abstractions, Application, Infrastructure, and WebApi as the five baseline projects at every size, with a small project merging Application.Abstractions into Application and hosts and a Worker project as allowed extras.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** `Application.Abstractions` holds the contracts that more than one module names: the pipeline's own positions, the cross-module ports, the transport-neutral failures, and the published module and integration events. It references Domain and the required LiteBus module abstractions, and nothing else. No type in it can name a use case, a handler, or a module.

That restriction is why shared contracts get a project rather than a folder. A folder inside Application cannot decline what a developer puts in it, so the folder meant for shared contracts collects every unplaced type. A small project with few cross-module contracts may merge them into Application, trading that compiler-enforced separation for one less project.

The project name states the admission test. Anything that is not a shared abstraction fails it. An implementation never belongs there. A type that cannot pass the test belongs to a module under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`.

Hosts support local hosting and diagnostics. They are hosts rather than layer projects. A Worker project hosts background work under `litespec/backend/architecture#create-a-worker-project-when-background-work-needs-its-own-process`.

**Example:**

```text
apps/api/src/{ProjectName}.Domain/
apps/api/src/{ProjectName}.Application.Abstractions/
apps/api/src/{ProjectName}.Application/
apps/api/src/{ProjectName}.Infrastructure/
apps/api/src/{ProjectName}.WebApi/
```

**Reviewed for:** Does every project fit the five baseline projects, the merged small layout, or the allowed hosts and Worker? Look at the solution file beside the declared project size.

### Assign Each Baseline Package to Its Owning Project

**ID:** `litespec/backend/dotnet/ext/layer-projects#assign-each-baseline-package-to-its-owning-project`

**Strength:** default

**Default:** Assign each baseline package to the owning layer named in the table in this section.

**Replacement:** A project names the owning layer of each baseline package in its project documentation.

**Rationale:** Without an owner, a package lands in the project that first needs it and spreads from there. The table assigns each baseline package to its one owning project. The narrowest project holds each package, and review can refuse it anywhere else. This provision states the .NET form of `litespec/workspace/dependencies#assign-each-baseline-package-to-its-owning-layer`.

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
| Application.Tests | xUnit v3, assertions, and NSubstitute for outside providers and the clock |
| Integration.Tests | xUnit v3, assertions, test host, Testcontainers PostgreSQL, and the Microsoft.Testing.Platform report and coverage extensions |
| Architecture.Tests | xUnit v3 and assertions |
| Acceptance.Tests | xUnit v3, assertions, the behavior runner its extension selects, test host, and the report and coverage extensions |

Application.Tests run each handler against the real repositories on the test database. A test substitutes only the outside providers and the clock, as `litespec/quality/testing#use-one-mock-rule-for-every-stack` requires.

Test projects reference xUnit v3 and run on Microsoft.Testing.Platform. The consumer `global.json` sets `"test": { "runner": "Microsoft.Testing.Platform" }`, because the .NET 10 `dotnet test` command refuses the VSTest mode for these projects. Domain.Tests carries FsCheck at the 3.6.0 pin `litespec.manifest.yaml` states for the property tests that `litespec/backend/testing#prove-every-calculation-with-property-tests` names and the model-based tests that `litespec/backend/testing#prove-every-aggregate-with-a-model-based-test` names.

The example adds a package to the narrowest owning project. A central version entry does not authorize every project to reference that package. Conditional packages such as LightBDD, SignalR, and WireMock are referenced only after their extension activates. The manifest pins no architecture-test library. A project that wants one adds it with owner approval.

**Reviewed for:** Does every baseline package reference sit in the project the ownership table assigns? Look at each package reference beside the ownership table.

### Run the LiteSpec Analyzers in Every Layer Project

**ID:** `litespec/backend/dotnet/ext/layer-projects#run-the-litespec-analyzers-in-every-layer-project`

**Strength:** default

**Default:** Reference `LiteSpec.Analyzers` at the manifest pin, with `PrivateAssets="all"`, from every Domain, Application.Abstractions, Application, Infrastructure, and WebApi project.

**Replacement:** A project names the projects its analyzer run covers in its project documentation.

**Rationale:** The analyzers report a breach of a .NET provision in the editor and the build, at the line that breaks it, with a link to the provision. Without them, a breach surfaces only in review, if at all. The package is a development dependency. It adds nothing to the built assemblies, so Domain stays package-free at run time. `LS2016` reports a pipeline-owned collaborator in a handler constructor. Its default list covers the LiteBus unit of work, idempotency store, and outbox. A project adds its uniqueness claim service. [Check .NET code with the analyzers](../../../analyzers/check-dotnet-code-with-the-analyzers.md) sets it up. [Analyzer rules](../../../analyzers/README.md) lists what each rule reports.

**Example:**

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

**Reviewed for:** Does every layer project file reference `LiteSpec.Analyzers` at the manifest pin with `PrivateAssets="all"`? Look at each layer project file.

### Use the Domain Module Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-domain-module-layout`

**Strength:** default

**Default:** Give each aggregate a folder named with its plural root name, holding its states, events, exceptions, and concepts in the files this provision names.

**Replacement:** A project names its Domain module layout in its Domain documentation.

**Rationale:** Each aggregate folder holds everything one aggregate owns in the files the layout names, so a reader finds the full aggregate in one place. Namespaces follow the folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. Each file sits at the lowest folder holding its consumers under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. This provision states the .NET form of `litespec/backend/domain#organize-a-module-by-aggregate-and-concept`.

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

**Reviewed for:** Does every Domain module folder hold one plural folder per aggregate with states, events, exceptions, and concepts? Look at each module folder.

### Use the Operation Folder Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-operation-folder-layout`

**Strength:** default

**Default:** Place each operation in a folder named for its use case under its module and aggregate, with the files this provision names.

**Replacement:** A project names its operation layout in its Application documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Type names derive from the use-case identifier with the architectural suffix, so `use-case/spending.submit-expense` owns `SubmitExpenseCommand` under `litespec/workspace/naming#use-architectural-suffixes`. Only a use case gets an operation folder. Any other type sits flat in the folder that owns it under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. An event handler or job that runs a use case sits in that use case's operation folder. One that runs none sits in the module `Notifications/` or `ExternalSystems/` folder named for what it sends or calls, under `litespec/workspace/naming#place-each-trigger-with-what-it-runs`. Each result type carries one case per result. Each refusal carries the refusal `code` from its result record. A query result type carries one member per declared `include` group. Each result part is named after its result under `litespec/workspace/naming#name-result-parts-after-their-result`.

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
      CreateDraftOnAuthorRegisteredHandler.cs   the reaction that runs this use case
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
    Notifications/
      SubscriberDigest/             a reaction that only sends this notification
        SendDigestOnPostPublishedHandler.cs
    ExternalSystems/
      SearchIndex/                  a reaction that only calls this system
        IndexPostOnPostPublishedHandler.cs
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

**Reviewed for:** Does every operation folder hold message, result, validator, and handler files named for the use case, with event handlers and jobs in the folder of what they run? Look at each operation folder.

### Use the Endpoint Folder Layout

**ID:** `litespec/backend/dotnet/ext/layer-projects#use-the-endpoint-folder-layout`

**Strength:** default

**Default:** Place each endpoint in a folder named for its use case under its module and aggregate, with the files this provision names.

**Replacement:** A project names its endpoint layout in its WebApi documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Transport types take the names their boundary roles assign under `litespec/backend/api#name-transport-types-by-their-boundary-role`. Transport mapping lives in WebApi under `litespec/backend/application#keep-mappings-at-the-owning-boundary`. Response models carry the query's declared `include` groups. Module nesting, including the single-aggregate flat form, follows `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`.

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

**Reviewed for:** Does every endpoint folder hold the endpoint, request and response models, and mappings named for the use case? Look at each endpoint folder.

### Keep Persistence in One Infrastructure Folder

**ID:** `litespec/backend/dotnet/ext/layer-projects#keep-persistence-in-one-infrastructure-folder`

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
      Commit/
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

Aggregate-specific configuration stays beside its aggregate. Session and commit plumbing stay under the Marten root.

**Reviewed for:** Does every Marten configuration, repository, commit, and serialization file sit in the one Infrastructure persistence folder? Look at the persistence files.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
