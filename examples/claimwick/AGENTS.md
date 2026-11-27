# Claimwick Agent Context

<!-- generated:agent-protocol -->
When this file disagrees with `litespec rules`, `litespec model query` or a record, the tool output and the record win; this file only points to them.

This project follows LiteSpec 7.0.0: size large, shape server, stacks dotnet, svelte, extensions none.
Layers: intent and behavior: litespec, implementation and proof: litespec.
Before a task, read the LiteSpec pages of this release at ../../docs and open only the pages the task touches.
Read this release only: never substitute another version when a page is missing.

Read `.litespec/project.yaml` before changing the project. (litespec/core/agent#select-task-context)

Follow one order on every task: start `litespec serve` or query the model for the records, run `litespec rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code. (litespec/core/agent#select-task-context, litespec/core/agent#load-applicable-provisions-before-editing, litespec/core/agent#inspect-local-examples-after-standards)

Let the record and the provisions decide the shape of new code; a neighbouring file is a pattern for style only, and when it contradicts the record, follow the record and report the neighbour. (litespec/core/agent#follow-the-record-not-the-neighbour)

Prefer the warm model service: `litespec_task_packet` before an edit, `litespec_check` on touched paths, `litespec_impact` before a rename, `litespec_verify_slice` after changing a use case. Without the service, `litespec model query <record-id>` returns one record with its links; `--search <text>` finds records and `--list <type>` lists every record of a type. (litespec/core/agent#select-task-context, litespec/core/model#keep-the-whole-specification-in-the-records)

Run `litespec rules <path>` before editing a file. (litespec/core/agent#load-applicable-provisions-before-editing)

Write new code with `litespec scaffold <use-case-id>` and derived data with `litespec sync`; never hand-write what either command owns. List parallel slices with `litespec plan-waves`, pending release changes with `litespec upgrade --check`, changed docs with `litespec docs review --since <base>`, and changed routes with `litespec ui review --since <base>`.

Build only what the specification states, and take anything missing to the owner. (litespec/core/layers#implement-only-declared-behavior, litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items) When a check, a record type, or a provision forces a shape the domain does not have, stop, name the missing concept, and raise it as an owner question or a backlog item instead of writing code around it. (litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code)

Never delete, skip, or weaken a test that proves a scenario, or change its expected values, unless the scenario changed in the same change; report a test you believe is wrong instead of editing it. (litespec/core/agent#protect-proofs-from-implementation-edits)

Declare every edge between use cases, reactions, jobs, notifications, module events, integration events, and external systems. An undeclared edge is the defect. (litespec/core/system#declare-every-edge-between-records)

Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run; only a live waiver covers one. (litespec/core/model#mark-every-check-with-its-level)

A project waiver sets a Standard aside only when it names the provision ID, states its kind, and, for a gap, names the expiry date, the `exitWhen` and the closing records in `closedBy`. (litespec/core/agent#apply-guidance-precedence, litespec/core/waivers#state-the-kind-of-every-waiver, litespec/core/waivers#name-the-records-that-close-a-gap) An explicit project convention in `.litespec/governance/conventions/` can replace a baseline Convention when it cites the Convention's ID. (litespec/core/agent#apply-guidance-precedence)

Before reporting done, run the model check over the change set and `hook pre-stop`, and state in the completion report what ran, what each judged provision decided, and what stays red. (litespec/core/agent#exercise-changed-use-cases-through-the-running-stack, litespec/core/agent#check-your-own-change-before-handing-it-back)
<!-- /generated:agent-protocol -->

## Project Commands

This example holds no solution file, C# project file or package manifest, so its build and test gates have no command here. A consumer names its own in this section. The commands below run against the records and the code in this folder.

Run metadata and model checks for each changed record, source, or test. Run `dotnet litespec model check` and query the model again after the change, and report what stays red. (litespec/core/system#update-records-with-behavior, litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema)

Compose each route from one floorplan in the frontend `packages/ui/DESIGN.md`, or name the decision behind a freestyle route. (litespec/core/ui-ia#compose-each-route-from-one-locked-archetype, litespec/core/ui-ia#declare-each-screen-job-and-archetype)

Reach each declared refusal in an evidence specification and assert it by its result's code. (litespec/frontend/testing#assert-each-declared-refusal-by-its-code)

Run `dotnet litespec model check` after changing UI configuration, the design contract, source locks, CSS, or primitives. Run the route suite after changing a route. (litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic, litespec/core/ui-verification#run-geometry-sensors-on-every-route)

Record a waiver before selecting another visual system or component base. (litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives)

Report every skipped check with its reason, and name the provisions `rules` returned and how each was judged. (litespec/core/release#report-verification-exactly)
