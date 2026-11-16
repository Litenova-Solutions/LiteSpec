# .NET Naming and Code Style

**Stack:** dotnet

**Concern:** workspace

This page defines how the shared [Naming and Code Style](../../workspace/naming.md) rules apply to C# in a .NET backend. It covers file names, cancellation tokens, implementation style, and architectural names.

## Concepts

### Files and Style

A C# file uses the exact name of its primary type, so a reader finds any type by its file. Only a small private or file-scoped nested type stays with its owner; anything bigger gets its own file under its own name.

A method that takes a cancellation token places it last and names it `cancellationToken`. Callers then pass cancellation without reading the signature.

File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes keep diffs readable across every project.

### C# Role Names

The Role Suffixes table on the shared [Naming and Code Style](../../workspace/naming.md) page lists the suffixes for handlers, endpoints, reactions, and jobs. The table below adds the C#-specific names: the repository interface carries the `I` prefix, options classes carry the `Options` suffix, and the modules project carries one assembly marker.

| Role | Pattern | Example |
|:---|:---|:---|
| Repository interface | `I{Aggregate}Repository` | `IPostRepository` |
| API mapping class | `{UseCase}ApiMappings` | `CreateDraftApiMappings` |
| Options class | `{ConfigurationPurpose}Options` | `EmailOptions` |
| Assembly marker | `{Product}AssemblyMarker` | `ClaimwickAssemblyMarker` |

Mapping classes use `ApiMappings` instead of the context-dependent `Mappings` suffix.

## Standards

### Match C# Files and Primary Types

**ID:** `litespec/backend/dotnet/naming#match-c-files-and-primary-types`

**Strength:** required

**Statement:** A C# file MUST contain one primary top-level type and take that type's exact name.

**Rationale:** A file named for its type lets a reader and a tool find the type by the path. Two top-level types in one file break that lookup and merge their histories in review. Only a small private or file-scoped nested type stays with its owner.

**Checked by:** `dotnet build` reports `LS3003` for a C# file whose name differs from its primary type.

### Put the Cancellation Token Last

**ID:** `litespec/backend/dotnet/naming#put-the-cancellation-token-last`

**Strength:** required

**Statement:** In the dotnet stack, a method that takes a `CancellationToken` MUST place it last, or second to last before a `params` array, and name it `cancellationToken`.

**Rationale:** A token in a fixed trailing position lets every caller pass cancellation without reading the signature, and one name lets every reader tell the token from the data beside it. A token in the middle of a signature is passed in the wrong position, and a renamed token hides what it is at the call site.

**Example:** `SubmitExpenseCommandHandler.HandleAsync` takes `SubmitExpenseCommand command` and then `CancellationToken cancellationToken`, so a caller passes the data first and cancellation last.

**Checked by:** `dotnet build` reports `LS3002` for a cancellation token that is misplaced or misnamed.

### Keep Implementation Style Consistent

**ID:** `litespec/backend/dotnet/naming#keep-implementation-style-consistent`

**Strength:** required

**Statement:** Production C# MUST use file-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes.

**Rationale:** File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes change diff readability rather than behavior. One shared setting removes the debate from every review and keeps generated and hand-written code consistent.

**Checked by:** `dotnet build` reports `LS3004` for a concrete class with no derived type that is not sealed.

### Apply Architectural Role Suffixes

**ID:** `litespec/backend/dotnet/naming#apply-architectural-role-suffixes`

**Strength:** required

**Statement:** In the dotnet stack, a handler, endpoint, reaction, or job MUST carry the suffix its role assigns in the Role Suffixes table.

**Rationale:** The suffix states the architectural role in the name, so a reader and a tool tell command coordination, projection, and boundaries apart without opening the file. This provision binds `litespec/workspace/naming#use-architectural-suffixes` for .NET.

**Checked by:** `dotnet build` reports `LS2005` for a handler missing its command or query suffix, `LS2006` for an endpoint missing its suffix, and `LS2007` for a reaction or job whose name hides its kind.

### Name Result Parts After Their Result

**ID:** `litespec/backend/dotnet/naming#name-result-parts-after-their-result`

**Strength:** required

**Statement:** In the dotnet stack, a type a use-case result reaches through its properties MUST be named after the result it belongs to.

**Rationale:** A result part named for its result keeps each part beside the result it belongs to and avoids collisions between use cases shaping the same concept. This provision binds `litespec/workspace/naming#name-result-parts-after-their-result` for .NET.

**Checked by:** `dotnet build` reports `LS2009` for a type a use-case result reaches through its properties whose name does not start with the result name.

### Name Repository Variables After Their Interface

**ID:** `litespec/backend/dotnet/naming#name-repository-variables-after-their-interface`

**Strength:** required

**Statement:** In the dotnet stack, a repository parameter, field, or local MUST be named after its interface without the `I` prefix.

**Rationale:** Naming the variable after its interface keeps one name per repository at every use site. This provision binds `litespec/workspace/naming#name-repository-variables-after-their-interface` for .NET.

**Checked by:** `dotnet build` reports `LS2010` for a repository parameter, field, or local whose name does not follow its interface.

## Conventions

### Name Every Folder and Type for Its Reason

**ID:** `litespec/backend/dotnet/naming#name-every-folder-and-type-for-its-reason`

**Strength:** default

**Default:** No folder is named `Shared`, `Common`, `Util`, `Utils`, `Helpers`, or `Misc`, and no type carries one of those names or ends in `Helper`, `Manager`, `Util`, or `Utils`, outside the declared shared kernel.

**Replacement:** A project names its shared kernel project in `stackConfig.dotnet.sharedKernel` and keeps every other folder and type named for its reason.

**Rationale:** A folder named `Shared` or a type ending in `Manager` states no reason in its name, and each such name collects unrelated code. The shared kernel project is the one declared exception, named in one place, so the exception stays visible instead of spreading.

**Example:** The declared shared kernel project passes; `Billing/Helpers/` and `OrderManager` outside it are reported.

**Checked by:** `litespec model check` reports `name-reason-missing` (level S) for a folder or type named for the absence of a reason outside the declared shared kernel.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/naming#match-c-files-and-primary-types`, `litespec/backend/dotnet/naming#put-the-cancellation-token-last`, `litespec/workspace/naming#use-architectural-suffixes`, and `litespec/workspace/naming#keep-namespaces-aligned-with-folders`.

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
