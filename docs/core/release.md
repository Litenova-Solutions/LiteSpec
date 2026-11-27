# Product Release

**Stack:** any

**Concern:** operations

A product release is the verified behavior a team ships as one immutable artifact, a build output left unchanged after it is made. This page states what each release runs, what its record holds, and what its verification report shows. It is for the team cutting a release, and for the reviewer judging whether the evidence supports it. A project selecting the [Delivery extension](../ext/delivery.md) adds that extension's deployment and rollback rules on top.

## Concepts

### The Release Gate

A release passes its gate when its blocking checks are green, its scenarios are verified, and every waiver it relies on is live. Whether a finding blocks follows its level: a G finding blocks, an S finding blocks where the reader producing it is qualified, and an R finding blocks nothing. There is no advisory level: `litespec model check` prints findings that block, and `litespec model review` reports the prose quality a person weighs.

`litespec model check --release` makes `reader-missing` block for a claimed layer, so running that command applies the gate. Under `--release`, a claimed layer whose reader is missing or failing blocks, since the gate judges no evidence a reader it trusts failed to read. A project selecting the [Delivery extension](../ext/delivery.md) adds its formal readiness gate, deployment, and rollback rules on top of this gate.

Completion is computed from proof: code exists, tests exist, and scenarios pass. The builder computes it on every run, nobody types it, and the book leads with it.

### How Readers Earn Trust

A stack reader is qualified by fixtures: a known good tree it passes and a known bad tree where it reports each planted defect. The fixtures prove the reader sees what it claims before it blocks any change. The .NET reader passes the clean tree and reports the planted undeclared write before its S findings block.

A qualified reader gates its scope with S checks. Where the reader is unqualified, the same checks report without blocking, so an unproven reader blocks no release on findings it fails to prove, and passes no release whose defects it fails to see.

### Verified Behavior and Flow Evidence

A use case is coded when its handler exists, and verified when every one of its scenarios has a passing proof through the public boundary, as the [model page](model.md) states.

A flow test proves connected use cases producing one observable product outcome through the deployed public boundary. It catches the wiring, permission, and data defects narrower suites miss, since those defects live where use cases meet, not inside any one of them.

### What a Release Change Runs

Each changed boundary selects the checks written for it. A change touching several boundaries runs every check each one selects, since each boundary carries failures the others miss.

| Changed boundary | Checks the change runs |
|:---|:---|
| Backend | The release build and the backend test suites |
| Frontend | Frozen installation, lint, type checking, tests, and the production build |
| Flow | Its flow tests through the deployed boundary |
| Intent or Behavior | G checks with the book |
| Extension | The verification its extension states |

### The Release Record

Each release creates one release record for its immutable artifact. The record names the source commit and the immutable artifact reference, and lists the included flows, use cases, and extensions. It stays non-authoritative: specifications own behavior and scenarios, so the record links outward instead of restating them. Release records live under `.litespec/releases/` unless the project records another location. One release record binds every result to the exact output it proves, so each artifact carries its own evidence, and one reader judges one release from one page. A project selecting the [Delivery extension](../ext/delivery.md) adds the evidence and operating readiness lists that extension states.

### The Verification Report

Each change ships with a verification report beside it: the exact commands run, their outcomes, the evidence scope, skipped checks with reasons, generated differences, scope limits, and covered use cases. The report tells a reader precisely what the evidence covers and what it leaves out.

Before the first production release, the consumer keeps a readiness record instead, with dated rehearsals, green gates, and a backup and restore rehearsal. A release record binds evidence to one artifact, and a product with no artifact yet has nothing to record in one.

Accepted debt lives as a gap waiver with an expiry date, an exit, and closing records. Its findings stay covered until the date, and `waiver-expired` reports after it. The [waivers page](waivers.md) states the waiver shape. Governance is a group of records beside the four record layers, not a fifth layer.

### The Adopted LiteSpec Release

A consumer adopts one LiteSpec release by installing the `LiteSpec.Cli` tool package at that release version and recording the same version in `reviewedLiteSpecVersion`. The two move together: the tool carries the manifest, schemas, docs, and templates the version names, and the recorded version says which contract the project reviewed.

Each LiteSpec release publishes the tool package and the site at the release version together. A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together, after reading the contract the new release states. This model governs LiteSpec itself. A consumer product has its own users, so its API compatibility, migration, deprecation, and rollback provisions still apply.

### Release Access and Blockers

Restricted release paths carry authentication and authorization. Restricted behavior without authentication accepts callers nobody identified, and without authorization it accepts actions nobody permitted. Both decisions call for a trusted identity and a declared permission before the behavior ships.

A release settles every expired waiver and every external claim it depends on, or excludes the affected behavior from every owning record. An expired waiver is a promise past its date, and an unverified external claim behind released behavior is a promise with unsettled terms: a provider fact, a law text, or a term nobody rechecked. Settling each one, or excluding the affected behavior, keeps the Intent and release records agreeing on what shipped. `waiver-expired` reports every expired waiver.

Generated application contracts stay deterministic: no timestamps, machine paths, or nondeterministic ordering. Deterministic output keeps contract diffs reviewable, so a reviewer sees the changes that matter.

## Standards

### Verify Connected Flows

**ID:** `litespec/core/release#verify-connected-flows`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** A completed flow MUST connect verified use cases to one observable outcome through a deployed public boundary, proved by its flow tests.

**Rationale:** Use cases passing in isolation still fail together: wiring, permissions, and data cross boundaries the isolated tests touch not at all. A flow test running verified use cases to one observable outcome through the deployed public boundary proves the connected path customers actually take. A flow change alters the connected path, so a flow test through the deployed public boundary alone proves it still reaches its outcome; narrower suites keep passing while the flow itself breaks.

