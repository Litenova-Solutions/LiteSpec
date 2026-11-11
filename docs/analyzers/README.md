# Overview

## Intent

The Analyzers section explains the LiteSpec.Analyzers package, which checks .NET server code against the LiteSpec standard while the compiler runs and reports each break as an error in the editor and the build. The package checks layer boundaries, modeling in Domain, the layer that holds the business rules, naming across the shared kernel and modules projects, and traceability from code to records in every layer, with one assertion rule in test projects. It reports one error per rule, with a link from each error to the standard it enforces; the [Analyzer rules overview](rules/README.md) lists every rule. A backend developer uses these pages to install the package once and to clear each finding by fixing the code it names.

## How the Pages Fit Together

Start with [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md) to install the package once for every project, confirm the layout, read the first findings, and record a deliberate departure. When a build reports a diagnostic, open its rule page from the [Analyzer rules overview](rules/README.md): the page states the provision the rule enforces, where the diagnostic sits, what the rule ignores, and how to fix the code, with a failing example beside a passing one. Every diagnostic also carries a help link that opens the provision it enforces, so an error in the editor leads straight to the rule it broke.

## How a Rule Decides to Run

Four facts predict whether a rule runs on a file.

First, the record layers the project claims in `layers` in `.litespec/project.yaml` decide which rules run at all. The build props set the `LiteSpecImplementation` and `LiteSpecProof` properties from the `layers` block and make them visible to the compiler, and a project sets either property itself to name another practice. A project that claims neither layer runs no rule, and the generator writes no source and reports no diagnostic. A claim of Implementation with either practice runs the traceability rules `LS5001` to `LS5012`, and `LiteSpecImplementation` set to `litespec` also runs every architecture rule. A claim of Proof runs `LS4001`. An absent property reads as `litespec`, which is what a project with no `layers` block claims.

Second, the analyzers work out the file's layer from its folder and, in a use case folder, its role suffix. A file under a `Domain/`, `Application/`, `Infrastructure/`, or `WebApi/` folder takes that layer. A file in a use case folder takes WebApi when its type name ends in `Endpoint`, `RequestModel`, or `ResponseModel`, and Application when it ends in `Command`, `Query`, `Result`, `Handler`, or `Validator`, as [the naming page](../workspace/naming.md) states. Test files take Test. When the project selects the `layer-projects` extension, the analyzers read the layer from the assembly name suffix instead, as the table below states, with an explicit `LiteSpecLayer` value winning when both exist.

| Assembly name suffix | Layer |
|:---|:---|
| `Shop.Domain` | Domain |
| `Shop.Application.Abstractions` | ApplicationAbstractions |
| `Shop.Application` | Application |
| `Shop.Infrastructure` | Infrastructure |
| `Shop.WebApi` | WebApi |
| `Shop.Worker` | Worker |
| `Shop.Domain.Tests` | Test |

Under `layer-projects`, any other assembly name opts the project out, and so does an explicit `LiteSpecLayer` value of `None`. Opting out turns off the whole package: no analyzer rule runs, and the generator writes no source and reports no diagnostic in an opted-out project. A declared `LiteSpecLayer` value that names no known layer still reports `LS2017`. The package sets the layout from `selectedExtensions` in `.litespec/project.yaml`, so no extra wiring is needed.

Third, a rule runs only on the files whose layer its provision covers. Domain rules run on Domain files. `LS2001` runs on Application, Infrastructure, WebApi, and Worker files. `LS2004`, `LS2005`, `LS2007`, and `LS2009` to `LS2016` run on Application, Infrastructure, and Worker files. `LS2003` and `LS2006` run on WebApi files. `LS2017` reports an unknown declared value in any project. `LS2018` runs on Application and WebApi files. `LS2019` and `LS2020` run on every layer except Test. Style rules run in every production layer. `LS4001` runs in test projects. `LS5001` to `LS5004` run in every layer, checking the kinds each layer holds. `LS5005` to `LS5008` run in Domain. `LS5009` runs in Domain, Application, Infrastructure, and WebApi, where the generator writes permission constants. `LS5011` runs in every layer, where the generator writes the registry. `LS5012` runs in Domain, Application, Infrastructure, and WebApi, where the generator writes role entries. The analyzers skip generated code.

Fourth, most rules decide as the compiler reads each declaration. Rules `LS1003`, `LS1004`, `LS3004`, `LS5007`, and `LS5008` decide at the end of compilation, after every type is known.

Each diagnostic carries a help link of the form `https://litespec.dev/5.1.0/<page>/#<slug>`, which opens the provision the rule enforces.

## How a Rule Reads Records

A traceability rule reads the records the code marks name. The package adds `.litespec/project.yaml` and the record pages as additional files, and each rule indexes the records under the docs roots the project file names. With no project file in reach, or a docs root it cannot resolve, every traceability rule and the generator stay silent; the CLI owns that configuration error.

Severity follows the check level: a blocking finding is an error and an advisory finding is a warning. A finding that a live departure covers stays silent: a departure is live when it has no `expiresOn` date or its date has not passed. An `.editorconfig` entry for the rule id overrides the computed severity, and a project-wide lowering needs a departure record.

## Severity and Compiler Support

Every rule reports an error and is enabled by default, so a finding fails the build. A traceability rule reports a warning instead where the finding is advisory, as How a Rule Reads Records states. The package keeps the traceability rules out of a project's warnings-as-errors promotion, so an advisory finding stays a warning while a blocking one fails the build. A project lowers the severity per rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.LS1011.severity = warning
dotnet_diagnostic.LS3004.severity = none
```

When a project departs from the enforced Standard, it records a departure that covers the provision, rather than suppressing the diagnostic silently.

The package targets `netstandard2.0` and needs the .NET 10 SDK, which the manifest names as the .NET stack release. Older compilers cannot load the analyzers.

## Detection Limits

Several rules match names rather than project references, because Domain owns no reference to the outer-layer packages. Judge these findings against the lists below.

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

Other name-based checks work the same way:

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
- A test is any method carrying an attribute named `Fact`, `Theory`, `Test`, `TestCase`, `TestCaseSource`, `TestMethod`, or `DataTestMethod`. An assertion is any call to a member of a type named `Assert`, or to a method whose name starts with `Assert`.

What each rule reports when a name matches lives on its rule page: see the [Analyzer rules overview](rules/README.md).

The analyzer does not check that a `Guid` inside an identifier is a version 7 value. Create the value with `Guid.CreateVersion7` and pass it in, which also satisfies `LS1009`.

## Pages in This Section

- [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md): install the package once, read the first findings, keep them as build errors, and lower one only where a departure allows it.
- [Analyzer rules overview](rules/README.md): find the reference page for each rule, grouped by layer.
