# Delivery

## Intent

Delivery holds the release, deployment and operations rules a project adds when running the product for other people takes a formal regime: release readiness gates, artifact promotion, branch protection, deployment and rollback, backup and restore, alerting and on-call routing, and the supply-chain gate. A project that only needs the core checks, contracts and review flow in the baseline carries none of it. A team that operates the product in production selects it, so each release deploys, recovers and answers alerts by a written procedure instead of improvisation.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The consumer enables `delivery` when it operates the product for other people in production, promotes one artifact through staging and production, or needs a formal release gate, deployment and rollback path, alerting regime, or supply-chain gate beyond the baseline checks.

## Baseline Relationship

This extension replaces no baseline provision. The baseline keeps the boundary-selected checks, contract regeneration and the release record; this extension adds the production regime around them.

This extension switches on no optional record type.

## Standards

### Meet Release Readiness Gates

**ID:** `litespec/ext/delivery#meet-release-readiness-gates`

**Statement:** A release MUST show green G checks, verified scenarios, and green S checks where the reader is qualified.

**Rationale:** The three parts prove the three risks are closed: G checks prove the record graph holds, verified scenarios prove each behavior through the public boundary, and S checks prove the code matches the records where a qualified reader exists. A release missing any part ships with that risk unjudged. `litespec model check --release` makes the release-readiness findings block, so the gate is a command.

**Example:** A reimbursement release shows green G checks, every scenario verified, and green S checks on the .NET reader.

**Checked by:** `litespec model check --release` blocks on `use-case-unverified`.

### Provide Release Data Recovery

**ID:** `litespec/ext/delivery#provide-release-data-recovery`

**Statement:** A release MUST provide repeatable schema creation or upgrade, backup instructions, and a tested restore.

**Rationale:** A product that cannot recreate its schema, back up its data, and restore from backup holds customer data it cannot recover. Repeatable setup plus a tested restore turns recovery from a hope into a rehearsed procedure.

**Reviewed for:** Which releases lack repeatable schema setup, backup instructions, or a tested restore? Look at the recovery evidence beside the release.

### Provide Release Operating Visibility

**ID:** `litespec/ext/delivery#provide-release-operating-visibility`

**Statement:** A release MUST provide liveness, readiness, trace-correlated diagnostics, and secrets outside source control.

**Rationale:** Operators cannot keep a product they cannot see: liveness and readiness signals drive restarts and traffic, and trace-correlated diagnostics connect a failure to its cause. Secrets stay outside source control because a committed secret is a published one.

**Reviewed for:** Which releases lack health signals, trace diagnostics, or secrets outside source control? Look at the operating evidence beside the release.

### Provide Release Delivery Controls

**ID:** `litespec/ext/delivery#provide-release-delivery-controls`

**Statement:** A release MUST provide CI, tested deployment and rollback paths, current operating limits, and the runbooks those limits name.

**Rationale:** CI, tested deployment and rollback paths, current limits, and the runbooks those limits name make every release repeatable and every failure recoverable. Without them, each deployment improvises and each incident starts from scratch.

**Reviewed for:** Which releases lack current CI, deployment, rollback, limits, or runbooks? Look at the delivery evidence beside the release.

### Record Release Evidence

**ID:** `litespec/ext/delivery#record-release-evidence`

**Statement:** A release record MUST list build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results.

**Rationale:** Build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results together show the artifact works and can be operated. A missing kind is evidence nobody produced.

**Reviewed for:** Which records lack a result for a listed evidence kind? Look at the evidence list on each release record.

### Record Operating Readiness

**ID:** `litespec/ext/delivery#record-operating-readiness`

**Statement:** A release record MUST list operating limits, alerts, runbooks, known operating conditions, and skipped checks.

**Rationale:** Limits, alerts, runbooks, known conditions, and skipped checks tell the maintainer what to watch, what to run, and what remains unverified after release. Without them, the release hands over an artifact with no operating instructions.

