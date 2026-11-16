# Product Release

**Stack:** any

**Concern:** operations

A product release is the verified behavior a team ships as one immutable artifact, a build output that never changes after it is made. Each release runs the checks its changed boundaries select, reports the completion the builder computes from proof, and records a release record with its verification report. A team applies these rules at every release. A project that selects the [Delivery extension](../ext/delivery.md) adds that extension's deployment and rollback rules on top.

## Concepts

### Git Is the Decision Record

A record present in git is decided, and the pull request that changes it is its review.

A record the product drops is deleted, and git keeps its history.

A project that names who answers for an area names them in CODEOWNERS.

### Completion Is Computed

Completion is computed from proof: code exists, tests exist, and scenarios pass. The builder computes it on every run, nobody types it, and the book leads with it.

### The Release Gate

The gate admits a release when its blocking checks are green, its scenarios are verified, and every waiver it relies on is in effect. A waiver is in effect while it is present and, when it sets `expiresOn`, before that date.

Whether a finding blocks follows the strength of the provision it reports. A finding on a required provision blocks unless a waiver covers it, and a finding on an advisory provision never blocks. The level letter states what a finding rests on: G findings read records only, S findings read code through a stack reader and report without blocking where that reader is not qualified, and A findings report without blocking, except the release-readiness findings `--release` names.

`litespec model check --release` makes the release-readiness findings block, so running that command applies the gate. Under `--release`, a claimed layer whose reader is missing or failing blocks, since the gate can't judge evidence it can't read. A project that selects the [Delivery extension](../ext/delivery.md) adds its formal readiness gate, deployment, and rollback rules on top of this gate.

### The Verification Report

Each change ships with a verification report beside it: the exact commands run, their outcomes, the evidence scope, skipped checks with reasons, generated differences, scope limits, and covered use cases. The report tells a reader precisely what the evidence covers and what it does not.

### Reader Qualification

A stack reader is qualified by fixtures: a known good tree it passes and a known bad tree where it reports each planted defect. The fixtures prove the reader sees what it claims before it blocks any change.

Only a qualified reader gates its scope with S checks. Where the reader isn't qualified, the same checks report without blocking, so an unproven reader neither blocks a release on findings it cannot prove nor passes a release whose defects it cannot see.

### Verified Behavior and Flow Evidence

A use case is coded when its handler exists, and verified when every one of its scenarios has a passing proof through the public boundary, as the [model page](model.md) states.

A flow test proves that connected use cases produce one observable product outcome through the deployed public boundary. It catches the wiring, permission, and data defects that narrower suites miss, because those defects live where use cases meet, not inside any one of them.

### The Release Record

Each release creates one release record for its immutable artifact. The record names the source commit and the immutable artifact reference, and lists the included flows, use cases, and extensions.

It stays non-authoritative: specifications own behavior and scenarios, so the record links outward instead of restating them. Release records live under `.litespec/releases/` unless the project records another location. A project that selects the [Delivery extension](../ext/delivery.md) adds the evidence and operating readiness lists that extension states.

### Boundaries Select Checks

Each changed boundary selects the checks written for it. A change that touches several boundaries runs every check each one selects, since each boundary carries failures the others can't see.

| Changed boundary | Checks the change runs |
|:---|:---|
| Backend | The release build and the backend test suites |
| Frontend | Frozen installation, lint, type checking, tests, and the production build |
| Flow | Its flow tests through the deployed boundary |
| Intent or Behavior | G checks with the book |
| Extension | The verification its extension states |

### Record Debt as a Waiver

Accepted debt never sits as an untracked compromise: it lives as a gap waiver with an expiry date, an exit, and closing records. Its findings are covered until the date, and `waiver-expired` reports after it. The [waivers page](waivers.md) states the waiver shape.

Governance is a group of records beside the four record layers, not a fifth layer.

### Readiness Before the First Release

A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. Before its first production release, the consumer keeps a readiness record instead, with dated rehearsals, green gates, and a backup and restore rehearsal.

### The Adopted LiteSpec Release

A consumer adopts one LiteSpec release by installing the `LiteSpec.Cli` tool package at that release version and recording the same version in `reviewedLiteSpecVersion`. The two move together: the tool carries the manifest, schemas, docs, and templates the version names, and the recorded version says which contract the project reviewed.

Each LiteSpec release publishes the tool package and the site at the release version together. A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together, after reading the contract the new release states.

## Standards

### Verify Connected Flows

**ID:** `litespec/core/release#verify-connected-flows`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A completed flow MUST connect verified use cases to one observable outcome through a deployed public boundary.

**Rationale:** Use cases that pass in isolation can still fail together: wiring, permissions, and data cross boundaries the isolated tests never touch. A flow test that runs verified use cases to one observable outcome through the deployed public boundary proves the connected path customers actually take.

**Reviewed for:** Does every completed flow show a passing test through the deployed boundary? Check the flow tests beside each included flow.

### Run Boundary-Selected Checks

