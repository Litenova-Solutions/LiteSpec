# Overview

The `LiteSpec.Analyzers` package checks .NET server code against the LiteSpec standard while the compiler runs, and reports each break in the editor and the build. The package checks layer boundaries, modeling in Domain (the layer that holds business rules), naming across the Kernel, Platform, and module projects, and traceability from code to records in every layer, with one assertion rule for test projects. Every diagnostic links to the provision it enforces, and the [Analyzer rules overview](rules/README.md) lists all 56 rules, each with its own reference page.

## How the Pages Fit Together

Start with [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md): it installs the package once for every project, confirms that each file resolves to the right layer, walks through reading and fixing the first findings, and shows how to record a waiver when a finding is wrong for the project. When a build names a rule ID, open that rule's page from the [Analyzer rules overview](rules/README.md): the page states which provision the rule enforces, where the diagnostic sits, what the rule ignores, and how to fix the code, with a failing example beside a passing one. Every diagnostic also carries a help link of the form `https://litespec.dev/7.0.0/<page>/#<slug>`. An error in the editor therefore opens the provision the rule enforces.

## How a Rule Decides to Run

Whether a rule runs on a file comes down to four facts.

### Claimed Record Layers

The record layers the project claims in `layers` in `.litespec/project.yaml` decide which rules run at all. The build props read that block into the `LiteSpecImplementation` and `LiteSpecProof` properties and make them visible to the compiler, and a project can also set either property itself to name another practice.

A project that claims neither layer runs no rule, and the generator writes no source and reports no diagnostic. A claim of Implementation with either practice runs the traceability rules `LS5003` to `LS5006`, `LS5009`, and `LS5011` to `LS5013`, and `LiteSpecImplementation` set to `litespec` also runs every architecture rule. A claim of Proof runs `LS4001`. An absent property reads as `litespec`, which matches what a project with no `layers` block claims.

### File Layers

The analyzers work out each file's layer from its folder and, inside a use case folder, from its role suffix. A file under a `Domain/`, `Application/`, `Infrastructure/`, or `WebApi/` folder takes that layer. In a use case folder, a type whose name ends in `Endpoint`, `RequestModel`, or `ResponseModel` takes WebApi, and one whose name ends in `Command`, `Query`, `Result`, `Handler`, or `Validator` takes Application, as [the naming page](../workspace/naming.md) states. Test files take Test.

When the project selects the `layer-projects` extension, the analyzers read the layer from the assembly name suffix instead, as the table below states, and an explicit `LiteSpecLayer` value wins when both exist.

| Assembly name suffix | Layer |
|:---|:---|
| `Shop.Domain` | Domain |
| `Shop.Application.Abstractions` | ApplicationAbstractions |
| `Shop.Application` | Application |
| `Shop.Infrastructure` | Infrastructure |
| `Shop.WebApi` | WebApi |
| `Shop.Worker` | Worker |
| `Shop.Domain.Tests` | Test |

Under `layer-projects`, an assembly name with no listed suffix opts the project out, as does an explicit `LiteSpecLayer` value of `None`. Opting out turns off the whole package: no analyzer rule runs, and the generator writes no source and reports no diagnostic in that project. A declared `LiteSpecLayer` value that names no known layer still reports `LS2017`. The package reads the layout from `selectedExtensions` in `.litespec/project.yaml`, so there is nothing else to wire.

In both layouts, three more suffixes name project roles: `Shop.Kernel` reads as Domain, so the business values answer the Domain rules; `Shop.Platform` reads as Infrastructure; and `Shop.Api` reads as WebApi beside the `WebApi` and `Worker` hosts. A module project such as `Shop.Orders` names no layer: each of its files takes its layer from its folder, as above.

### Rule Layers

Each rule runs only in the layers its provision covers:

- Domain rules run on Domain files, except `LS1014`, which runs in every production layer.
- `LS2001` runs on Application, Infrastructure, WebApi, and Worker files.
- `LS2004`, `LS2005`, `LS2007`, and `LS2009` to `LS2016` run on Application, Infrastructure, and Worker files.
- `LS2003` and `LS2006` run on WebApi files.
- `LS2017` reports an unknown declared value in any project.
- `LS2018` runs on Application and WebApi files.
- `LS2019` and `LS2020` run on every layer except Test.
- `LS2021` runs on Domain, Application, and Infrastructure files of the modules project, and `LS2022` on every layer except Test.
- `LS2023`, `LS2024`, and `LS2026` run on every layer except Test. `LS2025` runs on Infrastructure files inside a use case folder, and `LS2027` on Application files. `LS2028` runs in one-project-per-module compilations on every layer except Test.
- Style rules run in every production layer.
- `LS4001` runs in test projects.
- `LS5003` and `LS5004` run in every layer, checking the kinds each layer holds.
- `LS5005` and `LS5006` run in Domain.
- `LS5009` runs in Domain, Application, Infrastructure, and WebApi, where the generator writes permission constants.
- `LS5011` runs in every layer, where the generator writes the registry.
- `LS5012` runs in Domain, Application, Infrastructure, and WebApi, where the generator writes role entries.
- `LS5013` runs where the generator writes exception parts.

