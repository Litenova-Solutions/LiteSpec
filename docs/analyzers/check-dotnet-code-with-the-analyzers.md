# Check .NET Code with the Analyzers

## Purpose

The LiteSpec Roslyn analyzers report breaks in LiteSpec .NET rules as compiler errors on every build, so a team sees each break in the editor instead of finding it in review. This guide installs the analyzer package once for the backend projects and their tests, shows how to read and fix the first errors, and records a deliberate departure. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- The .NET 10 SDK, which the manifest names as the .NET stack release.
- A solution with the business projects and hosts that [the .NET architecture page](../backend/dotnet/architecture.md) requires: the shared kernel and modules projects holding every module, and a WebApi host for composition and HTTP plumbing.
- One shared build file that every project imports, such as `apps/api/Directory.Build.props`, with central package management enabled as [the .NET configuration page](../backend/dotnet/config.md#centralize-nuget-versions) describes.

## Procedure

### 1. Add the Package Once for Every Project

Add one `PackageReference` in `apps/api/Directory.Build.props` so every project inherits it:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="5.0.0" PrivateAssets="all" />
</ItemGroup>
```

`PrivateAssets="all"` keeps the analyzer out of any package you publish. Keep the version in one central place to satisfy `litespec/workspace/dependencies#pin-every-dependency-version-centrally`: with central package management, put the version in `apps/api/Directory.Packages.props` instead:

```xml
<PackageVersion Include="LiteSpec.Analyzers" Version="5.0.0" />
```

and omit `Version` from the `PackageReference`. Never add a second copy in a project file.

### 2. Confirm the Layout

Each rule runs only on the files whose layer its provision covers. The analyzers read each file's layer from its folder and, in a use case folder, its role suffix, as the [Analyzers overview](README.md) describes. Test files resolve as Test. When the project selects the `layer-projects` extension, the analyzers read the layer from the assembly name suffix instead: `Shop.Domain` resolves as Domain, and the same suffix rule maps Application, Infrastructure, WebApi, and Worker, with `Application.Abstractions` for the shared contracts project. An assembly whose name carries no layer suffix is not analyzed.

When a project name does not end in a layer name under `layer-projects`, set the layer in its project file:

```xml
<PropertyGroup>
  <LiteSpecLayer>Domain</LiteSpecLayer>
</PropertyGroup>
```

The values are `Domain`, `Application.Abstractions`, `Application`, `Infrastructure`, `WebApi`, `Worker`, `Test`, and `None`. A value naming no known layer reports `LS2017`. Set `None` to opt a project out: opting out turns off the whole package, so no analyzer rule runs and the generator writes no source and reports no diagnostic. An assembly name with no layer suffix opts the project out in the same way. The package sets the layout from `selectedExtensions` in `.litespec/project.yaml`, so no extra wiring is needed. This step implements the shared kernel and modules projects with their hosts in [the .NET architecture page](../backend/dotnet/architecture.md).

The package follows the layers the project claims the same way. A project that claims the record model alone sees no findings from this package, and the generator writes no source. A claim of Implementation with `own` runs only the traceability findings `LS5001` to `LS5012`, so a project can mark its code in its own layout before it takes the layer rules. A claim of Implementation with `litespec` runs every rule. To name a practice for one project without changing the project file, set the property in the project file:

```xml
<PropertyGroup>
  <LiteSpecImplementation>own</LiteSpecImplementation>
</PropertyGroup>
```

An absent property reads as `litespec`. An empty property means the layer is not claimed, and any other value reads as `litespec`.

### 3. Build and Read the First Diagnostics

Build the solution:

```sh
dotnet build apps/api/Shop.slnx
```

Each finding is an error by default, fails the build, and names the provision it enforces. A typical first line reads:

```text
error LS1002: Enum 'OrderStatus' models a closed set; use an abstract record with sealed cases and a stable Code (https://litespec.dev/5.0.0/backend/dotnet/domain/#model-every-closed-set-of-domain-values-without-enums)
```

The trailing address is the help link: it opens the provision the rule enforces, here `litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums`. In Visual Studio the same error appears in the Error List with its ID, file, and line, and the tooltip carries the help link. In the terminal, `dotnet build` prints the line above.

### 4. Fix a Finding

Work through one finding at a time, starting with the Domain layer. The two examples below cover the most common first errors. For any other error, open its rule page from the [Analyzer rules overview](rules/README.md), which states what the rule reports and how to fix the code.

#### Replace an Enum with a Closed Record Hierarchy

LS1002 reports any enum in Domain. Before:

```csharp
public enum OrderStatus
{
    Pending,
    Succeeded,
    Failed,
}
```

After the rewrite, following the shape on [the .NET domain page](../backend/dotnet/domain.md#model-every-closed-set-of-domain-values-without-enums):

```csharp
public abstract record OrderStatus
{
    public abstract string Code { get; }

    public static OrderStatus FromCode(string code) => code switch
    {
        "pending" => new OrderPendingStatus(),
        "succeeded" => new OrderSucceededStatus(),
        "failed" => new OrderFailedStatus(),
        _ => throw new OrderUnsupportedStatusException(code),
    };
}

public sealed record OrderPendingStatus : OrderStatus
{
    public override string Code => "pending";
}

public sealed record OrderSucceededStatus : OrderStatus
{
    public override string Code => "succeeded";
}

public sealed record OrderFailedStatus : OrderStatus
{
    public override string Code => "failed";
}
```

Boundary code converts a label through `FromCode`, and an unrecognized label fails validation instead of falling into a default case. Callers branch on the case type with a `switch` expression. Define `OrderUnsupportedStatusException` as a sealed `DomainException` subclass with a stable code, as `litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions` requires.

#### Pass Time in as an Argument

LS1009 reports any read of a clock, randomness, or new identifier in Domain, such as `DateTimeOffset.UtcNow`, `Guid.NewGuid`, or `Random.Shared`. Before:

```csharp
public void Publish()
{
    State = new PostPublishedState(DateTimeOffset.UtcNow);
}
```

After:

```csharp
public void Publish(DateTimeOffset publishedAt)
{
    State = new PostPublishedState(publishedAt);
}
```

The caller in Application supplies `publishedAt` from its own clock, and tests pass an explicit value, so Domain stays deterministic. This matches the `Publish` shape on [the .NET domain page](../backend/dotnet/domain.md#model-every-aggregate-lifecycle-with-state-records) and satisfies `litespec/backend/domain#pass-nondeterministic-values-into-domain`.

### 5. Let the Build Fail on Findings in CI

Every rule is an error by default, so `dotnet build` already fails on a finding and no extra step is needed. The `build` job compiles the solution with `dotnet build <solution>`, so a finding fails the pull request under `litespec/quality/ci#run-applicable-gates-on-every-pull-request`. See [the continuous integration page](../quality/ci.md#run-applicable-gates-on-every-pull-request) for the jobs each change selects. Lower a severity only where a departure allows it, as the next step describes.

### 6. Record a Deliberate Departure

When a finding is wrong for the project, record a departure that covers the provision, with its reason, as [the departures page](../core/departures.md) describes. Keep the record at `.litespec/governance/departures/<name>.yaml`, then suppress narrowly next to the code:

```csharp
#pragma warning disable LS1011 // departure: .litespec/governance/departures/domain-docs-deferred.yaml
public string LegacyLabel { get; }
#pragma warning restore LS1011
```

Or scope an `.editorconfig` entry to the one file:

```ini
[Shop/Legacy/LegacyLabels.cs]
dotnet_diagnostic.LS1011.severity = suggestion
```

The example above quiets `litespec/backend/dotnet/domain#document-every-public-domain-contract` for one legacy file only. Never set a project-wide `none` without the record: the departure carries the reason, and the suppression only quiets the compiler where the record allows.

## Check

Confirm the setup with these checks:

- `dotnet build apps/api/Shop.slnx` lists no `LS` errors, or lists only ones a departure covers.
- Add a temporary enum to a Domain folder and rebuild: the build reports `LS1002` naming the enum.
- Remove the enum and rebuild: the error is gone.
- Claim only `intent` and `behavior` in `layers` in `.litespec/project.yaml` and rebuild: the build lists no `LS` diagnostics. Claim `implementation: own` and rebuild with the enum in place: the build still lists no `LS1002`. Set `implementation: litespec` and rebuild: `LS1002` returns.
