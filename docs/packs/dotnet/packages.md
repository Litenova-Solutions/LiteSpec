# .NET Packages

**Stack:** dotnet

**Concern:** workspace

This page states which LiteBus modules each layer references and how the analyzers report breaches. It is for developers who add a package reference or read a build error with a provision link. Referencing only the module the code uses keeps each dispatch surface exact, and the analyzers report each breach at the line breaking it.

## Concepts

### LiteBus Modules and the Analyzers

LiteBus ships as modules for commands, queries, and events rather than one bus package. A layer references only the module its code dispatches, so each dispatch surface stays exact. A layer publishing no events carries no event contracts, and a unified bus package would pull contracts for commands, queries, and events into a layer dispatching only some of them.

The LiteSpec analyzers report a breach of a .NET provision where it happens, with a link to the provision. Without them, a breach surfaces only in review, if at all. The analyzer package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time.

### Outbox and Test Modules

The outbox, auditing, and problem details provisions use LiteBus at the `8.0.0-preview.8` pin. Module events and domain-event reactions travel through the LiteBus outbox in process: a Marten project stores outbox rows in its own session through `LiteBus.Outbox.Storage.Marten`, and a project persisting through Entity Framework Core uses `LiteBus.Outbox.Storage.EntityFrameworkCore`, with outbound integration events leaving through `LiteBus.Outbox.Dispatch.Amqp` or `LiteBus.Outbox.Dispatch.Kafka`.

The audit chain runs through `LiteBus.Auditing`, `LiteBus.Auditing.Abstractions` and, for a Marten store, `LiteBus.Auditing.Storage.Marten`. An ASP.NET Core host maps refusals to problem responses through `LiteBus.Extensions.AspNetCore`. Acceptance tests drain the outbox through `LiteBus.Testing.DurableMessaging`, as the [testing page](testing.md) describes, and outbox telemetry arrives through `LiteBus.Outbox.Extensions.OpenTelemetry`. Each resolves at its manifest pin.

Command handlers sending a second message or calling an outside system are reported at the build: the handler analyzer rule reports nested mediation in a command handler, and its companion rule reports an outside-world client in a command handler. Consequences then leave through event handlers after the commit.

## Standards

None.

## Conventions

### Run LiteSpec Analyzers in the Business Projects and Hosts

**ID:** `litespec/packs/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`

**Strength:** default

**Default:** Reference `LiteSpec.Analyzers` at the manifest pin, with `PrivateAssets="all"`, from the Kernel, Platform, and module projects and each host project.

**Replacement:** A project names the projects its analyzer run covers in its project documentation.

**Rationale:** The analyzers report a breach of a .NET provision in the editor and the build, at the line breaking it, with a link to the provision. Without them, a breach surfaces only in review, if at all. The analyzer package is a development dependency: it adds nothing to the built assemblies, so Domain stays package-free at run time.

`LS2016` reports a pipeline-owned collaborator in a handler constructor. Its default list covers the LiteBus unit of work, idempotency store, and outbox, and a project adds its uniqueness claim service. Settled by `decision/0103-dotnet-stack-binding`, which keeps the analyzer run in the business projects and hosts.

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
  </packageSourceMapping>
</packageSourceMapping>
```

A project reference to the analyzer project does not carry its dependency assemblies with it, so the analyzers fail to load. The local feed is the way.

**Reviewed for:** Does every Kernel, Platform, module, and host project file reference `LiteSpec.Analyzers` at the manifest pin with `PrivateAssets="all"`? Look at each business and host project file.
