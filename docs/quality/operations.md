# Operations

## Intent

Operating a product means starting it, watching it, and answering failures with procedures the records name, and this page states how a LiteSpec product does each: local startup, diagnostics, and alerts that each name their runbook. It applies to everyone who operates a release and to any behavior change that touches schema, external dependencies, background work, or failure recovery. A product that follows it stays observable with one maintainer on call; deployment, rollback, backup, restore and the alerting regime live in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Concepts

### Operating Records

Everything this page asks an operator to know lives in one of four records, each with a fixed home and one writer. A person writes the operations log and the secret inventory; the model builder writes the alert inventory and the sub-processor list from the project records. Only the operations log keeps the past: the other three describe the present. Each record names its owner from the project's owner list.

### Operations Log

The operations log is append-only: one file per month under `.litespec/behavior/operations/log/`, named `YYYY-MM.md`, with entries appended and never edited. It records branch-protection bypasses, runbook rehearsals, restores, production deployments, and incidents, so the dated rehearsal each runbook needs has one home.

### Alert Inventory

The alert inventory lists the baseline alerts and the runbook each one names. A runbook is the procedure that answers an alert. The model builder renders the inventory from the alert rules the project lists in `paths.alerts`, each with the quality requirement it measures, the runbook that answers it, and its severity, beside the severity routing on the limits page at `.litespec/behavior/operations/limits.yaml` where the project records one. The files hold alert rules in the YAML shape Prometheus rule files and Grafana alert provisioning share: `groups[].rules[]`, each rule named by `alert` or `title` and carrying `labels`. Nobody edits the inventory by hand.

### Secret Inventory

The secret inventory is hand-written at `.litespec/behavior/operations/secrets.json`. Each entry names the secret, its owner, where it lives, and its next rotation date, and never the value itself. The owner reviews each rotation date before it passes. An entry has this shape:

```json
{
  "name": "database-password",
  "owner": "operations",
  "location": "production vault",
  "rotationDate": "2027-02-01"
}
```

### Sub-Processor List

The sub-processor list names every provider that processes personal data for the product. The model builder renders it from the project's provider actors, the actor records that name its providers, on `.litespec/behavior/operations/sub-processors.md`. Nobody edits it by hand.

## Standards

### Start the Stack with One Local Command

**ID:** `litespec/quality/operations#start-the-stack-with-one-local-command`

**Statement:** A workspace MUST start WebApi, PostgreSQL, configured frontends, and its Worker where one exists, with one local command named in the consumer `AGENTS.md`.

**Rationale:** A stack that needs several terminals and a wiki page starts differently for every developer, and the differences become bug reports nobody can reproduce. One named command brings WebApi, PostgreSQL, the frontends, and the background Worker up together, so every developer and every agent starts from the same running system. The baseline command is Aspire's `AppHost`, with `ServiceDefaults` supplying the shared discovery, health, resilience, and telemetry defaults its resources rely on. A workspace that orchestrates the same set another way satisfies this rule with its own command: the obligation is that one command brings the stack up, not that one tool does.

**Reviewed for:** One command named in the consumer `AGENTS.md` starts WebApi, PostgreSQL, the frontends, and the Worker.

### Emit Correlated Diagnostics

**ID:** `litespec/quality/operations#emit-correlated-diagnostics`

**Statement:** A host MUST emit structured logs, traces, and metrics, with correlation identifiers on logs and traces and service labels on metrics.

**Rationale:** Each running service is a host, and one request often crosses several. Logs and traces carry timestamp, level, service, environment, trace identifier, and span identifier, and metrics carry service and environment labels. The correlation identifiers let one request be followed across every host it touched, which is the only trail a cross-host failure leaves. Stable event names keep dashboards working across releases instead of breaking each time a message is reworded.

**Reviewed for:** Logs and traces carry correlation identifiers, and metrics carry service labels.

### Point Every Runtime Record at Its Use Case

**ID:** `litespec/quality/operations#point-every-runtime-record-at-its-use-case`

**Statement:** A host MUST carry the use-case identity of every mediated message, resolved from the generated registry, as `litespec.use_case` on its span, its log scope, and its error report.

**Rationale:** A trace identifier joins the runtime records of one request to each other, but not to the specification: a person holding a slow span, a log line, or an error report still has to guess which use case it was. The identity is already a generated value, so a pipeline stage reads it from the registry once per message and stamps it on every surface under one name, and reactions, jobs, and outbox consumers carry it as well as requests. A metric carries it only as a label on an instrument whose series stay bounded: a duration histogram labeled by every use case multiplies its series by hundreds, so request instruments label by module instead.

**Example:** A Claimwick request to submit an expense opens a span and a log scope carrying `litespec.use_case = use-case/spending.submit-expense`, and an unhandled failure inside it reaches the error tracker with the same tag.