The analyzers skip generated code.

### Decision Time

Most rules decide as the compiler reads each declaration. Rules `LS1003`, `LS1004`, `LS2026`, and `LS3004` wait until the end of compilation, when every type is known.

## How a Rule Reads Records

A traceability rule reads the records that the code marks name. The package adds `.litespec/project.yaml` and the record pages as additional files, and each rule indexes the records under the docs roots the project file names. When no project file is in reach, or a docs root cannot be resolved, every traceability rule and the generator stay silent; the CLI owns that configuration error.

Severity follows the check level: a blocking finding is an error and an advisory finding is a warning. A finding covered by a live waiver stays silent: a waiver is live while it has no `expiresOn` date or its date has not passed.

An `.editorconfig` entry for the rule id overrides the computed severity, and lowering a rule project-wide needs a waiver record behind it.

## Severity and Compiler Support

Every rule reports an error and is enabled by default, so a finding fails the build. `LS3005` reports at info severity instead, because the primary-constructor advice is advisory: it never fails the build on its own. A traceability rule reports a warning instead where the finding is advisory, as How a Rule Reads Records states. The package keeps the traceability rules out of a project's warnings-as-errors promotion, so an advisory finding stays a warning while a blocking one fails the build. A project lowers a rule's severity in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.LS1011.severity = warning
dotnet_diagnostic.LS3004.severity = none
```

When a project sets the enforced Standard aside, it records a waiver covering the provision instead of suppressing the diagnostic silently.

The package targets `netstandard2.0` and needs the .NET 10 SDK, which the manifest names as the .NET stack release. Older compilers cannot load the analyzers.

## Detection Limits

Several rules match names instead of project references, because Domain holds no reference to the outer-layer packages. Judge these findings against the lists below.

`LS1001` reports a use whose namespace starts with one of these prefixes, followed by a dot or the end of the name:

```text
Microsoft.EntityFrameworkCore
Marten
Npgsql
Dapper
Microsoft.Data
System.Data
Microsoft.AspNetCore
System.Net.Http
Microsoft.Extensions
MediatR
LiteBus
Wolverine
MassTransit
Serilog
NLog
Newtonsoft.Json
System.Text.Json
System.Runtime.Serialization
System.Xml.Serialization
```

A namespace that only shares a prefix, such as `MartenStore`, does not match. Base class library namespaces outside this list, such as `System`, `System.Collections.Generic`, and `System.Linq`, never match.

The other name-based checks work the same way:

- An aggregate is any class that derives from a class named `AggregateRoot` with one type argument, in any namespace.
- A child entity is any class that derives from a class named `Entity` with one type argument, in any namespace.
- A Domain exception is any exception that derives from a class named `DomainException`, which itself derives from `System.Exception`.
- A domain event is any type that implements an interface named `IDomainEvent`.
- A command handler is any class whose name ends in `CommandHandler` or that implements an interface named `ICommandHandler`.
- A query handler is any class that implements an interface named `IQueryHandler`.
- An endpoint is any class that implements an interface named `IEndpoint`.
- An event handler is any class that implements an interface named `IEventHandler` or `IDomainEventReaction`. A job is any class that derives from a class named `BackgroundService` or implements an interface named `IHostedService`.
- An event handler handles the event its `IEventHandler<TEvent>` type argument names.
- A use-case result is any type whose name ends in `QueryResult` or `CommandResult`.
- A repository variable is any parameter, field, or local typed as an interface named `I{Name}Repository`.
- A session is any type named `IDocumentSession`, any type named `DbContext`, or any subclass of a type named `DbContext`.
- A test is any method carrying an attribute named `Fact`, `Theory`, `Scenario`, `Test`, `TestCase`, `TestCaseSource`, `TestMethod`, or `DataTestMethod`. An assertion is any call to a member of a type named `Assert`, or to a method whose name starts with `Assert`.

What each rule reports on a match is stated on its rule page: see the [Analyzer rules overview](rules/README.md).

The analyzer does not check that a `Guid` inside an identifier is a version 7 value. Create the value with `Guid.CreateVersion7` and pass it in; that also satisfies `LS1009`.

## Pages in This Section

- [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md): install the package once, read the first findings, keep them as build errors, and lower one only where a waiver allows it.
- [Analyzer rules overview](rules/README.md): find the reference page for each rule, grouped by layer.
