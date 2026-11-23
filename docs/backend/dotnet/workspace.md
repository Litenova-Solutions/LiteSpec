# .NET Workspace

**Stack:** dotnet

**Concern:** workspace

This page defines how the shared [Workspace Structure](../../workspace/structure.md) rules apply to .NET. It covers where the solutions sit for a backend under `apps/api/`, where the AppHost lives, and how a development shortcut stays out of release builds.

## Concepts

### Solutions and Project Homes

The API solution sits directly under `apps/api/`. It remains the solution builds and test runs name. Production projects live under `src/` and test projects under `tests/`, so a test dependency reaching production shows in the path. Module projects group under `src/Modules/` and host projects under `src/Hosts/`, beside the Kernel and Platform projects. Knowledge projects group under `src/Knowledge/`, with one model project per area and one set project per variant.

When the workspace holds a second .NET deployable, such as a command-line tool, a root solution lists both project sets for editors that open across them. The root solution adds an editor-wide view. Builds, test runs, and configuration still name the API solution.

### Where the AppHost Lives

An AppHost isn't an application. Nothing ships it and no user reaches it. It's the C# model of how the applications run together: which processes and services start, how they connect, which settings each one gets, and in what order they start. Run mode starts them for local work, and publish mode writes deployment files, such as a Compose file, from the same model.

Describing how the workspace runs is the job of the `deploy/` folder at the workspace root, beside the proxy, container and server configuration. The AppHost lives there as `deploy/{Product}.AppHost/`, and `apps/` keeps only the applications that ship. Outside `apps/api/`, the API's build settings and central package versions don't reach the AppHost, so its project file states its own.

## Standards

### List Every Deployable Unit in the Root Solution

**ID:** `litespec/backend/dotnet/workspace#list-every-deployable-unit-in-the-root-solution`

**Strength:** required

**Statement:** A root solution MUST list the .NET projects of more than one deployable unit, with builds, test runs, and configuration still naming the API solution.

**Rationale:** A workspace with a second .NET deployable, such as a command-line tool, otherwise has no single file an editor can open across both. The root solution gives editors that view. The API solution stays at `apps/api/` and remains the one builds and test runs name. Two solution files listing overlapping projects are a supported MSBuild arrangement, and the projects stay where `litespec/workspace/structure#keep-production-and-test-code-separate` puts them.

**Example:** A workspace with an API and a command-line tool carries `apps/api/{Product}.slnx`, `apps/cli/{Product}.Cli.slnx` where one exists, and a root `{Product}.slnx` listing both sets.

**Reviewed for:** Does the root solution list more than one deployable unit, with builds, test runs, and configuration still naming the API solution? Look at the solution files beside the build configuration.

### Keep the AppHost in the Deploy Folder

**ID:** `litespec/backend/dotnet/workspace#keep-the-apphost-in-the-deploy-folder`

**Strength:** required

**Statement:** An AppHost MUST live at `deploy/{Product}.AppHost/` at the workspace root, never under `apps/`.

**Rationale:** `apps/` holds the applications that ship, and an AppHost ships nowhere: it models how those applications run together, locally and in the deployment files it publishes. Under `apps/api/src/` it reads as one of the API's layers while it reaches out of that tree for every frontend it starts, and anywhere else under `apps/` it reads as one more application. In `deploy/`, beside the rest of the deployment description, its path says what it is. `ServiceDefaults` stays under `apps/api/src/`, because the API references it and the AppHost does not.

**Example:** A workspace with an API, a Worker and a frontend carries:

```text
apps/
  api/
    src/
      Hosts/{Product}.WebApi/
      Hosts/{Product}.Worker/
    tests/
      {Product}.Architecture.Tests/   references the AppHost for its model tests
  {frontend}/
deploy/
  {Product}.AppHost/
    {Product}.AppHost.csproj
    Program.cs
```

A root solution, where `litespec/backend/dotnet/workspace#list-every-deployable-unit-in-the-root-solution` asks for one, lists the AppHost, and the API solution lists it too when a test project references it.

**Reviewed for:** Does every AppHost sit at `deploy/{Product}.AppHost/`, with none under `apps/`? Look at each AppHost project path from the workspace root.

### Exclude Development Shortcuts from Release Builds

**ID:** `litespec/backend/dotnet/workspace#exclude-development-shortcuts-from-release-builds`

**Strength:** required

**Statement:** A .NET project that holds a development shortcut MUST be referenced only in Debug builds, MUST register the shortcut only behind `#if DEBUG`, MUST tag each shortcut type with `/// <development-shortcut>reason</development-shortcut>`, and MUST refuse to start outside the Development environment.

**Rationale:** A project referenced in every build puts its shortcut into the release artifact, where no deployment setting can remove it. A `ProjectReference` with `Condition="'$(Configuration)' == 'Debug'"` keeps the project out of release builds, and the `#if DEBUG` registration keeps the host from naming a type that a release build does not have. The types themselves stay outside the directive. The compiler drops a disabled region from the compilation, so `LS5014` could not report a shortcut that a release build compiles. The tag gives `LS5014` one way to find each shortcut, and the environment check stops a Debug build on a shared host from running the shortcut. This provision binds `litespec/quality/development#keep-development-shortcuts-out-of-release-builds` for .NET.

**Example:** Claimwick's development sign-in is a project that the WebApi host references only in Debug builds:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <ProjectReference Include="..\..\Claimwick.DevelopmentSignIn\Claimwick.DevelopmentSignIn.csproj" />
</ItemGroup>
```

The project's type carries the tag and refuses to run outside Development, and `Program.cs` registers it only in Debug builds:

```csharp
/// <development-shortcut>Sign in as Ava locally, without the identity provider.</development-shortcut>
public static class DevelopmentSignInRegistration
{
    public static IServiceCollection AddDevelopmentSignIn(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("The development sign-in runs only in the Development environment.");
        }

        return services;
    }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Services.AddDevelopmentSignIn(builder.Environment);
#endif
```

**Checked by:** `dotnet build` reports `LS5014` for a tagged type that a compilation without `DEBUG` declares, in every project that holds a shortcut. Review the host project file for the Debug-only `ProjectReference`, and `Program.cs` for the `#if DEBUG` registration, since no rule reads the project file.

## Conventions

None.
