# Delivery

**Stack:** any

**Concern:** operations

Delivery states the release, deployment, and operations rules for running the product for other people under a formal regime. It covers readiness gates, artifact promotion, deployment and rollback, backup and restore, alerts and runbooks, the supply-chain gate, container image build, rollout and runtime limits, and agent handoff. A team operating the product in production selects this extension, while a project needing only baseline checks, contracts, and review flow leaves it unselected.

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

Selecting `delivery` switches on two record types: `setting` and `runbook`. Resource bounds are setting records, and the rollback procedure is a runbook record.

### Release Gates and Evidence

Each release carries three kinds of proof: green G checks for the record graph, verified scenarios for behavior through the public boundary, and green S checks where a qualified reader exists. `litespec model check --release` turns a missing reader into a blocking finding, so the gate runs the same way each release.

Before the first production release, the team keeps a readiness record with dated rehearsals, green gates, and a rehearsal owner. The release record then lists one result per evidence kind, from build through rollback, plus operating limits, alerts, runbooks, known conditions, and skipped checks. Each persistence change publishes a schema artifact, the Marten plan or relational migration with its SQL, listing each destructive operation with its reason.

### Supply Chain and Promotion

The supply-chain gate runs in CI on every pull request. The build writes its inventory from the frozen lockfile beside the artifact under the release version, OSV-Scanner matches dependencies against OSV.dev advisories, and Gitleaks scans for leaked secrets, with each unhandled high-severity advisory covered by a waiver.

Each change publishes a book preview linked from the pull request, pairing the intent diff with the rendered page. CI builds one immutable artifact and promotes that same artifact through staging and production, so staging tests the bytes production ships. The default branch takes only reviewed pull requests with the jobs-table entries for the project size and a clean merge state. A direct push or bypassed check leaves a record stating actor, reason, and admitted change, either as a recovery decision or a dated operations-log entry. Consumer CI uses the job names and responsibilities from the jobs table, keeping release and branch-protection references pointed at jobs that exist.

### Data Recovery and Repeatable Deployments

Each release documents backup owner, schedule, retention, encryption, location, restore command, and recovery objectives. A restore rehearsal on representative data runs before the first production data, with the outcome in the operations log.

Each deployment states versioned artifact, declared configuration, schema step, readiness checks, and passing release job, letting any deployment run again from stated inputs. The rollback runbook states previous artifact reference, command, configuration and schema compatibility, data recovery condition, and the expand-and-contract sequence for destructive schema work, with the previous image digest for container releases. Each recurring job reports liveness that pages a person when the job goes silent, separating a quiet job from a stopped one. Each release exposes liveness, readiness, and trace-correlated diagnostics, with secrets outside source control.

### Alerts, Runbooks, and Objectives

Each baseline alert declares owner, threshold, evaluation window, severity, and runbook before production. The minimum set covers sustained readiness failures, unexpected HTTP errors, release-flow latency, PostgreSQL outages, and failed deployments. Each severity routes to a declared destination with an expected response time, separating the page that wakes someone from the ticket that waits for working hours.

Each runbook carries a dated rehearsal in the operations log before production, with restore rehearsed on representative data. Production objectives live as OpenSLO files compiled by Sloth and fed by OpenTelemetry signals, and each production objective links to its dashboard in the book.

### Handoff Between Agents

A production incident fix shipping before its records update travels with a gap waiver expiring within seven days, stating the shipped change and the records closing it in `closedBy`. The waiver stays visible until the records close it, and an expired waiver blocks.

An agent starting another agent writes a brief stating goal, context, sources, and output. The worker stays inside the brief's paths and asks only what the brief leaves unanswered. The handing-off agent re-runs the brief's verification and checks refusal or calculation work against example rows held back from the implementing worker, keeping an independent verdict on unseen cases.

### Container Images and Runtime

Release images use multi-stage builds with pinned base-image digests, run the application as a non-root user, and contain only runtime files. Source, tests, package caches, and local configuration stay out of the runtime image, as do environment-specific values, while connection strings, OIDC settings, exporter endpoints, and secrets arrive outside the image.

The deployment controller starts new replicas, waits for readiness, then shifts traffic, and the release's recorded end-to-end suite runs after the shift. Deployments use a read-only filesystem where the application permits it, drop unneeded capabilities, declare writable paths, and record CPU and memory bounds as setting records. The application runs as the entry process with a shutdown grace period past measured stop time, writes logs to standard output or error, and keeps mutable data outside the container filesystem.