**ID:** `litespec/core/release#run-boundary-selected-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release change MUST run every check selected by its changed backend, frontend, flow, Intent, Behavior, and extension boundaries.

**Rationale:** Each boundary carries failures the others can't see: frontend checks say nothing about migrations, and backend suites say nothing about rendered screens. Running every check the changed boundaries select keeps each change evidenced where its failure lives. A narrow category that passes alone certifies only part of the change.

**Reviewed for:** Does every change show a passing result for each check its boundaries select? Check the change report against the boundary table.

### Run Backend Release Checks

**ID:** `litespec/core/release#run-backend-release-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A backend change MUST pass the release build and the backend test suites.

**Rationale:** The release build compiles the solution in release mode, catching the conditional and optimization-gated breaks a debug build hides. The architecture, integration, and acceptance suites the size requires prove layering, wiring, and behavior in turn. Skipping any of them ships backend code the project's own suites don't fully prove.

**Reviewed for:** Does every backend change show a passing release build and suite results? Check the build and suite reports beside the change.

### Run Frontend Release Checks

**ID:** `litespec/core/release#run-frontend-release-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend change MUST run frozen installation, lint, type checking, tests, and production build checks.

**Rationale:** Each step guards a different failure: frozen installation pins dependencies, lint and type checking catch static defects, tests prove behavior, and the production build proves the shipped bundle compiles. A frontend that skips one arrives at release with that whole class of defect unchecked.

**Reviewed for:** Does every frontend change show passing install, lint, type, test, and build results? Check the frontend reports beside the change.

### Run Flow Checks

**ID:** `litespec/core/release#run-flow-checks`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A flow change MUST run its flow tests.

**Rationale:** A flow change alters the connected path, so only a flow test through the deployed public boundary can prove it still reaches its outcome. Narrower suites keep passing while the flow itself breaks.

**Reviewed for:** Does every flow change show passing flow tests? Check the flow reports against the deployed boundary.

### Check Intent and Behavior Changes with the Book

**ID:** `litespec/core/release#check-intent-and-behavior-changes-with-the-book`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent or Behavior change MUST pass the G checks with the book.

**Rationale:** G checks read records only: shape, references, links, reachability, coverage, and the Behavior rules the check list names. Without them, a renamed record or a moved file silently breaks the references other pages rely on, and the book renders a graph that no longer holds. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

**Checked by:** `litespec model check` exits zero when it reports no G finding, such as `record-invalid`, and the inspection was complete.

### Run Extension Checks

**ID:** `litespec/core/release#run-extension-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A selected project or applicable local extension MUST run its stated verification when affected.

**Rationale:** An extension adds behavior, tests, and operating surface beyond the baseline, and its stated verification is the only evidence written for that surface. Skipping it releases extended behavior the baseline checks never examine.

**Reviewed for:** Does every affected extension show its stated verification evidence? Check the extension evidence beside the change.

### Regenerate Application Contracts

**ID:** `litespec/core/release#regenerate-application-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A source change MUST regenerate OpenAPI and frontend API types when it changes their source contract.

**Rationale:** OpenAPI and frontend API types derive from a source contract, and clients build against the committed copies. A source change that leaves those copies stale ships clients built against a contract the server no longer honors.

**Reviewed for:** Does any committed contract differ from a fresh generation? Check the contract diff beside the source change.

### Keep Generated Contracts Stable

**ID:** `litespec/core/release#keep-generated-contracts-stable`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A generated application contract MUST be stable and exclude timestamps, machine paths, and nondeterministic ordering.

**Rationale:** Timestamps, machine paths, and unstable ordering make every regeneration look like a behavior change. Deterministic output keeps contract diffs reviewable, so a reviewer sees only the changes that matter.

**Reviewed for:** Does any contract diff hold timestamps, machine paths, or unstable order? Check the generated diff beside each contract.

### Qualify Each Stack Reader with Fixtures

**ID:** `litespec/core/release#qualify-each-stack-reader-with-fixtures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A stack reader MUST pass its known good and known bad fixtures before it gates with S checks.

**Rationale:** An unqualified reader blocks on noise or passes on silence, and each mistake trains the team to ignore its findings. Passing a known good tree and reporting each planted defect in a known bad tree proves the reader sees what it claims before it blocks any change.

**Example:** The .NET reader passes the clean tree and reports the planted undeclared write before its S findings block.

**Reviewed for:** Does every gating S reader show passing fixture runs? Check the fixture results beside the stack reader.

### Apply Release Access Controls

**ID:** `litespec/core/release#apply-release-access-controls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release with restricted access MUST provide authentication and authorization.

**Rationale:** Restricted behavior without authentication accepts callers nobody identified, and without authorization it accepts actions nobody permitted. Both decisions call for a trusted identity and a declared permission before the behavior ships.

**Reviewed for:** Does every restricted release path carry authentication and authorization? Check the guards on each restricted path.

### Record the Release Artifact

**ID:** `litespec/core/release#record-the-release-artifact`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST create one release record for its immutable artifact.