**Reviewed for:** Which records lack limits, alerts, runbooks, conditions, or omissions? Look at the readiness list on each release record.

### Keep a First-Release Readiness Record

**ID:** `litespec/ext/delivery#keep-a-first-release-readiness-record`

**Statement:** A consumer before its first production release MUST keep a readiness record with dated rehearsals, green gates, and a rehearsal owner.

**Rationale:** A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. The readiness record instead states what proves the product may take data: dated rehearsals, green gates, and a rehearsal owner who owns the backup and restore rehearsal.

**Reviewed for:** Which pre-release consumers lack dated rehearsals, green gates, or a rehearsal owner? Look at the readiness record beside the project.

### Review Schema Artifacts

**ID:** `litespec/ext/delivery#review-schema-artifacts`

**Statement:** A persistence change MUST publish a schema artifact that lists every destructive operation with its reason.

**Rationale:** A persistence change reaches production data, so the reviewer needs to see exactly what the database will do before it runs. The schema artifact shows that: the Marten schema plan and document transformation, or the generated EF Core migration with its SQL. Listing every destructive operation with its reason forces the author to name data loss where every reader sees it.

**Reviewed for:** Each persistence change carries a schema artifact with every destructive operation and its reason.

### Require Zero Surviving Mutants on Changed Lines

**ID:** `litespec/ext/delivery#require-zero-surviving-mutants-on-changed-lines`

**Statement:** Every pull request MUST show zero surviving mutants on its changed lines.

**Rationale:** Mutation testing judges test strength where the change is: Stryker.NET `--since` scopes the backend run and StrykerJS `--incremental` scopes the frontend run, so the gate costs minutes per pull request. A project-wide percentage is confounded by suite size, so a large suite hides weak new tests behind a high score, while a full nightly run stays a trend rather than a gate. Stryker.NET 5.0.0 and StrykerJS 9.x are free and maintained under Apache-2.0. The .NET form lives on `litespec/backend/dotnet/testing#gate-changed-lines-with-mutation-testing`. A surviving mutant no test can kill is decided once by a person through the equivalent-mutant escape hatch, recorded as an exception naming this provision.

**Reviewed for:** The mutation job report for the pull request shows zero surviving mutants on its changed lines.

### Enforce the Supply-Chain Gate

**ID:** `litespec/ext/delivery#enforce-the-supply-chain-gate`

**Statement:** CI MUST enforce the supply-chain gate on every pull request.

