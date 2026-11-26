# Operations

**Stack:** any
**Concern:** operations

Operating a product means starting it, watching it, and keeping its runtime state visible. This page states how a LiteSpec product does each: local startup, diagnostics, and the operating records that answer failures.

It applies to everyone who operates a release and to any change that touches schema, external dependencies, or background work. A product that follows it stays observable with one maintainer on call. Deployment, rollback, backup, restore, alerts, runbooks, and the alerting regime are stated in the [Delivery extension](../ext/delivery.md) for projects that select it.

## Concepts

### Operating Records

Everything an operator needs to know lives in an operating record: a file with a fixed home and one writer. A person writes the operations log and the secret inventory, while the model builder writes the sub-processor list from the project records. Only the operations log keeps the past; the others describe the present.

### Operations Log

The operations log is append-only. Each month gets one file under `.litespec/implementation/operations/log/`, named `YYYY-MM.md`, and entries are appended, never edited. The log records branch-protection bypasses, runbook rehearsals, restores, production deployments, and incidents. The dated rehearsal each runbook needs has its home here.

### Secret Inventory

The secret inventory is hand-written at `.litespec/implementation/operations/secrets.json`. Each entry names one secret, where it lives, and its next rotation date. It never holds the value itself. Each rotation date is reviewed before it passes. An entry looks like this:

```json
{
  "name": "database-password",
  "location": "production vault",
  "rotationDate": "2027-02-01"
}
```

### Sub-Processor List

The sub-processor list answers the question every customer asks: which providers touch our data. It names every provider that processes personal data for the product. The model builder renders it from the provider actors, the actor records that name the project's providers, onto `.litespec/implementation/operations/sub-processors.md`. Nobody edits it by hand.

## Standards

### Emit Correlated Diagnostics

**ID:** `litespec/quality/operations#emit-correlated-diagnostics`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST emit structured logs, traces, and metrics, with correlation identifiers on logs and traces and service labels on metrics.

**Rationale:** One request often crosses several hosts, and each running service is a host. Logs and traces carry timestamp, level, service, environment, trace identifier, and span identifier, while metrics carry service and environment labels. The correlation identifiers let one request be followed across every host it touched: the only trail a cross-host failure leaves. Stable event names keep dashboards working across releases instead of breaking each time a message is reworded.

**Reviewed for:** Do logs and traces carry correlation identifiers, and do metrics carry service labels? Look at one request's logs, traces, and metrics in the telemetry backend.

### Point Every Runtime Record at Its Use Case

**ID:** `litespec/quality/operations#point-every-runtime-record-at-its-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST carry the use-case identity of every mediated message, resolved from the generated registry, as `litespec.use_case` on its span, its log scope, and its error report.

**Rationale:** A trace identifier joins the runtime records of one request to each other, but not to the specification. A person holding a slow span, a log line, or an error report still has to guess which use case it was.

The identity is already a generated value, so a pipeline stage reads it from the registry once per message and stamps it on every surface under one name. Reactions, jobs, and outbox consumers carry it as well as requests. A metric carries it only as a label on an instrument whose series stay bounded: a duration histogram labeled by every use case multiplies its series by hundreds, so request instruments label by module instead.

**Example:** When Claimwick receives a request to submit an expense, the span and the log scope carry `litespec.use_case = use-case/spending.submit-expense`, and an unhandled failure inside it reaches the error tracker with the same tag.

**Reviewed for:** Does a pipeline stage stamp `litespec.use_case` on the span, log scope, and error report of each mediated message? Look at the mediator pipeline and one error report per host.

### Pass Trace Context in the W3C Format

**ID:** `litespec/quality/operations#pass-trace-context-in-the-w3c-format`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST read and write trace context as the [W3C Trace Context](https://www.w3.org/TR/trace-context/) `traceparent` and `tracestate` headers, using one configured propagator.

**Rationale:** An identifier that each host formats its own way correlates nothing across the boundary between hosts. The `traceparent` format is what OpenTelemetry implements and every managed backend accepts, so a trace crosses a frontend, an API, a background host, and a provider without translation. One propagator, the component that reads and writes the headers, is the part that matters: two configured propagators produce two identifiers for one request, and the trace splits at whichever hop reads the other one.

**Example:** The trace identifier in a Problem Details response is the `trace-id` field of the current `traceparent`, which is what `litespec/core/backend-api#return-stable-problem-details` returns.

**Reviewed for:** Does one configured propagator read and write W3C trace context on every host? Look at the telemetry setup of each host.