**Reviewed for:** Which pipeline stage reads the registry and stamps `litespec.use_case` on the span, the log scope, and the error report of each mediated message? Look at the mediator pipeline and one error report per host.

### Name the Requirement and Runbook on Every Alert

**ID:** `litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert`

**Statement:** Every alert rule MUST carry a `requirement` label naming the quality requirement it measures and a `runbook` label naming the runbook that answers it, both as record identifiers.

**Rationale:** An alert is the runtime half of a quality requirement: it fires when the measure the requirement promises is breached, and the runbook says what a person does next. Written as a file path or left out, the link is a string no tool follows, so an alert can outlive its requirement, a requirement can lose its last alert, and a renamed runbook breaks silently. Record identifiers in the alert's own labels keep the link in the file the alert lives in, in the syntax its tool already reads, and let the model check both ends. A baseline alert also carries its owner, window, and severity, as `litespec/ext/delivery#define-actionable-baseline-alerts` states for projects that select the Delivery extension.

**Example:** Claimwick's alert rule `submission-acknowledgement-slow` carries `requirement: requirement/spending.month-end-submissions` and `runbook: runbook/slow-submissions` in its labels.

**Checked by:** `litespec model check` reports `alert-link-missing` (level S) for an alert rule in the files `paths.alerts` names whose `requirement` label is missing or names no quality requirement, or whose `runbook` label is missing or names no runbook.

### Cover Every Quality Requirement with an Alert or a Check

**ID:** `litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check`

**Statement:** Every quality requirement that is not retired MUST be named by at least one alert rule or check result, or covered by an approved gap exception.

**Rationale:** A quality requirement nobody watches is a promise checked only when a customer notices it broken. An alert names it to watch production, and a check result, from a measured check or a drill, names it through its `check` to show it was measured before release; a requirement that cannot be watched yet says so as a gap exception with an owner and an expiry date, instead of passing silently.

**Example:** `requirement/spending.month-end-submissions` is named by the alert `submission-acknowledgement-slow` and by the k6 result its `check` names.

**Checked by:** `litespec model check` reports `quality-alert-missing` (level G) for a quality requirement that no alert rule names in its `requirement` label, whose `check` matches no measured-check or check result the model read, and that no approved gap exception covering `quality-alert-missing` lists in its `subject`.

### Point Each Operations File at Its Decision

**ID:** `litespec/quality/operations#point-each-operations-file-at-its-decision`

**Statement:** Every deployment, proxy, host, and CI workflow file the project lists in `paths.operations` MUST carry a `follows` mark naming the technical decision, exception, or provision it carries out.

**Rationale:** Operations files hold choices as load-bearing as code: the proxy's security headers, the backup schedule, the gates a pull request passes. Their reason usually lives in a comment that no tool reads, so a retired decision leaves its configuration behind unnoticed. A `follows` mark in the file's own comment syntax is the same link a technical class carries, so `decision-unimplemented` and `mark-unknown` cover these files too.

**Example:** Claimwick's `deploy/Caddyfile` opens with `# <follows>decision/0004-serve-receipts-behind-one-proxy</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a file in `paths.operations` with no `follows` mark and `mark-unknown` (level S) for a mark naming no decision, exception, or provision, and counts the file toward `decision-unimplemented`.

### Pass Trace Context in the W3C Format

**ID:** `litespec/quality/operations#pass-trace-context-in-the-w3c-format`