**Rationale:** Evidence scattered across builds and reports leaves no one place that says what one artifact proved. One release record per immutable artifact binds every result to the exact output it proves, so each artifact carries its own evidence.

**Reviewed for:** Does every release show one record naming its immutable artifact? Check the release records beside each artifact.

### Resolve Release Blockers

**ID:** `litespec/core/release#resolve-release-blockers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST settle every expired waiver and every external claim it depends on, or exclude the affected behavior from every owning record.

**Rationale:** An expired waiver is a promise past its date, and an unverified external claim behind released behavior is a promise with unsettled terms: a provider fact, a law text, or a term nobody rechecked. Settling each one, or excluding the affected behavior from every owning record, keeps the Intent and release records agreeing on what shipped. `waiver-expired` reports every expired waiver.

**Reviewed for:** Does any release depend on an expired waiver or an external claim without excluding the behavior? Check the waivers and claims beside the owning records.

### Report Verification Exactly

**ID:** `litespec/core/release#report-verification-exactly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A completion report MUST name exact commands, outcomes, evidence scope, skipped checks, generated differences, scope limits, and covered use cases.

**Rationale:** A vague report lets unrun checks hide among completed ones. Naming exact commands, outcomes, scope, omissions, and limits tells a reviewer precisely what the evidence covers and what it does not.

**Reviewed for:** Does every completion report name its commands, outcomes, scope, omissions, differences, limits, and covered use cases? Check the report beside the change.

### Identify Immutable Release Artifacts

**ID:** `litespec/core/release#identify-immutable-release-artifacts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST name its source commit and its immutable artifact reference.

**Rationale:** Without both references, evidence from one build can stand in for another, and nobody can tell which build a record proves. The source commit names the verified input and the artifact reference names the deployed output, which binds the record to one build.

**Reviewed for:** Does every release record name its source commit and artifact reference? Check the record header beside each artifact.

### List Included Release Behavior

**ID:** `litespec/core/release#list-included-release-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list included flows, use cases, and selected applicable extensions.

**Rationale:** Operators and reviewers need to know what an artifact contains and, by omission, what it does not. Listing the flows, use cases, and extensions states the behavior boundary exactly.

**Reviewed for:** Does every record list each included flow, use case, and extension? Check the behavior list on each release record.

### Keep Release Records Non-Authoritative

**ID:** `litespec/core/release#keep-release-records-non-authoritative`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST NOT restate a specification or copy a scenario.

**Rationale:** Specifications own behavior and scenarios. A release record that restates them becomes a second copy, and the copies drift until nobody knows which one holds. Linking outward keeps one home for each fact.

**Reviewed for:** Does any release record restate a specification or copy a scenario? Check the record against the linked specifications.

### Record Accepted Debt as a Waiver

**ID:** `litespec/core/release#record-accepted-debt-as-a-waiver`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** Accepted debt MUST live as a gap waiver with an `expiresOn` date, an `exit`, and `closedBy`.

**Rationale:** Accepted debt with no expiry date is forgotten work. A gap waiver with an `expiresOn` date, an `exit`, and `closedBy` turns it into a dated promise whose findings are covered until the date and whose `waiver-expired` reports after it. The [waivers page](waivers.md) states the waiver shape.

**Example:** Claimwick ships month-end export without its archive proof, so the debt lives as a gap waiver with an `expiresOn` of 2027-03-01, an `exit`, and `closedBy` naming `use-case/spending.export-expenses`; `waiver-expired` reports it after that date.

**Reviewed for:** Does every accepted debt show a gap waiver with an expiry date, an exit, and `closedBy`? Check the waivers beside each debt.

### State Each Release as a Complete Record

**ID:** `litespec/core/release#state-each-release-as-a-complete-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST state its complete behavior and evidence without depending on an earlier release record.

**Rationale:** A release judged from fragments forces the reader to dig through other records to fill the gaps. A complete record lets one reader judge one release from one page, with no upgrade guide filling in.

**Reviewed for:** Does any release record depend on an earlier record to be judged? Read the record alone beside its links.

## Conventions

### Store Release Records Together

**ID:** `litespec/core/release#store-release-records-together`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep release records under `.litespec/releases/`.

**Replacement:** A consumer records a release-path convention naming where its release records live.

**Rationale:** One folder for release records tells a maintainer where to find the evidence for any artifact. A record filed elsewhere looks missing while readers open the folder and find nothing there.

**Example:** `.litespec/releases/2026-08-21.md` records one immutable artifact and its evidence.

**Reviewed for:** Does every release record sit under `.litespec/releases/`, or does a recorded release path convention name where it lives? Check the release paths against the convention.

## Reference Example

This informative example demonstrates `litespec/core/release#verify-connected-flows` and `litespec/core/release#identify-immutable-release-artifacts`.

A reimbursement release includes the verified submit, approve, and reimburse use cases. The submit-and-reimburse flow passes against the deployed API and database. The record names the source commit and the image digest as the immutable artifact reference.
