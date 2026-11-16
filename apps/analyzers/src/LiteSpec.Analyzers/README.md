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

Each rule runs only on the files in the layers its provision covers. A file takes its layer from the nearest layer folder (`Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, with `Contracts/` counting as `Application`). Inside a use case, reaction, or job folder, the file's role suffix decides instead: `Endpoint`, `RequestModel`, and `ResponseModel` mean `WebApi`, while `Command`, `Query`, `Result`, `Handler`, `Validator`, `Job`, and `Worker` mean `Application`. A host project counts as `WebApi` or `Worker` by its assembly name, and a test project counts as `Test`.

A consumer on the layer-projects extension keeps one layer per project instead. Then the analyzers read the layer from the assembly name suffix (`Shop.Domain`, `Shop.Application.Abstractions`, `Shop.Application`, `Shop.Infrastructure`, `Shop.WebApi`, `Shop.Worker`, `Shop.Domain.Tests`), and any other assembly goes unanalyzed. The build props derive the layout from the project's selected extensions, so the project states nothing itself.

To declare the layer yourself, set `LiteSpecLayer` in the project file:

```xml
<PropertyGroup>
  <LiteSpecLayer>Domain</LiteSpecLayer>
</PropertyGroup>
```

The accepted values are `Domain`, `Application.Abstractions`, `Application`, `Infrastructure`, `WebApi`, `Worker`, `Test`, and `None`. One declared value covers every file in the project. `None` switches the whole package off for that project: no analyzer rule runs, and the generator emits neither source nor diagnostics. A file the rules cannot place stays quiet in the same way. A declared value that names no known layer still reports `LS2017`.

### Rules

| ID | Layer | What it reports | Provision |
|:---|:---|:---|:---|
| LS1001 | Domain | A persistence, web, messaging, mediator, logging, configuration, injection, or serialization type, or an Application, Infrastructure, or WebApi type | [Keep Domain free of outer-layer concerns](https://litespec.dev/7.0.0/backend/dotnet/domain/#keep-domain-free-of-outer-layer-concerns) |
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
| LS2017 | Every layer | A declared layer value that names no known layer | [Package each layer as its own project](https://litespec.dev/7.0.0/backend/dotnet/ext/layer-projects/#package-each-layer-as-its-own-project) |
| LS2018 | Application, WebApi | A type outside the file's layer: Infrastructure or WebApi from Application, ASP.NET Core or HttpContext from Application, or a Domain type or repository from WebApi | [Follow layer direction in every file](https://litespec.dev/7.0.0/backend/dotnet/architecture/#follow-layer-direction-in-every-file) |
| LS2019 | Every layer | A type declared in another use case folder, the message and result of another use case included | [Keep use case folders isolated](https://litespec.dev/7.0.0/backend/dotnet/architecture/#keep-use-case-folders-isolated) |
| LS2020 | Every layer | A type of another module declared outside its Contracts folder | [Reach another module only through Contracts](https://litespec.dev/7.0.0/backend/dotnet/architecture/#reach-another-module-only-through-contracts) |
| LS2021 | Domain, Application, Infrastructure | A command, query, or event mediator held by a type that is not an endpoint, reaction, or job | [Keep command handlers to load, call, and stage](https://litespec.dev/7.0.0/backend/application/#keep-command-handlers-to-load-call-and-stage) |
| LS2022 | Every layer | User code naming the event mediator or an event publisher | [Emit declared integration events after commit](https://litespec.dev/7.0.0/backend/application/#stage-declared-module-and-integration-events-in-the-commit) |
| LS3001 | All production projects | An asynchronous method whose name does not end in `Async` | [Reveal asynchrony and cancellation in operation names](https://litespec.dev/7.0.0/workspace/naming/#reveal-asynchrony-and-cancellation-in-operation-names) |
| LS3002 | All production projects | A `CancellationToken` parameter that is not last or not named `cancellationToken` | [Put the cancellation token last](https://litespec.dev/7.0.0/backend/dotnet/naming/#put-the-cancellation-token-last) |
| LS3003 | All production projects | A C# file whose name differs from its primary top-level type | [Match C# files and primary types](https://litespec.dev/7.0.0/backend/dotnet/naming/#match-c-files-and-primary-types) |
| LS3004 | All production projects | A concrete class with no derived type that is not sealed | [Keep implementation style consistent](https://litespec.dev/7.0.0/backend/dotnet/naming/#keep-implementation-style-consistent) |
| LS4001 | Test | A test method that contains no assertion | [Prove Each Scenario Through the Public Boundary](https://litespec.dev/7.0.0/quality/testing/#prove-each-scenario-through-the-public-boundary) |
| LS5003 | Every layer | A code element with more or fewer marks than its kind takes | [Point code and tests up to records](https://litespec.dev/7.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5004 | Every layer | A mark written in the style the project did not declare | [Write each mark in the declared style](https://litespec.dev/7.0.0/backend/dotnet/stack/#write-each-mark-in-the-declared-style) |
| LS5005 | Domain | A hand-written event `Classification` or exception `Code` differing from its record | [Generate runtime values from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5006 | Domain | A marked domain event or rule exception that is not `partial` | [Declare marked events and exceptions as partial types](https://litespec.dev/7.0.0/backend/dotnet/stack/#declare-marked-events-and-exceptions-as-partial-types) |
| LS5009 | Domain, Application, Infrastructure, WebApi | Two permission records generating one constant name | [Generate runtime values from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5011 | Every layer | A message two marked handlers serve with different use cases | [Point code and tests up to records](https://litespec.dev/7.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5012 | Domain, Application, Infrastructure, WebApi | A role allowing a permission no record declares | [Generate the registry, permissions, and roles from the records](https://litespec.dev/7.0.0/backend/dotnet/stack/#generate-the-registry-permissions-and-roles-from-the-records) |

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
