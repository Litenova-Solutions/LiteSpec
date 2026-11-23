# LiteSpec.Analyzers

## Audience

Developers who add the LiteSpec analyzers to a .NET application, and who read why a diagnostic appears on a line.

## What It Is

These Roslyn analyzers check a .NET application against the [LiteSpec](https://litespec.dev) standard. Each error names the provision it enforces and links to it.

## Quick Start

Add the package to each application project, or once in `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="7.0.0" PrivateAssets="all" />
</ItemGroup>
```

The package requires the .NET 9.0.300 SDK, Visual Studio 17.14, or later.

## Layout

### How a File Gets Its Layer

Each rule runs only on the files in the layers its provision covers. A file takes its layer from the nearest layer folder (`Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, with `Contracts/` counting as `Application`): a file's layer is its top folder in the module, and its use case is the next folder. A file in a sibling `{Product}.{Module}.Contracts` project counts as `Application` and maps to its owner module. A file with no layer folder on its path falls back to its role suffix: `Endpoint`, `RequestModel`, and `ResponseModel` mean `WebApi`, while `Command`, `Query`, `Result`, `Handler`, `Validator`, `Job`, and `Worker` mean `Application`. A module project names no layer, so each of its files takes its layer from its folder. A Kernel project counts as `Domain` and a Platform project as `Infrastructure`; a host project counts as `WebApi` or `Worker` by its assembly name (`Shop.Api` reads as `WebApi` beside `Shop.WebApi`), and a test project counts as `Test`.

A file the rules cannot place stays quiet.

### Rules

| ID | Layer | What it reports | Provision |
|:---|:---|:---|:---|
| LS1001 | Domain | A persistence, web, messaging, mediator, logging, configuration, injection, or serialization type, or an Application, Ports, Infrastructure, or WebApi type | [Keep Domain free of outer-layer concerns](https://litespec.dev/7.0.0/backend/dotnet/domain/#keep-domain-free-of-outer-layer-concerns) |
| LS1002 | Domain | An enum | [Model every closed set of domain values without enums](https://litespec.dev/7.0.0/backend/dotnet/domain/#model-every-closed-set-of-domain-values-without-enums) |
| LS1003 | Domain | An aggregate with no `State` property typed as an abstract record | [Model every aggregate lifecycle with state records](https://litespec.dev/7.0.0/backend/dotnet/domain/#model-every-aggregate-lifecycle-with-state-records) |
| LS1004 | Domain | A state case record that is not sealed | [Model every aggregate lifecycle with state records](https://litespec.dev/7.0.0/backend/dotnet/domain/#model-every-aggregate-lifecycle-with-state-records) |
| LS1005 | Domain | A thrown exception that is not a `DomainException` or an argument guard | [Reject business violations with Domain exceptions](https://litespec.dev/7.0.0/backend/dotnet/domain/#reject-business-violations-with-domain-exceptions) |
| LS1006 | Domain | A `DomainException` subclass that is not sealed or not named `...Exception` | [Reject business violations with Domain exceptions](https://litespec.dev/7.0.0/backend/dotnet/domain/#reject-business-violations-with-domain-exceptions) |
| LS1007 | Domain | A domain event that is not an immutable record named `...Event` | [Raise immutable domain facts](https://litespec.dev/7.0.0/backend/dotnet/domain/#raise-immutable-domain-facts) |
| LS1008 | Domain | A `double` or `float` | [Keep binary floating point out of exact quantities](https://litespec.dev/7.0.0/backend/domain/#keep-binary-floating-point-out-of-exact-quantities) |
| LS1009 | Domain | A read of the clock, randomness, or a new identifier | [Pass nondeterministic values into Domain](https://litespec.dev/7.0.0/backend/domain/#pass-nondeterministic-values-into-domain) |
| LS1010 | Domain | A repository that exposes `IQueryable`, a session, or a generic surface | [Keep repository interfaces in Domain](https://litespec.dev/7.0.0/backend/dotnet/domain/#keep-repository-interfaces-in-domain) |
| LS1011 | Domain | A public type or member without XML documentation | [Document every public Domain contract](https://litespec.dev/7.0.0/backend/dotnet/domain/#document-every-public-domain-contract) |
| LS1012 | Domain | An identifier that is not a `readonly record struct` wrapping one `Guid` | [Use strongly typed version 7 identifiers](https://litespec.dev/7.0.0/backend/dotnet/domain/#use-strongly-typed-version-7-identifiers) |
| LS1013 | Domain | A switch that leaves a sealed case of a state or closed-set record hierarchy to its discard arm | [Model every aggregate lifecycle with state records](https://litespec.dev/7.0.0/backend/dotnet/domain/#model-every-aggregate-lifecycle-with-state-records) |
| LS1014 | Every layer | A file outside an aggregate's `Domain/` folder constructing a type from that folder | [Construct aggregate parts only inside the root](https://litespec.dev/7.0.0/backend/dotnet/domain/#construct-aggregate-parts-only-inside-the-root) |
| LS1015 | Domain | A non-root aggregate type with a public setter, constructor, or mutable collection | [Model child entities as sealed classes](https://litespec.dev/7.0.0/backend/dotnet/domain/#model-child-entities-as-sealed-classes) |
| LS1016 | Domain | An aggregate property typed as another aggregate's root or entity | [Reference other aggregates by typed identifier](https://litespec.dev/7.0.0/backend/dotnet/domain/#reference-other-aggregates-by-typed-identifier) |
| LS1017 | Domain | A typed identifier or value type whose constructor throws and which offers no non-throwing parse | [Offer a non-throwing parse on validated value types](https://litespec.dev/7.0.0/backend/dotnet/domain/#offer-a-non-throwing-parse-on-validated-value-types) |
| LS2001 | Application, Infrastructure, WebApi, Worker | A handler, validator, endpoint, or persistence implementation that is not `internal sealed` | [Keep implementation types internal](https://litespec.dev/7.0.0/backend/dotnet/architecture/#keep-implementation-types-internal) |
| LS2003 | WebApi | An MVC controller | [Exclude MVC controllers from the stack](https://litespec.dev/7.0.0/backend/dotnet/api/#exclude-mvc-controllers-from-the-stack) |
| LS2004 | Application, Infrastructure, Worker | A command handler that writes through a document session or `DbContext` | [Stage aggregate writes through repositories](https://litespec.dev/7.0.0/backend/dotnet/persistence/#stage-aggregate-writes-through-repositories) |
| LS2005 | Application, Infrastructure, Worker | A command or query handler whose name lacks its role suffix | [Use architectural suffixes](https://litespec.dev/7.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2006 | WebApi | An endpoint whose name does not end in `Endpoint` | [Use architectural suffixes](https://litespec.dev/7.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2007 | Application, Infrastructure, Worker | A reaction or job whose name hides its kind | [Use architectural suffixes](https://litespec.dev/7.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2009 | Application, Infrastructure, Worker | A result part not named after its `{UseCase}QueryResult` or `{UseCase}CommandResult` | [Name result parts after their result](https://litespec.dev/7.0.0/workspace/naming/#name-result-parts-after-their-result) |
| LS2010 | Application, Infrastructure, Worker | A repository parameter, field, or local not named after its interface | [Name repository variables after their interface](https://litespec.dev/7.0.0/workspace/naming/#name-repository-variables-after-their-interface) |
| LS2011 | Application, Infrastructure, Worker | A query handler that takes a repository | [Project queries directly](https://litespec.dev/7.0.0/backend/application/#project-queries-directly) |
| LS2012 | Application, Infrastructure, Worker | A handler that throws a forbidden refusal | [Enforce target authorization in the pipeline](https://litespec.dev/7.0.0/backend/application/#enforce-target-authorization-in-the-pipeline) |
| LS2013 | Application, Infrastructure, Worker | An audit value read from the message in the handler | [Declare the audit target and reason beside the message](https://litespec.dev/7.0.0/backend/application/#declare-the-audit-target-and-reason-beside-the-message) |
| LS2014 | Application, Infrastructure, Worker | A command handler that finds or creates | [Implement one outcome path per command](https://litespec.dev/7.0.0/backend/application/#implement-one-outcome-path-per-command) |
| LS2015 | Application, Infrastructure, Worker | A command handler that changes records in a loop | [Issue per-record work from a job](https://litespec.dev/7.0.0/backend/application/#issue-per-record-work-from-a-job) |
| LS2016 | Application, Infrastructure, Worker | A handler that takes a pipeline-owned collaborator | [Keep uniqueness out of handlers](https://litespec.dev/7.0.0/backend/persistence/#keep-uniqueness-out-of-handlers) |
| LS2018 | Application, Ports, WebApi | A type outside the file's layer: Infrastructure or WebApi from Application, Application, Infrastructure, or WebApi from Ports, ASP.NET Core or HttpContext from Application or Ports, or a Domain type, Ports type, or repository from WebApi | [Follow layer direction in every file](https://litespec.dev/7.0.0/backend/dotnet/architecture/#follow-layer-direction-in-every-file) |
| LS2019 | Every layer | A type declared in another use case folder, the message and result of another use case included | [Keep use case folders isolated](https://litespec.dev/7.0.0/backend/dotnet/architecture/#keep-use-case-folders-isolated) |
| LS2020 | Every layer | A type of another module declared outside its Contracts folder | [Reach another module only through Contracts](https://litespec.dev/7.0.0/backend/dotnet/architecture/#reach-another-module-only-through-contracts) |
| LS2021 | Domain, Application, Infrastructure | A command mediator held outside a reaction or a job, a query mediator held outside a handler, reaction, or job, or an event mediator held outside the platform | [Keep command handlers to load, call, and stage](https://litespec.dev/7.0.0/backend/application/#keep-command-handlers-to-load-call-and-stage) |
| LS2022 | Every layer | User code naming the event mediator or an event publisher | [Emit declared integration events after commit](https://litespec.dev/7.0.0/backend/application/#stage-declared-module-and-integration-events-in-the-commit) |
| LS2023 | Every layer | A class implementing two message definition contracts | [Declare each pipeline concern in its own class](https://litespec.dev/7.0.0/workspace/design/#declare-each-pipeline-concern-in-its-own-class) |
| LS2024 | Every layer | Serializer, store, guard, or converter code inside a module | [Plug modules into the platform through contributions](https://litespec.dev/7.0.0/backend/dotnet/architecture/#plug-modules-into-the-platform-through-contributions) |
| LS2025 | Infrastructure | An `Infrastructure/{Port}/` adapter naming a Domain type outside identifiers, shared values and its ports' signatures, or a WebApi type | [Follow layer direction in every file](https://litespec.dev/7.0.0/backend/dotnet/architecture/#follow-layer-direction-in-every-file) |
| LS2026 | Every layer | A record declared with the same members in two use case folders | [Keep use case folders isolated](https://litespec.dev/7.0.0/backend/dotnet/architecture/#keep-use-case-folders-isolated) |
| LS2027 | Application | Archive, serialization, or document work in Application | [Produce file formats in Infrastructure](https://litespec.dev/7.0.0/backend/architecture/#produce-file-formats-in-infrastructure) |
| LS2028 | Every layer | A public module type outside its Contracts project and the entry type | [Expose only Contracts and the entry point](https://litespec.dev/7.0.0/backend/dotnet/architecture/#expose-only-contracts-and-the-entry-point) |
| LS2032 | Application, Infrastructure, Worker | A `Contains` over typed identifiers inside a queryable expression | [Compare typed identifiers by their value in queries](https://litespec.dev/7.0.0/backend/dotnet/persistence/#compare-typed-identifiers-by-their-value-in-queries) |
| LS2033 | Application, Infrastructure, Worker | A `LoadMany` of documents identified by a typed identifier | [Load typed identifier documents one at a time](https://litespec.dev/7.0.0/backend/dotnet/persistence/#load-typed-identifier-documents-one-at-a-time) |
| LS2034 | Application, Infrastructure, Worker | A mediator call in an event handler that names no mediation settings | [Pass explicit mediation settings from event handlers](https://litespec.dev/7.0.0/backend/dotnet/application/#pass-explicit-mediation-settings-from-event-handlers) |
| LS2035 | Application, Infrastructure | A generic exception thrown for a null check after a repository or session load | [Refuse an unknown record as not found](https://litespec.dev/7.0.0/backend/dotnet/application/#refuse-an-unknown-record-as-not-found) |
| LS2036 | Application, Ports, Infrastructure, WebApi, Worker | A static clock read outside Domain | [Read no clock outside the scoped clock](https://litespec.dev/7.0.0/backend/dotnet/application/#read-no-clock-outside-the-scoped-clock) |
| LS2037 | Application, Infrastructure, Worker | A read of the document identity value inside a queryable expression | [Compare typed identifiers by their underlying values in queries](https://litespec.dev/7.0.0/backend/dotnet/persistence/#compare-typed-identifiers-by-their-underlying-values-in-queries) |
| LS3001 | Every production layer | An asynchronous method whose name does not end in `Async` | [Reveal asynchrony and cancellation in operation names](https://litespec.dev/7.0.0/workspace/naming/#reveal-asynchrony-and-cancellation-in-operation-names) |
| LS3002 | Every production layer | A `CancellationToken` parameter that is not last or not named `cancellationToken` | [Put the cancellation token last](https://litespec.dev/7.0.0/backend/dotnet/naming/#put-the-cancellation-token-last) |
| LS3003 | Every production layer | A C# file whose name differs from its primary top-level type | [Match C# files and primary types](https://litespec.dev/7.0.0/backend/dotnet/naming/#match-c-files-and-primary-types) |
| LS3004 | Every production layer | A concrete class with no derived type that is not sealed | [Keep implementation style consistent](https://litespec.dev/7.0.0/backend/dotnet/naming/#keep-implementation-style-consistent) |
| LS3005 | Every production layer | A class or struct declaring a primary constructor | [Avoid primary constructors on classes and structs](https://litespec.dev/7.0.0/workspace/naming/#avoid-primary-constructors-on-classes-and-structs) |
| LS4001 | Test | A test method that contains no assertion | [Prove Each Scenario Through the Public Boundary](https://litespec.dev/7.0.0/quality/testing/#prove-each-scenario-through-the-public-boundary) |
| LS5003 | Every layer | A code element with more or fewer marks than its kind takes | [Point code and tests up to records](https://litespec.dev/7.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5004 | Every layer | A mark written in the style the project did not declare | [Write each mark in the declared style](https://litespec.dev/7.0.0/backend/dotnet/stack/#write-each-mark-in-the-declared-style) |
| LS5005 | Domain | A hand-written event `Classification` or exception `Code` differing from its record | [Generate runtime values from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5006 | Domain | A marked domain event or rule exception that is not `partial` | [Declare marked events and exceptions as partial types](https://litespec.dev/7.0.0/backend/dotnet/stack/#declare-marked-events-and-exceptions-as-partial-types) |
| LS5009 | Domain, Application, Infrastructure, WebApi | Two permission records generating one constant name | [Generate runtime values from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5011 | Every layer | A message two marked handlers serve with different use cases | [Point code and tests up to records](https://litespec.dev/7.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5012 | Domain, Application, Infrastructure, WebApi | A permission granting a role no record declares | [Generate the registry, permissions, and roles from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-the-registry-permissions-and-roles-from-the-records) |
| LS5014 | Every layer | A development shortcut compiled into a build without DEBUG, or tagged with no reason | [Keep development shortcuts out of release builds](https://litespec.dev/7.0.0/quality/development/#keep-development-shortcuts-out-of-release-builds) |
| LS5013 | Domain | A marked exception inheriting a non-virtual `Code` | [Define the shared Domain contracts once](https://litespec.dev/7.0.0/backend/dotnet/domain/#define-the-shared-domain-contracts-once) |

### Change a Rule's Severity

Every rule reports an error by default, so a finding fails the build. To lower a rule, set its severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.LS1011.severity = warning
dotnet_diagnostic.LS3004.severity = none
```

A project that sets a Standard aside records a waiver citing the provision, as the [LiteSpec model page](https://litespec.dev/7.0.0/core/model/) describes.

## Limits

The rules judge only the files the layer rules can place. Read [How a File Gets Its Layer](#how-a-file-gets-its-layer) before you treat a rule's silence on a file as approval.