### Separate Liveness and Readiness

**ID:** `litespec/quality/operations#separate-liveness-and-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST expose `/health/live` without dependency checks and `/health/ready` including its critical dependencies.

**Rationale:** Liveness answers whether the process is alive, and readiness answers whether it can safely serve traffic. When a dependency fails, readiness fails so the orchestrator stops sending traffic, while liveness stays true so the orchestrator does not restart a healthy process. One endpoint for both forces a choice between serving traffic the application cannot handle and restarting a process that only needs its dependency back.

**Reviewed for:** Does `/health/live` answer without dependency checks while `/health/ready` includes the critical dependencies? Look at the health check registrations of each host.

### Operate Background Work Independently

**ID:** `litespec/quality/operations#operate-background-work-independently`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST publish its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior.

**Rationale:** A Worker fails on its own schedule: its queue grows, its retries climb, and its oldest item ages, while WebApi keeps answering health checks. Without its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior, the failure stays invisible until the backlog is the incident.

**Reviewed for:** Does the Worker publish its own health, processing rate, failure count, retry count, oldest pending age, and shutdown behavior? Look at the Worker's health endpoints and metrics.

### Bound External Calls

**ID:** `litespec/quality/operations#bound-external-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every network call MUST declare an explicit timeout and cancellation path, with retries only for idempotent operations named by a bounded retry policy.

**Rationale:** Every network call waits on a system the product does not control. Without an explicit timeout and cancellation path, one slow provider holds a request thread until that unrelated system recovers, and each held thread is one less thread for healthy requests. Retries stay bounded and idempotent-only, because retrying a call that already acted repeats the act.

**Reviewed for:** Does each network call declare a timeout, a cancellation path, and bounded retries for idempotent operations only? Look at each HTTP client registration and its resilience policy.

### List Every Sub-Processor

**ID:** `litespec/quality/operations#list-every-sub-processor`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** A release MUST call only providers the generated sub-processor list names from the project's provider actors.

**Rationale:** Customers ask which providers touch their data, and the sub-processor list is the answer. The model builder renders it from the provider actor records, so a provider added in code but missing from the list answers that question wrong. Generating the list from records keeps the answer and the code in step.

**Reviewed for:** Does each provider the release calls appear on the generated sub-processor list? Compare the release's provider clients with `.litespec/implementation/operations/sub-processors.md`.

### Keep Production Secrets Out of the Repository

**ID:** `litespec/quality/operations#keep-production-secrets-out-of-the-repository`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Production environment values MUST live on the host, never in the repository, under `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`.

**Rationale:** A secret committed once lives in history after deletion, readable to everyone the repository ever reaches, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states. Production values live on the host, and the repository example file holds only placeholders, so the values stay with the deployment that needs them and out of every clone.

**Reviewed for:** Do production values live on the host, with only placeholders in the repository example file? Search the tree for production values and read the example file.

## Conventions

### Place Each Runbook at Its Scope

**ID:** `litespec/quality/operations#place-each-runbook-at-its-scope`

**Strength:** default

**Applies to:** artifact: record:runbook

**Default:** Place a runbook under `.litespec/implementation/runbooks/` when it answers product-wide failures, or under `.litespec/implementation/modules/<module>/runbooks/` when every record naming it sits in one module, stating trigger, impact, prerequisites, procedure, verification, recovery, and escalation, covering restore, rollback, schema failure, leaked secrets, dependency outages, and extension recovery.

**Replacement:** A consumer records a runbook-layout convention naming where its runbooks live and what each holds.

**Rationale:** An alert names its runbook, and the reader follows the pointer under pressure. A runbook sits at product level when it answers product-wide failures, and under its module when every caller sits in one module, so each pointer resolves to a procedure with the same fields: trigger, impact, prerequisites, procedure, verification, recovery, and escalation. A runbook kept anywhere else breaks the pointer the alert gave.

**Reviewed for:** Does each runbook sit under `.litespec/implementation/runbooks/` or its module's runbooks folder with its fields, or does the convention name another arrangement? Look at the runbook folders and their headings.

## Reference Example

This informative example demonstrates `litespec/quality/operations#emit-correlated-diagnostics` and `litespec/quality/operations#separate-liveness-and-readiness`.

A Claimwick host emits structured logs and traces that share one trace identifier across the API and the Worker. `/health/live` answers without dependency checks while `/health/ready` runs the database check, so traffic stops when the database fails and the process stays alive.
