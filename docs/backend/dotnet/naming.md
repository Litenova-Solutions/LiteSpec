# .NET Naming and Code Style

**Stack:** dotnet
**Concern:** workspace

## Intent

C# code in a .NET backend names its files and implementation shapes one way across every project. This page states those .NET rules: one primary type per file, a shared implementation style, and names that state their reason. The shared naming rules live on [Naming and Code Style](../../workspace/naming.md); this page holds only their .NET form.

## Concepts

### Files and Style

A C# file takes its primary type's exact name, so a reader finds any type by its file. File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes keep diffs readable across every project.

### C# Role Suffixes

| Role | Pattern | Example |
|:---|:---|:---|
| Repository interface | `I{Aggregate}Repository` | `IPostRepository` |
| API mapping class | `{UseCase}ApiMappings` | `CreateDraftApiMappings` |
| Options class | `{ConfigurationPurpose}Options` | `EmailOptions` |
| Assembly marker | `{Product}AssemblyMarker` | `ClaimwickAssemblyMarker` |

In the dotnet stack, the repository interface carries the `I` prefix, options classes carry the `Options` suffix, and the modules project carries one assembly marker.

Mapping classes use `ApiMappings` instead of the context-dependent `Mappings` suffix.

## Standards

### Match C# Files and Primary Types

**ID:** `litespec/backend/dotnet/naming#match-c-files-and-primary-types`

**Strength:** rule

**Statement:** A C# file MUST contain one primary top-level type and take that type's exact name.

**Rationale:** A file named for its type lets a reader and a tool find the type by the path. Two top-level types in one file break that lookup and merge their histories in review. Only a small private or file-scoped nested type stays with its owner.

**Checked by:** `dotnet build` reports `SA1649` for a C# file whose name differs from its primary type.

### Keep Implementation Style Consistent

**ID:** `litespec/backend/dotnet/naming#keep-implementation-style-consistent`

**Strength:** rule

**Statement:** Production C# MUST use file-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes.

**Rationale:** File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes change diff readability rather than behavior. One shared setting removes the debate from every review and keeps generated and hand-written code alike.

**Checked by:** `dotnet build` reports `LS3004` for a concrete class with no derived type that is not sealed.

## Conventions

### Name No Folder or Type for the Absence of a Reason

**ID:** `litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason`

**Strength:** default

**Default:** No folder is named `Shared`, `Common`, `Util`, `Utils`, `Helpers`, or `Misc`, and no type carries one of those names or ends in `Helper`, `Manager`, `Util`, or `Utils`, outside the declared shared kernel.

**Replacement:** A project names its shared kernel project in `stackConfig.dotnet.sharedKernel` and keeps every other folder and type named for its reason.

**Rationale:** A folder named `Shared` or a type ending in `Manager` tells the reader the author found no reason to name; each such name becomes the drawer where unrelated code gathers. The shared kernel project is the one declared exception, named in one place, so the exception stays visible instead of spreading.

**Example:** The declared shared kernel project passes; `Billing/Helpers/` and `OrderManager` outside it are reported.

**Checked by:** `litespec model check` reports `name-reason-missing` (level S) for a folder or type named for the absence of a reason outside the declared shared kernel.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/naming#match-c-files-and-primary-types`, `litespec/workspace/naming#use-architectural-suffixes`, and `litespec/workspace/naming#keep-namespaces-aligned-with-folders`.

```csharp
namespace Example.Posts.UseCases.CreateDraft;

internal sealed class CreateDraftCommandHandler(
    IPostRepository postRepository,
    IClock clock)
{
    public async Task<CreateDraftCommandResult> HandleAsync(
        CreateDraftCommand command,
        CancellationToken cancellationToken)
    {
        // Operation code.
    }
}
```
