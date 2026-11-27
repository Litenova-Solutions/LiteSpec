# Scheduled Jobs and Reports

**Stack:** any
**Concern:** application

Scheduled jobs and heavy reports run outside request handling: a timetable starts the jobs, and a Worker carries the reports a request fails to finish. This page states how a project records each schedule, runs each item once, and recovers after failure. It is for developers who schedule recurring work and operators who replay it. A batch that replays a finished item charges twice, so each cursor, lease, and export path is explicit.

## Concepts

### How Jobs Record Their Work

A job record carries the schedule as `every` or `cron` with its time zone, the selection of items, the batch size, and the command run per item. Claimwick's nightly close states `every: 1d` with the zone from `setting/reporting.close-time-zone`, so one setting moves the close for every tenant. A literal zone stays on jobs whose meaning is local to one place, such as a nightly statement at 9 AM for its readers.

```yaml
# job/reporting.nightly-close
kind: scheduled
schedule: { every: 1d, at: "02:00" }
timezone: setting/reporting.close-time-zone
selection: { query: ReadOverdueCloses }
batchSize: 100
runs: CloseOverdue ideal
cursor: { field: lastClosedAt }
```

Each occurrence takes its identity from the schedule ID plus the scheduled instant, so retries and replicas recognize the same planned work. The cursor marks where a batch stopped, and the store keeps the last scheduled, started, and completed time per occurrence. Each occurrence carries scheduled, processing, completed, or dead-letter state, so a restarted Worker tells finished work from work in flight and work not yet started.

### How the Worker Runs Each Item Once

The Worker runs scheduled work outside WebApi request handling, with its own scheduler lifetime and a shutdown order that drains running occurrences before schedule polling stops. Each job dispatches an Application command or a declared Application port, and enters through the use case message like every other caller. Handler code sits in the `Application/` folder of the use case it runs, beside the notification it sends or the outside system it calls when it runs none. No job mutates a session directly or calls WebApi endpoints.

Each planned occurrence carries a lease with its owner, expiry, and fencing value, and the fencing value rises with each claim. A replica renews the lease it holds and stops when renewal fails, while a live replica picks up a claim whose lease expired. In a product that isolates tenants, each occurrence and its lease scope to one tenant. Completion lands only with a current fencing value, and the occurrence identity scopes the command idempotency key, so overlapping replicas complete each occurrence once.

A lease or process failure replays a job whose completion stayed uncertain, so the command and its side effects absorb the repeat. Each job watches cancellation and ends its current step when it fires, and each job carries a bounded duration so a Worker stops without leaving running work it monitors. Manual replay opens a new attempt under the same occurrence identity, recorded in the audit trail where audit is selected. Occurrences older than the retention setting leave the store, keeping the key space bounded: a daily schedule retained for 90 days holds 90 rows, while a per-minute schedule with the same retention holds over 100,000.

### How the Worker Handles Failure

Bounded attempts with cancellation-aware backoff cap what one failed occurrence consumes, and a poison state parks the occurrence that fails permanently instead of retrying it forever. A dead scheduler store reads as an outage of the dependency, with backed-off and rate-limited logging and no poisoned occurrence, and retry and poison handling touch only claimed occurrences.

Each schedule states whether a missed occurrence is skipped, replayed once, or replayed per interval. Schedules use UTC unless the specification names a business-local time need with its IANA zone, and the business-local record defines the skipped and repeated hour around each daylight-saving transition. Tests move time with an injected clock across representative transitions, and production code reads time from that clock instead of the machine clock.

Runbook records carry inspection, manual replay, and disable, each naming the command, endpoint, or configuration value that performs it. Claimwick's disable procedure names the configuration value that stops the close and states what happens to an occurrence already claimed. Operators recover through those records instead of editing the store directly. Each occurrence resolves its scoped dependencies inside one execution scope, disposed after the command completes, and schedule definitions stay apart from command behavior, keeping timing changes separate from business code.

### How Reports Reach Storage

Reports cover one documented query or export the normal request path fails to serve, with the use case page carrying the measurements that prove the gap. Each report or export justifies its Worker, storage, and operating attention with a current cost boundary, and speculative systems wait until an accepted query needs them. The report meaning lives in Application under its module, while SQL, row mappings, and storage calls live in Infrastructure, and scheduling, chunking, and progress tracking live in Worker code apart from the report rules.

Report SQL binds every actor, tenant, filter, range, sort, and pagination value as a parameter, keeping tenant isolation intact and injection holes closed. The export query states its `paging` with `mode` of `pages` or `none` with the reason behind reading all rows at once. Each report declares its bounds as setting records for date range, rows, execution time, concurrent jobs, output size, and storage lifetime, and query plans pass review against representative data before the report ships.

An export past its request budget runs in a Worker, and its output lands in approved object storage under a tenant-scoped path with a named contact, an expiry, and a short-lived authorized download. Each export states its audit reason and target kind, honors cancellation, and drops expired output. Values starting with `=`, `+`, `-`, or `@` go out escaped outside formula fields, and each format states its character encoding with stable columns.

A report that reads ten thousand rows crosses the same actor, role, ownership, tenant, and sensitive-field boundaries as ten thousand single reads. The bulk path enforces the item-read rules, so the report exposes only rows its reader opens one by one.

Delivery that survives a process failure travels through [Durable Delivery](outbox.md). Report and object-storage packages each need a decision and a manifest pin.

