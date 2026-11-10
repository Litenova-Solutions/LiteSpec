# Delivery

**Stack:** any
**Concern:** operations

## Intent

Delivery holds the release, deployment and operations rules a project adds when running the product for other people takes a formal regime: release readiness gates, artifact promotion, deployment and rollback, backup and restore, alerts and runbooks, the supply-chain gate, container image build, rollout and runtime limits, and agent handoff. A project that only needs the core checks, contracts and review flow in the baseline carries none of it. A team that operates the product in production selects it, so each release deploys, recovers and answers alerts by a written procedure instead of improvisation, with container releases shipping the same image bytes they tested.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The consumer enables `delivery` when it operates the product for other people in production, promotes one artifact through staging and production, or needs a formal release gate, deployment and rollback path, alerting and runbook regime, or supply-chain gate beyond the baseline checks. It also enables `delivery` when production or staging deploys the API, the Worker, or a frontend as a container image, or when one agent starts another on the same tree.

## Baseline Relationship

This extension replaces no baseline provision. The baseline keeps the boundary-selected checks, contract regeneration and the release record; this extension adds the production regime around them. Its schema, rollback, and secret rules cite the baseline provisions that own those obligations instead of restating them.

This extension switches on `setting` and `runbook`: resource bounds are setting records under `litespec/ext/delivery#declare-writable-paths-and-limits`, and the rollback procedure is a runbook record under `litespec/ext/delivery#keep-rollback-executable`.

## Standards

### Meet Release Readiness Gates

**ID:** `litespec/ext/delivery#meet-release-readiness-gates`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST show green G checks, verified scenarios, and green S checks where the reader is qualified.

**Rationale:** The three parts prove the three risks are closed: G checks prove the record graph holds, verified scenarios prove each behavior through the public boundary, and S checks prove the code matches the records where a qualified reader exists. A release missing any part ships with that risk unjudged. `litespec model check --release` makes the release-readiness findings block, so the gate is a command.

**Example:** A reimbursement release shows green G checks, every scenario verified, and green S checks on the .NET reader.

**Checked by:** `litespec model check --release` blocks on `use-case-unverified`.

### Provide Release Data Recovery

**ID:** `litespec/ext/delivery#provide-release-data-recovery`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST provide repeatable schema creation or upgrade, backup instructions, and a tested restore.

**Rationale:** A product that cannot recreate its schema, back up its data, and restore from backup holds customer data it cannot recover. Repeatable setup plus a tested restore turns recovery from a hope into a rehearsed procedure.

**Reviewed for:** Which releases lack repeatable schema setup, backup instructions, or a tested restore? Look at the recovery evidence beside the release.

### Provide Release Operating Visibility

**ID:** `litespec/ext/delivery#provide-release-operating-visibility`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST provide liveness, readiness, trace-correlated diagnostics, and secrets outside source control.

**Rationale:** Operators cannot keep a product they cannot see: liveness and readiness signals drive restarts and traffic, and trace-correlated diagnostics connect a failure to its cause. Secrets stay outside source control because a committed secret is a published one.

**Reviewed for:** Which releases lack health signals, trace diagnostics, or secrets outside source control? Look at the operating evidence beside the release.

### Provide Release Delivery Controls

**ID:** `litespec/ext/delivery#provide-release-delivery-controls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST provide tested deployment and rollback paths, current operating limits, and the runbooks those limits name.

**Rationale:** Tested deployment and rollback paths, current limits, and the runbooks those limits name make every release repeatable and every failure recoverable. Without them, each deployment improvises and each incident starts from scratch.

**Reviewed for:** Which releases lack deployment, rollback, limits, or runbooks? Look at the delivery evidence beside the release.

### Record Release Evidence

**ID:** `litespec/ext/delivery#record-release-evidence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results.

**Rationale:** Build, test, contract, security, journey, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results together show the artifact works and can be operated. A missing kind is evidence nobody produced.

**Reviewed for:** Which records lack a result for a listed evidence kind? Look at the evidence list on each release record.

### Record Operating Readiness

**ID:** `litespec/ext/delivery#record-operating-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list operating limits, alerts, runbooks, known operating conditions, and skipped checks.

**Rationale:** Limits, alerts, runbooks, known conditions, and skipped checks tell the maintainer what to watch, what to run, and what remains unverified after release. Without them, the release hands over an artifact with no operating instructions.

**Reviewed for:** Which records lack limits, alerts, runbooks, conditions, or omissions? Look at the readiness list on each release record.

### Keep a First-Release Readiness Record

**ID:** `litespec/ext/delivery#keep-a-first-release-readiness-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer before its first production release MUST keep a readiness record with dated rehearsals, green gates, and a rehearsal owner.

**Rationale:** A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. The readiness record instead states what proves the product may take data: dated rehearsals, green gates, and a rehearsal owner who owns the backup and restore rehearsal.

**Reviewed for:** Which pre-release consumers lack dated rehearsals, green gates, or a rehearsal owner? Look at the readiness record beside the project.

### Review Schema Artifacts

**ID:** `litespec/ext/delivery#review-schema-artifacts`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** A persistence change MUST publish a schema artifact that lists every destructive operation with its reason.

