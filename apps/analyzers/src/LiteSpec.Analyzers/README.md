# LiteSpec.Analyzers

Roslyn analyzers that check a .NET application against the [LiteSpec](https://litespec.dev) standard. Each error names the provision it enforces and links to it.

## Install

Add the package to each application project, or once in `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="3.0.0" PrivateAssets="all" />
</ItemGroup>
```

The package needs the .NET 9.0.300 SDK, Visual Studio 17.14, or later.

## How a Project Gets Its Layer

A rule runs only in the layers its provision covers. The analyzers read the layer from the assembly name suffix: `Shop.Domain`, `Shop.Application.Abstractions`, `Shop.Application`, `Shop.Infrastructure`, `Shop.WebApi`, and `Shop.Worker` for production code, and `Shop.Domain.Tests` for test projects. Any other assembly is not analyzed.

To name the layer yourself, set `LiteSpecLayer` in the project file:

```xml
<PropertyGroup>
  <LiteSpecLayer>Domain</LiteSpecLayer>
</PropertyGroup>
```

The values are `Domain`, `Application.Abstractions`, `Application`, `Infrastructure`, `WebApi`, `Worker`, `Test`, and `None`. `None` turns the whole package off for that project: no analyzer rule runs, and the generator writes no source and reports no diagnostic. An assembly name with no layer suffix opts the project out in the same way. A declared value that names no known layer still reports `LS2017`.

## Rules

| ID | Layer | What it reports | Provision |
|:---|:---|:---|:---|
| LS1001 | Domain | A persistence, web, messaging, mediator, logging, configuration, injection, or serialization type | [Keep Domain free of outer-layer concerns](https://litespec.dev/3.0.0/backend/dotnet/domain/#keep-domain-free-of-outer-layer-concerns) |
| LS1002 | Domain | An enum | [Model every closed set of domain values without enums](https://litespec.dev/3.0.0/backend/dotnet/domain/#model-every-closed-set-of-domain-values-without-enums) |
| LS1003 | Domain | An aggregate with no `State` property typed as an abstract record | [Model every aggregate lifecycle with state records](https://litespec.dev/3.0.0/backend/dotnet/domain/#model-every-aggregate-lifecycle-with-state-records) |
| LS1004 | Domain | A state case record that is not sealed | [Model every aggregate lifecycle with state records](https://litespec.dev/3.0.0/backend/dotnet/domain/#model-every-aggregate-lifecycle-with-state-records) |
| LS1005 | Domain | A thrown exception that is not a `DomainException` or an argument guard | [Reject business violations with Domain exceptions](https://litespec.dev/3.0.0/backend/dotnet/domain/#reject-business-violations-with-domain-exceptions) |
| LS1006 | Domain | A `DomainException` subclass that is not sealed or not named `...Exception` | [Reject business violations with Domain exceptions](https://litespec.dev/3.0.0/backend/dotnet/domain/#reject-business-violations-with-domain-exceptions) |
| LS1007 | Domain | A domain event that is not an immutable record named `...Event` | [Raise immutable domain facts](https://litespec.dev/3.0.0/backend/dotnet/domain/#raise-immutable-domain-facts) |
| LS1008 | Domain | A `double` or `float` | [Keep binary floating point out of exact quantities](https://litespec.dev/3.0.0/backend/domain/#keep-binary-floating-point-out-of-exact-quantities) |
| LS1009 | Domain | A read of the clock, randomness, or a new identifier | [Pass nondeterministic values into Domain](https://litespec.dev/3.0.0/backend/domain/#pass-nondeterministic-values-into-domain) |
| LS1010 | Domain | A repository that exposes `IQueryable`, a session, or a generic surface | [Keep repository interfaces in Domain](https://litespec.dev/3.0.0/backend/dotnet/domain/#keep-repository-interfaces-in-domain) |
| LS1011 | Domain | A public type or member without XML documentation | [Document every public Domain contract](https://litespec.dev/3.0.0/backend/dotnet/domain/#document-every-public-domain-contract) |
| LS1012 | Domain | An identifier that is not a `readonly record struct` wrapping one `Guid` | [Use strongly typed version 7 identifiers](https://litespec.dev/3.0.0/backend/dotnet/domain/#use-strongly-typed-version-7-identifiers) |
| LS1013 | Domain | A domain event declared outside the `Events` folder | [Organize a module by aggregate and concept](https://litespec.dev/3.0.0/backend/domain/#organize-a-module-by-aggregate-and-concept) |
| LS2001 | Application, Infrastructure, WebApi, Worker | A handler, validator, endpoint, or persistence implementation that is not `internal sealed` | [Keep implementation types internal](https://litespec.dev/3.0.0/backend/dotnet/architecture/#keep-implementation-types-internal) |
| LS2003 | WebApi | An MVC controller | [Exclude MVC controllers from the stack](https://litespec.dev/3.0.0/backend/dotnet/api/#exclude-mvc-controllers-from-the-stack) |
| LS2004 | Application, Infrastructure, Worker | A command handler that writes through a document session or `DbContext` | [Stage aggregate writes through repositories](https://litespec.dev/3.0.0/backend/dotnet/persistence/#stage-aggregate-writes-through-repositories) |
| LS2005 | Application, Infrastructure, Worker | A command or query handler whose name lacks its role suffix | [Use architectural suffixes](https://litespec.dev/3.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2006 | WebApi | An endpoint whose name does not end in `Endpoint` | [Use architectural suffixes](https://litespec.dev/3.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2007 | Application, Infrastructure, Worker | A reaction or job whose name hides its kind | [Use architectural suffixes](https://litespec.dev/3.0.0/workspace/naming/#use-architectural-suffixes) |
| LS2008 | Application, Infrastructure, Worker | An event handler declared outside its `On{Fact}` folder | [Group event handlers by the fact they handle](https://litespec.dev/3.0.0/workspace/naming/#group-event-handlers-by-the-fact-they-handle) |
| LS2009 | Application, Infrastructure, Worker | A result part not named after its `{UseCase}QueryResult` or `{UseCase}CommandResult` | [Name result parts after their result](https://litespec.dev/3.0.0/workspace/naming/#name-result-parts-after-their-result) |
| LS2010 | Application, Infrastructure, Worker | A repository parameter, field, or local not named after its interface | [Name repository variables after their interface](https://litespec.dev/3.0.0/workspace/naming/#name-repository-variables-after-their-interface) |
| LS2011 | Application, Infrastructure, Worker | A query handler that takes a repository | [Project queries directly](https://litespec.dev/3.0.0/backend/application/#project-queries-directly) |
| LS2012 | Application, Infrastructure, Worker | A handler that throws a forbidden refusal | [Enforce target authorization in the pipeline](https://litespec.dev/3.0.0/backend/application/#enforce-target-authorization-in-the-pipeline) |
| LS2013 | Application, Infrastructure, Worker | An audit value read from the message in the handler | [Declare audit values beside the message](https://litespec.dev/3.0.0/backend/application/#declare-audit-values-beside-the-message) |
| LS2014 | Application, Infrastructure, Worker | A command handler that finds or creates | [Implement one outcome path per command](https://litespec.dev/3.0.0/backend/application/#implement-one-outcome-path-per-command) |
| LS2015 | Application, Infrastructure, Worker | A command handler that changes records in a loop | [Issue per-record work from a job](https://litespec.dev/3.0.0/backend/application/#issue-per-record-work-from-a-job) |
| LS2016 | Application, Infrastructure, Worker | A handler that takes a pipeline-owned collaborator | [Keep uniqueness out of handlers](https://litespec.dev/3.0.0/backend/persistence/#keep-uniqueness-out-of-handlers) |
| LS2017 | Every layer | A declared layer value that names no known layer | [Use five application projects](https://litespec.dev/3.0.0/backend/dotnet/architecture/#use-five-application-projects) |
| LS3001 | All production projects | An asynchronous method whose name does not end in `Async` | [Name asynchronous methods completely](https://litespec.dev/3.0.0/backend/dotnet/naming/#name-asynchronous-methods-completely) |
| LS3002 | All production projects | A `CancellationToken` that is not the last parameter, or not named `cancellationToken` | [Name asynchronous methods completely](https://litespec.dev/3.0.0/backend/dotnet/naming/#name-asynchronous-methods-completely) |
| LS3003 | All production projects | A file whose primary type does not match its name, or that holds more than one top-level type | [Match C# files and primary types](https://litespec.dev/3.0.0/backend/dotnet/naming/#match-c-files-and-primary-types) |
| LS3004 | All production projects | A concrete class with no derived type that is not sealed | [Keep implementation style consistent](https://litespec.dev/3.0.0/backend/dotnet/naming/#keep-implementation-style-consistent) |
| LS4001 | Test | A test method that contains no assertion | [Prove Each Scenario with a Test That Calls Its Use Case](https://litespec.dev/3.0.0/backend/dotnet/testing/#prove-each-scenario-with-a-test-that-calls-its-use-case) |
| LS5001 | Every layer | A code element of a recognized kind with no mark | [Point code and tests up to records](https://litespec.dev/3.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5002 | Every layer | A mark naming no usable record | [Point code and tests up to records](https://litespec.dev/3.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5003 | Every layer | A code element with more or fewer marks than its kind takes | [Point code and tests up to records](https://litespec.dev/3.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5004 | Every layer | A mark written in the style the project did not declare | [Write each mark in the declared style](https://litespec.dev/3.0.0/backend/dotnet/stack/#write-each-mark-in-the-declared-style) |
| LS5005 | Domain | A hand-written event `Classification` or exception `Code` differing from its record | [Generate runtime values from the records](https://litespec.dev/3.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5006 | Domain | A marked domain event or rule exception that is not `partial` | [Declare marked events and exceptions as partial types](https://litespec.dev/3.0.0/backend/dotnet/stack/#declare-marked-events-and-exceptions-as-partial-types) |
| LS5007 | Domain | A marked method raising an event its use case does not list | [Raise the declared events from the marked act](https://litespec.dev/3.0.0/backend/domain/#raise-the-declared-events-from-the-marked-act) |
| LS5008 | Domain | A listed event no method marked with its use case raises | [Raise the declared events from the marked act](https://litespec.dev/3.0.0/backend/domain/#raise-the-declared-events-from-the-marked-act) |
| LS5009 | Domain, Application, Infrastructure, WebApi | Two permission records generating one constant name | [Generate runtime values from the records](https://litespec.dev/3.0.0/backend/dotnet/stack/#generate-runtime-values-from-the-records) |
| LS5010 | Domain, Application, Infrastructure, WebApi | A permission record whose name opens with an undeclared verb | [State the scope of every permission](https://litespec.dev/3.0.0/core/access/#state-the-scope-of-every-permission) |
| LS5011 | Every layer | A message two marked handlers serve with different use cases | [Point code and tests up to records](https://litespec.dev/3.0.0/core/system/#point-code-and-tests-up-to-records) |
| LS5012 | Domain, Application, Infrastructure, WebApi | A role allowing a permission no record declares | [Generate the registry, permissions, and roles from the records](https://litespec.dev/3.0.0/backend/dotnet/stack/#generate-the-registry-permissions-and-roles-from-the-records) |

## Change a Rule's Severity

Every rule is an error by default, so a finding fails the build. Lower a rule's severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.LS1011.severity = warning
dotnet_diagnostic.LS3004.severity = none
```

A project that departs from a Standard records an exception that cites the provision, as the [LiteSpec model page](https://litespec.dev/3.0.0/core/model/) describes.
