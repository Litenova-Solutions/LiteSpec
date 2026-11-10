# Operations

**Stack:** any
**Concern:** operations

## Intent

Operating a product means starting it, watching it, and keeping its runtime state visible, and this page states how a LiteSpec product does each: local startup, diagnostics, and the operating records that answer failures. It applies to everyone who operates a release and to any change that touches schema, external dependencies, or background work. A product that follows it stays observable with one maintainer on call; deployment, rollback, backup, restore, alerts, runbooks and the alerting regime live in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Concepts

### Operating Records

Everything this page asks an operator to know lives in operating records with a fixed home and one writer. A person writes the operations log and the secret inventory; the model builder writes the sub-processor list from the project records. Only the operations log keeps the past: the others describe the present.

### Operations Log

The operations log is append-only: one file per month under `.litespec/implementation/operations/log/`, named `YYYY-MM.md`, with entries appended and never edited. It records branch-protection bypasses, runbook rehearsals, restores, production deployments, and incidents, so the dated rehearsal each runbook needs has one home.

### Secret Inventory

The secret inventory is hand-written at `.litespec/implementation/operations/secrets.json`. Each entry names the secret, where it lives, and its next rotation date, and never the value itself. Each rotation date is reviewed before it passes. An entry has this shape:

```json
{
  "name": "database-password",
  "location": "production vault",
  "rotationDate": "2027-02-01"
}
```

### Sub-Processor List

The sub-processor list names every provider that processes personal data for the product. The model builder renders it from the project's provider actors, the actor records that name its providers, on `.litespec/implementation/operations/sub-processors.md`. Nobody edits it by hand.

## Standards

### Start the Stack with One Local Command

**ID:** `litespec/quality/operations#start-the-stack-with-one-local-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A workspace MUST start WebApi, PostgreSQL, configured frontends, and its Worker where one exists, with one local command named in the consumer `AGENTS.md`.

**Rationale:** A stack that needs several terminals and a wiki page starts differently for every developer, and the differences become bug reports nobody can reproduce. One named command brings WebApi, PostgreSQL, the frontends, and the background Worker up together, so every developer and every agent starts from the same running system. The baseline command is Aspire's `AppHost`, with `ServiceDefaults` supplying the shared discovery, health, resilience, and telemetry defaults its resources rely on. A workspace that orchestrates the same set another way satisfies this rule with its own command: the obligation is that one command brings the stack up, not that one tool does.

**Reviewed for:** One command named in the consumer `AGENTS.md` starts WebApi, PostgreSQL, the frontends, and the Worker.

### Emit Correlated Diagnostics

**ID:** `litespec/quality/operations#emit-correlated-diagnostics`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST emit structured logs, traces, and metrics, with correlation identifiers on logs and traces and service labels on metrics.

**Rationale:** Each running service is a host, and one request often crosses several. Logs and traces carry timestamp, level, service, environment, trace identifier, and span identifier, and metrics carry service and environment labels. The correlation identifiers let one request be followed across every host it touched, which is the only trail a cross-host failure leaves. Stable event names keep dashboards working across releases instead of breaking each time a message is reworded.

**Reviewed for:** Logs and traces carry correlation identifiers, and metrics carry service labels.

### Point Every Runtime Record at Its Use Case

**ID:** `litespec/quality/operations#point-every-runtime-record-at-its-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST carry the use-case identity of every mediated message, resolved from the generated registry, as `litespec.use_case` on its span, its log scope, and its error report.

**Rationale:** A trace identifier joins the runtime records of one request to each other, but not to the specification: a person holding a slow span, a log line, or an error report still has to guess which use case it was. The identity is already a generated value, so a pipeline stage reads it from the registry once per message and stamps it on every surface under one name, and reactions, jobs, and outbox consumers carry it as well as requests. A metric carries it only as a label on an instrument whose series stay bounded: a duration histogram labeled by every use case multiplies its series by hundreds, so request instruments label by module instead.

**Example:** A Claimwick request to submit an expense opens a span and a log scope carrying `litespec.use_case = use-case/spending.submit-expense`, and an unhandled failure inside it reaches the error tracker with the same tag.

**Reviewed for:** Which pipeline stage reads the registry and stamps `litespec.use_case` on the span, the log scope, and the error report of each mediated message? Look at the mediator pipeline and one error report per host.

### Pass Trace Context in the W3C Format

**ID:** `litespec/quality/operations#pass-trace-context-in-the-w3c-format`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST read and write trace context as the [W3C Trace Context](https://www.w3.org/TR/trace-context/) `traceparent` and `tracestate` headers, using one configured propagator.

**Rationale:** An identifier that each host formats its own way correlates nothing across the boundary between them. `traceparent` is the format OpenTelemetry implements and every managed backend accepts, so a trace crosses a frontend, an API, a background host, and a provider without translation. One propagator, the component that reads and writes the headers, is the part that matters: two configured propagators produce two identifiers for one request, and the trace splits at whichever hop reads the other one.

