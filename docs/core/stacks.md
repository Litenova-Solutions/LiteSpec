# Technology Stacks

**Stack:** any

**Concern:** workspace

A stack binds the specification to one technology: the backend stack lives under `packs/dotnet/`, and a frontend stack under `packs/svelte/` or `packs/android/`. Selecting a stack applies exactly the pages, version pins, and checks that technology needs. This page states what a stack folder holds, where versions come from, and what each selection covers. It is for the team choosing its technologies, and for the agent resolving which pages apply.

## Concepts

### What a Stack Folder Holds

A stack is one folder with a `stack.md` page. That page states what the stack adds: its provisions, its version pins, and its checks. The standard ships three stacks: `docs/packs/dotnet/`, `docs/packs/svelte/`, and `docs/packs/android/`.

```yaml
stacks:
- dotnet
- svelte
```

Claimwick names the two stacks above in its project file. Each one applies its folder and every folder above it, so a stack restates no inherited principle. It also applies the subfolders of those folders holding no stack of their own. For example, a pack folder such as `components/` holds no `stack.md`, so it applies with its stack.

[Supported Scope](scope.md) states how size, shape, and stacks combine into one selection. A deeper page replaces the provisions it names, and everything it leaves unnamed keeps applying.

### Where Versions Come From

The standards-selected stack versions resolve from `litespec.manifest.yaml`, which owns the pins. One source for every pin keeps each consumer on the same tested combination. A version copied from anywhere else, prose, an example, a package search, or agent memory, drifts from the pin with the next edit. Consumer dependencies still live in package manifests and lockfiles.

### How a Change Names Its Boundary

Baseline behavior gives way to an extension or a waiver at times. A change then names every baseline provision ID it replaces, so reviewers see the exact boundary of the change, and everything unnamed stays active.

A waiver names the provision in `waives`; there is no separate type or folder. The command `rules <path>` shows the waiver in place of the provision. Unrelated stack provisions stay active, so a replacement stays inside the boundary it named. Each extension also states how it composes with the Behavior fields its boundary touches, such as the audit field, the tenant scope, or the job record.

Each selected stack stays visible: `.litespec/project.yaml` names each one, and the root `AGENTS.md` names the solution, the applications, and the commands. A visible selection tells an agent which placeholders to resolve without inferring them from the directory tree, where folders outnumber stacks or names overlap. It also tells a human reader what the project runs before opening any code. A project naming its selection elsewhere records an explicit local convention saying so.

### Selection Diagnostics

A selection names a stack, shape, page, or extension no page applies at times. The checks below report those dead selections, so they sit here instead of beside one provision.

| Family | Meaning | Codes |
|:---|:---|:---|
| `STACK_*` | A stack folder is outside the `packs/` docs, is missing, holds no `stack.md`, or requires or pairs with a stack the manifest does not ship. | `STACK_FOLDER`, `STACK_REFERENCE` |
| `SHAPE_PAGE` | A shape backend page is not a page directly in `docs/core/`. | `SHAPE_PAGE` |
| `LAYER_*` | A page sits in no known docs area, or no stack selection applies it. | `LAYER_UNLISTED`, `LAYER_UNREACHABLE` |
| `EXTENSION_*` | An extension sits outside its declared path, is missing from the manifest, or lacks its activation scope, kinds, criterion, baseline, or dependencies, or it replaces an unknown provision or restates one it replaces. | `EXTENSION_LOCATION`, `EXTENSION_UNLISTED`, `EXTENSION_SCOPE`, `EXTENSION_KIND`, `EXTENSION_ACTIVATION`, `EXTENSION_BASELINE`, `EXTENSION_DEPENDENCIES`, `EXTENSION_REPLACES_UNKNOWN`, `EXTENSION_REPLACES_ENFORCED` |

## Standards

### Apply the Complete Stack

**ID:** `litespec/core/stacks#apply-the-complete-stack`

**Strength:** required

**Statement:** A consumer selecting a stack MUST apply the pages in its folder, every folder above it, and their subfolders that hold no stack.

**Rationale:** The stack is the unit of conformance: a consumer claims no stack while silently omitting an applicable page. Applying the folder, every folder above it, and their stackless subfolders keeps inherited principles in force without restating them. A deeper page replaces the provisions it names, so everything it leaves unnamed keeps applying. Settled by decision/0122-isolate-three-stack-packs.

**Reviewed for:** Does the project apply every page its stack selection brings in? Compare the files against the stack folders, the folders above them, and their stackless subfolders.

## Conventions

None.
