# Add a Stack or an Extension

Add a stack or an extension to the LiteSpec release: write its pages, name them in `litespec.manifest.yaml`, and run the maintainer checks. A stack carries the rules for a backend or frontend technology, and an extension carries optional rules a project selects on top of its stacks. This guide is written for a LiteSpec maintainer, and it adds no rule of its own.

## Prerequisites

Start with:

- The LiteSpec repository and the [release manifest](../reference/manifest.md) that lists every stack and extension the release ships.
- The manifest schema at `schemas/litespec-manifest.schema.yaml` and the authoring checks the maintainer tool runs.
- The [stacks](../core/stacks.md) page for how a selection applies, and the [extensions index](../ext/README.md) for how an extension is declared.

## Procedure

Follow one of the two sections below. Each runs three steps in order: write the pages, name them in the manifest, and run the maintainer check.

### Add a Stack

1. Add the stack folder under `docs/backend/` or `docs/frontend/` with a `stack.md` page. The page states what the stack adds: its rules, its version pins, and its checks.
2. Add the manifest entry with the stack id and its `folder`. Add `requires` when a project also selects another stack with this one, and `pairsWith` when a frontend stack works beside only some backend stacks.
3. Run `litespec-maintain validate` and fix what it reports: a folder outside the two areas, a missing folder, a missing `stack.md`, an unknown `requires` or `pairsWith` name, and any backend or frontend page no selection reaches.

### Add an Extension

1. Write the extension page at `docs/ext/<id>.md`, or at `<stack folder>/ext/<id>.md` when it supports one stack only. The page uses the extension style with `Activation`, `Baseline Relationship`, and `Dependencies` sections, and the Activation states the scope and the criterion.
2. Add the manifest entry with the extension id and its `path`. Add `activationScope`, `applicableKinds` where the extension applies to specific record kinds, `replaces` where it replaces baseline provisions, and `switchesOn` where selecting it requires record types.
3. Run `litespec-maintain validate` and fix what it reports: a path that does not exist, a path outside the two allowed shapes, a scope the page does not state, kinds the page does not match, a missing criterion, and a replacement id that names no active provision.

## Check

Confirm the addition with these checks:

- `litespec-maintain validate` reports no defect for the new stack folder or extension page.
- The manifest entry names the folder or path the new page uses, and the stack folder holds a `stack.md` file.
- `litespec-maintain provisions` rebuilds the provision index so any new provisions on the added pages appear in the index.
