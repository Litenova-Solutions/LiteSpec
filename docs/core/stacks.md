# Technology Stacks

**Stack:** any
**Concern:** workspace

A stack binds the specification to one technology: the backend stack lives under `packs/dotnet/`, and a frontend stack under `packs/svelte/` or `packs/android/`. Selecting a stack applies exactly the pages, version pins, and checks that technology needs: the provisions in its folder and above it, the pins in the manifest, and replacements that name every baseline provision they replace.

## Concepts

### What a Stack Folder Holds

A stack is one folder with a `stack.md` page. That page states what the stack adds: its provisions, its version pins, and its checks. The standard ships three stacks: `docs/packs/dotnet/`, `docs/packs/svelte/`, and `docs/packs/android/`.

Selecting a stack applies its folder and every folder above it, so a stack never restates an inherited principle. It also applies the subfolders of those folders that hold no stack of their own. For example, a pack folder such as `components/` holds no `stack.md`, so it applies with its stack.

[Supported Scope](scope.md) states how size, shape, and stacks combine into one selection.

### Where Versions Come From

The standards-selected stack versions resolve from `litespec.manifest.yaml`, which owns the pins, as `litespec/core/principles#locate-package-versions-in-the-manifest` states. One source for every pin keeps each consumer on the same tested combination. A version copied from anywhere else, prose, an example, a package search, or agent memory, drifts from the pin with the next edit. Consumer dependencies still live in package manifests and lockfiles.

### How a Replacement Names What It Changes

Baseline behavior sometimes gives way to an extension or a waiver. A replacement then names every baseline provision ID it replaces, so reviewers see the exact boundary of the change, and everything unnamed stays active.

A waiver names the provision in `waives`; there is no separate type or folder. `rules <path>` shows the waiver in place of the provision. Unrelated stack provisions stay active, so a replacement stays inside the boundary it named.

Each extension also states how it composes with the Behavior fields its boundary touches, such as the audit field, the tenant scope, or the job record.

### Selection Diagnostics

A selection can name a stack, shape, page, or extension that no page applies. The checks below report those dead selections, so they sit here instead of beside one provision.

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

**Rationale:** The stack is the unit of conformance: a consumer cannot claim a stack while silently omitting an applicable page. Applying the folder, every folder above it, and their stackless subfolders keeps inherited principles in force without restating them. A deeper page replaces only the provisions it names, so everything it leaves unnamed keeps applying.

**Reviewed for:** Does the project apply every page its stack selection brings in? Compare the files against the stack folders, the folders above them, and their stackless subfolders.

### Name Every Provision a Replacement Replaces

**ID:** `litespec/core/stacks#name-every-provision-a-replacement-replaces`

**Strength:** required

**Statement:** An extension or a waiver MUST name every baseline provision ID it replaces.

**Rationale:** Naming every replaced provision ID draws the exact boundary of the change: everything unnamed stays active. A waiver names the provision in `waives`, and `rules <path>` shows the waiver in place of the provision. Without the list, a replacement silently widens its scope with each reading, and reviewers cannot tell which baseline provisions still bind the project.

**Reviewed for:** Does every extension and waiver name each baseline provision it replaces? Check each replacement against the baseline provisions it touches.

## Conventions

### Keep the Stack Visible

**ID:** `litespec/core/stacks#keep-the-stack-visible`

**Strength:** default

**Default:** Name each selected stack in `.litespec/project.yaml` and the solution, applications, and commands in the root `AGENTS.md`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** When `.litespec/project.yaml` names each selected stack and the root `AGENTS.md` names the solution, applications, and commands, an agent resolves the project placeholders directly. Inferring the selection from the directory tree fails whenever folders outnumber stacks or names overlap. A visible selection also tells a human reader what the project runs before opening any code.

**Reviewed for:** Does `.litespec/project.yaml` name each selected stack, and does the root `AGENTS.md` name the solution, applications, and commands? Check both files against the project layout.