## Standards

### Record the Schedule on the Job Record

**ID:** `litespec/core/jobs#record-the-schedule-on-the-job-record`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST record its schedule in structured form with its timezone on its job record.

**Rationale:** Tools read `every` or `cron` directly from the record, and the misfire behavior measures against the recorded time. A schedule in prose gives tools nothing to parse.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a job schedule that is neither an `every` duration nor a `cron` expression; each scheduled job record states its timezone.

### Name a Varying Time Zone by Its Source

**ID:** `litespec/core/jobs#name-a-varying-time-zone-by-its-source`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST state its time zone as a literal IANA zone or as the setting or aggregate field holding the zone.

**Rationale:** One schedule serves tenants in many zones. A literal zone freezes one country into every run, while a setting or tenant field lets each run read its own zone.

**Example:** Claimwick's nightly close names `setting/reporting.close-time-zone` instead of a literal zone, so the operator moves the close with one setting.

**Checked by:** `litespec model check` reports `record-invalid` for a schedule timezone that is neither a zone name nor a setting or field reference.

### Run One Command per Selected Item

**ID:** `litespec/core/jobs#run-one-command-per-selected-item`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A job MUST run one command per item in its `selection` within its `batchSize`, each in its own unit of work.

**Rationale:** One item in its own unit of work replays alone after failure. A batch sharing one unit of work replays its finished items beside the failed one.

**Reviewed for:** Does every job run one command per item in its `selection` within its `batchSize`, each in its own unit of work? Check the job record selection and the handler code.

### Resume Batch Work from Its Cursor

**ID:** `litespec/core/jobs#resume-batch-work-from-its-cursor`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A job that works through items in batches MUST name where resume state lives in its `cursor`.

**Rationale:** A batch that fails halfway resumes instead of restarting. The cursor holds where the run stopped, so the next run continues past finished items instead of replaying them or skipping unfinished ones.

**Reviewed for:** Does every batch job name its resume position in its cursor, with a scenario for the remainder after a halfway failure? Check the job records and the scenario tests.

### Tolerate Duplicate Execution

**ID:** `litespec/core/jobs#tolerate-duplicate-execution`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job Command and side effect MUST tolerate duplicate execution after lease or process failure.

**Rationale:** A lease or process failure replays a job whose completion stayed uncertain. A command that absorbs the repeat keeps one accepted effect, while work assuming exactly-once execution breaks on the first uncertain failure.

**Example:** Claimwick's nightly close replays the close command for the same scheduled instant after a Worker restart, and the second run records no second close.

**Reviewed for:** Does any job Command or side effect record more than one accepted effect after a replayed occurrence? Check the replay tests.

### Scope Command Idempotency by Occurrence

**ID:** `litespec/core/jobs#scope-command-idempotency-by-occurrence`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use its occurrence identity as the command idempotency key scope.

**Rationale:** Every retry of one occurrence replays the same scheduled business action. The occurrence identity scopes the key to that action, while a wider key merges separate occurrences into one.

**Reviewed for:** Does every retry of one occurrence use its occurrence identity as the idempotency key scope? Check the command idempotency keys.

### Allow Completion Only with a Current Fencing Value

**ID:** `litespec/core/jobs#allow-completion-only-with-a-current-fencing-value`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A Worker MUST mark completion only while its fencing value remains current.

**Rationale:** A stale fencing value marks ownership that moved to another replica. The Worker marks completion while its value stays current, so one result lands per occurrence.

**Reviewed for:** Does any completion write succeed on a stale fencing value? Check the store write path and its fencing tests.

### State Clock Behavior for Business-Local Schedules

**ID:** `litespec/core/jobs#state-clock-behavior-for-business-local-schedules`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A business-local schedule MUST record an IANA time-zone identifier and define skipped and repeated local-time behavior.

**Rationale:** Daylight-saving transitions omit one local hour and repeat another. The record names the zone and both behaviors, so no transition drops or doubles the local work.

**Reviewed for:** Does every business-local schedule record its IANA zone and the skipped and repeated local-time behavior? Check each schedule record.

### Scope Export Output to the Tenant

**ID:** `litespec/core/jobs#scope-export-output-to-the-tenant`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export MUST store its output under a tenant-scoped object-storage path.

**Rationale:** Export addresses follow a pattern callers guess. A tenant-scoped path keeps each tenant's exports under its own prefix, so a guessed address stays inside the guesser's own data.

**Example:** Claimwick stores each tenant's expense export under that tenant's prefix, so one tenant opens only its own files.

**Reviewed for:** Does every export store output under a tenant-scoped path in object storage? Check the export storage paths.

### Escape Formula-Leading Export Values

**ID:** `litespec/core/jobs#escape-formula-leading-export-values`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A CSV or spreadsheet export MUST escape formula-leading values unless the team intentionally fills a field with a formula.

**Rationale:** A value starting with `=`, `+`, `-`, or `@` turns into a live formula in the opener's spreadsheet, running with the opener's data. Escaping keeps data as data, except in a field the team fills with a formula on purpose.

**Example:** The library product escapes a member note starting with `+` in its CSV export, while its total column keeps its formula.

**Reviewed for:** Does every exported value starting with a formula character go out escaped, outside the formula fields the team fills on purpose? Check the CSV and spreadsheet writers.

## Conventions

None.