**Statement:** A host MUST read and write trace context as the [W3C Trace Context](https://www.w3.org/TR/trace-context/) `traceparent` and `tracestate` headers, using one configured propagator.

**Rationale:** An identifier that each host formats its own way correlates nothing across the boundary between them. `traceparent` is the format OpenTelemetry implements and every managed backend accepts, so a trace crosses a frontend, an API, a background host, and a provider without translation.

One propagator, the component that reads and writes the headers, is the part that matters: two configured propagators produce two identifiers for one request, and the trace splits at whichever hop reads the other one.

**Example:** The trace identifier in a Problem Details response is the `trace-id` field of the current `traceparent`, which is what `litespec/backend/api#return-stable-problem-details` returns.

**Reviewed for:** One propagator reads and writes W3C trace context on every host.

### Separate Liveness and Readiness

**ID:** `litespec/quality/operations#separate-liveness-and-readiness`

**Statement:** A host MUST expose `/health/live` without dependency checks and `/health/ready` including its critical dependencies.

**Rationale:** Liveness answers whether the process is alive; readiness answers whether it can safely serve traffic. When a dependency fails, readiness fails so the orchestrator stops sending traffic, while liveness stays true so the orchestrator does not restart a healthy process. One endpoint for both forces a choice between serving traffic the application cannot handle and restarting a process that only needs its dependency back.

**Reviewed for:** Liveness answers without dependency checks while readiness includes the critical ones.

### Apply Schema Changes Outside Request Startup

**ID:** `litespec/quality/operations#apply-schema-changes-outside-request-startup`

**Statement:** A hosted schema change MUST run as a dedicated release step before traffic shifts, reviewed against both application versions.

**Rationale:** Schema work is a release step, not a startup step. A replica that applies schema at startup races its siblings, and each one serves traffic against a half-applied schema until the race settles. Running the change as one dedicated step before traffic shifts, reviewed against both the old and the new application version, keeps every replica on the same schema from its first request.

**Reviewed for:** The schema step runs before traffic shifts, with no schema work at replica startup.

### Operate Background Work Independently

**ID:** `litespec/quality/operations#operate-background-work-independently`

**Statement:** A Worker MUST publish its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior.

**Rationale:** A Worker fails on its own schedule: its queue grows, its retries climb, its oldest item ages, while WebApi keeps answering health checks. Without its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior, the failure stays invisible until the backlog is the incident. WebApi readiness otherwise hides a failed Worker while the queue grows unobserved.

**Reviewed for:** The Worker publishes its own health, rates, counts, oldest pending age, and shutdown behavior.

### Bound External Calls

**ID:** `litespec/quality/operations#bound-external-calls`

**Statement:** Every network call MUST declare an explicit timeout and cancellation path, with retries only for idempotent operations named by a bounded retry policy.

**Rationale:** Every network call waits on a system the product does not control. Without an explicit timeout and cancellation path, one slow provider holds a request thread until that unrelated system recovers, and each held thread is one less thread for healthy requests. Retries stay bounded and idempotent-only, because retrying a call that already acted repeats the act.

**Reviewed for:** Each network call declares a timeout, a cancellation path, and bounded retries for idempotent operations only.

### List Every Sub-Processor

**ID:** `litespec/quality/operations#list-every-sub-processor`

**Statement:** A release MUST call only providers the generated sub-processor list names from the project's provider actors.

**Rationale:** Customers ask which providers touch their data, and the sub-processor list is the answer. The model builder renders it from the provider actor records, so a provider added in code but missing from the list answers that question wrong. Generating the list from records keeps the answer and the code in step.

**Reviewed for:** Each provider the release calls appears on the generated sub-processor list.

### Name Every Runbook from an Alert, Job, Reaction or Failure Policy

**ID:** `litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`

**Statement:** Every runbook record MUST be named by at least one alert, job, reaction or failure policy.

**Rationale:** A runbook nobody names answers no failure: an alert, a job, a reaction or a failure policy is the caller that sends a person to the procedure, and without that link the procedure waits undiscovered while the failure it answers is handled from memory. Naming each runbook from at least one caller keeps every procedure reachable from the event that needs it.

**Example:** Claimwick's job `send-event-change-notices` names `runbook/clear-stuck-notices` in its `runbook` field, so the procedure is reachable from the work that needs it.

**Checked by:** `litespec model check` reports `runbook-unused` (level A) for a runbook record that no alert label, job, reaction or failure policy names.

### Keep Production Secrets Out of the Repository

**ID:** `litespec/quality/operations#keep-production-secrets-out-of-the-repository`

**Statement:** Production environment values MUST live on the host, never in the repository.

**Rationale:** A secret committed once lives in history after deletion, readable to everyone the repository ever reaches. Keeping production values on the host and only placeholders in a repository example file keeps the values with the deployment that needs them and out of every clone.

**Reviewed for:** Production values live on the host, with only placeholders in the repository example file.

## Conventions

### Keep Runbooks Near Project Documentation

**ID:** `litespec/quality/operations#keep-runbooks-near-project-documentation`

**Default:** Place runbooks under `.litespec/behavior/runbooks/` stating trigger, impact, prerequisites, procedure, verification, recovery, and escalation, covering restore, rollback, schema failure, leaked secrets, dependency outages, and extension recovery.

**Replacement:** A consumer records a runbook-layout convention naming where its runbooks live and what each holds.

**Rationale:** An alert names its runbook, and the reader follows the pointer under pressure. One folder for every runbook keeps each pointer resolving to a procedure with the same fields: trigger, impact, prerequisites, procedure, verification, recovery, and escalation. A runbook kept anywhere else breaks the pointer the alert gave.

**Reviewed for:** Each runbook sits under `.litespec/behavior/runbooks/` with its fields, or the runbook-layout convention names another arrangement.

## Reference Example

This informative example demonstrates `litespec/quality/operations#emit-correlated-diagnostics` and `litespec/quality/operations#separate-liveness-and-readiness`.

A Claimwick host emits structured logs and traces with one trace identifier across the API and the Worker, answers `/health/live` without dependency checks, and answers `/health/ready` with its database check, so traffic stops when the database fails while the process stays alive.
