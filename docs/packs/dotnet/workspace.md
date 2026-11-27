# .NET Workspace

**Stack:** dotnet

**Concern:** workspace

This page states where .NET solutions, hosts, and the AppHost live in the workspace. It is for developers who open the backend, add a deployable, or keep development shortcuts out of release builds. Production projects sit under `src/` and test projects under `tests/`, so a test dependency reaching production shows in the path.

## Concepts

### Solutions and Project Homes

The API solution sits directly under `apps/api/`. It stays the solution builds and test runs name. Production projects live under `src/` and test projects under `tests/`, so a test dependency reaching production shows in the path.

Module projects group under `src/Modules/` and host projects under `src/Hosts/`, beside the Kernel and Platform projects. Knowledge projects group under `src/Knowledge/`, with one model project per area and one set project per variant.

When the workspace holds a second .NET deployable, such as a command-line tool, a root solution lists both project sets for editors opening across them. The root solution adds an editor-wide view. Builds, test runs, and configuration still name the API solution.

### Where the AppHost Lives

An AppHost is not an application. Nothing ships it and no user reaches it. It is the C# model of how the applications run together: which processes and services start, how they connect, which settings each one gets, and in what order they start. Run mode starts them for local work, and publish mode writes deployment files, such as a Compose file, from the same model.

Describing how the workspace runs is the job of the `deploy/` folder at the workspace root, beside the proxy, container and server configuration. The AppHost lives there as `deploy/{Product}.AppHost/`, and `apps/` keeps only the applications that ship. Outside `apps/api/`, the API's build settings and central package versions pass no settings to the AppHost, so its project file states its own. `ServiceDefaults` stays under `apps/api/src/`, because the API references it and the AppHost does not.

A workspace with an API, a Worker and a frontend carries:

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

A root solution, where one is asked for, lists the AppHost. The API solution lists it too when a test project references it.

### Fast Local Checks with Fuse

Fuse, the local C# check accelerator at [https://fuse.codes](https://fuse.codes), is free and optional. One run of `fuse init` per repository sets it up.

After that, `fuse check` runs the checks for changed files and `fuse build --changed` builds what changed. A Claimwick developer runs `fuse init` once after cloning, then `fuse check` before each commit instead of waiting for a full build.

## Standards

### Exclude Development Shortcuts from Release Builds

**ID:** `litespec/packs/dotnet/workspace#exclude-development-shortcuts-from-release-builds`

**Strength:** required

**Statement:** A .NET project that holds a development shortcut MUST stay out of release builds: referenced only in Debug builds, registered only behind `#if DEBUG`, with each shortcut type carrying its `development-shortcut` tag and refusing to run outside the Development environment.

**Rationale:** A project referenced in every build puts its shortcut into the release artifact, where no deployment setting removes it. A `ProjectReference` with `Condition="'$(Configuration)' == 'Debug'"` keeps the project out of release builds, and the `#if DEBUG` registration keeps the host from naming a type a release build does not have. The types themselves stay outside the directive.

The compiler drops a disabled region from the compilation, so `LS5014` reports no shortcut a release build compiles. The tag gives `LS5014` one way to find each shortcut, and the environment check stops a Debug build on a shared host from running the shortcut.

**Example:** Claimwick's development sign-in is a project the WebApi host references only in Debug builds:

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
