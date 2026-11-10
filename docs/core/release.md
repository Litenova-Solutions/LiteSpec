# Product Release

**Stack:** any

**Concern:** operations

## Intent

A product release is the verified behavior a team ships as one immutable artifact. Teams apply this page at every release to run the checks their boundaries select, to read completion computed from proof, and to write the release record and the verification report. A project that selects the [Delivery extension](../ext/delivery.md) adds its deployment and rollback rules on top.

## Concepts

### Git Is the Decision Record

A record present in git is decided, and the pull request that changes it is its review. A record the product drops is deleted; git keeps its history. A project that names who answers for an area names them in CODEOWNERS.

### Completion Is Computed

Completion is computed from proof: has code, has tests, scenarios pass. The builder computes it on every run, and the book leads with it. Nobody types it.

### The Release Gate

The gate admits a release when its G checks are green, its scenarios are verified, its S checks are green where the reader is qualified, and every departure it relies on is in effect. A departure is in effect while it is present and, when it sets `expiresOn`, before that date. G checks read records only and always block; every missing required link is G. S checks read code through a stack reader: they block where that reader is qualified and report otherwise. A checks report without blocking, except the release-readiness findings `--release` names. `litespec model check --release` makes the release-readiness findings block, so the gate is a command. A project that selects the [Delivery extension](../ext/delivery.md) adds its formal readiness gate, deployment and rollback rules on top of this gate.

### The Verification Report

Each change ships with a verification report beside it: the exact commands run, their outcomes, the evidence scope, skipped checks with reasons, generated differences, scope limits, and covered use cases. The report tells a reader of the report precisely what the evidence covers and what it does not.

### Reader Qualification

A stack reader is qualified by fixtures: a known good tree it passes and a known bad tree where it reports each planted defect. Only a qualified reader gates its scope with S checks; elsewhere the same checks report without blocking.

### Verified Behavior and Journey Evidence

A use case is implemented when its handler exists, and verified when every one of its scenarios has a passing proof through the public boundary, as the [model page](model.md) states. A journey test proves that connected use cases produce one observable product outcome through the deployed public boundary, catching the wiring, permission, and data defects that narrower suites miss.

### The Release Record

Each release creates one release record for its immutable artifact. The record names the source commit and the immutable artifact reference, and lists the included journeys, use cases, and extensions. It stays non-authoritative: specifications own behavior and scenarios, so the record links outward instead of restating them. Release records live under `.litespec/releases/` unless the project records another location. A project that selects the [Delivery extension](../ext/delivery.md) adds the evidence and operating readiness lists that extension states.

### Boundaries Select Checks

Each changed boundary adds the checks written for it. A change that touches several boundaries runs every check each one selects.

| Changed boundary | Checks the change runs |
|:---|:---|
| Backend | The release build and the backend test suites |
| Frontend | Frozen installation, lint, type checking, tests, and the production build |
| Journey | Its journey tests through the deployed boundary |
| Intent or Behavior | G checks with the book |
| Extension | The verification its extension states |

### Debt, Gaps, and the First Release

Accepted debt never sits as an untracked compromise: it lives as a departure record with an expiry date, whose findings are covered until the date and whose `departure-expired` reports after it. The [model page](model.md) states the departure shape. Governance is a group of records beside the four record layers, not a fifth layer.

A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. Before its first production release, the consumer keeps a readiness record instead, with dated rehearsals, green gates, and a backup and restore rehearsal.

### The Adopted LiteSpec Release

A consumer adopts one LiteSpec release by installing the `LiteSpec.Cli` tool package at that release version and recording the same version in `reviewedLiteSpecVersion`. Each LiteSpec release publishes the tool package and the site at the release version together. A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together, after reading the contract the new release states.

## Standards

### Verify Connected Journeys

**ID:** `litespec/core/release#verify-connected-journeys`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A completed journey MUST connect verified use cases to one observable outcome through a deployed public boundary.

**Rationale:** Use cases that pass in isolation can still fail together: wiring, permissions, and data cross boundaries the isolated tests never touch. A journey test that runs verified use cases to one observable outcome through the deployed public boundary proves the connected path customers actually take.

**Reviewed for:** Which completed journeys lack a passing test through the deployed boundary? Look at the journey tests beside each included journey.

### Run Boundary-Selected Checks

**ID:** `litespec/core/release#run-boundary-selected-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release change MUST run every check selected by its changed backend, frontend, journey, Intent, Behavior, and extension boundaries.

**Rationale:** Each boundary carries failures the others cannot see: frontend checks say nothing about migrations, and backend suites say nothing about rendered screens. Running every check the changed boundaries select keeps each change evidenced where its failure lives. A narrow category that passes alone certifies only part of the change.

**Reviewed for:** Which changes lack a passing result for a check their boundaries select? Look at the change report beside the boundary table.

### Run Backend Release Checks

**ID:** `litespec/core/release#run-backend-release-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A backend change MUST pass the release build and the backend test suites.

