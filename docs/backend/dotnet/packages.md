# .NET Packages

**Stack:** dotnet
**Layer:** workspace

## Intent

A .NET backend references only the LiteBus modules its code dispatches and reports .NET breaches in the editor and the build. This page states that module scope and the analyzer setup that reports each breach at the line that breaks it. The shared dependency rules live on [Dependencies](../../workspace/dependencies.md); this page holds only their .NET form.

## Standards

None.

## Conventions

### Reference Only the LiteBus Module Required

**ID:** `litespec/backend/dotnet/packages#reference-only-the-litebus-module-required`

**Strength:** default

**Default:** Reference the LiteBus command, query, or event module that the code actually uses.

**Replacement:** A project names the LiteBus modules each layer references in its project documentation.

**Rationale:** A unified bus package pulls contracts for commands, queries, and events into a layer that dispatches only some of them. Referencing only the module the code uses keeps each dispatch surface exact.

**Reviewed for:** Which project files reference a LiteBus module their code never dispatches? Look at each project file.

### Run LiteSpec Analyzers in the Application Project and Hosts

**ID:** `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-application-project-and-hosts`

**Strength:** default

**Default:** Reference `LiteSpec.Analyzers` at the manifest pin, with `PrivateAssets="all"`, from the application project and each host project.

**Replacement:** A project names the projects its analyzer run covers in its project documentation.

**Rationale:** The analyzers report a breach of a .NET provision in the editor and the build, at the line that breaks it, with a link to the provision. Without them, a breach surfaces only in review, if at all. The package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time. `LS2016` reports a pipeline-owned collaborator in a handler constructor; its default list covers the LiteBus unit of work, idempotency store, and outbox, and a project adds its uniqueness claim service. [Check .NET code with the analyzers](../../analyzers/check-dotnet-code-with-the-analyzers.md) sets it up, and [Analyzer rules](../../analyzers/README.md) lists what each rule reports.

**Example:**

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

**Reviewed for:** Which application or host project files miss `LiteSpec.Analyzers` at the manifest pin with `PrivateAssets="all"`? Look at each application and host project file.

### Raise the LiteBus Handler Analyzers to Errors

**ID:** `litespec/backend/dotnet/packages#raise-the-litebus-handler-analyzers-to-errors`

**Strength:** default

**Default:** A project using LiteBus sets `LB1023` and `LB1024` to `error` in `.editorconfig` and lists its outside-world client types in `litebus.LB1024.forbidden_types`.

**Replacement:** A project whose command handlers run on another mediator names the analyzers that report nested mediation and outside-world clients in its handlers.

**Rationale:** Both LiteBus rules are off by default. `LB1023` reports nested mediation in a command handler and `LB1024` reports an outside-world client in a command handler, so consequences leave through event handlers after the commit under `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`. The client list replaces the defaults, so it names every port that leaves the process. The same file lists the project pipeline-owned collaborators under the key the [Configuration section of the LS2016 rule page](../../analyzers/rules/ls2016.md#configuration) names, such as its uniqueness claim service.

**Example:**

```text
dotnet_diagnostic.LB1023.severity = error
dotnet_diagnostic.LB1024.severity = error
litebus.LB1024.forbidden_types = Claimwick.Payouts.PayoutBankClient
litespec.LS2016.pipeline_owned_types = Claimwick.Spending.ExpenseKeyClaims
```

**Reviewed for:** Does `.editorconfig` set `LB1023` and `LB1024` to error and list `litebus.LB1024.forbidden_types`? Look at `.editorconfig`.
