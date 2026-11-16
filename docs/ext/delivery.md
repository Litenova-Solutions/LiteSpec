# Delivery

**Stack:** any
**Concern:** operations

Delivery states the release, deployment, and operations rules for running the product for other people under a formal regime. It covers release readiness gates, artifact promotion, deployment and rollback, backup and restore, alerts and runbooks, the supply-chain gate, container image build, rollout and runtime limits, and agent handoff. A project that needs only the baseline checks, contracts, and review flow selects nothing here. A team that operates the product in production selects this extension, so each release deploys, recovers, and responds to alerts by a written procedure, and each container release ships the image bytes it tested.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

You select `delivery` when any of the following holds:

- You operate the product for other people in production.
- You promote one artifact through staging and production.
- You need a formal release gate, a deployment and rollback path, an alerting and runbook regime, or a supply-chain gate beyond the baseline checks.
- Production or staging deploys the API, the Worker, or a frontend as a container image.
- One agent starts another on the same tree.

## Baseline Relationship

The baseline keeps the boundary-selected checks, contract regeneration, and the release record. This extension adds the production regime around them, and replaces no baseline provision. Its schema, rollback, and secret rules cite the baseline provisions that own those obligations instead of restating them.

Selecting `delivery` switches on two record types: `setting` and `runbook`. Resource bounds are setting records, as `litespec/ext/delivery#declare-writable-paths-and-limits` states, and the rollback procedure is a runbook record, as `litespec/ext/delivery#keep-rollback-ready-to-run` states.

## Standards

### Meet Release Readiness Gates

**ID:** `litespec/ext/delivery#meet-release-readiness-gates`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST show green G checks, verified scenarios, and green S checks where the reader is qualified.

**Rationale:** The three parts prove the three risks are closed: G checks prove the record graph holds, verified scenarios prove each behavior through the public boundary, and S checks prove the code matches the records where a qualified reader exists. A release missing any part ships with that risk unchecked. `litespec model check --release` makes the release-readiness findings block, so the gate runs as a command.

**Example:** Claimwick's reimbursement release shows green G checks, every scenario verified, and green S checks on the .NET reader.

**Checked by:** `litespec model check --release` blocks on `use-case-unverified`.

### Keep Release Data Recoverable

**ID:** `litespec/ext/delivery#keep-release-data-recoverable`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST provide repeatable schema creation or upgrade, backup instructions, and a tested restore.

**Rationale:** A product that cannot recreate its schema, back up its data, and restore from backup holds customer data it cannot recover. Repeatable setup plus a tested restore makes recovery a rehearsed procedure.

**Example:** Claimwick's release ships its schema creation script, its backup instructions, and the log of its latest restore rehearsal.

**Reviewed for:** Does every release provide repeatable schema setup, backup instructions, and a tested restore? Check the recovery evidence beside the release.

### Expose Release Health and Diagnostics

**ID:** `litespec/ext/delivery#expose-release-health-and-diagnostics`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST expose liveness, readiness, and trace-correlated diagnostics, and keep secrets outside source control.

**Rationale:** Operators cannot keep a product they cannot see: liveness and readiness signals drive restarts and traffic, and trace-correlated diagnostics connect each failure to its cause. Secrets stay outside source control because anything committed there is exposed to everyone who can read it.

**Example:** Claimwick's release exposes liveness, readiness, and trace-correlated diagnostics, with its database password supplied outside source control.

**Reviewed for:** Does every release expose health signals and trace diagnostics, and keep secrets outside source control? Check the operating evidence beside the release.

### Keep Deployment, Limits, and Runbooks Ready

**ID:** `litespec/ext/delivery#keep-deployment-limits-and-runbooks-ready`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST ship with tested deployment and rollback paths, current operating limits, and the runbooks those limits name.

**Rationale:** Tested deployment and rollback paths, current limits, and the runbooks those limits name make every release repeatable and every failure recoverable. Without tested paths, each deployment is improvised; without current limits and their runbooks, each incident is handled without a procedure.

**Example:** Claimwick's release ships its deployment script, its rollback runbook, its current memory and CPU limits, and the runbooks those limits name.

**Reviewed for:** Does every release ship with deployment, rollback, limits, and runbooks? Check the delivery evidence beside the release.

### Record Release Evidence

**ID:** `litespec/ext/delivery#record-release-evidence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list build, test, contract, security, flow, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results.

**Rationale:** Build, test, contract, security, flow, schema, backup, deployment, readiness, diagnostics, smoke-test, and rollback results together show the artifact works and can be operated. A missing kind means nobody produced that evidence.

**Example:** Claimwick's release record lists a result for each kind, from build through rollback.

**Reviewed for:** Does every release record list a result for each evidence kind? Check the evidence list on each release record.

### Record Operating Readiness

**ID:** `litespec/ext/delivery#record-operating-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST list operating limits, alerts, runbooks, known operating conditions, and skipped checks.

**Rationale:** Limits, alerts, runbooks, known conditions, and skipped checks tell the maintainer what to watch, what to run, and what remains unverified after release. Without them, the release hands over an artifact with no operating instructions.

**Example:** Claimwick's release record lists its memory limits, its slow-submission alert, the runbook that answers it, and the checks it skipped.

**Reviewed for:** Does every release record list limits, alerts, runbooks, conditions, and skipped checks? Check the readiness list on each release record.

### Keep a First-Release Readiness Record