**Rationale:** A persistence change reaches production data, so the reviewer needs to see exactly what the database will do before it runs. The schema artifact shows that: the Marten schema plan and document transformation, or a generated relational migration with its SQL. Listing every destructive operation with its reason forces the author to name data loss where every reader sees it.

**Reviewed for:** Which persistence changes lack a schema artifact with every destructive operation and its reason? Look at the schema artifact beside each change.

### Enforce the Supply-Chain Gate

**ID:** `litespec/ext/delivery#enforce-the-supply-chain-gate`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST enforce the supply-chain gate on every pull request.

**Rationale:** When a new advisory names a component and a version range, the team must answer one question fast: does any running release contain it. Without a component inventory beside the artifact, that answer means rebuilding the dependency graph of a commit somebody has to find first. [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states the practice, and the frozen lockfile the build resolved is what the inventory is generated from. OSV-Scanner matches dependencies against OSV.dev advisories and Gitleaks scans for leaked secrets, so advisories and secrets each have one evidence tool.

**Example:** The build job that produces the artifact generates the inventory from the same frozen lockfile. It stores the inventory beside the artifact, under the release version.

**Reviewed for:** Which supply-chain reports lack scan, pin, inventory, or license evidence, or a departure covering each unhandled high-severity advisory? Look at the supply-chain report beside the artifact.

### Publish a Book Preview for Every Change

**ID:** `litespec/ext/delivery#publish-a-book-preview-for-every-change`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST publish a preview of the book for every change.

**Rationale:** The intent diff names what moved, while the preview shows it rendered, with the changed records readable beside their neighbors. The reader clicks from the comment into the preview for full context.

**Reviewed for:** Which pull requests lack a link to a book preview built from the change? Look at the pull request description.

### Promote Verified Artifacts

**ID:** `litespec/ext/delivery#promote-verified-artifacts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST build one immutable artifact and promote that same artifact through staging and production.

**Rationale:** Staging tests prove something about the artifact they ran against. When production deploys a fresh build from the same branch instead, it ships code the tests never saw, because the branch may have moved between the two builds. Promoting the one tested artifact through staging and production keeps the tested code and the shipped code identical.

**Reviewed for:** Which staging or production deployments reference an artifact other than the one the release job passed? Look at the deployment references.

### Protect the Default Branch

**ID:** `litespec/ext/delivery#protect-the-default-branch`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The default branch MUST require the jobs table entries for the project's size, a reviewed pull request, and a clean merge state.

**Rationale:** The default branch is the source every environment deploys from, so an unreviewed or unchecked commit there reaches production with no further review. Requiring the jobs table entries, a reviewed pull request, and a clean merge state keeps every commit on the branch reviewed and verified. A direct push or a bypassed check is permitted only during a documented repository recovery action.

**Reviewed for:** Which default branch settings omit the jobs-table entries, a reviewed pull request, or a clean merge state? Look at the branch protection settings.

### Record Every Branch Protection Bypass

**ID:** `litespec/ext/delivery#record-every-branch-protection-bypass`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A direct push or bypassed check on the default branch MUST produce a record naming the actor, the reason, and the change it admitted.

**Rationale:** The bypass permission exists so a recovery can proceed when the gates cannot run. A recovery nobody wrote down is indistinguishable from a habit, so the next reviewer cannot tell which commits on the branch were reviewed. The record names the actor, the reason, and the admitted change, which is the attribution [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states for the same event.

**Example:** The record is the recovery decision itself where one exists. A bypass smaller than a decision takes a dated entry in the operations log at `.litespec/implementation/operations/log/`.

**Reviewed for:** Which bypass entries in the operations log or recovery decision omit their actor, reason, or admitted change? Look at each bypass entry.

### Keep a Canonical Job Graph

**ID:** `litespec/ext/delivery#keep-a-canonical-job-graph`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Consumer CI MUST use the job names and responsibilities the jobs table on the [Continuous Integration page](../quality/ci.md) states.

**Rationale:** The release job reruns the table's jobs by name, and the branch protection settings require them by name. When a consumer renames a job or moves its checks elsewhere, those references point at a job that no longer exists, and the gate stops checking without failing. Fixed names and responsibilities keep every reference resolving to the checks the table states.

**Reviewed for:** Which consumer workflows use job names or responsibilities the jobs table does not state? Look at the workflow files.

### Keep Each Alert Threshold to Its Requirement's Measure

**ID:** `litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An alert rule MUST state every number its requirement's `measure` names in its own expression or threshold.

**Rationale:** The requirement's measure is the agreed target, and the alert is the one place it is enforced at runtime. A measure that says two seconds and an alert that fires at five drift apart silently, because nobody re-reads the alert when the requirement changes. Comparing the numbers is a coarse check, since a measure is prose, so it advises rather than blocks.

**Example:** `requirement/expense-submission.month-end-submissions` measures "at least 99 percent within two seconds", and its alert's expression compares the 0.99 quantile against 2.

**Checked by:** `litespec model check` reports `alert-threshold-drift` (level A) for an alert rule whose own text carries none of the decimal numbers its requirement's `measure` names.

### Record Each Drill Run as a Check Result

**ID:** `litespec/ext/delivery#record-each-drill-run-as-a-check-result`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A drill or measuring harness that checks a quality requirement MUST write each run as a check result naming the requirement's `check`, kept under the project's `paths.checkResults`.

**Rationale:** A drill rehearses a procedure against a running deployment, and a harness measures from outside: neither is a test, and neither runs in the test job, so their outcome lives only in a terminal or a pasted note that no tool reads. Writing each run as a small result file in Proof gives the quality requirement the same evidence a measured check gives it, under the same `check` name, with the date it ran and the value it saw.

**Example:** Claimwick's month-end load harness writes `.litespec/proof/checks/2026-11-20-month-end-load.json` holding `"check": "month-end-load"`, `"passed": true`, `"value": "99.4 percent acknowledged within two seconds"`, and `"ranOn": "2026-11-20"`, and `requirement/expense-submission.month-end-submissions` names `month-end-load` in its `check`.

**Reviewed for:** Which drill or measuring harness runs lack a check result file naming the requirement's `check` under `paths.checkResults`? Look at the harness run records beside the files under `paths.checkResults`.

### Name the Requirement and Runbook on Every Alert

**ID:** `litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every alert rule MUST carry a `requirement` label naming the quality requirement it measures and a `runbook` label naming the runbook that answers it, both as record identifiers.

**Rationale:** An alert is the runtime half of a quality requirement: it fires when the measure the requirement promises is breached, and the runbook says what a person does next. Written as a file path or left out, the link is a string no tool follows, so an alert can outlive its requirement, a requirement can lose its last alert, and a renamed runbook breaks silently. Record identifiers in the alert's own labels keep the link in the file the alert lives in, in the syntax its tool already reads, and let the model check both ends. A baseline alert also carries its window and severity, as `litespec/ext/delivery#define-actionable-baseline-alerts` states.

**Example:** Claimwick's alert rule `submission-acknowledgement-slow` carries `requirement: requirement/expense-submission.month-end-submissions` and `runbook: runbook/slow-submissions` in its labels.

**Checked by:** `litespec model check` reports `alert-link-missing` (level S) for an alert rule in the files `paths.alerts` names whose `requirement` label is missing or names no quality requirement, or whose `runbook` label is missing or names no runbook.

### Cover Every Quality Requirement with an Alert or a Check

**ID:** `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Every quality requirement that is not retired MUST be named by at least one alert rule or check result, or covered by a departure.

**Rationale:** A quality requirement nobody watches is a promise checked only when a customer notices it broken. An alert names it to watch production, and a check result, from a measured check or a drill, names it through its `check` to show it was measured before release; a requirement that cannot be watched yet says so as a departure with a reason, an exit and an expiry date, instead of passing silently.

**Example:** `requirement/expense-submission.month-end-submissions` is named by the alert `submission-acknowledgement-slow` and by the k6 result its `check` names.

**Checked by:** `litespec model check` reports `quality-alert-missing` (level G) for a quality requirement that no alert rule names in its `requirement` label, whose `check` matches no measured-check or check result the model read, and that no departure covering `quality-alert-missing` lists in its `subject`.

### Point Each Operations File at Its Decision

**ID:** `litespec/ext/delivery#point-each-operations-file-at-its-decision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every deployment, proxy, host, and CI workflow file the project lists in `paths.operations` MUST carry a `follows` mark naming the technical decision, departure, or provision it carries out.

**Rationale:** Operations files hold choices as load-bearing as code: the proxy's security headers, the backup schedule, the gates a pull request passes. Their reason usually lives in a comment that no tool reads, so a retired decision leaves its configuration behind unnoticed. A `follows` mark in the file's own comment syntax is the same link a technical class carries, so `decision-unimplemented` and `mark-unknown` cover these files too.

**Example:** Claimwick's `deploy/Caddyfile` opens with `# <follows>decision/0004-serve-receipts-behind-one-proxy</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a file in `paths.operations` with no `follows` mark and `mark-unknown` (level S) for a mark naming no decision, departure, or provision, and counts the file toward `decision-unimplemented`.

### Name Every Runbook from an Alert, Job, Reaction or Failure Policy

**ID:** `litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Every runbook record MUST be named by at least one alert, job, reaction or failure policy.

**Rationale:** A runbook nobody names answers no failure: an alert, a job, a reaction or a failure policy is the caller that sends a person to the procedure, and without that link the procedure waits undiscovered while the failure it answers is handled from memory. Naming each runbook from at least one caller keeps every procedure reachable from the event that needs it.

**Example:** Claimwick's job `send-event-change-notices` names `runbook/clear-stuck-notices` in its `runbook` field, so the procedure is reachable from the work that needs it.

**Checked by:** `litespec model check` reports `runbook-unused` (level A) for a runbook record that no alert label, job, reaction or failure policy names.

### Define Backup and Restore Behavior

**ID:** `litespec/ext/delivery#define-backup-and-restore-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST document backup owner, schedule, retention, encryption, location, restore command, and recovery objectives.

**Rationale:** A backup is only as good as its restore: without a named owner, a schedule, a retention bound, encryption, a location, a restore command, and recovery objectives, nobody knows what is kept, who restores it, or how much loss is accepted. Writing all seven down before production data arrives leaves nothing to decide during the incident.

**Reviewed for:** Which release documentation omits a backup owner, schedule, retention, encryption, location, restore command, or recovery objective? Look at the release documentation.

### Exercise a Restore Before the First Production Data

**ID:** `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST rehearse a restore on representative data before its first deployment holds production data.

**Rationale:** Backups fail in ways only a restore reveals: a missing key, a wrong permission, a dump the new version cannot read. Rehearsing on representative data before production data arrives finds those failures while they cost nothing. The first restore attempt is the wrong time to learn the backup cannot restore.

**Reviewed for:** Which operations logs lack a restore rehearsal on representative data before the first production data? Look at the operations log entries.

### Use a Repeatable Deployment

**ID:** `litespec/ext/delivery#use-a-repeatable-deployment`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST use versioned artifacts, declared configuration, a schema step, readiness checks, and a passing release job.

**Rationale:** A repeatable deployment names everything it ships: a versioned artifact, declared configuration, a schema step, readiness checks, and a passing release job. Deploying from an uncommitted tree or a mutable branch reference makes the deployed content unknowable afterwards, because the same reference points at different code a week later. Named inputs let any deployment be repeated and any incident be traced to its exact content.

**Reviewed for:** Which deployments omit a versioned artifact, declared configuration, a schema step, readiness checks, or a passing release job? Look at the deployment definitions.

### Keep Rollback Executable

**ID:** `litespec/ext/delivery#keep-rollback-executable`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** The rollback runbook MUST state the previous artifact reference, command, configuration and schema compatibility, data recovery condition, and the expand-and-contract sequence for destructive schema work, and name the previous image digest for a container release.

**Rationale:** Rollback is written during the incident if it is not written before it, and an incident is the worst time to work out which artifact was live, which command restores it, and whether the schema still fits. Stating the previous artifact reference, the command, the configuration and schema compatibility, the data recovery condition, and the expand-and-contract sequence keeps each answer ready. For a container release the runbook names the previous image digest, which names one immutable image where a tag alone could have moved since the release went out. Rollback is a required runbook, so `litespec/ext/delivery#exercise-every-runbook-before-release` rehearses it.

**Reviewed for:** Which rollback runbooks omit their artifact reference, command, compatibility, data condition, expand-and-contract sequence, or, for a container release, the previous image digest? Look at each rollback runbook.

### Report Liveness from Every Recurring Job

**ID:** `litespec/ext/delivery#report-liveness-from-every-recurring-job`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Every recurring job MUST report liveness that pages a person when the job goes silent.

**Rationale:** A recurring job with nothing to do is quiet, and a recurring job that stopped is quiet too. Without a liveness signal that pages a person on silence, the two stay indistinguishable until somebody notices the missing output. Liveness turns a quiet job from an observation into an alarm.

**Reviewed for:** Which recurring jobs lack liveness that pages a person on silence? Look at each job and its alert rules.

### Define Actionable Baseline Alerts

**ID:** `litespec/ext/delivery#define-actionable-baseline-alerts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A baseline alert MUST declare owner, threshold, evaluation window, severity, and runbook before a production release.

**Rationale:** Each declared field answers one failure mode:

- An alert with no owner waits for somebody to adopt it.
- An alert with no threshold or evaluation window fires on noise.
- An alert with no severity waits in the wrong queue.
- An alert with no runbook asks its receiver to invent the response.

Declaring all five before production makes each alert answerable the first time it fires.

**Reviewed for:** Which baseline alerts omit an owner, threshold, window, severity, or runbook? Look at each alert rule.

### Cover the Baseline Alert Set

**ID:** `litespec/ext/delivery#cover-the-baseline-alert-set`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Baseline coverage MUST include sustained readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments.

**Rationale:** Five failures end a release when nobody observes them: readiness that never recovers, HTTP errors the code did not expect, a slowing release flow, a lost database, and a deployment that failed partway. Naming them as the minimum coverage keeps a team from declaring alerts only for the failure it just survived.

**Reviewed for:** Which of the five baseline failures (readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments) lack an alert? Look at the alert rules.

### Route Each Severity to a Declared Destination

**ID:** `litespec/ext/delivery#route-each-severity-to-a-declared-destination`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An alert inventory MUST state, for each severity it uses, the destination that receives the alert and the response time expected of it.

**Rationale:** A declared severity with no destination is a label: every alert arrives in the same place, and the page that wakes somebody is read beside the one that can wait. Routing each severity to its destination with an expected response time separates the page from the ticket, which is the separation [Prometheus alerting practice](https://prometheus.io/docs/practices/alerting/) states. The limits page declares the routing beside the generated inventory.

**Example:** A two-severity inventory routes `page` to the on-call destination with a stated response time, and `ticket` to the queue reviewed each working day.

**Reviewed for:** Which severities lack a destination or an expected response time on the limits page? Look at the limits page.

### Exercise Every Runbook Before Release

**ID:** `litespec/ext/delivery#exercise-every-runbook-before-release`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Each required runbook MUST carry a dated rehearsal in the operations log before a production release, with restore rehearsed on representative data.

**Rationale:** A runbook is a claim until somebody runs it: steps go stale, commands change, permissions lapse. A dated rehearsal in the operations log proves each required runbook still works against the current system. The restore rehearsal is the one `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data` requires.

**Reviewed for:** Which required runbooks lack a dated rehearsal in the operations log? Look at the operations log entries.

### State Availability Objectives as OpenSLO Files

**ID:** `litespec/ext/delivery#state-availability-objectives-as-openslo-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A production release MUST state its availability and latency objectives as OpenSLO files compiled by Sloth.

**Rationale:** OpenSLO files state objectives as code, and Sloth validates them in CI and compiles them into Prometheus recording rules and burn-rate alerts. OpenTelemetry supplies the signals the objectives read, as `litespec/quality/operations#emit-correlated-diagnostics` states. Sloth and OpenTelemetry are free and maintained under Apache-2.0, and availability stays evidenced in production only.

**Reviewed for:** Which OpenSLO files fail Sloth validation with their generated alerts loaded? Look at the Sloth validation output.

### Attach Dashboard Links to Production Objectives

**ID:** `litespec/ext/delivery#attach-dashboard-links-to-production-objectives`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each production objective MUST link to its dashboard in the book.

**Rationale:** The book states intent while dashboards show production truth, and the link joins them: a reviewer clicks from the objective to the numbers behind it. Copying numbers into the page instead would freeze production truth at commit time.

**Reviewed for:** Which production objectives in the book lack a link to their dashboard? Look at the objective pages in the book.

### Match Every Shipped Package to Its Pin

**ID:** `litespec/ext/delivery#match-every-shipped-package-to-its-pin`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials that the manifest pins MUST ship at the pinned version.

**Rationale:** The bill of materials (SBOM) is the computed list of what ships, so it is the one place a pin can be checked against instead of trusted. A version that drifted from its pin carries advisories and behavior the release never reviewed. The bill is generated, so the fact stays written once, and a record per package would rewrite hundreds of facts on every update.

**Example:** Claimwick's bill of materials sits at `.litespec/proof/sbom/api.spdx.json`, the project file names that folder in `paths.sbom`, and a direct `pkg:nuget/Marten` at a version other than the manifest pin is reported.

**Checked by:** `litespec model check` reports `dependency-version-drift` (level S) for a direct package whose version differs from its manifest pin.

### Accept Every Shipped Package License Explicitly

**ID:** `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials MUST carry a license the project accepts in `dependencies.licenses`, or one its `admits` entry names.

**Rationale:** A copyleft or commercial license binds the product the day the package ships, and a license nobody accepted is found at an audit rather than at the change that brought it. Listing the accepted licenses once, and naming any other on the admitting decision, keeps the license choice a decision with a person's name on it.

**Example:** Claimwick accepts `MIT`, `Apache-2.0`, and `BSD-3-Clause` in `dependencies.licenses`, so a direct package under `AGPL-3.0-only` that no decision admits is reported.

**Checked by:** `litespec model check` reports `dependency-license-unknown` (level S) for a direct package whose license the project does not accept and no `admits` entry names.

### Close Incident Departures Within Seven Days

**ID:** `litespec/ext/delivery#close-incident-departures-within-seven-days`

**Strength:** required

**Applies to:** artifact: record:departure

**Statement:** A production incident fix that ships before its records are updated MUST carry an incident departure expiring within seven days.

**Rationale:** A production incident ships its fix at once. The incident departure records what shipped and why, and the records are updated before it expires. An expired departure is a blocking finding, so the gap stays visible until the records close it.

**Reviewed for:** Which incident fixes lack a departure, or carry an expired one? Look at the departure records beside each fix.

### Write a Brief Before Handing Off

**ID:** `litespec/ext/delivery#write-a-brief-before-handing-off`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent that starts another agent MUST give it a written brief stating goal, context, sources, and output.

**Rationale:** Two agents sharing one tree collide without a written split of goal, context, sources, and output. The brief states what the worker owns and what it builds on. When workers collide, the defect lies in the brief rather than in either worker.

**Example:** A brief carries four sections, each answering one question for the worker.

```text
Goal: the change the worker owns.
Context: the repository facts the change builds on.
Sources: the records and pages to read first.
Output: the files to change and the report to write.
```

**Reviewed for:** Which briefs lack goal, context, sources, or output? Look at the briefs the agent starting the handoff wrote.

### Restrict a Worker to Its Paths

**ID:** `litespec/ext/delivery#restrict-a-worker-to-its-paths`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A worker MUST stay inside the paths its brief names and commit nothing.

**Rationale:** The brief names the paths the worker may change. A worker that stays inside them cannot rewrite what another worker owns. One that roams the whole tree overwrites parallel work and commits what nobody asked for.

**Checked by:** `git diff --name-only` lists only the brief's named paths. The worker commits nothing.

### Ask Only Unanswered Questions

**ID:** `litespec/ext/delivery#ask-only-unanswered-questions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A worker MUST ask no question its brief answers.

**Rationale:** The brief answers the questions the agent handing off the work already settled. A worker that re-asks them spends a round trip on a reading failure. Questions stay for what the brief leaves genuinely unanswered.

**Reviewed for:** Which worker questions repeat answers the brief gives? Look at the worker questions beside the brief.

### Verify Before Accepting Worker Output

**ID:** `litespec/ext/delivery#verify-before-accepting-worker-output`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent handing off work MUST re-run the verification its brief names before accepting worker output.

**Rationale:** The agent handing off the work owns the result, so it re-runs the verification the brief named and watches it pass. A done claim without that run is a claim, not evidence. Accepting on trust moves the worker's defect into the handing-off agent's change.

**Reviewed for:** Which accepted outputs lack a watched verification run? Look at the verification runs the agent handing off the work reports.

### Hold Back Example Rows from the Implementing Worker

**ID:** `litespec/ext/delivery#hold-back-example-rows-from-the-implementing-worker`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent handing off work MUST check a refusal or calculation implementation against example rows held back from the worker that wrote it.

**Rationale:** Example rows given to the implementing worker become the values the implementation is tuned to pass, so the check proves nothing. Rows held back stay an independent verdict: the agent handing off the work runs them after the worker finishes, and the implementation passes or fails on unseen cases.

**Reviewed for:** Which refusal or calculation implementations were checked only against rows the worker saw? Look at the held-back rows and the check runs.

### Build Release Images in Stages

**ID:** `litespec/ext/delivery#build-release-images-in-stages`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST use multi-stage builds and pinned base-image digests.

**Rationale:** Build tooling has no place in a runtime image: each compiler and package cache widens the attack surface of every deployment. Multi-stage builds leave that tooling in the build stage. A pinned base-image digest fixes the exact reviewed base, so a rebuild months later starts from the same bytes.

**Reviewed for:** Which release images miss multi-stage builds or pinned base-image digests? Look at the Dockerfiles.

### Run Images as Non-Root

**ID:** `litespec/ext/delivery#run-images-as-non-root`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST run its application with a non-root runtime user.

**Rationale:** A compromised application process inherits the rights of its runtime user. A non-root user confines the attacker to what that user may touch. Root would hand over the whole container.

**Reviewed for:** Which release images run their application as root? Look at the Dockerfiles and image configuration.

### Copy Only Runtime Files

**ID:** `litespec/ext/delivery#copy-only-runtime-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A runtime image MUST contain only files required to run the deployed application.

**Rationale:** Every file in the runtime image ships to every replica and waits for an attacker to read it. Copying only the files the application needs keeps the image small to transfer and to inspect. Each extra file is cost with no runtime purpose.

**Reviewed for:** Which runtime images carry files outside the declared runtime manifest? Look at the image file listing beside the declared runtime manifest.

### Exclude Build Material from Runtime Images

**ID:** `litespec/ext/delivery#exclude-build-material-from-runtime-images`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A runtime image MUST NOT contain source, tests, package caches, or local configuration.

**Rationale:** Source, tests, package caches, and local configuration serve the build and never the running application. Carrying them into production leaks internals to anyone who pulls the image. Secrets stay out under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Reviewed for:** Which runtime images contain source, tests, package caches, or local configuration? Look at the runtime image contents.

### Supply Runtime Configuration Externally

**ID:** `litespec/ext/delivery#supply-runtime-configuration-externally`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment platform MUST supply connection strings, OIDC settings, exporter endpoints, and secrets outside the image.

**Rationale:** Connection strings, OIDC settings, exporter endpoints, and secrets differ by environment. Supplying them outside the image lets one image serve every environment. Baking them in would rebuild and re-test the image for each environment it enters.

**Reviewed for:** Which connection strings, OIDC settings, exporter endpoints, or secrets live inside the image? Look at the deployment manifests.

### Exclude Environment Configuration from Images

**ID:** `litespec/ext/delivery#exclude-environment-configuration-from-images`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST NOT contain environment-specific configuration.

**Rationale:** One immutable image deploys unchanged into staging and production. Environment values baked into the image break that promise: the tested bytes and the deployed bytes differ. External values keep the image identical everywhere it runs.

**Reviewed for:** Which release images contain environment-specific configuration? Look at the release image contents.

### Wait for Replica Readiness

**ID:** `litespec/ext/delivery#wait-for-replica-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment controller MUST start new replicas and wait for readiness before shifting traffic.

**Rationale:** New replicas need time to load before they serve: a replica that receives traffic mid-startup fails its first requests. The controller starts them, waits for readiness, and only then shifts traffic. Readiness proves the new version serves before it becomes the version.

**Reviewed for:** Which deployments shift traffic before new replicas reach readiness? Look at the deployment controller behavior.

### Test Released Traffic

**ID:** `litespec/ext/delivery#test-released-traffic`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release deployment MUST run the release's recorded end-to-end suite after traffic reaches the new version.

**Rationale:** An image that builds can still fail where it runs: routing, configuration, and data differ from the build machine. Running the release's recorded end-to-end suite after traffic shifts tests the deployed path itself. The release record names the suite, so the check runs the same tests every release.

**Reviewed for:** Which releases skip the recorded end-to-end suite after traffic shifts? Look at the release evidence.

### Limit Container Privileges

**ID:** `litespec/ext/delivery#limit-container-privileges`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST use a read-only filesystem when the application permits it and drop unnecessary capabilities.

**Rationale:** A read-only filesystem and dropped capabilities shrink what a compromised process can do next: no writes outside declared paths, no privileged calls it never needed. The limits apply where the application permits them. Each kept capability is an action an attacker inherits.

**Reviewed for:** Which deployments keep a writable filesystem or unnecessary capabilities where the application permits the limits? Look at the deployment manifests.

### Declare Writable Paths and Limits

**ID:** `litespec/ext/delivery#declare-writable-paths-and-limits`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST declare writable paths and record CPU and memory bounds as setting records.

**Rationale:** Declared writable paths name the mutable storage the application needs, and CPU and memory bounds as setting records cap what one deployment may consume. The bounds protect the node's capacity from one hungry replica. The declarations also tell the next reader what the application was sized for.

**Reviewed for:** Which deployments miss declared writable paths or CPU and memory setting records? Look at the deployment manifests beside the setting records.

### Run the Application as PID 1

**ID:** `litespec/ext/delivery#run-the-application-as-pid-1`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A container MUST run the application as its entry process.

**Rationale:** The platform stops a container by signaling its entry process. An application running as PID 1 receives that signal and shuts down on purpose. A wrapper that swallows the signal leaves the platform waiting until it kills the container mid-work.

**Reviewed for:** Which containers run something other than the application as their entry process? Look at the image entrypoints.

### Set Sufficient Shutdown Time

**ID:** `litespec/ext/delivery#set-sufficient-shutdown-time`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST set a shutdown grace period longer than measured request or Worker stop time.

**Rationale:** Stopping a replica interrupts its in-flight requests and Worker operations. A grace period longer than the measured stop time lets that work finish or record a safe interruption. A shorter period kills work the system then has to reconcile.

**Reviewed for:** Which deployments set a shutdown grace period no longer than measured request or Worker stop time? Look at the deployment manifests beside the measured stop times.

### Write Runtime Logs to Streams

**ID:** `litespec/ext/delivery#write-runtime-logs-to-streams`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A containerized application MUST write logs to standard output or standard error.

**Rationale:** Container filesystems vanish with their replicas, so a log file inside the container dies with the evidence it holds. Standard output and standard error hand each line to the platform's log collection. The platform then keeps logs past the life of any one replica.

**Reviewed for:** Which containerized applications write logs somewhere other than standard output or standard error? Look at the application logging configuration.

### Keep Mutable Data External

**ID:** `litespec/ext/delivery#keep-mutable-data-external`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A containerized application MUST keep mutable data outside its container filesystem.

**Rationale:** The platform recreates replicas at will: deploys, scaling, and node failures each replace containers with fresh ones. Mutable data kept inside the container filesystem dies with its replica. External storage keeps the data alive across every recreation.

**Reviewed for:** Which mutable data lives inside a container filesystem? Look at the application storage design.

### Apply OCI Release Labels

**ID:** `litespec/ext/delivery#apply-oci-release-labels`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST include OCI source, revision, version, and created-time labels.

**Rationale:** Source, revision, version, and created-time labels in the OCI standard form connect each image to the commit and release that produced it. An incident responder reads the running image's labels and opens the exact source. Images without labels force that join through guesswork.

**Reviewed for:** Which release images miss OCI source, revision, version, or created-time labels? Look at the built images.

### Record Image Digests

**ID:** `litespec/ext/delivery#record-image-digests`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST store the deployed image digest rather than only an image tag.

**Rationale:** The release record stores the deployed digest, not just the tag. A tag can move to a newer image after release, while the digest names the exact bytes that went out. The digest makes the deployed artifact reproducible and the rollback target exact.

**Reviewed for:** Which release records store only an image tag with no deployed digest? Look at the release records.

### Exclude Build Timestamps from Contracts

**ID:** `litespec/ext/delivery#exclude-build-timestamps-from-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A published OpenAPI document or generated client MUST NOT contain a build timestamp.

**Rationale:** A generated-at field or a build-time comment changes the OpenAPI document and generated clients on every build. The diff then reports change where no contract changed, which buries real breaks. Contracts carry version identity instead of build time.

**Reviewed for:** Which published OpenAPI documents or generated clients contain a build timestamp? Look at the generated contracts.

### Separate Frontend Configuration Classes

**ID:** `litespec/ext/delivery#separate-frontend-configuration-classes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST distinguish public build-time configuration from server-only runtime secrets.

**Rationale:** A frontend build produces a browser bundle that anyone may download. Public build-time configuration belongs there, while server-only runtime secrets travel through mounts or runtime injection instead. The build distinguishes the two classes so each value lands in its own delivery.

**Reviewed for:** Which frontend builds mix public configuration with server-only secrets? Look at the frontend image build.

### Use Protected Frontend Secret Delivery

**ID:** `litespec/ext/delivery#use-protected-frontend-secret-delivery`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST use secret mounts or runtime injection for secrets.

**Rationale:** Secret mounts and runtime injection hand secrets to the build without writing them into a layer. Nothing secret then persists in the image history. A pulled image reveals its layers to anyone, so image history stays free of secrets.

**Reviewed for:** Which frontend secrets travel outside mounts or runtime injection? Look at the frontend image build.

### Exclude Insecure Frontend Secret Carriers

**ID:** `litespec/ext/delivery#exclude-insecure-frontend-secret-carriers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST NOT use `ARG`, copied environment files, or image layers for secrets.

**Rationale:** `ARG` values, copied environment files, and image layers all persist into the image history. A secret delivered through any of them stays readable to anyone who pulls the image. Secrets stay out of layers under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Reviewed for:** Which frontend image builds use `ARG`, copied environment files, or image layers for secrets? Look at the frontend image builds.

## Conventions

### Treat Check Warnings as Failures

**ID:** `litespec/ext/delivery#treat-check-warnings-as-failures`

**Strength:** default

**Applies to:** artifact: any

**Default:** Treat build and lint warnings as failures unless the build configuration records a narrow suppression with its reason.

**Replacement:** A consumer records a warnings convention naming which warnings fail its build.

**Rationale:** Warnings that nobody reviews accumulate until a real defect hides among them. Failing on warnings forces each one to be fixed or narrowly suppressed with its reason, so a changed compiler, linter, or dependency boundary surfaces immediately.

**Reviewed for:** Which builds pass with unreviewed warnings outside recorded suppressions? Look at the build logs beside the warnings convention.

### Apply the Documented Defaults

**ID:** `litespec/ext/delivery#apply-the-documented-defaults`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep one workflow per job the jobs table lists and use the job names the jobs table states.

**Replacement:** A consumer records a workflow-layout convention naming its workflow files and job names.

**Rationale:** One workflow per job keeps each gate readable, runnable, and skippable on its own. When containerized integration tests or browser tests join CI, they join as jobs the table names rather than as hidden steps inside another job, so the selection rules and the branch protection keep working unchanged.

**Reviewed for:** Which consumer workflow files match no jobs-table job and name, with no workflow-layout convention naming another arrangement? Look at the workflow files beside the jobs table.

### Use Stable Service Names

**ID:** `litespec/ext/delivery#use-stable-service-names`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep service, resource, meter, and trace-source names identical across environments.

**Replacement:** A consumer records a service-naming convention naming how its services, resources, meters, and trace sources are named.

**Rationale:** Dashboards and alerts reference services by name. When a name changes with each deployment, every reference breaks and must be rebuilt before the new deployment can be watched. Identical names across environments keep one dashboard and one alert set working everywhere.

**Reviewed for:** Which service, resource, meter, or trace-source names differ across environments, with no service-naming convention naming another arrangement? Look at the environment configurations.

### Keep Deployment Descriptors Together

**ID:** `litespec/ext/delivery#keep-deployment-descriptors-together`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep compose files, proxy configuration, the environment example, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts under one `deploy/` tree.

**Replacement:** A consumer records a deployment-layout convention naming where its deployment descriptors live.

**Rationale:** Deployment spreads across compose files, proxy configuration, environment examples, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts. Scattered across the workspace, one of them is always the file nobody finds during an incident. One `deploy/` tree keeps every descriptor where the operator looks first.

**Reviewed for:** Which deployment descriptors sit outside one `deploy/` tree, with no deployment-layout convention naming another arrangement? Look at the repository layout.

### Keep Scripts at the Root

**ID:** `litespec/ext/delivery#keep-scripts-at-the-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep bootstrap, release, and CI helper scripts under root `scripts/`.

**Replacement:** A project that organizes scripts per application records its own script layout in its project documentation.

**Rationale:** Bootstrap, release, and CI helper scripts serve the whole workspace, so the root `scripts/` folder keeps them where every caller looks. An application-specific script may stay inside its application when no other workspace calls it.

**Reviewed for:** Which bootstrap, release, or CI helper scripts sit outside root `scripts/`, with no project documentation naming its own script layout? Look at the repository layout.

### Place Dockerfiles Beside Deployables

**ID:** `litespec/ext/delivery#place-dockerfiles-beside-deployables`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep one Dockerfile beside each deployable application.

**Replacement:** A consumer records a dockerfile-layout convention naming where its Dockerfiles live.

**Rationale:** One Dockerfile beside each deployable application keeps the image definition with the project that defines its runtime output. A reader finds the build next to the code it ships. A shared folder of Dockerfiles separates each build from its owner.

**Reviewed for:** Which deployable applications keep no Dockerfile beside them, with no dockerfile-layout convention naming where Dockerfiles live? Look at each application folder.

### Build from the Workspace Root

**ID:** `litespec/ext/delivery#build-from-the-workspace-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Build images from the workspace root when shared project or package files are required.

**Replacement:** A consumer records a build-context convention naming the context its image builds use.

**Rationale:** Shared project and package files sit above any one application, so the build starts from the workspace root to reach them. The Dockerfile excludes what the build does not need, including unrelated secrets. A narrower context would copy shared inputs into each build or miss them.

**Reviewed for:** Which image builds that need shared inputs do not start from the workspace root, with no build-context convention naming another context? Look at the image build commands.

## Dependencies

Packages: None.

Related extensions: `stage` for the period before the first customer data.

Guides: None.
