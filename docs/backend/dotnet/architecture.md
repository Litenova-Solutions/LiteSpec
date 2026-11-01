# .NET Architecture

## Intent

A .NET backend is 5 projects: Domain, Application.Abstractions, Application, Infrastructure, and WebApi, wired together by host projects that own service registration. This page states the .NET form of that shape: which projects exist, what each holds, which types stay internal, and where service registration lives, so every type keeps one home and dependencies point inward. The shared architecture rules live on [Backend Architecture](../architecture.md); this page holds only their .NET form.

## Concepts

### The Five Projects

Each part of the backend is a .NET project with one job. Domain holds the business model. Application.Abstractions holds the contracts that more than one module names. Application holds the messages, handlers, and module-owned ports. Infrastructure holds the technical adapters, and WebApi hosts the HTTP boundary. `AppHost` and `ServiceDefaults` support local hosting and diagnostics; they are hosts, not application layers.

### Visibility and Composition

A type is visible only to the projects that need it: implementations stay `internal sealed`, and a message, port, or marker is public only when another project names it. Service registration lives in the hosts, because Domain and Application declare abstractions and a registration call inside either would point the dependency the wrong way.

## Standards

### Keep One Application Assembly

**ID:** `litespec/backend/dotnet/architecture#keep-one-application-assembly`

**Statement:** An application MUST place commands, queries, results, validators, handlers, event handler classes, and module-owned ports in one Application project, and cross-module contracts in Application.Abstractions, or in Application where the project merges them under `litespec/backend/dotnet/architecture#use-five-application-projects`.

**Rationale:** Splitting Application into Write, Read, Contracts, and event-handler assemblies would buy a deployment boundary the stack never uses, and every use case would pay for it with a project reference to cross. The separation this stack keeps is the one [Fowler describes as CQRS](https://www.martinfowler.com/bliki/CQRS.html): the command and query models differ, while one assembly holds them. Event handlers live in Application `On{Fact}` folders of the aggregate whose records they change; job host classes live in Worker or Infrastructure with the scheduling concerns, under `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`.

This provision states the .NET form of `litespec/backend/architecture#keep-one-application-part`.

**Reviewed for:** Which command, query, result, validator, handler, or event handler files escape the Application project, or which cross-module contracts escape Application.Abstractions and the merged Application? Look at the Application project files beside the shared contracts.

### Mirror Modules in Folders and Namespaces

**ID:** `litespec/backend/dotnet/architecture#mirror-modules-in-folders-and-namespaces`

**Statement:** Every project MUST give each module one folder and namespace.

**Rationale:** The folder name derives from the module identifier, so `module/spending` owns the `Spending` folder in every project, and the namespace follows the folder under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. The same module names in every layer let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a map. This provision states the .NET form of `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`.

**Example:** The `Spending` module owns one folder in every project, and each namespace follows its folder:

```text
Claimwick.Domain/Spending/Expenses/Expense.cs
Claimwick.Application/Spending/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler above lives in namespace `Claimwick.Application.Spending.Expenses.SubmitExpense`.

**Reviewed for:** Which projects miss one folder and namespace per module, or name one outside its module? Look at the module folders and namespaces in every project.

### Keep Implementation Types Internal

**ID:** `litespec/backend/dotnet/architecture#keep-implementation-types-internal`

**Statement:** A handler, validator, definition, persistence implementation, or endpoint implementation MUST be declared `internal sealed`.

**Rationale:** A public type is an offer to every project, so an implementation left public invites callers it was never written for. Messages, results, markers, and ports stay public only when another project uses them; handlers, validators, and implementations stay `internal sealed`. Assembly scanning reads internal types through the one public marker, so it never justifies widening visibility.

This provision states the .NET form of `litespec/backend/architecture#keep-implementation-types-hidden`.

**Checked by:** `dotnet build` reports `LS2001` for an implementation type that is not `internal sealed`.

## Conventions

### Use Five Application Projects

**ID:** `litespec/backend/dotnet/architecture#use-five-application-projects`

**Default:** Use Domain, Application.Abstractions, Application, Infrastructure, and WebApi as the five baseline projects at every size, with a small project merging Application.Abstractions into Application and hosts and a Worker project as allowed extras.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** `Application.Abstractions` holds the contracts that more than one module names: the pipeline's own positions, the cross-module ports, the transport-neutral failures, and the published integration events. It references Domain and the required LiteBus module abstractions, and nothing else, so no type in it can name a use case, a handler, or a module.

That refusal is why it is a project rather than a folder. A folder inside Application cannot decline what a developer puts in it, and the folder that tries becomes the place every unplaced type goes. A small project with few cross-module contracts may merge them into Application, trading that protection for one less project.

An implementation never belongs there. The project name states the test, and a type that cannot pass it belongs to a module under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`.

This provision states the .NET form of `litespec/backend/architecture#use-five-application-parts`.

**Example:**

```text
apps/api/src/{ProjectName}.Domain/
apps/api/src/{ProjectName}.Application.Abstractions/
apps/api/src/{ProjectName}.Application/
apps/api/src/{ProjectName}.Infrastructure/
apps/api/src/{ProjectName}.WebApi/
```

**Reviewed for:** Which projects sit outside the five baseline projects, the merged small layout, or the allowed hosts and Worker? Look at the solution file beside the declared project size.

### Keep Composition in Hosts

**ID:** `litespec/backend/dotnet/architecture#keep-composition-in-hosts`

**Default:** Keep service registration in `Program.cs` and host registration modules.

**Replacement:** A project names its registration layout in its host documentation.

**Rationale:** Domain and Application declare abstractions for the hosts to implement. A registration call inside either layer makes the abstraction depend on the thing it abstracts, inverting the dependency direction. Registration in `Program.cs` and host modules keeps that direction intact, and one visible call order shows which layer wires first.

This provision states the .NET form of `litespec/backend/architecture#keep-service-registration-in-hosts`.

**Example:** One registration entry point per outer layer has this shape:

```text
InfrastructureServiceRegistration.AddInfrastructure(...)
WebApiServiceRegistration.AddWebApi(...)
```

`AddInfrastructure` receives configuration and host environment values needed to bind and validate provider options. `AddWebApi` owns Problem Details, authentication, authorization, endpoint discovery, and OpenAPI. `Program.cs` keeps their call order visible.

**Reviewed for:** Which service registration calls sit outside `Program.cs` and the host registration modules? Look at the registration calls.

### Use One Public Assembly Marker per Scanned Project

**ID:** `litespec/backend/dotnet/architecture#use-one-public-assembly-marker-per-scanned-project`

**Default:** Expose one `public static` assembly marker in each project that an approved scanner reads.

**Replacement:** A project names its scanner assembly reference in its host documentation.

**Rationale:** Scanners need an assembly reference, and without a marker developers make handlers public to supply one. One public static marker per scanned project gives the scanner its reference while every handler stays internal.

**Reviewed for:** Which scanned projects miss their one public static assembly marker, or hold more than one? Look at each scanned project.