**ID:** `litespec/ext/delivery#keep-a-first-release-readiness-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A consumer before its first production release MUST keep a readiness record with dated rehearsals, green gates, and a rehearsal owner.

**Rationale:** A release record binds evidence to one artifact, so a product with no artifact yet has nothing to record in one. The readiness record instead states the evidence that the product is ready to take data: dated rehearsals, green gates, and a rehearsal owner responsible for the backup and restore rehearsal.

**Example:** Claimwick's readiness record names its rehearsal owner and dates its backup and restore rehearsal before the first customer data arrives.

**Reviewed for:** Does every pre-release consumer keep dated rehearsals, green gates, and a rehearsal owner? Check the readiness record beside the project.

### Review Schema Artifacts

**ID:** `litespec/ext/delivery#review-schema-artifacts`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** A persistence change MUST publish a schema artifact that lists every destructive operation with its reason.

**Rationale:** A persistence change reaches production data, so the reviewer needs to see exactly what the database will do before it runs. The schema artifact shows that: the Marten schema plan and document transformation, or a generated relational migration with its SQL. Listing every destructive operation with its reason forces the author to name data loss where every reader sees it.

**Example:** Claimwick's expense-table change publishes its Marten schema plan with the dropped column listed as destructive, with its reason.

**Reviewed for:** Does every persistence change publish a schema artifact that lists each destructive operation with its reason? Check the schema artifact beside each change.

### Enforce the Supply-Chain Gate

**ID:** `litespec/ext/delivery#enforce-the-supply-chain-gate`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST enforce the supply-chain gate on every pull request.

