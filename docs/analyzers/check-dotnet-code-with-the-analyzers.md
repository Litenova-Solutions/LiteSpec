# Check .NET Code with the Analyzers

The LiteSpec Roslyn analyzers report breaks in LiteSpec .NET rules as compiler errors on every build, so a team sees each break in the editor instead of finding it in review. This guide installs the analyzer package once for the backend projects and their tests, shows how to read and fix the first errors, and records a deliberate waiver where a finding is wrong for the project. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

You need:

- The .NET 10 SDK, which the manifest names as the .NET stack release.
- A solution laid out with the business projects and hosts that [the .NET architecture page](../backend/dotnet/architecture.md) requires: the shared kernel and modules projects holding every module, plus a WebApi host for composition and HTTP plumbing.
- One shared build file imported by every project, such as `apps/api/Directory.Build.props`, with central package management enabled as [the .NET configuration page](../backend/dotnet/config.md#centralize-nuget-versions) describes.

## Procedure

### 1. Add the Package Once for Every Project

Put a single `PackageReference` in `apps/api/Directory.Build.props` so every project inherits the analyzers:

```xml
<ItemGroup>
  <PackageReference Include="LiteSpec.Analyzers" Version="7.0.0" PrivateAssets="all" />
</ItemGroup>
```

`PrivateAssets="all"` keeps the analyzer out of any package you publish. Keep the version in one central place, which satisfies `litespec/workspace/dependencies#pin-every-dependency-version-centrally`: with central package management, declare it in `apps/api/Directory.Packages.props` instead:

```xml
<PackageVersion Include="LiteSpec.Analyzers" Version="7.0.0" />
```

and drop `Version` from the `PackageReference`. Do not add a second copy in any project file.

### 2. Confirm the Layout

A rule only runs in the layers its provision covers, so next confirm that every file resolves to the layer you mean. The analyzers read the layer from the file's folder and, inside a use case folder, from its role suffix, as the [Analyzers overview](README.md) describes; test files resolve as Test. When the project selects the `layer-projects` extension, the layer comes from the assembly name suffix instead: `Shop.Domain` resolves as Domain, and the same suffix rule maps Application, Infrastructure, WebApi, and Worker, with `Application.Abstractions` for the shared contracts project. An assembly whose name carries no layer suffix is not analyzed.

Under `layer-projects`, a project whose name ends in no layer name declares its layer in its project file:

```xml
<PropertyGroup>
  <LiteSpecLayer>Domain</LiteSpecLayer>
</PropertyGroup>
```

The accepted values are `Domain`, `Application.Abstractions`, `Application`, `Infrastructure`, `WebApi`, `Worker`, `Test`, and `None`. A value that names no known layer reports `LS2017`. `None` opts a project out and turns off the whole package there: no analyzer rule runs, and the generator writes no source and reports no diagnostic. An assembly name with no layer suffix opts the project out in the same way. The package takes the layout from `selectedExtensions` in `.litespec/project.yaml`, so there is nothing else to wire. This step puts in place the shared kernel and modules projects with their hosts from [the .NET architecture page](../backend/dotnet/architecture.md).

The package also follows the record layers the project claims. A project that claims the record model alone gets no findings from this package, and the generator writes no source. A claim of Implementation with `own` runs only the traceability findings `LS5001` to `LS5012`, so a project can mark its code in its own layout before taking on the layer rules. A claim of Implementation with `litespec` runs every rule. To name a practice for one project without changing `.litespec/project.yaml`, set the property in that project's file:

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

Each finding is an error by default: it fails the build and points at the provision it enforces. A typical first line looks like this:

```text
error LS1002: Enum 'OrderStatus' models a closed set; use an abstract record with sealed cases and a stable Code (https://litespec.dev/7.0.0/backend/dotnet/domain/#model-every-closed-set-of-domain-values-without-enums)
```

The address at the end is the help link. It opens the provision the rule enforces, here `litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums`. Visual Studio lists the same error in the Error List with its ID, file, and line, and the tooltip carries the help link; in the terminal, `dotnet build` prints the line above.

### 4. Fix a Finding

Work through the findings one at a time, starting with the Domain layer. The two examples below cover the errors most projects see first; for anything else, open the rule's page from the [Analyzer rules overview](rules/README.md), which shows what the rule reports and how to fix the code.

#### Replace an Enum with a Closed Record Hierarchy

LS1002 reports every enum in Domain. Before:

```csharp
public enum OrderStatus
{
    Pending,
    Succeeded,
    Failed,
}
```

After the rewrite, in the shape [the .NET domain page](../backend/dotnet/domain.md#model-every-closed-set-of-domain-values-without-enums) shows:

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

Boundary code turns a label into a case through `FromCode`. A label nobody recognizes fails validation instead of falling into a default case, and callers branch on the case type with a `switch` expression. Define `OrderUnsupportedStatusException` as a sealed `DomainException` subclass with a stable code, as `litespec/backend/dotnet/domain#reject-business-violations-with-domain-exceptions` requires.

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

The caller in Application supplies `publishedAt` from its own clock, and tests pass an explicit value, so Domain stays deterministic. This follows the `Publish` shape on [the .NET domain page](../backend/dotnet/domain.md#model-every-aggregate-lifecycle-with-state-records) and satisfies `litespec/backend/domain#pass-nondeterministic-values-into-domain`.

### 5. Let the Build Fail on Findings in CI

There is nothing more to set up: every rule is an error by default, so `dotnet build` already fails on a finding. The `build` job compiles the solution with `dotnet build <solution>`, which means a finding fails the pull request under `litespec/quality/ci#run-applicable-gates-on-every-pull-request`. [The continuous integration page](../quality/ci.md#run-applicable-gates-on-every-pull-request) lists the jobs each change selects. Lower a severity only where a waiver allows it, as the next step describes.

### 6. Record a Deliberate Waiver

When a finding is wrong for the project, record a waiver that covers the provision, with its reason, as [the waivers page](../core/waivers.md) describes. Keep the record at `.litespec/governance/waivers/<name>/<name>.yaml`, then suppress narrowly beside the code:

```csharp
#pragma warning disable LS1011 // waiver: .litespec/governance/waivers/domain-docs-deferred/domain-docs-deferred.yaml
public string LegacyLabel { get; }
#pragma warning restore LS1011
```

Or scope an `.editorconfig` entry to the single file:

```ini
[Shop/Legacy/LegacyLabels.cs]
dotnet_diagnostic.LS1011.severity = suggestion
```

That entry quiets `litespec/backend/dotnet/domain#document-every-public-domain-contract` for one legacy file only. Never set a project-wide `none` without the record behind it: the waiver holds the reason, and the suppression quiets the compiler only where the record allows.

## Check

Confirm the setup with these checks:

- `dotnet build apps/api/Shop.slnx` lists no `LS` errors, or only ones a waiver covers.
- Add a temporary enum to a Domain folder and rebuild: the build reports `LS1002` naming the enum.
- Remove the enum and rebuild: the error is gone.
- Claim only `intent` and `behavior` in `layers` in `.litespec/project.yaml` and rebuild: the build lists no `LS` diagnostics. Claim `implementation: own` and rebuild with the enum in place: the build still lists no `LS1002`. Set `implementation: litespec` and rebuild: `LS1002` returns.
