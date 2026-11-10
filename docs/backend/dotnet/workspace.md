# .NET Workspace

**Stack:** dotnet
**Concern:** workspace

## Intent

A .NET backend lives under `apps/api/`, with the solution beside its projects. This page states that .NET layout: the workspace solution and where the AppHost lives, so every file has one fixed address. The shared workspace rules live on [Workspace Structure](../../workspace/structure.md); this page holds only their .NET form.

## Concepts

### Solutions and Project Homes

The API solution sits directly under `apps/api/` and remains the solution builds and test runs name. Production projects live under `src/` and test projects under `tests/`, so a test dependency reaching production shows in the path. When the workspace holds a second .NET deployable, a root solution lists both sets for editors that open across them.

### The AppHost Placement

An AppHost that runs the API alone stays under `apps/api/src/` with the projects it hosts. An AppHost that also runs frontends orchestrates the workspace rather than the API, so it lives at `apps/apphost/` beside the applications it runs.

## Standards

### Keep One Workspace Solution Beside the API Solution

**ID:** `litespec/backend/dotnet/workspace#keep-one-workspace-solution-beside-the-api-solution`

**Strength:** rule

**Statement:** A root solution MUST list the .NET projects of more than one deployable unit, with builds, test runs, and configuration still naming the API solution.

**Rationale:** The API solution stays at `apps/api/` and remains the one builds and test runs name. A workspace with a second .NET deployable, such as a command-line tool, otherwise has no single file an editor can open across both. Two solution files listing overlapping projects are a supported MSBuild arrangement, and the projects stay where `litespec/workspace/structure#keep-production-and-test-code-separate` puts them.

**Example:** A workspace with an API and a command-line tool carries `apps/api/{Product}.slnx`, `apps/cli/{Product}.Cli.slnx` where one exists, and a root `{Product}.slnx` listing both sets.

**Reviewed for:** Which root solution files list a single deployable, or which builds, test runs, or configuration name outside the API solution? Look at the solution files beside the build configuration.

### Place a Workspace AppHost Beside the Applications It Runs

**ID:** `litespec/backend/dotnet/workspace#place-a-workspace-apphost-beside-the-applications-it-runs`

**Strength:** rule

**Statement:** An AppHost that runs more than one application under `apps/` MUST live at `apps/apphost/{ProjectName}.AppHost/` rather than under `apps/api/src/`.

**Rationale:** An AppHost that runs the API and its dependencies alone stays under `apps/api/src/` with the projects it hosts. An AppHost that also runs the frontends orchestrates the workspace rather than the API: under `apps/api/src/` it reads as one of the API's layers while reaching out of that tree for every frontend it starts. Beside the applications it runs, its path says what it serves. `ServiceDefaults` stays under `apps/api/src/`, because the API references it and the AppHost does not.

**Example:** A workspace with an API and three frontends carries:

```text
apps/
  api/
    src/
      {Product}.WebApi/
    tests/
      {Product}.Architecture.Tests/   references the AppHost for its model tests
  apphost/
    {Product}.AppHost/
      {Product}.AppHost.csproj
      Program.cs
  {frontend}/
```

The workspace solution of `litespec/backend/dotnet/workspace#keep-one-workspace-solution-beside-the-api-solution` lists the AppHost, and the API solution lists it too when a test project references it.

**Reviewed for:** Which AppHosts sit outside their placed home for what they run? Look at each AppHost path beside the applications it runs.

## Conventions

None.