**Rationale:** When a new advisory names a component and a version range, the team needs one fast answer: does any running release contain it. Without a component inventory beside the artifact, finding that answer means rebuilding the dependency graph from a commit that somebody must first identify. [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states the practice, and the frozen lockfile the build resolved is what the inventory is generated from. OSV-Scanner matches dependencies against OSV.dev advisories, and Gitleaks scans for leaked secrets, so advisories and secrets each have one evidence tool.

**Example:** Claimwick's build job generates its inventory from the same frozen lockfile it built from, and stores that inventory beside the artifact under the release version.

**Reviewed for:** Does every supply-chain report show scan, pin, inventory, and license evidence, with a waiver covering each unhandled high-severity advisory? Check the supply-chain report beside the artifact.

### Publish a Book Preview for Every Change

**ID:** `litespec/ext/delivery#publish-a-book-preview-for-every-change`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST publish a preview of the book for every change.

**Rationale:** The intent diff names what changed, while the preview shows it rendered, with the changed records readable beside their neighbors. The reader clicks from the comment into the preview for full context.

**Example:** Claimwick's pull request comment links to a preview where the changed requirement reads beside its feature.

**Reviewed for:** Does every pull request link to a book preview built from the change? Check the pull request description.

### Promote Verified Artifacts

**ID:** `litespec/ext/delivery#promote-verified-artifacts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST build one immutable artifact and promote that same artifact through staging and production.

**Rationale:** Staging tests prove something about the artifact they ran against. When production deploys a fresh build from the same branch instead, it ships code the tests never saw, because the branch may have moved between the two builds. Promoting the one tested artifact through staging and production keeps the tested code and the shipped code identical.

**Example:** Claimwick's release job builds one image, staging tests that image, and production deploys that same image.

**Reviewed for:** Does every staging or production deployment reference the artifact the release job passed? Check the deployment references.

### Protect the Default Branch

**ID:** `litespec/ext/delivery#protect-the-default-branch`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The default branch MUST require the jobs table entries for the project's size, a reviewed pull request, and a clean merge state.

**Rationale:** Every environment deploys from the default branch. A commit that lands there unreviewed or unchecked reaches production with no further review. Requiring the jobs table entries for the project's size, a reviewed pull request, and a clean merge state keeps every commit on the branch reviewed and verified. Only a documented repository recovery action may push directly or bypass a check.

**Reviewed for:** Does the default branch require the jobs-table entries, a reviewed pull request, and a clean merge state? Check the branch protection settings.

### Record Every Branch Protection Bypass

**ID:** `litespec/ext/delivery#record-every-branch-protection-bypass`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A direct push or bypassed check on the default branch MUST produce a record naming the actor, the reason, and the change it admitted.

**Rationale:** Gates can't run during some recoveries, so the bypass permission lets a recovery proceed. An unrecorded recovery is indistinguishable from skipping the gates by habit, so the next reviewer cannot tell which commits on the branch were reviewed. The record names the actor, the reason, and the admitted change, which is the attribution [NIST SP 800-218](https://csrc.nist.gov/publications/detail/sp/800-218/final) states for the same event.

**Example:** Where a recovery decision exists, it serves as the record. A bypass smaller than a decision takes a dated entry in the operations log at `.litespec/implementation/operations/log/`.

**Reviewed for:** Does every bypass entry name its actor, reason, and admitted change? Check each bypass entry in the operations log or the recovery decision.

### Keep a Canonical Job Graph

**ID:** `litespec/ext/delivery#keep-a-canonical-job-graph`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Consumer CI MUST use the job names and responsibilities the jobs table on the [Continuous Integration page](../quality/ci.md) states.

**Rationale:** The release job reruns the table's jobs by name, and the branch protection settings require them by name. When a consumer renames a job or moves its checks elsewhere, those references point at a job that no longer exists. The gate then stops checking without failing. Fixed names and responsibilities keep every reference pointed at the checks the table states.

**Example:** Claimwick's CI uses the jobs table names, so its release job and branch protection resolve every job.

**Reviewed for:** Does every consumer workflow use only the job names and responsibilities the jobs table states? Check the workflow files.

### Tie Alert Thresholds to Requirement Measures

**ID:** `litespec/ext/delivery#tie-alert-thresholds-to-requirement-measures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An alert rule MUST state every number its requirement's `measure` names in its own expression or threshold.

**Rationale:** The requirement's measure states the agreed target, and the alert is the one place that target is enforced at runtime. When the requirement changes, nobody re-reads the alert, so a measure that says two seconds and an alert that fires at five drift apart silently. Comparing the numbers is coarse because a measure is prose, so the check advises rather than blocks.

**Example:** Claimwick's `requirement/approval-reminders.month-end-submission` measures "at least 99 percent within two seconds", and its alert's expression compares the 0.99 quantile against 2.

**Checked by:** `litespec model check` reports `alert-threshold-drift` (level A) for an alert rule whose own text carries none of the decimal numbers its requirement's `measure` names.

### Record Each Drill Run as a Check Result

**ID:** `litespec/ext/delivery#record-each-drill-run-as-a-check-result`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A drill or measuring harness that checks a quality requirement MUST write each run as a check result naming the requirement's `check`, kept under the project's `paths.checkResults`.

**Rationale:** A drill rehearses a procedure against a running deployment, and a harness measures it from outside. Neither is a test, and neither runs in the test job. Without a result file, the outcome lives only in a terminal or a pasted note that no tool reads. Writing each run as a small result file in Proof gives the quality requirement the same evidence a measured check gives it: the same `check` name, the date it ran, and the value it saw.

**Example:** Claimwick's month-end load harness writes `.litespec/proof/checks/2026-11-20-month-end-load.json` holding `"check": "month-end-load"`, `"passed": true`, `"value": "99.4 percent acknowledged within two seconds"`, and `"ranOn": "2026-11-20"`, and `requirement/approval-reminders.month-end-submission` names `month-end-load` in its `check`.

**Reviewed for:** Does every drill or measuring harness run write a check result file naming the requirement's `check` under `paths.checkResults`? Check the harness run records beside the files under `paths.checkResults`.

### Name the Requirement and Runbook on Every Alert

**ID:** `litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every alert rule MUST carry a `requirement` label naming the quality requirement it measures and a `runbook` label naming the runbook that answers it, both as record identifiers.

**Rationale:** An alert watches a quality requirement at runtime: it fires when the measure the requirement promises is breached, and the runbook says what a person does next. When that link is a file path or missing entirely, no tool can follow it. An alert can then outlive its requirement, a requirement can lose its last alert, and a renamed runbook breaks silently. Record identifiers in the alert's own labels keep the link in the file the alert lives in, in the syntax its tool already reads, and let the model check both ends. A baseline alert also carries its window and severity, as `litespec/ext/delivery#define-actionable-baseline-alerts` states.

**Example:** Claimwick's alert rule `submission-acknowledgement-slow` carries `requirement: requirement/approval-reminders.month-end-submission` and `runbook: runbook/slow-submissions` in its labels.

**Checked by:** `litespec model check` reports `alert-link-missing` (level S) for an alert rule in the files `paths.alerts` names whose `requirement` label is missing or names no quality requirement, or whose `runbook` label is missing or names no runbook.

### Cover Every Quality Requirement with an Alert or a Check

**ID:** `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Every quality requirement that is not retired MUST be named by at least one alert rule or check result, or covered by a waiver.

**Rationale:** A quality requirement nobody watches is a promise checked only when a customer notices it broken. An alert watches it in production by naming the requirement, and a check result, from a measured check or a drill, shows it was measured before release by naming it through its `check`. A requirement that can't be watched yet says so as a gap waiver, with a reason, an exit, closing records, and an expiry date, instead of passing silently.

**Example:** Claimwick's `requirement/approval-reminders.month-end-submission` is named by the alert `submission-acknowledgement-slow` and by the k6 result its `check` names.

**Checked by:** `litespec model check` reports `quality-alert-missing` (level G) for a quality requirement that no alert rule names in its `requirement` label, whose `check` matches no measured-check or check result the model read, and that no waiver covering `quality-alert-missing` lists in its `subject`.

### Link Each Operations File to Its Decision

**ID:** `litespec/ext/delivery#link-each-operations-file-to-its-decision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every deployment, proxy, host, and CI workflow file the project lists in `paths.operations` MUST carry a `follows` mark naming the technical decision, waiver, or provision it carries out.

**Rationale:** Operations files hold choices as consequential as code: the proxy's security headers, the backup schedule, the gates a pull request passes. Their reasons usually live in comments no tool reads, so when a decision retires, its configuration stays behind unnoticed. A `follows` mark in the file's own comment syntax is the same link a technical class carries, so `decision-unimplemented` and `mark-unknown` cover these files too.

**Example:** Claimwick's `deploy/Caddyfile` opens with `# <follows>decision/0004-serve-receipts-behind-one-proxy</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a file in `paths.operations` with no `follows` mark and `mark-unknown` (level S) for a mark naming no decision, waiver, or provision, and counts the file toward `decision-unimplemented`.

### Link Every Runbook to Its Caller

**ID:** `litespec/ext/delivery#link-every-runbook-to-its-caller`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Every runbook record MUST be named by at least one alert, job, reaction, or failure policy.

**Rationale:** A runbook nobody names answers no failure. An alert, a job, a reaction, or a failure policy is the caller that sends a person to the procedure. Without that link, the procedure waits undiscovered while the failure it answers gets handled from memory. Naming each runbook from at least one caller keeps every procedure reachable from the event that needs it.

**Example:** Claimwick's job `send-event-change-notices` names `runbook/clear-stuck-notices` in its `runbook` field, so the procedure is reachable from the work that needs it.

**Checked by:** `litespec model check` reports `runbook-unused` (level G) for a runbook record that no alert label, job, reaction or failure policy names.

### Define Backup and Restore Behavior

**ID:** `litespec/ext/delivery#define-backup-and-restore-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST document backup owner, schedule, retention, encryption, location, restore command, and recovery objectives.

**Rationale:** A backup is only as good as its restore. Without a named owner, a schedule, a retention bound, encryption, a location, a restore command, and recovery objectives, nobody knows what is kept, who restores it, or how much loss is accepted. Writing all seven down before production data arrives leaves nothing to decide during the incident.

**Example:** Claimwick's release names its backup owner, its nightly schedule, its 30-day retention, its encryption, its storage location, its restore command, and its recovery objectives.

**Reviewed for:** Does the release documentation state a backup owner, schedule, retention, encryption, location, restore command, and recovery objective? Check the release documentation.

### Rehearse a Restore Before the First Production Data

**ID:** `litespec/ext/delivery#rehearse-a-restore-before-the-first-production-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST rehearse a restore on representative data before its first deployment holds production data.

**Rationale:** Backups fail in ways only a restore reveals: a missing key, a wrong permission, a dump the new version can't read. A rehearsal on representative data finds those failures while they cost nothing, before production data arrives.

**Reviewed for:** Does the operations log show a restore rehearsal on representative data before the first production data? Check the operations log entries.

### Repeat Every Deployment from Named Inputs

**ID:** `litespec/ext/delivery#repeat-every-deployment-from-named-inputs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST use versioned artifacts, declared configuration, a schema step, readiness checks, and a passing release job.

**Rationale:** A repeatable deployment names everything it ships: a versioned artifact, declared configuration, a schema step, readiness checks, and a passing release job. Deploying from an uncommitted tree or a mutable branch reference makes the deployed content unknowable afterwards, because the same reference points at different code a week later. Named inputs let any deployment run again, and let any incident trace back to its exact content.

**Reviewed for:** Does every deployment use a versioned artifact, declared configuration, a schema step, readiness checks, and a passing release job? Check the deployment definitions.

### Keep Rollback Ready to Run

**ID:** `litespec/ext/delivery#keep-rollback-ready-to-run`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** The rollback runbook MUST state the previous artifact reference, command, configuration and schema compatibility, data recovery condition, and the expand-and-contract sequence for destructive schema work, and name the previous image digest for a container release.

**Rationale:** Rollback is written during the incident if it is not written before it, and an incident is the worst time to work out which artifact was live, which command restores it, and whether the schema still fits. Stating the previous artifact reference, the command, the configuration and schema compatibility, the data recovery condition, and the expand-and-contract sequence for destructive schema work keeps each answer ready before it is needed. For a container release the runbook names the previous image digest, which names one immutable image where a tag alone could have moved since the release went out. Rollback is a required runbook, so `litespec/ext/delivery#rehearse-every-runbook-before-release` rehearses it.

**Reviewed for:** Does every rollback runbook state its artifact reference, command, compatibility, data condition, expand-and-contract sequence, and, for a container release, the previous image digest? Check each rollback runbook.

### Report Liveness from Every Recurring Job

**ID:** `litespec/ext/delivery#report-liveness-from-every-recurring-job`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Every recurring job MUST report liveness that pages a person when the job goes silent.

**Rationale:** A recurring job with nothing to do is quiet, and a recurring job that stopped is quiet too. Without a liveness signal that pages a person on silence, the two stay indistinguishable until somebody notices the missing output.

**Reviewed for:** Does every recurring job report liveness that pages a person when the job goes silent? Check each job and its alert rules.

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

Declaring all five before the production release makes each alert answerable the first time it fires.

**Reviewed for:** Does every baseline alert declare an owner, threshold, window, severity, and runbook? Check each alert rule.

### Cover the Baseline Alert Set

**ID:** `litespec/ext/delivery#cover-the-baseline-alert-set`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Baseline coverage MUST include sustained readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments.

**Rationale:** Five failures end a release when nobody observes them: readiness that never recovers, unexpected HTTP errors, a slowing release flow, a lost database, and a deployment that failed partway. Naming them as the minimum coverage keeps a team from declaring alerts only for the failure it just survived.

**Reviewed for:** Does the alert set cover all five baseline failures: readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments? Check the alert rules.

### Route Each Severity to a Declared Destination

**ID:** `litespec/ext/delivery#route-each-severity-to-a-declared-destination`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An alert inventory MUST state, for each severity it uses, the destination that receives the alert and the response time expected of it.

**Rationale:** A severity with no destination is only a label: every alert lands in the same place, and the page that should wake somebody sits beside the ticket that can wait. Routing each severity to its destination, with the response time expected of it, separates the page from the ticket, which is the separation [Prometheus alerting practice](https://prometheus.io/docs/practices/alerting/) states. The limits page declares the routing beside the generated inventory.

**Example:** Claimwick's two-severity inventory routes `page` to the on-call destination with a stated response time, and `ticket` to the queue reviewed each working day.

**Reviewed for:** Does every severity state a destination and an expected response time on the limits page? Check the limits page.

### Rehearse Every Runbook Before Release

**ID:** `litespec/ext/delivery#rehearse-every-runbook-before-release`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Each required runbook MUST carry a dated rehearsal in the operations log before a production release, with restore rehearsed on representative data.

**Rationale:** A runbook nobody has run is a claim: steps go stale, commands change, and permissions lapse. A dated rehearsal in the operations log proves each required runbook still works against the current system. The restore rehearsal is the one `litespec/ext/delivery#rehearse-a-restore-before-the-first-production-data` requires.

**Reviewed for:** Does every required runbook carry a dated rehearsal in the operations log? Check the operations log entries.

### State Availability Objectives as OpenSLO Files

**ID:** `litespec/ext/delivery#state-availability-objectives-as-openslo-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A production release MUST state its availability and latency objectives as OpenSLO files compiled by Sloth.

**Rationale:** OpenSLO files state objectives as code, and Sloth validates them in CI and compiles them into Prometheus recording rules and burn-rate alerts. OpenTelemetry supplies the signals the objectives read, as `litespec/quality/operations#emit-correlated-diagnostics` states. Sloth and OpenTelemetry are free and maintained under Apache-2.0. Availability is evidenced in production only.

**Reviewed for:** Does every OpenSLO file pass Sloth validation with its generated alerts loaded? Check the Sloth validation output.

### Attach Dashboard Links to Production Objectives

**ID:** `litespec/ext/delivery#attach-dashboard-links-to-production-objectives`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each production objective MUST link to its dashboard in the book.

**Rationale:** The book states intent while dashboards show production truth, and the link joins them: a reviewer clicks from the objective to the numbers behind it. Copying numbers into the page instead would freeze production truth at commit time.

**Reviewed for:** Does every production objective in the book link to its dashboard? Check the objective pages in the book.

### Match Every Shipped Package to Its Pin

**ID:** `litespec/ext/delivery#match-every-shipped-package-to-its-pin`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials that the manifest pins MUST ship within its pin: a stable release of the pinned major, or the exact version where the manifest pins one.

**Rationale:** The bill of materials (SBOM) is the computed list of what ships, so it is the one place a pin can be checked instead of trusted. A version outside its pin carries a major's breaking changes, or a prerelease's behavior, that the release never reviewed. The bill is generated, so the fact stays written once; a record per package would rewrite hundreds of facts on every update.

**Example:** Claimwick's bill of materials sits at `.litespec/proof/sbom/api.spdx.json`, the project file names that folder in `paths.sbom`, and a direct `pkg:nuget/Marten` at 10.0.0 is reported because the manifest pins `9.x`.

**Checked by:** `litespec model check` reports `dependency-version-drift` (level S) for a direct package whose version is outside its manifest pin.

### Accept Every Shipped Package License Explicitly

**ID:** `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials MUST carry a license the project accepts in `dependencies.licenses`, or one its `admits` entry names.

**Rationale:** A copyleft or commercial license binds the product the day the package ships. When nobody accepted the license at the change that brought it, the license is discovered at an audit instead. Listing the accepted licenses once, and naming any other license on the admitting decision, keeps each license choice a decision that names the person who made it.

**Example:** Claimwick accepts `MIT`, `Apache-2.0`, and `BSD-3-Clause` in `dependencies.licenses`, so a direct package under `AGPL-3.0-only` that no decision admits is reported.

**Checked by:** `litespec model check` reports `dependency-license-unknown` (level S) for a direct package whose license the project does not accept and no `admits` entry names.

### Close Incident Waivers Within Seven Days

**ID:** `litespec/ext/delivery#close-incident-waivers-within-seven-days`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** A production incident fix that ships before its records are updated MUST carry a gap waiver expiring within seven days.

**Rationale:** A production incident ships its fix at once, before the records catch up. The gap waiver records what shipped and why, and names the records to update in `closedBy`. Those records are updated before the waiver expires, and an expired waiver is a blocking finding, so the gap stays visible until the records close it.

**Reviewed for:** Does every incident fix that shipped before its records carry a waiver, with none expired? Check the waiver records beside each fix.

### Write a Brief Before Handing Off

**ID:** `litespec/ext/delivery#write-a-brief-before-handing-off`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent that starts another agent MUST give it a written brief stating goal, context, sources, and output.

**Rationale:** Two agents sharing one tree collide without a written split of work. The brief states what the worker owns and what it builds on: its goal, context, sources, and output. When workers collide, the defect lies in the brief rather than in either worker.

**Example:** A brief carries four sections, each answering one question for the worker.

```text
Goal: the change the worker owns.
Context: the repository facts the change builds on.
Sources: the records and pages to read first.
Output: the files to change and the report to write.
```

**Reviewed for:** Does every brief state goal, context, sources, and output? Check the briefs the agent starting the handoff wrote.

### Restrict a Worker to Its Paths

**ID:** `litespec/ext/delivery#restrict-a-worker-to-its-paths`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A worker MUST stay inside the paths its brief names and commit nothing.

**Rationale:** The brief names the paths the worker may change. A worker that stays inside them can't rewrite what another worker owns. One that roams the whole tree overwrites parallel work and commits what nobody asked for.

**Checked by:** `git diff --name-only` lists only the brief's named paths. The worker commits nothing.

### Ask Only Unanswered Questions

**ID:** `litespec/ext/delivery#ask-only-unanswered-questions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A worker MUST ask no question its brief answers.

**Rationale:** The brief settles the questions the handing-off agent already answered. A worker that re-asks them spends a round trip on a reading failure. Questions are for what the brief leaves genuinely unanswered.

**Reviewed for:** Do the worker's questions stay within what the brief leaves unanswered? Check the worker questions beside the brief.

### Verify Before Accepting Worker Output

**ID:** `litespec/ext/delivery#verify-before-accepting-worker-output`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent handing off work MUST re-run the verification its brief names before accepting worker output.

**Rationale:** The agent handing off the work owns the result. It re-runs the verification the brief named and watches it pass, because a done claim without that run is unverified. Accepting on trust moves the worker's defect into the handing-off agent's change.

**Reviewed for:** Does every accepted output show a watched verification run? Check the verification runs the agent handing off the work reports.

### Check Implementations Against Held-Back Example Rows

**ID:** `litespec/ext/delivery#check-implementations-against-held-back-example-rows`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent handing off work MUST check a refusal or calculation implementation against example rows held back from the worker that wrote it.

**Rationale:** Example rows given to the implementing worker become the values the implementation is tuned to pass, so the check proves nothing. Rows held back from the worker stay an independent verdict: the handing-off agent runs them after the worker finishes, and the implementation passes or fails on unseen cases.

**Reviewed for:** Was every refusal or calculation implementation checked against rows its worker never saw? Check the held-back rows and the check runs.

### Build Release Images in Stages

**ID:** `litespec/ext/delivery#build-release-images-in-stages`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST use multi-stage builds and pinned base-image digests.

**Rationale:** Build tooling has no place in a runtime image: each compiler and package cache widens the attack surface of every deployment. Multi-stage builds leave that tooling in the build stage. A pinned base-image digest fixes the exact reviewed base, so a rebuild months later starts from the same bytes.

**Reviewed for:** Does every release image use multi-stage builds and pinned base-image digests? Check the Dockerfiles.

### Run Images as Non-Root

**ID:** `litespec/ext/delivery#run-images-as-non-root`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST run its application with a non-root runtime user.

**Rationale:** A compromised application process inherits the rights of its runtime user. A non-root user confines the attacker to what that user can access. Root would expose the whole container.

**Reviewed for:** Does every release image run its application with a non-root user? Check the Dockerfiles and image configuration.

### Copy Only Runtime Files

**ID:** `litespec/ext/delivery#copy-only-runtime-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A runtime image MUST contain only files required to run the deployed application.

**Rationale:** Every file in the runtime image ships to every replica, where an attacker can read it. Copying only the files the application needs keeps the image small, quick to transfer, and quick to inspect. Each extra file adds cost with no runtime purpose.

**Reviewed for:** Does every runtime image contain only the files the declared runtime manifest names? Check the image file listing beside the declared runtime manifest.

### Exclude Build Material from Runtime Images

**ID:** `litespec/ext/delivery#exclude-build-material-from-runtime-images`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A runtime image MUST NOT contain source, tests, package caches, or local configuration.

**Rationale:** Source, tests, package caches, and local configuration serve the build and never the running application. Carrying them into production leaks internals to anyone who pulls the image. Secrets stay out under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Reviewed for:** Is every runtime image free of source, tests, package caches, and local configuration? Check the runtime image contents.

### Supply Runtime Configuration Externally

**ID:** `litespec/ext/delivery#supply-runtime-configuration-externally`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment platform MUST supply connection strings, OIDC settings, exporter endpoints, and secrets outside the image.

**Rationale:** Connection strings, OIDC settings, exporter endpoints, and secrets differ by environment. Supplying them outside the image lets one image serve every environment. Baking them in would force a rebuild and retest for each environment the image enters.

**Reviewed for:** Do the connection strings, OIDC settings, exporter endpoints, and secrets all live outside the image? Check the deployment manifests.

### Exclude Environment Configuration from Images

**ID:** `litespec/ext/delivery#exclude-environment-configuration-from-images`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST NOT contain environment-specific configuration.

**Rationale:** One immutable image deploys unchanged into staging and production. Environment values baked into the image break that promise, because the tested bytes and the deployed bytes differ. External values keep the image identical everywhere it runs.

**Reviewed for:** Is every release image free of environment-specific configuration? Check the release image contents.

### Wait for Replica Readiness

**ID:** `litespec/ext/delivery#wait-for-replica-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment controller MUST start new replicas and wait for readiness before shifting traffic.

**Rationale:** New replicas need time to load before they serve, and a replica that receives traffic mid-startup fails its first requests. The controller starts the new replicas, waits for readiness, and only then shifts traffic, so the new version proves it can serve before it takes traffic.

**Reviewed for:** Does every deployment wait for new replicas to reach readiness before shifting traffic? Check the deployment controller behavior.

### Run the End-to-End Suite After Traffic Shifts

**ID:** `litespec/ext/delivery#run-the-end-to-end-suite-after-traffic-shifts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release deployment MUST run the release's recorded end-to-end suite after traffic reaches the new version.

**Rationale:** An image that builds can still fail where it runs, because routing, configuration, and data differ from the build machine. Running the release's recorded end-to-end suite after traffic shifts tests the deployed path itself. The release record names the suite, so the same tests run every release.

**Reviewed for:** Does every release run its recorded end-to-end suite after traffic shifts? Check the release evidence.

### Limit Container Privileges

**ID:** `litespec/ext/delivery#limit-container-privileges`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST use a read-only filesystem when the application permits it and drop unnecessary capabilities.

**Rationale:** A read-only filesystem and dropped capabilities shrink what a compromised process can do next: no writes outside declared paths, no privileged calls the application never needed. The limits apply where the application permits them, and each kept capability is an action an attacker inherits.

**Reviewed for:** Does every deployment use a read-only filesystem and drop unnecessary capabilities where the application permits the limits? Check the deployment manifests.

### Declare Writable Paths and Limits

**ID:** `litespec/ext/delivery#declare-writable-paths-and-limits`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST declare writable paths and record CPU and memory bounds as setting records.

**Rationale:** Declared writable paths name the mutable storage the application needs. CPU and memory bounds as setting records cap what one deployment can consume, which protects the node's capacity from one replica consuming more than its share, and tells the next reader what the application was sized for.

**Reviewed for:** Does every deployment declare writable paths and record CPU and memory bounds as setting records? Check the deployment manifests beside the setting records.

### Run the Application as PID 1

**ID:** `litespec/ext/delivery#run-the-application-as-pid-1`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A container MUST run the application as its entry process.

**Rationale:** The platform stops a container by signaling its entry process. An application running as PID 1 receives that signal and shuts down on purpose. A wrapper that swallows the signal leaves the platform waiting until it kills the container mid-work.

**Reviewed for:** Does every container run the application as its entry process? Check the image entrypoints.

### Allow Time for Graceful Shutdown

**ID:** `litespec/ext/delivery#allow-time-for-graceful-shutdown`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment MUST set a shutdown grace period longer than measured request or Worker stop time.

**Rationale:** Stopping a replica interrupts its in-flight requests and Worker operations. A grace period longer than the measured stop time lets that work finish, or record a safe interruption. A shorter period kills work the system then has to reconcile.

**Reviewed for:** Does every deployment set a shutdown grace period longer than measured request or Worker stop time? Check the deployment manifests beside the measured stop times.

### Write Runtime Logs to Streams

**ID:** `litespec/ext/delivery#write-runtime-logs-to-streams`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A containerized application MUST write logs to standard output or standard error.

**Rationale:** A container filesystem vanishes with its replica, so a log file inside the container is lost with it. Standard output and standard error hand each line to the platform's log collection, which keeps logs past the life of any one replica.

**Reviewed for:** Does every containerized application write logs to standard output or standard error? Check the application logging configuration.

### Keep Mutable Data Outside the Container

**ID:** `litespec/ext/delivery#keep-mutable-data-outside-the-container`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A containerized application MUST keep mutable data outside its container filesystem.

**Rationale:** The platform recreates replicas at will: each deploy, scaling event, and node failure replaces containers with fresh ones. Mutable data kept inside the container filesystem dies with its replica. External storage keeps the data alive across every recreation.

**Reviewed for:** Does any mutable data live inside a container filesystem? Check the application storage design.

### Apply OCI Release Labels

**ID:** `litespec/ext/delivery#apply-oci-release-labels`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release image MUST include OCI source, revision, version, and created-time labels.

**Rationale:** Labels in the OCI standard form connect each image to the commit and release that produced it: source, revision, version, and created time. An incident responder reads the running image's labels and opens the exact source. Without labels, joining a running image to its source takes guesswork.

**Reviewed for:** Does every release image carry OCI source, revision, version, and created-time labels? Check the built images.

### Record Image Digests

**ID:** `litespec/ext/delivery#record-image-digests`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release record MUST store the deployed image digest rather than only an image tag.

**Rationale:** A tag can move to a newer image after release, so a tag alone no longer says what went out. The digest names the exact bytes that shipped, which makes the deployed artifact reproducible and the rollback target exact.

**Reviewed for:** Does every release record store the deployed image digest rather than only a tag? Check the release records.

### Exclude Build Timestamps from Contracts

**ID:** `litespec/ext/delivery#exclude-build-timestamps-from-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A published OpenAPI document or generated client MUST NOT contain a build timestamp.

**Rationale:** A generated-at field or a build-time comment changes the OpenAPI document and generated clients on every build. The diff then reports change where no contract changed, and that noise hides real breaking changes. Contracts carry version identity instead of build time.

**Reviewed for:** Is every published OpenAPI document and generated client free of build timestamps? Check the generated contracts.

### Separate Public Configuration from Runtime Secrets

**ID:** `litespec/ext/delivery#separate-public-configuration-from-runtime-secrets`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST distinguish public build-time configuration from server-only runtime secrets.

**Rationale:** A frontend build produces a browser bundle that anyone can download. Public build-time configuration belongs in that bundle, while server-only runtime secrets travel through mounts or runtime injection instead. The build distinguishes the two classes, so each value lands in its own delivery.

**Reviewed for:** Does every frontend build keep public configuration separate from server-only secrets? Check the frontend image build.

### Pass Frontend Secrets by Mount or Runtime Injection

**ID:** `litespec/ext/delivery#pass-frontend-secrets-by-mount-or-runtime-injection`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST use secret mounts or runtime injection for secrets.

**Rationale:** Secret mounts and runtime injection hand secrets to the build without writing them into a layer. Nothing secret then persists in the image history. A pulled image reveals its layers to anyone, so image history stays free of secrets.

**Reviewed for:** Does every frontend secret travel through a mount or runtime injection? Check the frontend image build.

### Keep Frontend Secrets Out of Arguments, Files, and Layers

**ID:** `litespec/ext/delivery#keep-frontend-secrets-out-of-arguments-files-and-layers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend image build MUST NOT use `ARG`, copied environment files, or image layers for secrets.

**Rationale:** `ARG` values, copied environment files, and image layers all persist into the image history, so a secret delivered through any of them stays readable to anyone who pulls the image. Secrets stay out of layers under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Reviewed for:** Is every frontend image build free of `ARG`, copied environment files, and image layers carrying secrets? Check the frontend image builds.

## Conventions

### Treat Check Warnings as Failures

**ID:** `litespec/ext/delivery#treat-check-warnings-as-failures`

**Strength:** default

**Applies to:** artifact: any

**Default:** Treat build and lint warnings as failures unless the build configuration records a narrow suppression with its reason.

**Replacement:** A consumer records a warnings convention naming which warnings fail its build.

**Rationale:** Warnings nobody reviews accumulate until a real defect hides among them. Failing on warnings forces each one to be fixed, or narrowly suppressed with its reason, so a changed compiler, linter, or dependency boundary surfaces immediately.

**Reviewed for:** Does any build pass with unreviewed warnings outside recorded suppressions? Check the build logs beside the warnings convention.

### Give Each Jobs-Table Job Its Own Workflow

**ID:** `litespec/ext/delivery#give-each-jobs-table-job-its-own-workflow`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep one workflow per job the jobs table lists and use the job names the jobs table states.

**Replacement:** A consumer records a workflow-layout convention naming its workflow files and job names.

**Rationale:** One workflow per job keeps each gate readable, runnable, and skippable on its own. When containerized integration tests or browser tests join CI, they join as jobs the table names, not as hidden steps inside another job, so the selection rules and the branch protection keep working unchanged. Hidden steps would run without a name the release job or branch protection can require.

**Reviewed for:** Does every consumer workflow file match a jobs-table job and name, or does a workflow-layout convention name another arrangement? Check the workflow files beside the jobs table.

### Use Stable Service Names

**ID:** `litespec/ext/delivery#use-stable-service-names`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep service, resource, meter, and trace-source names identical across environments.

**Replacement:** A consumer records a service-naming convention naming how its services, resources, meters, and trace sources are named.

**Rationale:** Dashboards and alerts reference services by name. When a name changes with each deployment, every reference breaks, and each one must be rebuilt before the new deployment can be watched. Identical names across environments keep one dashboard and one alert set working everywhere.

**Reviewed for:** Do service, resource, meter, and trace-source names stay identical across environments, or does a service-naming convention name another arrangement? Check the environment configurations.

### Keep Deployment Descriptors Together

**ID:** `litespec/ext/delivery#keep-deployment-descriptors-together`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep compose files, proxy configuration, the environment example, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts under one `deploy/` tree.

**Replacement:** A consumer records a deployment-layout convention naming where its deployment descriptors live.

**Rationale:** A deployment spreads across compose files, proxy configuration, environment examples, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts. Scattered across the workspace, one of them is always the file nobody finds during the incident. One `deploy/` tree keeps every descriptor where the operator looks first.

**Reviewed for:** Does every deployment descriptor sit under one `deploy/` tree, or does a deployment-layout convention name another arrangement? Check the repository layout.

### Keep Scripts at the Root

**ID:** `litespec/ext/delivery#keep-scripts-at-the-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep bootstrap, release, and CI helper scripts under root `scripts/`.

**Replacement:** A project that organizes scripts per application records its own script layout in its project documentation.

**Rationale:** Bootstrap, release, and CI helper scripts serve the whole workspace, so the root `scripts/` folder keeps them where every caller looks. A script that only one application calls can stay inside that application.

**Reviewed for:** Does every bootstrap, release, or CI helper script sit under root `scripts/`, or does project documentation name another script layout? Check the repository layout.

### Place Dockerfiles Beside Their Applications

**ID:** `litespec/ext/delivery#place-dockerfiles-beside-their-applications`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep one Dockerfile beside each deployable application.

**Replacement:** A consumer records a dockerfile-layout convention naming where its Dockerfiles live.

**Rationale:** The image definition belongs with the project that defines its runtime output. One Dockerfile beside each deployable application puts the build next to the code it ships, where the reader looks first. A shared folder of Dockerfiles separates each build from its owner.

**Reviewed for:** Does every deployable application keep a Dockerfile beside it, or does a dockerfile-layout convention name another place? Check each application folder.

### Build from the Workspace Root

**ID:** `litespec/ext/delivery#build-from-the-workspace-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Build images from the workspace root when shared project or package files are required.

**Replacement:** A consumer records a build-context convention naming the context its image builds use.

**Rationale:** Shared project and package files sit above any one application, so the build starts from the workspace root to reach them. The Dockerfile excludes what the build does not need, including unrelated secrets. A narrower context would copy shared inputs into each build or miss them.

**Reviewed for:** Does every image build that needs shared inputs start from the workspace root, or does a build-context convention name another context? Check the image build commands.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
