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

The Role Suffixes table on the shared [Naming and Code Style](../../workspace/naming.md) page lists the suffixes for handlers, endpoints, reactions, and jobs. The table below adds the C#-specific names: the repository interface carries the `I` prefix, options classes carry the `Options` suffix, and each module project carries one entry type.

| Role | Pattern | Example |
|:---|:---|:---|
| Repository interface | `I{Aggregate}Repository` | `IPostRepository` |
| API mapping class | `{UseCase}ApiMappings` | `CreateDraftApiMappings` |
| Options class | `{ConfigurationPurpose}Options` | `EmailOptions` |
| Module entry type | `{Module}Module` | `SpendingModule` |

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

### Name Every Event Type by Its Kind

**ID:** `litespec/backend/dotnet/naming#name-every-event-type-by-its-kind`

**Strength:** required

**Statement:** An event type MUST end in the suffix of its kind: `DomainEvent` for a domain event, `ModuleEvent` for a module event, and `IntegrationEvent` for an integration event.

**Rationale:** A codebase holds all three kinds at once: the domain event an aggregate raises for its own module, the module event other modules consume, and the integration event that leaves the product. A bare `Event` suffix, or no suffix, never says which contract the type carries: whether the fact stays inside its module, is versioned for other modules, or has external consumers. The kind suffix puts that answer in the name, beside the `Command`, `Query`, `Result`, and `Id` suffixes the Role Suffixes table assigns. eShopOnContainers names its events `DomainEvent` and `IntegrationEvent` the same way.

**Example:** In the Claimwick product, the expense aggregate raises `ExpenseApprovedDomainEvent` from `Domain/Expenses/Events/`, and the spending Contracts project publishes `ExpenseReimbursedModuleEvent` for the reporting module. A reaction keeps the fact without the suffix: `SubmitPayoutOnExpenseApprovedHandler` answers the approval.

**Checked by:** `dotnet build` reports `LS2031` for an event type whose name does not end in the suffix of its kind.

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
namespace Example.Posts.Application.CreateDraft;

internal sealed class CreateDraftCommandHandler
{
    private readonly IPostRepository _postRepository;
    private readonly IClock _clock;

    public CreateDraftCommandHandler(IPostRepository postRepository, IClock clock)
    {
        _postRepository = postRepository;
        _clock = clock;
    }

    public async Task<CreateDraftCommandResult> HandleAsync(
        CreateDraftCommand command,
        CancellationToken cancellationToken)
    {
        // Operation code.
    }
}
```
