# Overview

## Intent

The Analyzers section explains the LiteSpec.Analyzers package, which checks .NET server code against the LiteSpec standard while the compiler runs and reports each break as an error in the editor and the build. The package checks project boundaries, modeling in Domain, the project that holds the business rules, naming across the application projects, and traceability from code to records in every layer, with one assertion rule in test projects, reporting 46 errors, one per rule, with a link from each error to the standard it enforces. A backend developer uses these pages to install the package once and to clear each finding by fixing the code it names.

## How the Pages Fit Together

Start with [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md) to install the package once for every project, confirm each project layer, read the first findings, and record an exception for a deliberate departure. When a build reports a diagnostic, open its rule page from the [Analyzer rules overview](rules/README.md): the page states the provision the rule enforces, where the diagnostic sits, what the rule ignores, and how to fix the code, with a failing example beside a passing one. Every diagnostic also carries a help link that opens the provision it enforces, so an error in the editor leads straight to the rule it broke.

## How a Rule Decides to Run

Three facts predict whether a rule runs in a project.

First, the analyzers work out the project layer from the `LiteSpecLayer` MSBuild property when it is set, and from the assembly name suffix otherwise. The `LiteSpecLayer` property wins when both exist.

| Assembly name suffix | Layer |
|:---|:---|
| `Shop.Domain` | Domain |
| `Shop.Application.Abstractions` | ApplicationAbstractions |
| `Shop.Application` | Application |
| `Shop.Infrastructure` | Infrastructure |
| `Shop.WebApi` | WebApi |
| `Shop.Worker` | Worker |
| `Shop.Domain.Tests` | Test |

Any other assembly name opts the project out, and so does an explicit `LiteSpecLayer` value of `None`. No rule runs in an opted-out project.

Second, a rule registers only in the layers its provision covers. Domain rules run in Domain. `LS2001` runs in Application, Infrastructure, WebApi, and Worker. `LS2004`, `LS2005`, and `LS2007` to `LS2016` run in Application, Infrastructure, and Worker. `LS2003` and `LS2006` run in WebApi. `LS2017` reports an unknown declared value in any project. Style rules run in every production project. `LS4001` runs in test projects. `LS5001` to `LS5004` run in every layer, checking the kinds each layer holds. `LS5005` to `LS5008` run in Domain. `LS5009` and `LS5010` run in Application, Infrastructure, and WebApi. `LS5011` runs in every layer, where the generator writes the registry. `LS5012` runs in Domain, Application, Infrastructure, and WebApi, where the generator writes role entries. The analyzers skip generated code.

Third, most rules decide as the compiler reads each declaration. Rules `LS1003`, `LS1004`, and `LS3004` decide at the end of compilation, after every type is known.

Each diagnostic carries a help link of the form `https://litespec.dev/3.0.0/<page>/#<slug>`, which opens the provision the rule enforces.

## How a Rule Reads Records

A traceability rule reads the records the code marks name. The package adds `litespec.project.json` and the record pages as additional files, and each rule indexes the records under the docs roots the project file names. With no project file in reach, or a docs root it cannot resolve, every traceability rule and the generator stay silent; the CLI owns that configuration error.

Severity follows the record the finding is about: a blocking finding is an error and a reported one is a warning. A finding about a draft record reports as a warning, and a finding about a retired record is not reported. A finding that an approved exception covers stays silent. An `.editorconfig` entry for the rule id overrides the computed severity, and a project-wide lowering carries an exception record.

## Severity and Compiler Support

Every rule reports an error and is enabled by default, so a finding fails the build. A traceability rule reports a warning instead where the record is a draft, as How a Rule Reads Records states. The package keeps the traceability rules out of a project's warnings-as-errors promotion, so a draft finding stays a warning while a blocking one fails the build. A project lowers the severity per rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.LS1011.severity = warning
dotnet_diagnostic.LS3004.severity = none
```

When a project departs from the enforced Standard, it records an exception that covers the provision, rather than suppressing the diagnostic silently.

The package targets `netstandard2.0` and needs the .NET 9.0.300 SDK, Visual Studio 17.14, or later. Older compilers cannot load the analyzers.

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
- An event handler is any class that implements an interface named `IEventHandler` or `IDomainEventReaction`. A scheduled automation is any class that derives from a class named `BackgroundService` or implements an interface named `IHostedService`.
- An event handler handles the event its `IEventHandler<TEvent>` type argument names.
- A use-case result is any type whose name ends in `QueryResult` or `CommandResult`.
- A repository variable is any parameter, field, or local typed as an interface named `I{Name}Repository`.
- A session is any type named `IDocumentSession`, any type named `DbContext`, or any subclass of a type named `DbContext`.
- A test is any method carrying an attribute named `Fact`, `Theory`, `Test`, `TestCase`, `TestCaseSource`, `TestMethod`, or `DataTestMethod`. An assertion is any call to a member of a type named `Assert`, or to a method whose name starts with `Assert`.

What each rule reports when a name matches lives on its rule page: see the [Analyzer rules overview](rules/README.md).

The analyzer does not check that a `Guid` inside an identifier is a version 7 value. Create the value with `Guid.CreateVersion7` and pass it in, which also satisfies `LS1009`.

## Pages in This Section

- [Check .NET Code with the Analyzers](check-dotnet-code-with-the-analyzers.md): install the package once, read the first findings, keep them as build errors, and lower one only where an exception allows it.
- [Analyzer rules overview](rules/README.md): find the reference page for each of the 46 rules, grouped by layer.