**Example:** The trace identifier in a Problem Details response is the `trace-id` field of the current `traceparent`, which is what `litespec/backend/api#return-stable-problem-details` returns.

**Reviewed for:** One propagator reads and writes W3C trace context on every host.

### Separate Liveness and Readiness

**ID:** `litespec/quality/operations#separate-liveness-and-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST expose `/health/live` without dependency checks and `/health/ready` including its critical dependencies.

**Rationale:** Liveness answers whether the process is alive; readiness answers whether it can safely serve traffic. When a dependency fails, readiness fails so the orchestrator stops sending traffic, while liveness stays true so the orchestrator does not restart a healthy process. One endpoint for both forces a choice between serving traffic the application cannot handle and restarting a process that only needs its dependency back.

**Reviewed for:** Liveness answers without dependency checks while readiness includes the critical ones.

### Apply Schema Changes Outside Request Startup

**ID:** `litespec/quality/operations#apply-schema-changes-outside-request-startup`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** A hosted schema change MUST run as a dedicated release step before traffic shifts, reviewed against both application versions, under `litespec/backend/persistence#control-production-schema-changes`.

**Rationale:** Schema work is a release step, not a startup step, as `litespec/backend/persistence#control-production-schema-changes` states. A replica that applies schema at startup races its siblings, and each one serves traffic against a half-applied schema until the race settles. Running the change as one dedicated step before traffic shifts, reviewed against both the old and the new application version, keeps every replica on the same schema from its first request.

**Reviewed for:** The schema step runs before traffic shifts, with no schema work at replica startup.

### Operate Background Work Independently

**ID:** `litespec/quality/operations#operate-background-work-independently`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST publish its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior.

**Rationale:** A Worker fails on its own schedule: its queue grows, its retries climb, its oldest item ages, while WebApi keeps answering health checks. Without its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior, the failure stays invisible until the backlog is the incident. WebApi readiness otherwise hides a failed Worker while the queue grows unobserved.

**Reviewed for:** The Worker publishes its own health, rates, counts, oldest pending age, and shutdown behavior.

### Bound External Calls

**ID:** `litespec/quality/operations#bound-external-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every network call MUST declare an explicit timeout and cancellation path, with retries only for idempotent operations named by a bounded retry policy.

**Rationale:** Every network call waits on a system the product does not control. Without an explicit timeout and cancellation path, one slow provider holds a request thread until that unrelated system recovers, and each held thread is one less thread for healthy requests. Retries stay bounded and idempotent-only, because retrying a call that already acted repeats the act.

**Reviewed for:** Each network call declares a timeout, a cancellation path, and bounded retries for idempotent operations only.

### List Every Sub-Processor

**ID:** `litespec/quality/operations#list-every-sub-processor`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** A release MUST call only providers the generated sub-processor list names from the project's provider actors.

**Rationale:** Customers ask which providers touch their data, and the sub-processor list is the answer. The model builder renders it from the provider actor records, so a provider added in code but missing from the list answers that question wrong. Generating the list from records keeps the answer and the code in step.

**Reviewed for:** Each provider the release calls appears on the generated sub-processor list.

### Keep Production Secrets Out of the Repository

**ID:** `litespec/quality/operations#keep-production-secrets-out-of-the-repository`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Production environment values MUST live on the host, never in the repository, under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Rationale:** A secret committed once lives in history after deletion, readable to everyone the repository ever reaches, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states. Keeping production values on the host and only placeholders in a repository example file keeps the values with the deployment that needs them and out of every clone.

**Reviewed for:** Production values live on the host, with only placeholders in the repository example file.

## Conventions

### Keep Runbooks Near Project Documentation

**ID:** `litespec/quality/operations#keep-runbooks-near-project-documentation`

**Strength:** default

**Applies to:** artifact: record:runbook

**Default:** Place a runbook under `.litespec/implementation/runbooks/` when it answers product-wide failures, or under `.litespec/implementation/modules/<module>/runbooks/` when every record naming it sits in one module, stating trigger, impact, prerequisites, procedure, verification, recovery, and escalation, covering restore, rollback, schema failure, leaked secrets, dependency outages, and extension recovery.

**Replacement:** A consumer records a runbook-layout convention naming where its runbooks live and what each holds.

**Rationale:** An alert names its runbook, and the reader follows the pointer under pressure. A runbook sits at product level when it answers product-wide failures and under its module when every caller sits in one module, so each pointer resolves to a procedure with the same fields: trigger, impact, prerequisites, procedure, verification, recovery, and escalation. A runbook kept anywhere else breaks the pointer the alert gave.

**Reviewed for:** Each runbook sits under `.litespec/implementation/runbooks/` or its module's runbooks folder with its fields, or the runbook-layout convention names another arrangement.

## Reference Example

This informative example demonstrates `litespec/quality/operations#emit-correlated-diagnostics` and `litespec/quality/operations#separate-liveness-and-readiness`.

A Claimwick host emits structured logs and traces with one trace identifier across the API and the Worker, answers `/health/live` without dependency checks, and answers `/health/ready` with its database check, so traffic stops when the database fails while the process stays alive.