**Rationale:** When a new advisory names a component and a version range, the team must answer one question fast: does any running release contain it. Without a component inventory beside the artifact, that answer means rebuilding the dependency graph of a commit somebody has to find first. [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states the practice, and the frozen lockfile the build resolved is what the inventory is generated from. OSV-Scanner matches dependencies against OSV.dev advisories and Gitleaks scans for leaked secrets, so advisories and secrets each have one evidence tool.

**Example:** The build job that produces the artifact generates the inventory from the same frozen lockfile. It stores the inventory beside the artifact, under the release version.

**Reviewed for:** The supply-chain report shows scan, pin, inventory, and license evidence with an exception for each unhandled high-severity advisory.

### Publish a Book Preview for Every Change

**ID:** `litespec/ext/delivery#publish-a-book-preview-for-every-change`

**Statement:** CI MUST publish a preview of the book for every change.

**Rationale:** The intent diff names what moved, while the preview shows it rendered, with the changed records readable beside their neighbors. The reader clicks from the comment into the preview for full context.

**Reviewed for:** The pull request links a book preview built from the change.

### Promote Verified Artifacts

**ID:** `litespec/ext/delivery#promote-verified-artifacts`

**Statement:** CI MUST build one immutable artifact and promote that same artifact through staging and production.

**Rationale:** Staging tests prove something about the artifact they ran against. When production deploys a fresh build from the same branch instead, it ships code the tests never saw, because the branch may have moved between the two builds. Promoting the one tested artifact through staging and production keeps the tested code and the shipped code identical.

**Reviewed for:** The staging and production deployments reference the one artifact the release job passed.

### Protect the Default Branch

**ID:** `litespec/ext/delivery#protect-the-default-branch`

**Statement:** The default branch MUST require the jobs table entries for the project's size, a reviewed pull request, and a clean merge state.

**Rationale:** The default branch is the source every environment deploys from, so an unreviewed or unchecked commit there reaches production with no further review. Requiring the jobs table entries, a reviewed pull request, and a clean merge state keeps every commit on the branch reviewed and verified. A direct push or a bypassed check is permitted only during a documented repository recovery action.

**Reviewed for:** The default branch settings require the jobs-table entries, a reviewed pull request, and a clean merge state.

### Record Every Branch Protection Bypass

**ID:** `litespec/ext/delivery#record-every-branch-protection-bypass`

**Statement:** A direct push or bypassed check on the default branch MUST produce a record naming the actor, the reason, and the change it admitted.

**Rationale:** The bypass permission exists so a recovery can proceed when the gates cannot run. A recovery nobody wrote down is indistinguishable from a habit, so the next reviewer cannot tell which commits on the branch were reviewed. The record names the actor, the reason, and the admitted change, which is the attribution [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states for the same event.

**Example:** The record is the recovery decision itself where one exists. A bypass smaller than a decision takes a dated entry in the operations log at `.litespec/behavior/operations/log/`.

**Reviewed for:** Each bypass entry in the operations log or recovery decision names its actor, reason, and admitted change.

### Keep a Canonical Job Graph

**ID:** `litespec/ext/delivery#keep-a-canonical-job-graph`

**Statement:** Consumer CI MUST use the job names and responsibilities the jobs table on the [Continuous Integration page](../quality/ci.md) states.

**Rationale:** The release job reruns the table's jobs by name, and the branch protection settings require them by name. When a consumer renames a job or moves its checks elsewhere, those references point at a job that no longer exists, and the gate stops checking without failing. Fixed names and responsibilities keep every reference resolving to the checks the table states.

**Reviewed for:** The consumer workflows use the job names and responsibilities the jobs table states.

### Keep Each Alert Threshold to Its Requirement's Measure

**ID:** `litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure`

**Statement:** An alert rule MUST state every number its requirement's `measure` names in its own expression or threshold.

**Rationale:** The requirement's measure is the agreed target, and the alert is the one place it is enforced at runtime. A measure that says two seconds and an alert that fires at five drift apart silently, because nobody re-reads the alert when the requirement changes. Comparing the numbers is a coarse check, since a measure is prose, so it advises rather than blocks.

**Example:** `requirement/spending.month-end-submissions` measures "at least 99 percent within two seconds", and its alert's expression compares the 0.99 quantile against 2.

**Checked by:** `litespec model check` reports `alert-threshold-drift` (level A) for an alert rule whose own text carries none of the decimal numbers its requirement's `measure` names.

### Record Each Drill Run as a Check Result

**ID:** `litespec/ext/delivery#record-each-drill-run-as-a-check-result`

**Statement:** A drill or measuring harness that checks a quality requirement MUST write each run as a check result naming the requirement's `check`, kept under the project's `paths.checkResults`.

**Rationale:** A drill rehearses a procedure against a running deployment, and a harness measures from outside: neither is a test, and neither runs in the test job, so their outcome lives only in a terminal or a pasted note that no tool reads. Writing each run as a small result file in Proof gives the quality requirement the same evidence a measured check gives it, under the same `check` name, with the date it ran and the value it saw.

**Example:** Claimwick's month-end load harness writes `.litespec/proof/checks/2026-11-20-month-end-load.json` holding `"check": "month-end-load"`, `"passed": true`, `"value": "99.4 percent acknowledged within two seconds"`, and `"ranOn": "2026-11-20"`, and `requirement/spending.month-end-submissions` names `month-end-load` in its `check`.

**Reviewed for:** Which drill or measuring harness runs lack a check result file naming the requirement's `check` under `paths.checkResults`? Look at the harness run records beside the files under `paths.checkResults`.

### Define Backup and Restore Behavior

**ID:** `litespec/ext/delivery#define-backup-and-restore-behavior`

**Statement:** A release MUST document backup owner, schedule, retention, encryption, location, restore command, and recovery objectives.

**Rationale:** A backup is only as good as its restore: without a named owner, a schedule, a retention bound, encryption, a location, a restore command, and recovery objectives, nobody knows what is kept, who restores it, or how much loss is accepted. Writing all seven down before production data arrives leaves nothing to decide during the incident.

**Reviewed for:** The release documentation states backup owner, schedule, retention, encryption, location, restore command, and recovery objectives.

### Exercise a Restore Before the First Production Data

**ID:** `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data`

**Statement:** A project MUST rehearse a restore on representative data before its first deployment holds production data.

**Rationale:** Backups fail in ways only a restore reveals: a missing key, a wrong permission, a dump the new version cannot read. Rehearsing on representative data before production data arrives finds those failures while they cost nothing. The first restore attempt is the wrong time to learn the backup cannot restore.

**Reviewed for:** The operations log holds a restore rehearsal on representative data before the first production data.

### Use a Repeatable Deployment

**ID:** `litespec/ext/delivery#use-a-repeatable-deployment`

**Statement:** A deployment MUST use versioned artifacts, declared configuration, a schema step, readiness checks, and a passing release job.

**Rationale:** A repeatable deployment names everything it ships: a versioned artifact, declared configuration, a schema step, readiness checks, and a passing release job. Deploying from an uncommitted tree or a mutable branch reference makes the deployed content unknowable afterwards, because the same reference points at different code a week later. Named inputs let any deployment be repeated and any incident be traced to its exact content.

**Reviewed for:** Each deployment names versioned artifacts, declared configuration, a schema step, readiness checks, and a passing release job.

### Keep Rollback Executable

**ID:** `litespec/ext/delivery#keep-rollback-executable`

**Statement:** The rollback runbook MUST state the previous artifact reference, command, configuration and schema compatibility, data recovery condition, and the expand-and-contract sequence for destructive schema work.

**Rationale:** Rollback is written during the incident if it is not written before it, and an incident is the worst time to work out which artifact was live, which command restores it, and whether the schema still fits. Stating the previous artifact reference, the command, the configuration and schema compatibility, the data recovery condition, and the expand-and-contract sequence keeps each answer ready. Rollback is a required runbook, so `litespec/ext/delivery#exercise-every-runbook-before-release` rehearses it.

**Reviewed for:** The rollback runbook states its artifact reference, command, compatibility, data condition, and expand-and-contract sequence.

### Report Liveness from Every Recurring Job

**ID:** `litespec/ext/delivery#report-liveness-from-every-recurring-job`

**Statement:** Every recurring job MUST report liveness that pages a person when the job goes silent.

**Rationale:** A recurring job with nothing to do is quiet, and a recurring job that stopped is quiet too. Without a liveness signal that pages a person on silence, the two stay indistinguishable until somebody notices the missing output. Liveness turns a quiet job from an observation into an alarm.

**Reviewed for:** Each recurring job reports liveness that pages a person on silence.

### Define Actionable Baseline Alerts

**ID:** `litespec/ext/delivery#define-actionable-baseline-alerts`

**Statement:** A baseline alert MUST declare owner, threshold, evaluation window, severity, and runbook before a production release.

**Rationale:** Each declared field answers one failure mode:

- An alert with no owner waits for somebody to adopt it.
- An alert with no threshold or evaluation window fires on noise.
- An alert with no severity waits in the wrong queue.
- An alert with no runbook asks its receiver to invent the response.

Declaring all five before production makes each alert answerable the first time it fires.

**Reviewed for:** Each baseline alert declares owner, threshold, window, severity, and runbook.

### Cover the Baseline Alert Set

**ID:** `litespec/ext/delivery#cover-the-baseline-alert-set`

**Statement:** Baseline coverage MUST include sustained readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments.

**Rationale:** Five failures end a release when nobody observes them: readiness that never recovers, HTTP errors the code did not expect, a slowing release flow, a lost database, and a deployment that failed partway. Naming them as the minimum coverage keeps a team from declaring alerts only for the failure it just survived.

**Reviewed for:** The baseline alerts cover readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments.

### Route Each Severity to a Declared Destination

**ID:** `litespec/ext/delivery#route-each-severity-to-a-declared-destination`

**Statement:** An alert inventory MUST state, for each severity it uses, the destination that receives the alert and the response time expected of it.

**Rationale:** A declared severity with no destination is a label: every alert arrives in the same place, and the page that wakes somebody is read beside the one that can wait. Routing each severity to its destination with an expected response time separates the page from the ticket, which is the separation [Prometheus alerting practice](https://prometheus.io/docs/practices/alerting/) states. The limits page declares the routing beside the generated inventory.

**Example:** A two-severity inventory routes `page` to the on-call destination with a stated response time, and `ticket` to the queue reviewed each working day.

**Reviewed for:** Each severity states its destination and expected response time on the limits page.

### Exercise Every Runbook Before Release

**ID:** `litespec/ext/delivery#exercise-every-runbook-before-release`

**Statement:** Each required runbook MUST carry a dated rehearsal in the operations log before a production release, with restore rehearsed on representative data.

**Rationale:** A runbook is a claim until somebody runs it: steps go stale, commands change, permissions lapse. A dated rehearsal in the operations log proves each required runbook still works against the current system. The restore rehearsal is the one `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data` requires.

**Reviewed for:** Each required runbook carries a dated rehearsal in the operations log.

### State Availability Objectives as OpenSLO Files

**ID:** `litespec/ext/delivery#state-availability-objectives-as-openslo-files`

**Statement:** A production release MUST state its availability and latency objectives as OpenSLO files compiled by Sloth.

**Rationale:** OpenSLO files state objectives as code, and Sloth validates them in CI and compiles them into Prometheus recording rules and burn-rate alerts. OpenTelemetry supplies the signals the objectives read, as `litespec/quality/operations#emit-correlated-diagnostics` states. Sloth and OpenTelemetry are free and maintained under Apache-2.0, and availability stays evidenced in production only.

**Reviewed for:** The OpenSLO files pass Sloth validation with their generated alerts loaded.

### Attach Dashboard Links to Production Objectives

**ID:** `litespec/ext/delivery#attach-dashboard-links-to-production-objectives`

**Statement:** Each production objective MUST link to its dashboard in the book.

**Rationale:** The book states intent while dashboards show production truth, and the link joins them: a reviewer clicks from the objective to the numbers behind it. Copying numbers into the page instead would freeze production truth at commit time.

**Reviewed for:** Each production objective in the book links its dashboard.

### Match Every Shipped Package to Its Pin

**ID:** `litespec/ext/delivery#match-every-shipped-package-to-its-pin`

**Statement:** Every direct package in the project's generated bill of materials that the manifest pins MUST ship at the pinned version.

**Rationale:** The bill of materials (SBOM) is the computed list of what ships, so it is the one place a pin can be checked against instead of trusted. A version that drifted from its pin carries advisories and behavior the release never reviewed. The bill is generated, so the fact stays written once, and a record per package would rewrite hundreds of facts on every update.

**Example:** Claimwick's bill of materials sits at `.litespec/proof/sbom/api.spdx.json`, the project file names that folder in `paths.sbom`, and a direct `pkg:nuget/Marten` at a version other than the manifest pin is reported.

**Checked by:** `litespec model check` reports `dependency-version-drift` (level S) for a direct package whose version differs from its manifest pin.

### Accept Every Shipped Package License Explicitly

**ID:** `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`

**Statement:** Every direct package in the project's generated bill of materials MUST carry a license the project accepts in `dependencies.licenses`, or one its `admits` entry names.

**Rationale:** A copyleft or commercial license binds the product the day the package ships, and a license nobody accepted is found at an audit rather than at the change that brought it. Listing the accepted licenses once, and naming any other on the admitting decision, keeps the license choice a decision with a person's name on it.

**Example:** Claimwick accepts `MIT`, `Apache-2.0`, and `BSD-3-Clause` in `dependencies.licenses`, so a direct package under `AGPL-3.0-only` that no decision admits is reported.

**Checked by:** `litespec model check` reports `dependency-license-unknown` (level S) for a direct package whose license the project does not accept and no `admits` entry names.

## Conventions

### Treat Check Warnings as Failures

**ID:** `litespec/ext/delivery#treat-check-warnings-as-failures`

**Default:** Treat build and lint warnings as failures unless the build configuration records a narrow suppression with its reason.

**Replacement:** A consumer records a warnings convention naming which warnings fail its build.

**Rationale:** Warnings that nobody reviews accumulate until a real defect hides among them. Failing on warnings forces each one to be fixed or narrowly suppressed with its reason, so a changed compiler, linter, or dependency boundary surfaces immediately.

**Reviewed for:** Which builds pass with unreviewed warnings outside recorded suppressions? Look at the build logs beside the warnings convention.

### Apply the Documented Defaults

**ID:** `litespec/ext/delivery#apply-the-documented-defaults`

**Default:** Keep one workflow per job the jobs table lists and use the job names the jobs table states.

**Replacement:** A consumer records a workflow-layout convention naming its workflow files and job names.

**Rationale:** One workflow per job keeps each gate readable, runnable, and skippable on its own. When containerized integration tests or browser tests join CI, they join as jobs the table names rather than as hidden steps inside another job, so the selection rules and the branch protection keep working unchanged.

**Reviewed for:** Each consumer workflow file matches one jobs-table job and name, or the workflow-layout convention names another arrangement.

### Use Stable Service Names

**ID:** `litespec/ext/delivery#use-stable-service-names`

**Default:** Keep service, resource, meter, and trace-source names identical across environments.

**Replacement:** A consumer records a service-naming convention naming how its services, resources, meters, and trace sources are named.

**Rationale:** Dashboards and alerts reference services by name. When a name changes with each deployment, every reference breaks and must be rebuilt before the new deployment can be watched. Identical names across environments keep one dashboard and one alert set working everywhere.

**Reviewed for:** Service, resource, meter, and trace-source names match across environments, or the service-naming convention names another arrangement.

### Keep Deployment Descriptors Together

**ID:** `litespec/ext/delivery#keep-deployment-descriptors-together`

**Default:** Keep compose files, proxy configuration, the environment example, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts under one `deploy/` tree.

**Replacement:** A consumer records a deployment-layout convention naming where its deployment descriptors live.

**Rationale:** Deployment spreads across compose files, proxy configuration, environment examples, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts. Scattered across the workspace, one of them is always the file nobody finds during an incident. One `deploy/` tree keeps every descriptor where the operator looks first.

**Reviewed for:** The deployment descriptors sit under one `deploy/` tree, or the deployment-layout convention names another arrangement.

### Keep Scripts at the Root

**ID:** `litespec/ext/delivery#keep-scripts-at-the-root`

**Default:** Keep bootstrap, release, and CI helper scripts under root `scripts/`.

**Replacement:** A project that organizes scripts per application records its own script layout in its project documentation.

**Rationale:** Bootstrap, release, and CI helper scripts serve the whole workspace, so the root `scripts/` folder keeps them where every caller looks. An application-specific script may stay inside its application when no other workspace calls it.

**Reviewed for:** Bootstrap, release, and CI helper scripts sit under root `scripts/`; where replaced, the project documentation names the project's own script layout.

## Dependencies

Packages: None.

Related extensions: `containers` for image rollout and rollback material, and `stage` for the period before the first customer data.

Guides: None.
