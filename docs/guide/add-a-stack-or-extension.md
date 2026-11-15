# Add a Stack or an Extension

LiteSpec grows in two ways: a new stack carries the rules for a backend or frontend technology, and a new extension carries optional rules a project selects on top of its stacks. Either one joins the release when its pages exist and `litespec.manifest.yaml` names them. This guide covers the pages, the manifest entry, and the maintainer checks that confirm the addition. It is written for a LiteSpec maintainer, and it adds no rule of its own.

## Prerequisites

Start with:

- The LiteSpec repository and the [release manifest](../reference/manifest.md) that lists every stack and extension the release ships.
- The manifest schema at `schemas/litespec-manifest.schema.yaml` and the authoring checks the maintainer tool runs.
- The [stacks](../core/stacks.md) page for how a selection applies, and the [extensions index](../ext/README.md) for how an extension is declared.

## Procedure

### Add a Stack

Add a stack in three steps.

First, add the stack folder under `docs/backend/` or `docs/frontend/` with a `stack.md` page. The page states what the stack adds: its rules, its version pins, and its checks.

Second, add the manifest entry with the stack id and its `folder`. Add `requires` when a project also selects another stack with this one, and `pairsWith` when a frontend stack works beside only some backend stacks.

Third, run `litespec-maintain validate` and fix what it reports: a folder outside the two areas, a missing folder, a missing `stack.md`, an unknown `requires` or `pairsWith` name, and any backend or frontend page no selection reaches.

### Add an Extension

Add an extension in three steps.

First, write the extension page at `docs/ext/<id>.md`, or at `<stack folder>/ext/<id>.md` when it supports one stack only. The page uses the extension style with `Activation`, `Baseline Relationship`, and `Dependencies` sections, and the Activation states the scope and the criterion.

Second, add the manifest entry with the extension id and its `path`. Add `activationScope`, `applicableKinds` where the extension applies to specific record kinds, `replaces` where it replaces baseline provisions, and `switchesOn` where selecting it requires record types.

Third, run `litespec-maintain validate` and fix what it reports: a path that does not exist, a path outside the two allowed shapes, a scope the page does not state, kinds the page does not match, a missing criterion, and a replacement id that names no active provision.

## Check

Confirm the addition with these checks:

- `litespec-maintain validate` reports no defect for the new stack folder or extension page.
- The manifest entry names the folder or path the new page uses, and the stack folder holds a `stack.md` file.
- `litespec-maintain provisions` rebuilds the provision index so any new provisions on the added pages appear in the index.
