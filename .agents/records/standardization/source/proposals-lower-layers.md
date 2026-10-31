# Proposals for the Behavior, Implementation, Proof and Governance Layers

**Created:** 2026-10-31
**Source:** `inputs/outside-review-lower-layers.md` (81 findings, ids `BE`, `IM`, `PR`, `GO`), reviewed against LiteSpec `v3` at `c9b2725`.
**Owner direction, 2026-10-31:** approval stays loose. LiteSpec serves one-person teams as well as larger ones, so no proposal here adds a second reviewer, a person-only approver, a sampled-review gate or an emergency-change process. Approval findings are kept only where the code or the docs are wrong on their own terms.

Each proposal says what changes, why, what it looks like, and what it costs. Examples use Lantern, the hotel booking product from the Intent deck. Proposals assume the naming grammar and renames already in `spec.md` Part B.

## Behavior

### BP1. Atomic Writes Across Aggregates Are a Named Exception

**Revised 2026-10-31 after reading the largest consumer:** 56 of its 93 atomic use cases write two or more aggregates, each citing an `atomicity` rule that states the business reason ("an order without its hold oversells"), and one erasure writes 31 aggregates in one transaction. A hard ban would force 56 redesigns of a pattern the consumer already uses with discipline. The proposal is therefore: an atomic write across aggregates stays allowed only through an `atomicity` rule (already checked by `consistency-unexplained`), the rule names the aggregates it binds, the use case's `changes` must equal that set, and more than a stated number of aggregates (recommended: 4) reports at tier A as a saga candidate. The original proposal follows for the record.

**Original proposal:** `consistency: atomic` is allowed only when a use case writes one aggregate. A use case that writes several aggregates says `compensated` (each write has an undo) or `per-record` (each write stands alone). Reading other aggregates to check a rule stays allowed.

**Why:** LiteSpec defines an aggregate as the one thing kept consistent in one step (`behavior.md:34`), then lets a use case make several aggregates consistent in one step (`:763-769`). Both cannot be the boundary. The aggregate is the boundary in DDD practice because a transaction across aggregates is where lock contention and lost updates appear first (Vernon, Effective Aggregate Design, 2011).

**Before and after (Lantern, booking a room):**

```json
// Before: one transaction writes the booking and the room's inventory
"changes": ["aggregate/booking.booking", "aggregate/inventory.room-night"],
"consistency": { "mode": "atomic", "because": ["rule/booking.room-still-free"] }

// After, option 1: the hold is one aggregate that owns the room-night claim
"changes": ["aggregate/booking.hold"],
"consistency": { "mode": "atomic" }

// After, option 2: two writes, each with its undo
"changes": ["aggregate/booking.booking", "aggregate/inventory.room-night"],
"consistency": { "mode": "compensated" }
```

**Cost:** a schema rule and a check change; projects that use multi-aggregate atomic writes either redraw the aggregate or record an exception.

### BP2. Define the Consistency Modes in One Table

**What changes:** one table defines each mode and which effect timings it permits. `best-effort`, which no page defines, is removed: `per-record` already covers "each write stands alone".

| Mode | Means | Effects allowed |
|:---|:---|:---|
| `atomic` | One aggregate, all or nothing | any timing |
| `per-record` | Each write commits on its own; a failure leaves earlier writes in place | `after-commit`, `async` |
| `compensated` | Each write has a named undo that runs when a later write fails | `after-commit`, `async` |

**Why:** today the meaning is split over `consistency`, effect `timing` and `sharesTransaction`, and a reader cannot tell `best-effort` from `per-record` (`behavior.md:118-119`, `:787-791`).

### BP3. Call Orchestration a Saga

**What changes:** the aggregate kind `process` becomes `saga`, defined as a coordinator: its steps run use cases on other aggregates, each step names its compensation, and a driver moves it. A thing that only has a lifecycle (a booking that goes Held, Paid, Confirmed) stays an `entity` with states and transitions. The glossary gives "process manager" as an alias.

**Why:** today one kind holds two different models, a state machine and an orchestrator (`behavior.md:57-66`). "Saga" is the term developers and agents already know for steps with compensations.

