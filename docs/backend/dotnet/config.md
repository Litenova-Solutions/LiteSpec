# .NET Configuration

**Stack:** dotnet

**Concern:** workspace

A .NET backend pins its SDK, build settings, package versions, and local tools to fixed versions through files at fixed paths. This page defines the .NET form of the shared [Configuration](../../workspace/config.md) rules: what each file sets, so every machine builds the same output.

## Concepts

### The Four Pinning Files

Four files at fixed paths decide how the backend builds, wherever it builds.

The root `global.json` names the lowest SDK that can build the workspace. `apps/api/Directory.Build.props` sets the target framework and the build settings every project shares. `apps/api/Directory.Packages.props` pins every NuGet version in one place, so project files name packages without versions. The root `.config/dotnet-tools.json` pins the local tools that documented commands invoke.

## Standards

### Set the SDK Floor at the Workspace Root

**ID:** `litespec/backend/dotnet/config#set-the-sdk-floor-at-the-workspace-root`

**Strength:** required

**Statement:** The root `global.json` MUST set the first feature band of the manifest .NET major release as its SDK version and roll forward with `latestFeature`.

**Rationale:** The first feature band of the manifest's major release is the lowest SDK that builds the workspace, and `latestFeature` roll-forward lets a later feature band of the same major release build it too. A newer major release needs a workspace change, so a local machine and CI stay on one major line. The compiler and its analyzers ship with the SDK, so a different SDK can still build different output within that line; the floor sets the minimum, not identical output. The manifest names the major release; `Directory.Build.props` sets its target framework.

**Reviewed for:** Does the root `global.json` set the manifest major release's first feature band with `latestFeature` roll-forward? Look at the root `global.json`.

### Centralize .NET Build Settings

**ID:** `litespec/backend/dotnet/config#centralize-net-build-settings`

**Strength:** required

**Statement:** `apps/api/Directory.Build.props` MUST set the target framework of the manifest .NET stack release, and enable nullable types, implicit usings, and warnings as errors.

**Rationale:** Build settings drift project by project when each project owns its own: one project targets an older framework, another drops nullable types, and warnings pile up where nobody reads them. One file owns the framework, nullability, usings, and warnings-as-errors, so a change applies to every project and review sees it in one diff.

**Reviewed for:** Does `apps/api/Directory.Build.props` set the manifest target framework with nullable types, implicit usings, and warnings as errors? Look at that file.

### Centralize NuGet Versions

**ID:** `litespec/backend/dotnet/config#centralize-nuget-versions`

**Strength:** required

**Statement:** `apps/api/Directory.Packages.props` MUST enable central package management and carry an exact version within each manifest pin.

**Rationale:** A version written in a project file is invisible to the manifest that owns it, and two projects drift to two versions of one package. Central management leaves project files naming packages only, while one file carries the exact version within each manifest pin, so an upgrade touches one place.

**Reviewed for:** Does `apps/api/Directory.Packages.props` enable central management with the manifest pins, with versions in no project file? Look at the central file beside the project files.

### Commit the Tool Manifest

**ID:** `litespec/backend/dotnet/config#commit-the-tool-manifest`

**Strength:** required

**Statement:** The root `.config/dotnet-tools.json` MUST pin every local .NET tool that a documented command invokes.

**Rationale:** A tool installed by version range resolves differently on two machines, and a build that passes locally fails in CI. Pinning every tool a documented command invokes keeps local and CI runs on one version; when a tool ships in the manifest's .NET release, its pin matches that release. The manifest pins the Stryker.NET runner that `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines` names.

**Reviewed for:** Does the root `.config/dotnet-tools.json` pin every local tool a documented command invokes, including the Stryker.NET runner? Look at the tool manifest.

## Conventions

None.