**Rationale:** The release build compiles the solution in release mode, catching the conditional and optimization-gated breaks a debug build hides. The architecture, integration, and acceptance suites the size requires prove architecture layering, wiring, and behavior in turn. Skipping any of them ships backend code the project's own suites do not fully prove.

**Reviewed for:** Which backend changes lack a passing release build or suite result? Look at the build and suite reports beside the change.

### Run Frontend Release Checks

**ID:** `litespec/core/release#run-frontend-release-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend change MUST run frozen installation, lint, type checking, tests, and production build checks.

**Rationale:** Each step guards a different failure: frozen installation pins dependencies, lint and type checking catch static defects, tests prove behavior, and the production build proves the shipped bundle compiles. A frontend that skips one arrives at release with that whole class of defect unchecked.

**Reviewed for:** Which frontend changes lack a passing install, lint, type, test, or build result? Look at the frontend reports beside the change.

### Run Journey Checks

**ID:** `litespec/core/release#run-journey-checks`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** A journey change MUST run its journey tests.

**Rationale:** A journey change alters the connected path, so only a journey test through the deployed public boundary can prove it still reaches its outcome. Narrower suites keep passing while the journey itself breaks.

**Reviewed for:** Which journey changes lack passing journey tests? Look at the journey reports against the deployed boundary.

### Run Consistency Checks

**ID:** `litespec/core/release#run-consistency-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent or Behavior change MUST pass the G checks with the book.

**Rationale:** G checks read records only: shape, references, links, reachability, coverage, and the Behavior rules the check list names. Without them, a renamed record or a moved file silently breaks the references other pages rely on, and the book renders a graph that no longer holds. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

**Checked by:** `litespec model check` exits zero with no G finding.

### Run Extension Checks

**ID:** `litespec/core/release#run-extension-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A selected project or applicable local extension MUST run its stated verification when affected.

**Rationale:** An extension adds behavior, tests, and operating surface beyond the baseline. Its stated verification is the only evidence written for that surface, so skipping it releases extended behavior the baseline checks never examine.

**Reviewed for:** Which affected extensions lack their stated verification evidence? Look at the extension evidence beside the change.

### Regenerate Application Contracts

**ID:** `litespec/core/release#regenerate-application-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A source change MUST regenerate OpenAPI and frontend API types when it changes their source contract.

**Rationale:** OpenAPI and frontend API types derive from a source contract. A source change that leaves the committed copies stale ships clients built against a contract the server no longer honors.

**Reviewed for:** Which committed contracts differ from a fresh generation? Look at the contract diff beside the source change.

### Keep Generated Contracts Stable

**ID:** `litespec/core/release#keep-generated-contracts-stable`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A generated application contract MUST be stable and exclude timestamps, machine paths, and nondeterministic ordering.

**Rationale:** Timestamps, machine paths, and unstable ordering make every regeneration look like a behavior change. Deterministic output keeps contract diffs reviewable, so a reviewer sees only the changes that matter.

**Reviewed for:** Which contract diffs hold timestamps, machine paths, or unstable order? Look at the generated diff beside each contract.

### Qualify the Reader with Known Good and Known Bad Fixtures

**ID:** `litespec/core/release#qualify-the-reader-with-known-good-and-known-bad-fixtures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A stack reader MUST pass its known good and known bad fixtures before it gates with S checks.

**Rationale:** An unqualified reader blocks on noise or passes on silence, and each mistake trains the team to ignore its findings. Passing a known good tree and reporting each planted defect in a known bad tree proves the reader sees what it claims before it blocks any change.

**Example:** The .NET reader passes the clean tree and reports the planted undeclared write before its S findings block.

**Reviewed for:** Which gating S readers lack passing fixture runs? Look at the fixture results beside the stack reader.

### Apply Release Access Controls

**ID:** `litespec/core/release#apply-release-access-controls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release with restricted access MUST provide authentication and authorization.

**Rationale:** Restricted behavior without authentication accepts callers nobody identified, and without authorization it accepts actions nobody permitted. Both decisions call for a trusted identity and a declared permission before the behavior ships.

**Reviewed for:** Which restricted release paths lack authentication or authorization? Look at the guards on each restricted path.

### Record the Release Artifact

**ID:** `litespec/core/release#record-the-release-artifact`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST create one release record for its immutable artifact.

**Rationale:** Evidence scattered across builds and reports answers no single question. One release record per immutable artifact binds every result to the exact output it proves.

