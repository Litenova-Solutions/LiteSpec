# Remove Fields Nothing Reads

LiteSpec keeps settings in a project file, a release manifest, a pattern catalog and a design contract. Each file holds fields that tools read. This decision removes every field that no tool reads, so each file shows only values that a check uses. A project that sets a removed field fails validation until it deletes that field.

## Context

Several fields in the project file, the release manifest, the pattern catalog and the design contract have no reader in any tool. Some of them repeat a value that another file keeps, so the two copies can disagree.

## Options

LiteSpec compared 3 options for fields that no tool reads. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Remove every field no tool reads, and keep the toolchain pins | A file shows only what a check reads, so a value cannot drift from the value the tool uses. | A project that sets a removed field fails validation until it deletes the field. |
| Keep every field and document that tools ignore it | No project breaks. | A field with no reader drifts unseen, and readers meet settings that do nothing. |
| Keep every field and add a check that reads each one | Each field gains a reader. | Each check adds code for a field that no product behavior needs, against the rule that tools stay small. |

## Decision

A field that no tool reads leaves its file. A value that a page cites stays, because the page reads it. An example is a toolchain pin that a stack page names.

## Consequences

The project file loses these fields:

- `conformance`, which `layers` replaces.
- `unstructuredDocs`, `apiSolution`, `sites`, `surfaces`, `refusals.prefixes` and `paths.records`.

It moves `paths.frontends` to `frontends`, and it moves `paths.uiPackage` to `ui.package`.

The pattern catalog loses `registry`, `decision.record` and `option.reference`. The design contract loses `kind` and `shell`. The release manifest loses its `$schema` key, its `profiles` enum and the duplicated baseline fields.

The manifest keeps its `stack` pins, which the Blazor, Android and guide pages cite. The repository's own Node and pnpm versions must equal those pins.

## Revisit Conditions

This decision reopens when a tool needs one of the removed fields to decide a check, or when a team needs a value that a removed field carried and its replacement cannot state.

A removed field that a check starts to read would make that field a reader again. A team that needs a value that no current field can state is the other case.