Each release image carries OCI source, revision, version, and created-time labels, and each release record stores the image digest rather than only a tag. Published OpenAPI documents and generated clients carry version identity instead of build timestamps, keeping diffs quiet until contracts change. Frontend builds keep public build-time configuration apart from server-only secrets, delivering secrets through mounts or runtime injection instead of `ARG`, copied environment files, or image layers.

### Workspace Layout Habits

Build and lint warnings fail unless the build configuration records a narrow suppression with its reason, keeping each warning fixed or explained. Each jobs-table job gets its own workflow under its table name, and service, resource, meter, and trace-source names stay identical across environments. Compose files, proxy configuration, the environment example, database setup, observability, DNS records, alert definitions, service definitions, and bootstrap scripts live under one `deploy/` tree. Bootstrap, release, and CI helper scripts live under root `scripts/`, each Dockerfile sits beside its application, and image builds start from the workspace root to reach shared inputs.

## Standards

### Meet Release Readiness Gates

**ID:** `litespec/ext/delivery#meet-release-readiness-gates`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST show green G checks, verified scenarios, and green S checks where the reader is qualified.

**Rationale:** A release carries three risks: a graph break nobody recorded, a behavior nobody verified, and code the records left undescribed. Green G checks close the first, verified scenarios close the second, and green S checks close the third where a qualified reader exists. `litespec model check --release` turns a missing reader into a blocking finding, so the gate runs the same way each release.

**Example:** Claimwick's reimbursement release shows green G checks, every scenario verified, and green S checks on the .NET reader.

**Checked by:** `litespec model check` blocks on `use-case-unverified` (level S).

### Tie Alert Thresholds to Requirement Measures

**ID:** `litespec/ext/delivery#tie-alert-thresholds-to-requirement-measures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An alert rule MUST state every number its requirement's `measure` names in its own expression or threshold.

**Rationale:** The requirement's `measure` states the agreed target in prose, and the alert enforces that target at runtime. The two drift apart silently when the requirement changes and nobody re-reads the alert. Comparing the decimal numbers both state keeps the check coarse but honest: prose resists exact parsing, so the check asks only that the alert carry the numbers the measure promises.

**Example:** Claimwick's `requirement/approval-reminders.month-end-submission` measures "at least 99 percent within two seconds", and its alert's expression compares the 0.99 quantile against 2.

**Checked by:** `litespec model check` reports `alert-threshold-drift` (level G) for an alert rule whose own text carries none of the decimal numbers its requirement's `measure` names.

### Record Each Drill Run as a Check Result

**ID:** `litespec/ext/delivery#record-each-drill-run-as-a-check-result`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A drill or measuring harness that checks a quality requirement MUST write each run as a check result naming the requirement's `check`, kept under the project's `paths.checkResults`.

**Rationale:** A drill rehearses a procedure against a running deployment, and a harness measures it from outside. Both outcomes live in terminals and pasted notes by default, which no tool reads. A small result file under `paths.checkResults` gives the quality requirement the same dated evidence a measured check gives it: the `check` name, the date it ran, and the value seen.

**Example:** Claimwick's month-end load harness writes `.litespec/proof/checks/2026-11-20-month-end-load.json` holding `"check": "month-end-load"`, `"passed": true`, `"value": "99.4 percent acknowledged within two seconds"`, and `"ranOn": "2026-11-20"`, and `requirement/approval-reminders.month-end-submission` names `month-end-load` in its `check`.

**Reviewed for:** Does every drill or measuring harness run write a check result file naming the requirement's `check` under `paths.checkResults`? Check the harness run records beside the files under `paths.checkResults`.

### Name the Requirement and Runbook on Every Alert

**ID:** `litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every alert rule MUST carry a `requirement` label naming the quality requirement it measures and a `runbook` label naming the runbook that answers it, both as record identifiers.

**Rationale:** An alert fires when a promised measure breaks, and the runbook tells the receiver what to do next. Record identifiers in the alert's own labels keep that link in the file the alert lives in, in the syntax its tool already reads. The model check then follows both ends, confirming the requirement still exists and the runbook still exists.

**Example:** Claimwick's alert rule `submission-acknowledgement-slow` carries `requirement: requirement/approval-reminders.month-end-submission` and `runbook: runbook/slow-submissions` in its labels.

**Checked by:** `litespec model check` reports `alert-link-missing` (level S) for an alert rule in the files `paths.alerts` names whose `requirement` label is missing or names no quality requirement, or whose `runbook` label is missing or names no runbook.

### Cover Every Quality Requirement with an Alert or a Check

**ID:** `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** Every quality requirement that is not retired MUST be named by at least one alert rule or check result, or covered by a waiver.

