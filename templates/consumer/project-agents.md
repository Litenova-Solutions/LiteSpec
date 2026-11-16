# <Project> Agent Context

<!-- generated:agent-protocol -->
When this file disagrees with `__LITESPEC_COMMAND__ rules`, `__LITESPEC_COMMAND__ model query` or a record, the tool output and the record win; this file only points to them.

This project follows LiteSpec __LITESPEC_PACKAGE_VERSION__: size __SIZE__, shape __SHAPE__, stacks __STACKS__, extensions __EXTENSIONS__.
Layers: __LAYERS__.
Before a task, read https://litespec.dev/__LITESPEC_VERSION__/select/__SELECTION_KEY__/llms.txt and fetch only the pages the task touches.
UI work: the pattern pages listed in that index.

Read `.litespec/project.yaml` before changing the project. (litespec/core/agent#select-task-context)

Follow one order on every task: query the model for the records, run `__LITESPEC_COMMAND__ rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code. (litespec/core/agent#select-task-context, litespec/core/agent#load-applicable-provisions-before-editing, litespec/core/agent#inspect-local-examples-after-standards)

`__LITESPEC_COMMAND__ model query <record-id>` returns one record with its links. Query the record before changing behavior it describes, and query again when a link leads further. Find records by text with `__LITESPEC_COMMAND__ model query --search <text>` and list every record of a type with `__LITESPEC_COMMAND__ model query --list <type>`. (litespec/core/agent#select-task-context)

Run `__LITESPEC_COMMAND__ rules <path>` before editing a file. (litespec/core/agent#load-applicable-provisions-before-editing)

Answer each judgment provision `__LITESPEC_COMMAND__ rules --since <base>` lists before reporting done. (litespec/core/agent#load-applicable-provisions-before-editing)

Write each record from a record in the layer above it, and never compute a record from code. (litespec/core/layers#write-each-layer-from-the-layer-above-it, litespec/core/model#keep-code-out-of-intent-and-behavior-fields)

Build only what the specification states, and take anything missing to the owner. (litespec/core/layers#implement-only-declared-behavior, litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items)

Never delete, skip, or weaken a test that proves a scenario, or change its expected values, unless the scenario changed in the same change; report a test you believe is wrong instead of editing it. (litespec/core/agent#protect-proofs-from-implementation-edits)

Declare every edge between use cases, reactions, jobs, notifications, module events, integration events, and external systems. An undeclared edge is the defect. (litespec/core/system#declare-every-edge-between-records)

Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run; only a live waiver covers one. (litespec/core/model#mark-every-check-with-its-level)

A project waiver record sets a Standard aside only when it names the provision ID, states its kind, and, for a gap, names the expiry date, the exit and the closing records in `closedBy`. (litespec/core/agent#apply-guidance-precedence, litespec/core/waivers#state-the-kind-of-every-waiver, litespec/core/waivers#name-the-records-that-close-a-gap)

An explicit project convention in `.litespec/governance/conventions/` can replace a baseline Convention when it cites the Convention's ID. (litespec/core/agent#apply-guidance-precedence)

Read `.litespec/product.yaml` and the affected feature files before changing behavior that touches money, payouts, or terms. (litespec/core/model#write-every-record-in-its-home)

Record a product decision for any changed product choice. Research outside `.litespec/` is source material, never authority. (litespec/core/intent#treat-generated-research-as-source-material)
<!-- /generated:agent-protocol -->

## Project Commands

When a change touches an API boundary, run the backend gates for it. (litespec/core/release#run-boundary-selected-checks)

```bash
dotnet build <api-solution> --configuration Release
dotnet test <api-solution> --configuration Release --no-build
```

When a change touches a frontend listed in `.litespec/project.yaml`, run the root pnpm gates for it. (litespec/core/release#run-boundary-selected-checks)

When a change touches a record, a source file, or a test, run the metadata and model checks for it. Query the model again after the change, and report whatever stays red. (litespec/core/system#update-records-with-behavior, litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema)

Name each skipped check with its reason, and name the provisions `rules` returned with how each was judged. (litespec/core/release#report-verification-exactly)

Compose every route from one floorplan in the frontend `DESIGN.md`, or name the decision that justifies a freestyle route. (litespec/frontend/ui#compose-each-route-from-one-floorplan, litespec/frontend/ui#justify-each-freestyle-route-with-a-decision-record)

In a test title, cite each path of each use case a route calls, or record an exclusion with its reach.

Run `__LITESPEC_COMMAND__ model check` after changing UI configuration, the design contract, source locks, CSS, or primitives. Run the route suite after changing a route. (litespec/frontend/ui#name-the-evidence-for-each-ui-change, litespec/frontend/ui#pass-every-route-suite-check)

Record a waiver before choosing another visual system or component base. (litespec/frontend/ui#record-one-shared-ui-package)
