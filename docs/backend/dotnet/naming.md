# .NET Naming and Code Style

## Intent

C# code in a .NET backend names its files, asynchronous methods, and implementation shapes one way across every project. This page states those .NET rules: one primary type per file, complete async signatures, and a shared implementation style. The shared naming rules live on [Naming and Code Style](../../workspace/naming.md); this page holds only their .NET form.

## Concepts

### Files, Signatures, and Style

A C# file takes its primary type's exact name, so a reader finds any type by its file. An asynchronous method ends in `Async` and takes its cancellation token last, so callers see both asynchrony and cancellation in the signature. File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes keep diffs readable across every project.

### C# Role Suffixes

| Role | Pattern | Example |
|:---|:---|:---|
| Repository interface | `I{Aggregate}Repository` | `IPostRepository` |
| API mapping class | `{UseCase}ApiMappings` | `CreateDraftApiMappings` |
| Options class | `{ConfigurationPurpose}Options` | `EmailOptions` |
| Assembly marker | `{Layer}AssemblyMarker` | `ApplicationAssemblyMarker` |

In the dotnet stack, the repository interface carries the `I` prefix, options classes carry the `Options` suffix, and each scanned project carries an assembly marker.

Mapping classes use `ApiMappings` instead of the context-dependent `Mappings` suffix.

## Standards

### Match C# Files and Primary Types

**ID:** `litespec/backend/dotnet/naming#match-c-files-and-primary-types`

**Statement:** A C# file MUST contain one primary top-level type and take that type's exact name.

**Rationale:** A file named for its type lets a reader and a tool find the type by the path. Two top-level types in one file break that lookup and merge their histories in review. Only a small private or file-scoped nested type stays with its owner.

**Checked by:** `dotnet build` reports `LS3003` for a C# file whose name differs from its primary type.

### Name Asynchronous Methods Completely

**ID:** `litespec/backend/dotnet/naming#name-asynchronous-methods-completely`

**Statement:** A method returning `Task` or `ValueTask` MUST end in `Async` and take `CancellationToken cancellationToken` last when cancelable.

**Rationale:** The `Async` suffix tells the caller the method is asynchronous, and the trailing `CancellationToken` tells the caller it can cancel. A cancelable method that hides either invites blocking calls and uncancellable waits. A method is cancelable when it performs I/O or awaits. This provision states the .NET form of `litespec/workspace/naming#name-asynchronous-operations-completely`.

**Checked by:** `dotnet build` reports `LS3001` for an async method name without `Async` and `LS3002` for a cancellation token that is misplaced or misnamed.

### Keep Implementation Style Consistent

**ID:** `litespec/backend/dotnet/naming#keep-implementation-style-consistent`

**Statement:** Production C# MUST use file-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes.

**Rationale:** File-scoped namespaces, braces on every body, explicit access modifiers, and sealed concrete classes change diff readability rather than behavior. One shared setting removes the debate from every review and keeps generated and hand-written code alike.

**Checked by:** `dotnet build` reports `LS3004` for a concrete class with no derived type that is not sealed.

### Name Result Parts After Their Result

**ID:** `litespec/backend/dotnet/naming#name-result-parts-after-their-result`

**Statement:** Each C# file holding a use-case result part MUST be named `{UseCase}QueryResult{Part}.cs` or `{UseCase}CommandResult{Part}.cs` after its result.

**Rationale:** The file name carries the full role suffix, so a reader tells the owning result without opening the file. The type inside carries the same name under `litespec/backend/dotnet/naming#match-c-files-and-primary-types`. This provision states the .NET form of `litespec/workspace/naming#name-result-parts-after-their-result`.

**Example:** `ListPostsQueryResultItem.cs` holds the item part of the list-posts result, and `ReadLoanQueryResultHold.cs` holds the hold part of the read-loan result; a bare `PostSummary.cs` hides its owner.

**Checked by:** `dotnet build` reports `LS2009` for a result part not named after its result.

### Group Event Handlers by the Fact They Handle

**ID:** `litespec/backend/dotnet/naming#group-event-handlers-by-the-fact-they-handle`

**Statement:** Each C# event handler MUST live in a folder named `On{Fact}` under its subscriber aggregate folder, in a file named `{Action}On{Fact}Handler.cs` with its namespace ending in `.On{Fact}`.

**Rationale:** The folder answers what happens when the fact is recorded with one listing: `Loans/Loans/OnLoanBorrowed/` holds `SendDueNoticeOnLoanBorrowedHandler.cs` beside every other handler of the borrowed fact in that module. `dotnet build` reports a misnamed handler as `LS2007` and a handler outside its fact folder as `LS2008`. This provision states the .NET form of `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`.

**Checked by:** `dotnet build` reports `LS2007` for an event handler or scheduled job name hiding its kind and `LS2008` for an event handler outside its fact folder.

### Name Repository Variables After Their Interface

**ID:** `litespec/backend/dotnet/naming#name-repository-variables-after-their-interface`

**Statement:** Each C# repository parameter, field or local MUST be named after its interface without the `I` prefix, ending in `Repository`.

**Rationale:** The variable repeats the interface name in camel case, so a reader tells the guarded aggregate from the use: `expenseRepository` serves `IExpenseRepository`, and `loanRepository` serves `ILoanRepository`. A private field keeps its leading underscore, as `_expenseRepository`. `dotnet build` reports any other name as `LS2010`. This provision states the .NET form of `litespec/workspace/naming#name-repository-variables-after-their-interface`.

**Checked by:** `dotnet build` reports `LS2010` for a repository variable not named after its interface.

## Conventions

### Name No Folder or Type for the Absence of a Reason

**ID:** `litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason`

**Default:** No folder is named `Shared`, `Common`, `Util`, `Utils`, `Helpers`, or `Misc`, and no type carries one of those names or ends in `Helper`, `Manager`, `Util`, or `Utils`, outside the declared shared kernel.

**Replacement:** A project names its shared kernel folder in `stackConfig.dotnet.sharedKernel` and keeps every other folder and type named for its reason.

**Rationale:** A folder named `Shared` or a type ending in `Manager` tells the reader the author found no reason to name; each such name becomes the drawer where unrelated code gathers. The domain shared kernel is the one declared exception, named in one place, so the exception stays visible instead of spreading.

**Example:** The declared `Example.Domain/Shared/` folder passes; `Billing/Helpers/` and `OrderManager` outside it are reported.

**Checked by:** `litespec model check` reports `name-reason-missing` (level S) for a folder or type named for the absence of a reason outside the declared shared kernel.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/naming#match-c-files-and-primary-types`, `litespec/workspace/naming#use-architectural-suffixes`, and `litespec/workspace/naming#keep-namespaces-aligned-with-folders`.

```csharp
namespace Example.Application.Posts.CreateDraft;

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