**Lantern:** "refund a cancelled stay" is a saga: release the room night, refund the card, send the confirmation, each with its undo. The booking itself is an entity.

### BP4. One Automation Record Instead of Reaction and Job

**What changes:** reactions and jobs merge into one record, `automation`, with a trigger:

```json
{ "type": "automation", "id": "automation/booking.release-expired-holds",
  "triggeredBy": { "schedule": { "every": "PT1M" } },
  "effects": [{ "runs": "use-case/booking.release-hold", "timing": "async" }] }

{ "type": "automation", "id": "automation/booking.send-confirmation-on-booking-paid",
  "triggeredBy": { "event": "event/booking.paid" },
  "effects": [{ "sends": "notification/booking.confirmation" }] }
```

`triggeredBy` is one of `event`, `schedule` or `manual`. Fields that only make sense for scheduled or manual work (`runbook`, `runsWhere`, `guard`, `cursor`) are allowed only with those triggers.

**Why:** the two records share `effects`, `fulfills`, `serves`, `onFailure`, `idempotency` and `reads`, and differ only in what starts them (`behavior.md:195-225`). Two tables, two sets of checks and two names cost every reader, and most of all a one-person team.

### BP5. Entry Points Are Doors; the Door Authenticates, the Use Case Authorizes

**What changes:** an entry point is only a way in from outside: `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`. `reaction` and `job` stop being entry-point kinds; their trigger lives on the automation (BP4). The split of duties becomes one sentence: the door declares how a caller proves who they are (`auth`: session, token, device, link, app, none), and the use case declares who may do the act (`grant` and `tenant`).

**Why:** today `behavior.md:1233-1239` requires kind and auth on every entry point while `access.md:291-299` requires only kind, and a reaction "entry point" has an auth with no caller. AsyncAPI makes the same split between a channel and the operations on it.

**Lantern:** `entry-point/lantern-app` declares `auth: [session]`; `use-case/booking.hold-room` declares `grant: permission/book-room`, `tenant: own`.

### BP6. Typed Inputs and Payloads