**Reviewed for:** Does every completed flow show a passing test through the deployed boundary, and every flow change its passing flow tests? Check the flow tests beside each included flow.

### Run Boundary-Selected Checks

**ID:** `litespec/core/release#run-boundary-selected-checks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release change MUST run every check selected by its changed backend, frontend, flow, Intent, Behavior, and extension boundaries.

**Rationale:** Each boundary carries failures the others miss: frontend checks say nothing about migrations, and backend suites say nothing about rendered screens. Running every check the changed boundaries select keeps each change evidenced where its failure lives. A narrow category passing alone certifies part of the change. The release build catches the conditional and optimization-gated breaks a debug build hides; frozen installation, lint, type checking, tests, and the production build each guard one failure class of a frontend; and an affected extension's stated verification is the written evidence for its surface.

**Reviewed for:** Does every change show a passing result for each check its boundaries select? Check the change report against the boundary table.

### Check Intent and Behavior Changes with the Book

**ID:** `litespec/core/release#check-intent-and-behavior-changes-with-the-book`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Intent or Behavior change MUST pass the G checks with the book.

**Rationale:** G checks read records alone: shape, references, links, reachability, coverage, and the Behavior rules the check list names. Without them, a renamed record or a moved file silently breaks the references other pages rely on, and the book renders a graph that no longer holds. The command `litespec model check --since <ref>` blocks on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

**Checked by:** `litespec model check` exits zero when it reports no G finding, such as `record-invalid`, and the inspection was complete.

### Run One LiteSpec Build

**ID:** `litespec/core/release#run-one-litespec-build`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST run one LiteSpec build at a time: the `LiteSpec.Cli` version the tool manifest pins, the `LiteSpec.Analyzers` version the build references, and the CLI running the checks name the same build.

**Rationale:** Checks written against one build misread code written against another. An old tool reports findings the current build fixed, and a lagging analyzer runs its newest rules at no point, so the team fixes findings that apply nowhere and misses the ones that do. Moving the tool manifest, the analyzer reference, and the running CLI together keeps every finding about the build the code follows.

**Example:** The Claimwick expense product pins `LiteSpec.Cli` 7.0.0 in `.config/dotnet-tools.json`, references `LiteSpec.Analyzers` 7.0.0 from its central package file, and runs the checks with the 7.0.0 CLI.

**Checked by:** `litespec model check` reports `toolchain-drift` when any two of the three name different builds.

### Regenerate Application Contracts

**ID:** `litespec/core/release#regenerate-application-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A source change MUST regenerate OpenAPI and frontend API types when it changes their source contract.

**Rationale:** OpenAPI and frontend API types derive from a source contract, and clients build against the committed copies. A source change leaving those copies stale ships clients built against a contract the server no longer honors.

**Reviewed for:** Does any committed contract differ from a fresh generation? Check the contract diff beside the source change.

### Qualify Each Stack Reader with Fixtures

**ID:** `litespec/core/release#qualify-each-stack-reader-with-fixtures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A stack reader MUST pass its known good and known bad fixtures before it gates with S checks.

**Rationale:** An unqualified reader blocks on noise or passes on silence, and each mistake trains the team to ignore its findings. Passing a known good tree and reporting each planted defect in a known bad tree proves the reader sees what it claims before it blocks any change.

**Example:** The .NET reader passes the clean tree and reports the planted undeclared write before its S findings block.

**Reviewed for:** Does every gating S reader show passing fixture runs? Check the fixture results beside the stack reader.

### Record the Release Artifact

**ID:** `litespec/core/release#record-the-release-artifact`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST create one release record for its immutable artifact.

**Rationale:** Evidence scattered across builds and reports leaves no one place saying what one artifact proved. One release record per immutable artifact binds every result to the exact output it proves, so each artifact carries its own evidence.

**Reviewed for:** Does every release show one record naming its immutable artifact? Check the release records beside each artifact.

### Report Verification Exactly

**ID:** `litespec/core/release#report-verification-exactly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A completion report MUST name exact commands, outcomes, evidence scope, skipped checks, generated differences, scope limits, and covered use cases.

**Rationale:** A vague report lets unrun checks hide among completed ones. Naming exact commands, outcomes, scope, omissions, differences, limits, and covered use cases tells a reviewer precisely what the evidence covers and what it leaves out.

**Reviewed for:** Does every completion report name its commands, outcomes, scope, omissions, differences, limits, and covered use cases? Check the report beside the change.

### Boot the Production Configuration Before Release

**ID:** `litespec/core/release#boot-the-production-configuration-before-release`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Before a release, a server project MUST boot its production containers from the committed configuration against an empty database and confirm that the readiness path answers, each public path answers behind the forwarded https scheme, and no stub adapter serves traffic.

**Rationale:** A product that only ever ran with development settings fails in production on what development hides: a missing migration, a proxy that strips the scheme, or a stand-in adapter left bound. Booting the production configuration once before release finds those failures while they cost a rerun instead of an outage.

**Example:** Before the Claimwick release, the team boots the production compose file with the example environment file, migrates the empty database, and sees the readiness path answer and the expense API answer behind the proxy, with no stub mail sender bound.

**Checked by:** `litespec verify production-smoke` reports `production-readiness-failed`, `path-failed` and `stub-adapter-shown` for the project that names a production block in `verify`.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/release#verify-connected-flows` and `litespec/core/release#record-the-release-artifact`.

A reimbursement release includes the verified submit, approve, and reimburse use cases. The submit-and-reimburse flow passes against the deployed API and database. The release record names the source commit and the image digest as the immutable artifact reference.
