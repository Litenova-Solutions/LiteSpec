# .NET Packages

**Stack:** dotnet

**Concern:** workspace

This page defines how the shared [Dependencies](../../workspace/dependencies.md) rules apply to .NET. It covers which LiteBus modules each layer references and how the analyzers report breaches at the line that breaks them.

## Concepts

### LiteBus Modules and the Analyzers

LiteBus ships as modules for commands, queries, and events rather than one bus package. A layer references only the module its code dispatches, so each dispatch surface stays exact. A layer that never publishes events carries no event contracts.

The LiteSpec analyzers report a breach of a .NET provision where it happens, with a link to the provision. Without them, a breach surfaces only in review, if at all. The analyzer package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time.

### Outbox and Test Modules

The outbox, auditing, and problem details provisions need LiteBus at the `8.0.0-preview.3` pin. Module events and domain-event reactions travel through the LiteBus outbox in process: a Marten project stores outbox rows in its own session through `LiteBus.Outbox.Storage.Marten`, and a project that persists through Entity Framework Core uses `LiteBus.Outbox.Storage.EntityFrameworkCore`, with outbound integration events leaving through `LiteBus.Outbox.Dispatch.Amqp` or `LiteBus.Outbox.Dispatch.Kafka`. The audit chain runs through `LiteBus.Auditing`, `LiteBus.Auditing.Abstractions` and, for a Marten store, `LiteBus.Auditing.Storage.Marten`. An ASP.NET Core host maps refusals to problem responses through `LiteBus.Extensions.AspNetCore`. Acceptance tests drain the outbox through `LiteBus.Testing.DurableMessaging`, as `litespec/backend/dotnet/testing#drain-the-outbox-in-acceptance-tests` states, and outbox telemetry arrives through `LiteBus.Outbox.Extensions.OpenTelemetry`. Each resolves at its manifest pin.

## Standards

None.

## Conventions

### Reference Only the LiteBus Module Required

**ID:** `litespec/backend/dotnet/packages#reference-only-the-litebus-module-required`

**Strength:** default

**Default:** Reference the LiteBus command, query, or event module that the code actually uses.

**Replacement:** A project names the LiteBus modules each layer references in its project documentation.

**Rationale:** A unified bus package pulls contracts for commands, queries, and events into a layer that dispatches only some of them. Referencing only the module the code uses keeps each dispatch surface exact.

**Reviewed for:** Does every project file reference only the LiteBus modules its code dispatches? Look at each project file.

### Run LiteSpec Analyzers in the Business Projects and Hosts

**ID:** `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`

**Strength:** default

**Default:** Reference `LiteSpec.Analyzers` at the manifest pin, with `PrivateAssets="all"`, from the Kernel, Platform, and module projects and each host project.

**Replacement:** A project names the projects its analyzer run covers in its project documentation.

**Rationale:** The analyzers report a breach of a .NET provision in the editor and the build, at the line that breaks it, with a link to the provision. Without them, a breach surfaces only in review, if at all. The analyzer package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time. `LS2016` reports a pipeline-owned collaborator in a handler constructor. Its default list covers the LiteBus unit of work, idempotency store, and outbox, and a project adds its uniqueness claim service. [Check .NET code with the analyzers](../../analyzers/check-dotnet-code-with-the-analyzers.md) sets it up, and [Analyzer rules](../../analyzers/README.md) lists what each rule reports. Settled by [decision/0103-dotnet-stack-binding](decision/0103-dotnet-stack-binding).

**Example:**

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

An unreleased build is consumed the same way, through a local feed. Pack the analyzers from the LiteSpec checkout, so the package carries the analyzer, rule library, and mark reader assemblies in its analyzer folder:

```sh
dotnet pack apps/analyzers/src/LiteSpec.Analyzers
```

Then point a local source at the packages folder, with source mapping for `LiteSpec.*`:

```xml
<packageSources>
  <add key="litespec-local" value="../LiteSpec/artifacts/packages" />
</packageSources>
<packageSourceMapping>
  <packageSource key="litespec-local">
    <package pattern="LiteSpec.*" />
  </packageSource>
</packageSourceMapping>
```

A project reference to the analyzer project does not carry its dependency assemblies with it, so the analyzers fail to load. Use the local feed instead.

**Reviewed for:** Does every Kernel, Platform, module, and host project file reference `LiteSpec.Analyzers` at the manifest pin with `PrivateAssets="all"`? Look at each business and host project file.

### Raise the LiteBus Handler Analyzers to Errors

**ID:** `litespec/backend/dotnet/packages#raise-the-litebus-handler-analyzers-to-errors`

**Strength:** default

**Default:** A project using LiteBus sets `litebus_forbid_nested_commands = true` in `.editorconfig`, sets `LB1023` and `LB1024` to `error`, and lists its outside-world client types in `litebus.LB1024.forbidden_types`.

**Replacement:** A project whose command handlers run on another mediator names the analyzers that report nested mediation and outside-world clients in its handlers.

**Rationale:** Both LiteBus rules are off by default, so a command handler can send a second message or call an outside system with no report. `LB1023` reports nested mediation in a command handler and reports nothing until `litebus_forbid_nested_commands` is `true`. The runtime counterpart is `ForbidNestedCommands()` on the command module builder, which throws `NestedMediationException` before the inner pipeline starts, so the option pairs the build check with that guarantee. `LB1024` reports an outside-world client in a command handler. Consequences then leave through event handlers after the commit, under `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`. The client list replaces the defaults, so it names every port that leaves the process. The same file lists the project pipeline-owned collaborators under the key the [Configuration section of the LS2016 rule page](../../analyzers/rules/ls2016.md#configuration) names, such as its uniqueness claim service.

**Example:**

```text
litebus_forbid_nested_commands = true
dotnet_diagnostic.LB1023.severity = error
dotnet_diagnostic.LB1024.severity = error
litebus.LB1024.forbidden_types = Claimwick.Payouts.PayoutBankClient
litespec.LS2016.pipeline_owned_types = Claimwick.Spending.ExpenseKeyClaims
```

**Reviewed for:** Does `.editorconfig` set `litebus_forbid_nested_commands` to `true`, `LB1023` and `LB1024` to error, and list `litebus.LB1024.forbidden_types`? Look at `.editorconfig`.