**Rationale:** A quality requirement nobody watches gets proven only when a customer reports the breach. An alert watches it in production by naming it, and a check result from a measured check or a drill shows it measured before release. A requirement that resists watching says so as a gap waiver with a reason, an exit, closing records, and an expiry date, staying visible instead of passing silently.

**Example:** Claimwick's `requirement/approval-reminders.month-end-submission` is named by the alert `submission-acknowledgement-slow` and by the k6 result its `check` names.

**Checked by:** `litespec model check` reports `quality-alert-missing` (level G) for a quality requirement that no alert rule names in its `requirement` label, whose `check` matches no measured-check or check result the model read, and that no waiver covering `quality-alert-missing` lists in its `appliesTo`.

### Link Each Operations File to Its Decision

**ID:** `litespec/ext/delivery#link-each-operations-file-to-its-decision`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every deployment, proxy, host, and CI workflow file the project lists in `paths.operations` MUST carry a `follows` mark naming the technical decision, waiver, or provision it carries out.

**Rationale:** Operations files hold choices as consequential as code: the proxy security headers, the backup schedule, the gates a pull request passes. Their reasons usually sit in comments no tool reads, so a retired decision leaves its configuration behind unnoticed. A `follows` mark in the file's own comment syntax links each file to its decision, waiver, or provision, and the model counts the file toward `decision-unimplemented`.

**Example:** Claimwick's `deploy/Caddyfile` opens with `# <follows>decision/0004-serve-receipts-behind-one-proxy</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a file in `paths.operations` with no `follows` mark and `mark-unknown` (level S) for a mark naming no decision, waiver, or provision, and counts the file toward `decision-unimplemented`.

### Link Every Runbook to Its Caller

**ID:** `litespec/ext/delivery#link-every-runbook-to-its-caller`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Every runbook record MUST be named by at least one alert, job, reaction, or failure policy.

**Rationale:** A runbook nobody calls waits undiscovered while its failure gets handled from memory. An alert, a job, a reaction, or a failure policy calling the runbook keeps each procedure reachable from the event needing it. The check lists the runbooks with no caller, so dead procedures surface instead of accumulating.

**Example:** Claimwick's job `send-event-change-notices` names `runbook/clear-stuck-notices` in its `runbook` field, so the procedure is reachable from the work that needs it.

**Checked by:** `litespec model check` reports `runbook-unused` (level G) for a runbook record that no alert label, job, reaction or failure policy names.

### Match Every Shipped Package to Its Pin

**ID:** `litespec/ext/delivery#match-every-shipped-package-to-its-pin`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials that the manifest pins MUST ship within its pin: a stable release of the pinned major, or the exact version where the manifest pins one.

**Rationale:** The bill of materials is the computed list of what ships, so it is the one place a pin gets checked instead of trusted. A version outside its pin carries a major's breaking changes or a prerelease behavior the release left unreviewed. The bill is generated, so the fact stays written once while packages churn.

**Example:** Claimwick's bill of materials sits at `.litespec/proof/sbom/api.spdx.json`, the project file names that folder in `paths.sbom`, and a direct `pkg:nuget/Marten` at 10.0.0 is reported because the manifest pins `9.x`.

**Checked by:** `litespec model check` reports `dependency-version-drift` (level S) for a direct package whose version is outside its manifest pin.

### Accept Every Shipped Package License Explicitly

**ID:** `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every direct package in the project's generated bill of materials MUST carry a license the project accepts in `dependencies.licenses`, or one its `admits` entry names.

**Rationale:** A copyleft or commercial license binds the product the day the package ships. A license nobody accepted at the bringing change surfaces at an audit instead. Listing the accepted licenses once, and stating any other license on its admitting decision, keeps each license choice attached to the person choosing it.

**Example:** Claimwick accepts `MIT`, `Apache-2.0`, and `BSD-3-Clause` in `dependencies.licenses`, so a direct package under `AGPL-3.0-only` that no decision admits is reported.

**Checked by:** `litespec model check` reports `dependency-license-unknown` (level S) for a direct package whose license the project does not accept and no `admits` entry names.

### Restrict a Worker to Its Paths

**ID:** `litespec/ext/delivery#restrict-a-worker-to-its-paths`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A worker MUST stay inside the paths its brief names and commit nothing.

**Rationale:** Two workers sharing one tree collide without a written split of work. The brief states the worker's paths, and a worker staying inside them leaves other workers' work untouched. The check reads the changed file list, and the no-commit rule keeps the handing-off agent in charge of what lands.

**Checked by:** `git diff --name-only` lists only the brief's named paths. The worker commits nothing.

## Conventions

None.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
