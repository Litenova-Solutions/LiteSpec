# <Project> Agent Context

<!-- generated:agent-protocol -->
When this file disagrees with `__LITESPEC_COMMAND__ rules`, `__LITESPEC_COMMAND__ model query` or a record, the tool output and the record win; this file only points to them. Between a record and its explanation file, the record wins; a disagreement is a defect in the explanation.

This project follows LiteSpec __LITESPEC_PACKAGE_VERSION__: size __SIZE__, shape __SHAPE__, stacks __STACKS__, extensions __EXTENSIONS__.
Layers: __LAYERS__.
Before a task, read the LiteSpec pages of this release at __LITESPEC_DOCS__ and open only the pages the task touches.
Read this release only: never substitute another version when a page is missing.

Read `.litespec/project.yaml` before changing the project. (litespec/core/agent#select-task-context)

Follow one order on every task: start `__LITESPEC_COMMAND__ serve` or query the model for the records, run `__LITESPEC_COMMAND__ rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code. (litespec/core/agent#select-task-context, litespec/core/agent#load-applicable-provisions-before-editing, litespec/core/agent#inspect-local-examples-after-standards)

Let the record and the provisions decide the shape of new code; a neighbouring file is a pattern for style only, and when it contradicts the record, follow the record and report the neighbour. (litespec/core/agent#follow-the-record-not-the-neighbour)

Prefer the warm model service: `litespec_task_packet` before an edit, `litespec_check` on touched paths, `litespec_impact` before a rename, `litespec_verify_slice` after changing a use case. Without the service, `__LITESPEC_COMMAND__ model query <record-id>` returns one record with its links; `--search <text>` finds records and `--list <type>` lists every record of a type. (litespec/core/agent#select-task-context, litespec/core/model#keep-the-whole-specification-in-the-records)

Run `__LITESPEC_COMMAND__ rules <path>` before editing a file. (litespec/core/agent#load-applicable-provisions-before-editing)

Write new code with `__LITESPEC_COMMAND__ scaffold <use-case-id>` and derived data with `__LITESPEC_COMMAND__ sync`; never hand-write what either command owns. List parallel slices with `__LITESPEC_COMMAND__ plan-waves`, pending release changes with `__LITESPEC_COMMAND__ upgrade --check`, changed docs with `__LITESPEC_COMMAND__ docs review --since <base>`, and changed routes with `__LITESPEC_COMMAND__ ui review --since <base>`.

Build only what the specification states, and take anything missing to the owner. (litespec/core/layers#implement-only-declared-behavior, litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items) When a check, a record type, or a provision forces a shape the domain does not have, stop, name the missing concept, and raise it as an owner question or a backlog item instead of writing code around it. (litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code)

Never delete, skip, or weaken a test that proves a scenario, or change its expected values, unless the scenario changed in the same change; report a test you believe is wrong instead of editing it. (litespec/core/agent#protect-proofs-from-implementation-edits)

Declare every edge between use cases, reactions, jobs, notifications, module events, integration events, and external systems. An undeclared edge is the defect. (litespec/core/system#declare-every-edge-between-records)

Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run; only a live waiver covers one. (litespec/core/model#mark-every-check-with-its-level)

A project waiver sets a Standard aside only when it names the provision ID, states its kind, and, for a gap, names the expiry date, the `exitWhen` and the closing records in `closedBy`. (litespec/core/agent#apply-guidance-precedence, litespec/core/waivers#state-the-kind-of-every-waiver, litespec/core/waivers#name-the-records-that-close-a-gap) An explicit project convention in `.litespec/governance/conventions/` can replace a baseline Convention when it cites the Convention's ID. (litespec/core/agent#apply-guidance-precedence)

Before reporting done, run the model check over the change set and `hook pre-stop`, and state in the completion report what ran, what each judged provision decided, and what stays red. (litespec/core/agent#exercise-changed-use-cases-through-the-running-stack, litespec/core/agent#check-your-own-change-before-handing-it-back)
<!-- /generated:agent-protocol -->

## Project Commands

Write the project-owned commands for this repository below this line. `__LITESPEC_COMMAND__ agent sync` keeps the region above current; it never touches this section.