**Reviewed for:** Which releases lack one record naming the immutable artifact? Look at the release records beside each artifact.

### Resolve Release Blockers

**ID:** `litespec/core/release#resolve-release-blockers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST settle every expired departure and every external claim it depends on, or exclude the affected behavior from every owning record.

**Rationale:** An expired departure is a promise past its date, and an unverified external claim behind released behavior is a promise with unsettled terms: a provider fact, a law text, or a term nobody rechecked. Settling each one, or excluding the affected behavior from every owning record, keeps the Intent and release records agreeing on what shipped. `departure-expired` reports every expired departure.

**Reviewed for:** Which releases depend on an expired departure or an external claim without excluding the behavior? Look at the departures and claims beside the owning records.

### Report Verification Exactly

**ID:** `litespec/core/release#report-verification-exactly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A completion report MUST name exact commands, outcomes, evidence scope, skipped checks, generated differences, scope limits, and covered use cases.

**Rationale:** A vague report lets unrun checks hide among completed ones. Naming exact commands, outcomes, scope, omissions, and limits tells a reviewer precisely what the evidence covers and what it does not.

**Reviewed for:** Which completion reports lack commands, outcomes, scope, omissions, differences, limits, or covered use cases? Look at the report beside the change.

### Identify Immutable Release Artifacts

**ID:** `litespec/core/release#identify-immutable-release-artifacts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST name source commit and immutable artifact reference.

**Rationale:** The source commit names the verified input and the artifact reference names the deployed output. Together they bind the record to one build, so evidence from another build cannot stand in for it.

**Reviewed for:** Which release records lack a source commit or artifact reference? Look at the record header beside each artifact.

### List Included Release Behavior

**ID:** `litespec/core/release#list-included-release-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list included journeys, use cases, and selected applicable extensions.

**Rationale:** The behavior boundary of an artifact is exactly its journeys, use cases, and extensions. Listing them tells operators and reviewers what the artifact contains and, by omission, what it does not.

**Reviewed for:** Which records omit an included journey, use case, or extension? Look at the behavior list on each release record.

### Keep Release Records Non-Authoritative

**ID:** `litespec/core/release#keep-release-records-non-authoritative`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST NOT restate a specification or copy a scenario.

**Rationale:** Specifications own behavior and scenarios. A release record that restates them becomes a second copy, and the copies drift until nobody knows which one holds. The record links outward instead.

**Reviewed for:** Which release records restate a specification or copy a scenario? Look at the record beside the linked specifications.

### Record Accepted Debt as a Departure

**ID:** `litespec/core/release#record-accepted-debt-as-a-departure`

**Strength:** required

**Applies to:** artifact: record:departure

**Statement:** Accepted debt MUST live as a departure record with an expiry date.

**Rationale:** Accepted debt with no expiry date is forgotten work. A departure with an `expiresOn` and `exit` turns it into a dated promise whose findings are covered until the date and whose `departure-expired` reports after it. The [model page](model.md) states the departure shape.

**Example:** Claimwick ships month-end export without its archive proof, so the debt lives as a departure with an `expiresOn` of 2027-03-01 and an `exit`; `departure-expired` reports it after that date.

**Reviewed for:** Which accepted debts lack a departure with an expiry date? Look at the departures beside each debt.

### State Each Release as a Complete Record

**ID:** `litespec/core/release#state-each-release-as-a-complete-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST state its complete behavior and evidence without depending on an earlier release record.

**Rationale:** A release judged from fragments is judged by archaeology: the reader digs through other records to fill the gaps. A complete record lets one reader judge one release from one page, with no upgrade guide filling in.

**Reviewed for:** Which release records depend on an earlier record to be judged? Look at the record alone beside its links.

## Conventions

### Store Release Records Together

**ID:** `litespec/core/release#store-release-records-together`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep release records under `.litespec/releases/`.

**Replacement:** A consumer records a release-path convention naming where its release records live.

**Rationale:** One folder for release records tells a maintainer where to find the evidence for any artifact. A record filed elsewhere looks missing while readers open the folder and find nothing there.

**Example:** `.litespec/releases/2026-08-21.md` records one immutable artifact and its evidence.

**Reviewed for:** Which release records sit outside `.litespec/releases/` without a recorded release path convention? Look at the release paths beside the convention.

## Reference Example

This informative example demonstrates `litespec/core/release#verify-connected-journeys` and `litespec/core/release#identify-immutable-release-artifacts`.

A reimbursement release includes verified submit, approve, and reimburse use cases. The submit-and-reimburse journey passes against the deployed API and database, and the record names the source commit and the image digest as the immutable artifact reference.