**What changes:** each use-case input and each integration-event payload field gets a `type` from a small closed list (`text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `id`) with an optional `of` for the record an `id` refers to. An integration event may name a `dataschema` URL, as CloudEvents does.

```json
"input": [
  { "name": "roomId", "type": "id", "of": "aggregate/inventory.room", "origin": "caller" },
  { "name": "nights", "type": "integer", "rules": ["between 1 and 30"], "origin": "caller" }
]
```

**Why:** `trust: payload` says the fields are fit to act on, but nothing says what they are (`behavior.md:103-104`, `:116`). With a type, the checker can compare records with the generated OpenAPI contract, and agents generate the right code first time.

### BP7. Effect Verbs That Do Not Collide

**What changes:** the effect verb `issues` becomes `runs`, and `publishes` becomes `emits`. `sends` and `calls` stay.

**Why:** `issue` is also a use-case verb meaning "create a document or credential", and `publish` a use-case verb meaning "make visible" (`behavior.md:154`, `:160`). An effect line `"issues": "use-case/booking.issue-voucher"` reads as nonsense today; `"runs": "use-case/booking.issue-voucher"` does not.

### BP8. One Meaning for Operator, System and Anonymous

**What changes:** an interactive job is triggered `manual` (BP4); a process driver run by a person is `person`; `setBy: operator` keeps its meaning (a configuration owner). The auth kind `anonymous` becomes `none`; the grant `anonymous` stays, meaning "anyone may".

**Why:** each word currently names two or three different things (`behavior.md:66`, `:217`, `:249`; `access.md:15`, `:25-27`).

### BP9. Rule Kinds Describe Meaning Only

**Revised 2026-10-31:** the `atomicity` kind stays. In the largest consumer it is the reason record that 56 multi-aggregate use cases cite, so it is not a duplicate of `consistency`: the rule states the group and why, the use case states that it commits as that group. The proposal narrows to one check, that a use case's `changes` equals the group its `atomicity` rule names.

**Original proposal:** the rule kind `atomicity` is removed, because `consistency` on the use case already states it. The difference between a model default and a tunable default is one sentence: a value the business may change is a setting; a value the model needs to be correct is a `default` rule.

### BP10. Privacy Positions That Match the Law

**What changes:** `pseudonymize` stays an erasure position, but a pseudonymized class remains `personal`, keeps its basis and counts in `risk-without-acceptance`. A new position, `anonymize`, means the data can no longer identify anyone. The retention schedule gains one column, `purpose`, and its claim narrows from "GDPR article 30 order" to "the time limits of a record of processing".

**Why:** under GDPR Recital 26 pseudonymized data is still personal data; today a class marked `pseudonymise` passes the erasure check as if it were gone (`behavior.md:49`, `:421-431`). Purpose drives how long data may be kept, and the schedule never says it (`:288-296`). A one-person team is bound by the same law. Counsel should confirm the wording.

**Lantern:** the traveller's name on a past booking is `retain` for seven years with purpose "tax records"; their email after the stay is `anonymize` after 90 days.

### BP11. Refusal Codes Are Retired, Never Renamed

**What changes:** a provision that a refusal code, once in use, is retired with its replacement named, and never reused.

**Why:** `behavior.md:893` calls the code a public contract; clients branch on it. Renaming it silently breaks them.

### BP12. Scenarios Name the Rule They Exercise, and Share Their Background

**What changes:** a scenario may name the rules it shows in `exercises`, and a use case may hold a `background`, the shared Given that every scenario starts from. Both words come from Gherkin, where `Rule` groups scenarios and `Background` holds shared context.

```json
"background": "Ana has a session and the Lisbon hotel has one double room free on 12 May.",
"records": [{ "type": "scenario", "id": "scenario/hold-room.second-hold-is-refused",
  "exercises": ["rule/hold.one-hold-per-room-night"], "given": "Ben already holds the room.", ... }]
```

**Why:** today a rule is proved only through a test mark, and every scenario repeats the same setup (`behavior.md:965-987`).

### BP13. Smaller Clarifications

- `parts` is stated as a list of names that use cases act on, not records, so it no longer contradicts "child entities never appear as records" (`behavior.md:42`, `:1343-1347`).
- A lagging query is called a read model once, so readers from event-driven practice recognise it.
- `dependsOn` stays a plain list; typed context-map relations are not worth the weight for one bounded context.

## Implementation

### IP1. Release Hygiene the Validator Enforces

**What changes:** the manifest pins `LiteSpec.Analyzers` 3.0.0, and `litespec-maintain validate` fails when the analyzer pin differs from the analyzer project version or a stack page cites an LS rule the pinned release does not ship. Orphaned pins (`preact`, `@preact/preset-vite`) are removed; the `cn` pin is checked against what the shadcn helper really imports; `NetArchTest.Rules` (last released 2021) is replaced by ArchUnitNET or dropped where the analyzers already cover the rule.

**Why:** today a consumer who follows the manifest gets the 2.0.0 analyzers, which have none of the LS5001 to LS5012 traceability rules the stack pages promise (`litespec.manifest.json:324`).

### IP2. Fail Loud in CI, Stay Quiet on a Laptop

**What changes:** three silent failures become visible.

| Today | Proposed |
|:---|:---|
| Without Node, every frontend S finding only reports (`reader-unavailable`) | Reports locally; blocks when the `CI` environment variable is set |
| `LiteSpecLayer=Domian` silently disables every rule | The analyzer reports an unknown layer name |
| A `*.Worker` project resolves to no layer and is never checked | Worker is a layer with its own rules; jobs live there or in Infrastructure, stated once |
| An unknown finding code defaults to tier G | A test fails the build when a code is missing from the tier table |

**Why:** a green build should mean the checks ran. The local relaxation keeps a solo developer without Node unblocked.

### IP3. Mark Style Belongs to the Language

**What changes:** today a project declares one mark style, but frontends always use a typed constant and operations files always use a `<follows>` comment, recorded as whichever style the project declared. The proposal: C# uses `xml` or `attribute` as the project declares; TypeScript uses the typed constant; YAML and other operations files use the comment. The reader records the style it saw, and `mark-style-mixed` checks within one language only. One shared parser reads all three, replacing the separate Blazor and operations regexes.

**Why:** the "one style" rule cannot fire where the code ignores it (`FrontendReadBuilder.cs:133`, `OperationsMarkReader.cs:80`, `DotnetScreenReader.cs:714-718`).

### IP4. Check That a Mark Matches Its Folder

**What changes:** the namespace check that frontends already have applies to the backend: a handler in `Booking/Holds/HoldRoom/` marked `use-case/booking.release-hold` is reported.

**Why:** copying a handler and forgetting its mark is the most common agent mistake, and the folder already says what the mark should be (`backend/architecture.md:101`; `FrontendModuleMirror.cs:44-47`).

### IP5. Recognise Code by Symbol, Not by Name

**What changes:** marks and handlers are recognised through the Roslyn semantic model: the generated mark attribute by its full name, a handler by the interface it implements, wherever it comes from. A state-changing public method on an aggregate root needs a `use-case` mark even when it raises no event.

**Why:** a domain attribute named `Follows` is read as a mark today, a handler on another mediator is never checked, and a mutation without an event is invisible to `write-undeclared` (`MarkReader.cs:18-21`; `ls5001.md:15`; `model.md:302`).

### IP6. Provision Ids That Survive a Retitle

**What changes:** the `**ID:**` line under each provision heading becomes the identity. A heading may change; the id may not. `litespec-maintain validate` checks that ids are unique and that no id from the last release disappeared without a recorded removal.

**Why:** today the id is computed from the heading (`authoring.md:556`), so fixing a heading's wording changes the id cited in every consumer's code marks. The id line already exists on every provision; it only stops being derived.

### IP7. Size-Aware Code Rules

**What changes:** rules that fit a large team but not a small one become a Default with a Replacement, tied to the project's size:

| Rule today (MUST) | Proposed |
|:---|:---|
| Five application projects | Default for every size; a small project may merge Application.Abstractions into Application. Hosts and Worker are listed as allowed extras |
| Property tests for every calculation rule | Default at size large; optional at small |
| Model-based tests for every aggregate | Default at size large; optional at small |
| Zero surviving mutants on changed lines | A threshold per project, with a record for equivalent mutants |

**Why:** a one-person project should not need seven projects and three test techniques before its first feature ships (`backend/dotnet/architecture.md:5`, `:21-23`; `backend/testing.md:149-173`). Fowler's test pyramid recommends proportions, not mandates.

### IP8. Every MUST Says Honestly How It Is Enforced

**What changes:** every Statement carries Checked by with a command or finding code that exists, or Reviewed for. `litespec-maintain validate` checks that a Checked-by line names an existing code, command or shipped file. Where a check already exists but is not cited, the citation is added.

**Why:** 548 Statements in the backend, frontend and workspace pages carry 105 Checked-by lines, and some of those name a text grep, a script no template ships, or a file that does not exist.

### IP9. One Page: Write Code for a Use Case

**What changes:** one Implementation page with a table, element by element: where it goes, what mark it carries, what checks it. The mark rules keep one home on `model.md`; `system.md` and the stack pages link to it.

**Why:** "where does this handler go and how is it marked" is answered across six pages today.

## Proof

### PP1. Fix the Two Proof Bugs First

- **Browser tests count.** The builder reads Playwright outcomes by test title, so a scenario proved in the browser can be passing, verified and exercised (`ModelBuilder.cs:615`).
- **A draft does not silence an approved record.** Blocking follows a record's own status. A new draft requirement shows as a label on the use cases that fulfil it; it no longer turns their code findings into reports (`Authority.cs:46-49`, `Severity.cs:59-61`).

### PP2. Proof State Lives in a Tracked File

**What changes:** `docs/proof/proofs.json` (a generated register, committed) records for each scenario the hash of the scenario and of the test body at its last passing run. A proof is stale when either hash changes. The git-ignored `.model/proofs.json` cache goes.

**Why:** today a fresh clone or a CI job starts with an empty cache, so nothing is ever stale there, and rerunning an unchanged test clears staleness locally (`ProofLedger.cs:95-106`). `docs/proof/` already exists for "evidence the tools compute".

### PP3. Loose Daily, Strict at Release

**What changes:** a new finding, `use-case-unverified`, reports a use case whose scenarios are not all proved. Day to day it reports without blocking. `litespec model check --release` turns the release-readiness items (verified scenarios, answered tier 1 questions) into blocking findings, so the gate `release.md:389` describes becomes a command.

**Why:** today nothing blocks on unverified scenarios, so the release gate depends on someone reading the book. Making it a flag keeps daily work loose, which suits a solo developer, and makes the release honest.

### PP4. Four Proof Words, One Table

| Word | Of | Means |
|:---|:---|:---|
| verified | use case | every scenario has a passing proof through the public boundary that is not stale |
| met | requirement | everything fulfilling it is verified, and its measure's check passes |
| tested | quality requirement, technical decision, external system, integration event | a passing test that asserts something links to it |
| exercised | journey step | a passing proof of a use case serving the step |

**Why:** "verified" means two different things today, a test with no assertion makes a record "tested", and the indirect proof of background use cases is undocumented (`model.md:331-336`; `ModelBuilder.cs:780-783`, `:810`).

### PP5. Check Only What Changed, When You Ask

**What changes:** `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>`, and reports the rest.

**Why:** a project adopting LiteSpec, or a solo developer picking it up again after months, otherwise writes one exception per old finding or freezes everything. SonarQube's "new code" gate is the established answer.

### PP6. Finding Codes With One Grammar

**What changes:** every finding code reads `<subject>-<problem>`, with the subject a record type or field and the problem one of twelve words, each with one meaning:

| Problem | Means | Replaces |
|:---|:---|:---|
| `missing` | a required part is absent | `without`, `-missing` |
| `unknown` | a name matches no record | `unresolved`, `unknown` |
| `undeclared` | code or prose does something its record does not say | |
| `unrecorded` | code exists and no record names it | |
| `unimplemented` | a record has no code | |
| `unreached` | nothing reaches it | |
| `drift` | two sources of one value disagree | `mismatch`, `drift` |
| `invalid` | wrong shape or form | `-form`, `-shape` |
| `duplicate`, `clash` | two records share one identity | |
| `expired` | a date has passed | |
| `reopened` | an approval no longer matches its hash | |

Examples: `use-case-without-scenario` becomes `use-case-scenario-missing`; `link-unresolved` becomes `link-unknown`; `screen-calls-mismatch` becomes `screen-calls-drift`. The diff's change reasons use the same grammar. A code whose tier depends on the record is split in two (`result-unproven` for success results) or its table row states the rule.

**Why:** 123 codes with synonyms for one problem cannot be learned; a fixed grammar makes them guessable.

### PP7. The Book Renders, the Builder Judges

**What changes:** the book reads every status from `model.json` and renders it with the model's own words; the judgement code in the book (about 5,900 lines across `apps/cli/src/LiteSpec.Cli/Book/`) moves into the builder or goes.

**Why:** the book recomputes proof state with its own vocabulary (`not-run` and `norun`, `proved-indirect` shown as `passing`), against the provision that the book shows only computed judgements.

### PP8. Smaller Proof Items

- `known-gap-expired` fires on the date; the tools page says "with its check still red". Keep the date rule and fix the page, so an exception that outlives its fix is cleaned up.
- A flaky state, derived from the last runs of a proof, reports at tier A.
- A browser success proof must assert the result's sentence or the scenario's Then text, not any string inside `expect`.
- `Finding.Blocks`, a second blocking rule with no caller, is deleted.

## Governance

### GP1. Approval Stays Loose, and the Pages Say So

**Decision:** no new approval gate. A one-person team approves its own records, and an agent's reading is an acceptable second reading for tier 1.

**What changes:** the pages describe the loose rule the code already applies:

- `release.md:397-405` stops saying an agent may only advise; it says an agent reading counts as the second reading, and the reading's report is kept beside the manifest.
- `review.securityReviewer` is optional, and `release.md:257`, `scope.md:164` and `project-file.md:117` stop describing checks that do not exist. The security queue stays a list to read, not a gate.
- A project whose owners list holds one name needs no `--second-reviewer`; the guard says so in its message.

**Two approval bugs are fixed because they make the record wrong, not because they tighten anything:**

- Approving a single record promotes its status from `draft` to `approved` (or `reviewed`), as the batch path does. Today it writes approval fields and leaves the record draft (`Approver.cs:115-125`).
- Re-approving a changed record writes the new approver and date. Today it keeps the old ones beside a new hash (`Approver.cs:115-125`).

**Not adopted, by owner direction:** a distinct-person second reviewer, owner versus approver identities, approval fields inside the hash, manifests verified by the check, a sampled-review gate, an emergency-change process, tiers on deviations.

### GP2. One Exception Record

**What changes:** the four ways to relax a check today (deviation, known gap, the `deferredProvisions` list, and `check.phase: frozen`) become one record type, `exception`:

```json
{ "type": "exception", "id": "exception/payment-retries-later",
  "kind": "gap",
  "covers": ["use-case-scenario-missing"],
  "subject": ["use-case/booking.retry-payment"],
  "reason": "Retries ship after the first partner goes live.",
  "owner": "founder", "status": "approved",
  "expiresOn": "2027-03-01" }
```

`kind` is `departure` (the project follows a different rule from a LiteSpec provision, named in `covers`) or `gap` (a check stays red until a date). An exception covers only the finding codes it names, or the codes its provision governs. It applies once its status is `approved`, which one person can set. When `expiresOn` passes, `exception-expired` reports it.

**Why:** today a draft known gap silences a blocking finding while a deviation must be approved first (`Suppression.cs:38-41`, `:81-87`); a deviation silences every finding on its subject whatever provision it cites (`:45-61`); the frozen phase and deferrals have no reason or end date; and four date fields (`reviewBy`, `endsBy`, `until`, `decideBy`) mean "until". One record is one thing to learn, which matters most to a one-person team, and it fixes the asymmetries without adding a reviewer.

### GP3. Words a Newcomer Can Look Up

- The risk values are defined in the glossary in one line each. `door` becomes `physical-access`, because "door" already means an entry point across `behavior.md`.
- "Task authorizer" is defined: the person who gave the agent the task; in a one-person team, the owner.
- Governance is one statement on every page: a group of records beside the four layers, not a fifth layer.

### GP4. Agent Rules That Follow Risk

**What changes:** the agent may skip the model query for a structural change only when the change touches no record that carries `risk` or an approval (`agent.md:71`).

**Why:** today a one-line change to a money rule qualifies to skip the query that would show its tier and open questions. The query costs seconds.

### GP5. Examples That Pass Their Own Rules

The known-gap example at `model.md:575-586` and the expiry example at `release.md:541-549` are corrected so the validator would accept them.

## What These Proposals Do Not Change

- Approval strictness (GP1).
- The four record layers and the Intent work in `spec.md`.
- Reactions keep their glossary alias "policy"; the merge in BP4 changes the record, not the idea.
- `dependsOn` stays a plain list.

## Findings Covered and Dropped

| Area | Covered by proposals | Dropped by owner direction |
|:---|:---|:---|
| Behavior | BE1 to BE17 (BP1 to BP13); BE18 is a note served by BP6 | none |
| Implementation | IM1 to IM24 (IP1 to IP9) | none |
| Proof | PR1 to PR19 (PP1 to PP8) | none |
| Governance | GO3 and GO6 (GP1 bugs), GO5, GO7, GO13, GO17 (GP2), GO9 and GO10 as documentation (GP1), GO16, GO19, GO20 (GP3), GO18 (GP4), GO21 (GP5) | GO1, GO2 (reversed: agent reading allowed), GO4 (see below), GO8, GO11, GO12, GO14, GO15 |

GO4, a question's own `tier: 3` letting a price auto-approve, is already covered by `spec.md` decision D16, which derives a question's tier from its record's `risk`. With approval loose that decision is a choice, not a fix: keep D16 (one field instead of two; a founder answers a money question in seconds) or drop it (a recommended price may auto-approve after the veto window). Recommended: keep D16.
