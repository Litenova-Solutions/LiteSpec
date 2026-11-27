# Operations

**Stack:** any
**Concern:** operations

This page describes the operating records and runtime signals of a LiteSpec product. It is for the person who starts the system, watches it in production, and answers failures. Clear records and steady signals keep that work calm with one maintainer on call.

## Concepts

### Operating Records

Each operating fact lives in one file with one writer. You edit the operations log and the secret inventory by hand. The model builder renders the sub-processor list from the provider actor records.

The operations log is append-only. Each month gets one file under `.litespec/implementation/operations/log/`, named `YYYY-MM.md`, with earlier entries left in place. The log holds branch-protection bypasses, runbook rehearsals, restores, production deployments, and incidents.

The secret inventory lives at `.litespec/implementation/operations/secrets.json`. Each entry names one secret, where it lives, and its next rotation date, with the value itself kept elsewhere. Each rotation date gets a review before it passes.

```json
{
  "name": "database-password",
  "location": "production vault",
  "rotationDate": "2027-02-01"
}
```

The sub-processor list answers which providers touch customer data. It names each provider that processes personal data for the product. The builder writes it to `.litespec/implementation/operations/sub-processors.md`, so it takes no hand edits.

### Diagnostics and Health

Hosts emit structured logs, traces, and metrics. Logs and traces carry timestamp, level, service, environment, trace identifier, and span identifier, while metrics carry service and environment labels. Shared identifiers let you follow one request across each host it touched.

A pipeline stage reads the use-case identity from the generated registry once per message and stamps it on the span, the log scope, and the error report under one name: `litespec.use_case`. Reactions, jobs, and outbox consumers carry it alongside requests. Request metric instruments label by module instead, since a duration histogram labeled by each use case multiplies its series by hundreds.

When Claimwick receives a request to submit an expense, the span and the log scope carry `litespec.use_case = use-case/spending.submit-expense`. A failure inside that request reaches the error tracker with the same tag, so the trail stays joined.

Hosts read and write trace context as W3C Trace Context `traceparent` and `tracestate` headers, using one configured propagator. Two configured propagators produce two identifiers for one request, and the trace splits at the hop that reads the other one. The trace identifier in a Problem Details response is the `trace-id` field of the current `traceparent`.

Each host exposes `/health/live` with no dependency checks and `/health/ready` with its critical dependencies. When a dependency fails, readiness fails so the orchestrator stops sending traffic, while liveness stays true so the orchestrator keeps a healthy process running. The Worker publishes its own liveness, readiness, processing rate, failure count, retry count, oldest pending age, and shutdown behavior, so a growing queue shows before it turns into an incident.

Network calls declare an explicit timeout and a cancellation path. Retries stay bounded and cover idempotent operations only, since a repeated call that acted repeats the act. Each HTTP client registration carries its resilience policy.

### Secrets, Providers, and Runbooks

Production values live on the host. The repository example file holds placeholders only, so the values stay with the deployment that needs them and out of each clone. A release calls only the providers the generated sub-processor list names from the project provider actors.

A runbook sits at product level under `.litespec/implementation/runbooks/` when it answers product-wide failures, and under its module `runbooks/` folder when each record naming it sits in one module. Each runbook holds trigger, impact, prerequisites, procedure, verification, recovery, and escalation, covering restore, rollback, schema failure, leaked secrets, dependency outages, and extension recovery. An alert names its runbook, and you follow the pointer under pressure.

## Standards

None.

## Conventions

None.
