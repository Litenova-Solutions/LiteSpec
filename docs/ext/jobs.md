# Scheduled Jobs

## Intent

Scheduled jobs start work from a timetable instead of from a request, such as nightly statements, delayed reminders, and calendar-based billing. This page states how a project records each schedule on a job record with its kind, scope, lease, and issued use cases, and runs it in a Worker with leases that survive restarts and overlapping replicas. A team applies it for recurring, delayed, or calendar-based work, so each occurrence runs once even when a replica fails mid-job.

## Activation

Activation scope: `local`.

Applicable specification kinds: `job`, `use-case`, `journey`.

The consumer enables `jobs` for recurring, delayed, or calendar-based work. Each schedule lives on a job record of kind `scheduled`, `one-off`, or `operator`, as `litespec/core/behavior#declare-every-job-with-kind-and-structured-schedule` states. Selecting it activates `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`: the project declares the execution host that runs the scheduler.

## Baseline Relationship

This extension replaces no baseline provision.

This extension switches on no optional record type: the job, setting, and runbook records belong to the core. Schedules bound their retention with setting records and operators recover through runbook records.

## Standards

### Carry the Schedule on the Job Record

**ID:** `litespec/ext/jobs#carry-the-schedule-on-the-job-record`

**Statement:** A scheduled or one-off job MUST carry its schedule in structured form with its timezone on its job record.

**Rationale:** The job record states when the work runs: `every` or `cron` with its timezone, so tools read the rhythm instead of parsing prose. A schedule kept outside the record runs with no reviewed moment attached, and misfire behavior has no stated time to measure against. `litespec/core/behavior#declare-every-job-with-kind-and-structured-schedule` states the record fields, and the `schedule-unstructured` check reports a schedule no tool can read.

**Reviewed for:** Each scheduled and one-off job record carries a structured schedule with its timezone.

### Issue One Run per Item in Scope

**ID:** `litespec/ext/jobs#issue-one-run-per-item-in-scope`

**Statement:** A job MUST issue one command per item in its `scope` within its `batchSize`, each in its own unit of work.

**Rationale:** The scope states what each run selects, the batch size bounds how many items one batch takes, and `issues` names the command run per item. Each item runs as one command in its own unit of work, as `litespec/backend/application#issue-per-record-work-from-a-job` requires: a failed item replays without replaying its batch.

**Reviewed for:** Each job run issues one command per item in its scope within its batch size, each in its own unit of work.

### Resume Driven Work Through the Cursor

**ID:** `litespec/ext/jobs#resume-driven-work-through-the-cursor`

**Statement:** A job driving a process MUST name where resume state lives in its `cursor`.

**Rationale:** A batch that fails halfway resumes instead of restarting: the cursor holds where the run stopped, beside the process state it drives. Without it, the next run replays finished items or skips unfinished ones, and a scenario covering the remainder resumes has no stated position to assert.

**Reviewed for:** Each job driving a process names its resume position in its cursor, with a scenario covering the remainder after a halfway failure.

### Run Operator Jobs Under Their Guard

**ID:** `litespec/ext/jobs#run-operator-jobs-under-their-guard`

**Statement:** An operator job run MUST stay inside its `runsWhere` under its `guard`.

**Rationale:** Operator work is interactive platform work with no aggregate, such as a rehearsal harness, so no model bounds where it runs or who runs it. `runsWhere` names the deployment scope it may touch and the guard names who may run it and when. `litespec/core/behavior#guard-every-operator-job` states the record fields, and the `operator-job-without-guard` check reports an operator job with no guard.

**Reviewed for:** Each operator job run stays inside its runsWhere under its guard.

### Run Jobs in Worker

**ID:** `litespec/ext/jobs#run-jobs-in-worker`

**Statement:** A scheduled job MUST run in Worker outside WebApi request handling.

**Rationale:** Scheduled work runs in a Worker, outside WebApi request handling. The Worker owns the schedule's lifetime and shuts its replicas down in a controlled order. Request handling has neither a scheduler nor a shutdown order for background work.

**Reviewed for:** Worker sources hold every scheduled job, and WebApi request handling holds none.

### Observe Job Cancellation

**ID:** `litespec/ext/jobs#observe-job-cancellation`

**Statement:** A scheduled job MUST observe cancellation.

**Rationale:** Each job observes cancellation, so an occurrence ends its current step instead of running past the moment its owner asked it to stop.

**Reviewed for:** Each job handler threads cancellation into its waits and stops its current step when cancellation fires.

### Bound Job Duration

**ID:** `litespec/ext/jobs#bound-job-duration`

**Statement:** A scheduled job MUST complete within a bounded duration.

**Rationale:** Each job bounds its duration, so a Worker stops without stranding work it can no longer watch.

**Reviewed for:** Each job handler carries a duration bound, and the occurrence store shows completions inside it.

### Stop Jobs During Graceful Shutdown

**ID:** `litespec/ext/jobs#stop-jobs-during-graceful-shutdown`

**Statement:** A scheduled job MUST stop accepting new work during graceful shutdown.

**Rationale:** Each job stops accepting new work during graceful shutdown. An unbounded job turns every deploy into a wait for work that never ends.

**Reviewed for:** The Worker shutdown path stops schedule polling before it drains running occurrences.

### Store Execution Leases

**ID:** `litespec/ext/jobs#store-execution-leases`

**Statement:** A scheduled-job store MUST record lease owner, expiry, and fencing value in PostgreSQL.

**Rationale:** Each planned occurrence carries a lease with its owner, expiry, and fencing value in PostgreSQL. The lease names the one replica that may complete that occurrence. The `lease` field on the job record names the lock each run claims. The outbox extension cites this definition for its own claims.

**Reviewed for:** The lease rows in PostgreSQL carry owner, expiry, and fencing value for each occurrence.

### Renew Only Owned Leases

**ID:** `litespec/ext/jobs#renew-only-owned-leases`

**Statement:** A Worker replica MUST renew only its active lease and stop when renewal fails.

**Rationale:** A replica renews only the lease it holds and only while it stays active. A failed renewal means another replica owns the occurrence now. The loser stops instead of completing work it no longer owns.

**Reviewed for:** Replica logs show no renewal after a failed renewal, and the loser stops its occurrence.

### Prevent Duplicate Completion

**ID:** `litespec/ext/jobs#prevent-duplicate-completion`

**Statement:** A scheduled-job lease MUST prevent two replicas from completing the same occurrence.

**Rationale:** Overlapping replicas must not complete the same occurrence twice. The lease admits one completion per occurrence. Two completions would bill, notify, or reconcile the same planned work twice.

**Reviewed for:** The lease admits one completion per occurrence across overlapping replicas.

### Generate Monotonic Fencing Values

**ID:** `litespec/ext/jobs#generate-monotonic-fencing-values`

**Statement:** A scheduled-job store MUST generate each fencing value monotonically per occurrence.

**Rationale:** Each fencing value rises monotonically per occurrence, so a later claim always presents a higher value than an earlier one. A Worker that lost its lease presents a stale value, and `litespec/ext/jobs#restrict-completion-by-fencing-value` refuses completion on a stale value.

**Reviewed for:** Fencing values in the store rise per occurrence, and a stale value refuses completion.

### Scope Occurrences and Leases by Tenant

**ID:** `litespec/ext/jobs#scope-occurrences-and-leases-by-tenant`

**Statement:** A scheduled-job occurrence and its lease MUST carry tenant scope when the product isolates tenants.

**Rationale:** Products that isolate tenants scope each occurrence and its lease to one tenant. One tenant's schedules then never trigger, block, or expose another tenant's work. Unscoped leases would let a replica mix tenants inside one occurrence.

**Reviewed for:** Occurrence and lease rows carry tenant scope in products that isolate tenants.

### Dispatch Application Behavior

**ID:** `litespec/ext/jobs#dispatch-application-behavior`

**Statement:** A scheduled job MUST dispatch an Application Command or approved Application port.

**Rationale:** The job record names the use cases its run issues, and the use case boundary lives in Application. Each job therefore dispatches an Application Command or an approved Application port. The schedule stays a trigger while the behavior stays owned.

**Reviewed for:** Each job handler calls an Application Command or an approved Application port.

### Exclude Direct Storage and HTTP Execution

**ID:** `litespec/ext/jobs#exclude-direct-storage-and-http-execution`

**Statement:** A scheduled job MUST NOT mutate a Marten session or DbContext directly, bypass Domain behavior, or call WebApi endpoints.

**Rationale:** A job that mutates a Marten session or DbContext directly, bypasses Domain behavior, or calls WebApi endpoints skips the command boundary. That boundary carries authorization, transaction, and validation the job would then lack. Jobs enter through Commands like every other caller.

**Reviewed for:** Job sources mutate no Marten session or DbContext and call no WebApi endpoint.

### Bound Job Retries

**ID:** `litespec/ext/jobs#bound-job-retries`

**Statement:** A scheduled job MUST use bounded attempts, cancellation-aware backoff, and a poison state for permanent failure.

**Rationale:** Bounded attempts with cancellation-aware backoff cap what one failed occurrence consumes. A poison state parks the permanently failing occurrence instead of retrying it forever. Without the bound, one poison occurrence eats the Worker's whole capacity.

**Reviewed for:** Each schedule declares bounded attempts with cancellation-aware backoff and a poison state.

### Tolerate Duplicate Execution

**ID:** `litespec/ext/jobs#tolerate-duplicate-execution`

**Statement:** A scheduled job Command and side effect MUST tolerate duplicate execution after lease or process failure.

**Rationale:** A lease or process failure can replay a job whose completion stayed uncertain. The job Command and its side effects tolerate that duplicate execution. Work that assumes exactly-once breaks on the first uncertain failure.

**Reviewed for:** Job Commands and side effects show one accepted effect after a replayed occurrence.

### Treat Unavailable Schedule Stores as Outages

**ID:** `litespec/ext/jobs#treat-unavailable-schedule-stores-as-outages`

**Statement:** A Worker MUST treat an unavailable scheduler store as the store outage `litespec/ext/outbox#distinguish-dependency-outage` defines, with backed-off, rate-limited logging.

**Rationale:** An unavailable scheduler store is an outage of the dependency, not a failed occurrence. The Worker treats it as the store outage `litespec/ext/outbox#distinguish-dependency-outage` defines, with backed-off, rate-limited logging. Retry and poison outcomes belong to claimed occurrences, not to a store nobody can reach.

**Reviewed for:** Worker logs during a store outage show backed-off, rate-limited entries and no poisoned occurrences.

### Persist Occurrence Recovery Points

**ID:** `litespec/ext/jobs#persist-occurrence-recovery-points`

**Statement:** A scheduled-job store MUST persist each occurrence's last scheduled, started, and completed time.

**Rationale:** Last scheduled, started, and completed times persist per occurrence. After a restart, those points separate missed work from completed work. Memory alone cannot make that split, because the restart wipes it.

**Reviewed for:** Each occurrence row carries last scheduled, started, and completed times.

### Define Misfire Recovery

**ID:** `litespec/ext/jobs#define-misfire-recovery`

**Statement:** A scheduled-job specification MUST define skipped, once-replayed, or interval-replayed behavior for missed occurrences.

**Rationale:** Downtime and delay leave occurrences past their time. The specification states whether each missed occurrence is skipped, replayed once, or replayed per interval. The choice controls the business effect: a skipped reminder stays silent while a replayed one arrives late.

**Reviewed for:** Each schedule states skipped, once-replayed, or interval-replayed behavior for missed occurrences.

### Provide Operational Recovery Procedures

**ID:** `litespec/ext/jobs#provide-operational-recovery-procedures`

**Statement:** A scheduled-job owner MUST provide Runbook records for inspection, manual replay, and disable.

**Rationale:** Stalled, failed, or unsafe schedules need controlled operator actions: inspection, manual replay, and disable. Runbook records carry those procedures. An operator without them improvises against the store during the incident.

Each procedure names the interface that performs it. A procedure that says "disable the schedule" without naming a command, an endpoint, or a configuration value remains undiscovered until the incident it was written for. Avoid an interface that writes to the schedule store directly, because it bypasses the occurrence identity `litespec/ext/jobs#derive-stable-occurrence-identity` depends on.

**Example:** A disable procedure names the configuration value or the administrative endpoint that stops the schedule. It states what happens to an occurrence already claimed.

**Reviewed for:** Runbook records cover inspection, manual replay, and disable for each schedule.

### Derive Stable Occurrence Identity

**ID:** `litespec/ext/jobs#derive-stable-occurrence-identity`

**Statement:** A scheduled-job store MUST derive each occurrence identity from its schedule ID and scheduled instant.

**Rationale:** Schedule ID plus scheduled instant gives each planned time one stable identity across retries and replicas. Retries of one occurrence then share their idempotency scope, and two replicas recognize the same occurrence.

**Reviewed for:** Occurrence identities in the store combine schedule ID with scheduled instant.

### Remove Occurrences Past Retention

**ID:** `litespec/ext/jobs#remove-occurrences-past-retention`

**Statement:** A scheduled-job store MUST remove occurrences older than the schedule's retention setting record.

**Rationale:** Occurrences older than the retention setting record leave the store so the key space stays bounded.

The key space grows with the schedule's frequency and its retention. A per-second schedule kept for a year holds more than 30 million rows. [Quartz states the same practice](https://quartz-scheduler.net/documentation/best-practices.html) for a persistent job store.

**Example:** A daily schedule that retains occurrences for 90 days holds 90 rows. A minute schedule with the same retention holds more than 100,000 rows. That is a different storage decision.

**Reviewed for:** The store holds no occurrence older than its schedule's retention setting record.

### Persist Occurrence State

**ID:** `litespec/ext/jobs#persist-occurrence-state`

**Statement:** A scheduled-job store MUST persist scheduled, processing, completed, and dead-letter state with safe execution details.

**Rationale:** Scheduled, processing, completed, and dead-letter states persist with attempt, lease, start, completion, and safe error data. The persisted state tells operators and recovery code where each occurrence stands. A store without states cannot separate work in flight from work never started.

**Reviewed for:** Each occurrence row carries scheduled, processing, completed, or dead-letter state with safe execution details.

### Scope Command Idempotency by Occurrence

**ID:** `litespec/ext/jobs#scope-command-idempotency-by-occurrence`

**Statement:** A scheduled job MUST use its occurrence identity as the command idempotency scope.

**Rationale:** The occurrence identity scopes the command idempotency key. Every retry of one occurrence then replays the same scheduled business action. A key scoped any wider would merge separate occurrences into one.

**Reviewed for:** Retries of one occurrence share the idempotency scope of its occurrence identity.

### Restrict Completion by Fencing Value

**ID:** `litespec/ext/jobs#restrict-completion-by-fencing-value`

**Statement:** A Worker MUST mark completion only while its fencing value remains current.

**Rationale:** The Worker marks completion only while its fencing value stays current. A stale value means another replica owns the occurrence now. Completing past that point would write one replica's result over another's ownership.

**Reviewed for:** Completion writes fail on a stale fencing value in the store.

### Audit Manual Replay

**ID:** `litespec/ext/jobs#audit-manual-replay`

**Statement:** A manual replay MUST create a new attempt for the same occurrence identity: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A manual replay creates a new attempt under the same occurrence identity: an audit record where audit is selected, a standalone record otherwise. The replay keeps the original scheduled instant and its history. A replay recorded as a new occurrence would rewrite when the work was due.

**Reviewed for:** Each manual replay adds an audited attempt under its original occurrence identity.

### Prefer UTC Schedules

**ID:** `litespec/ext/jobs#prefer-utc-schedules`

**Statement:** A scheduled job MUST use a UTC schedule unless its specification names a business-local time requirement and its IANA time zone.

**Rationale:** A local schedule runs twice on one date each year and skips an hour on another. A daylight-saving transition does that to a wall clock. That behavior is correct for a job whose meaning is local, such as a nightly statement at 9 AM for a reader. It is a defect for every other job. Naming the time zone in the specification makes the transition behavior reviewable.

**Reviewed for:** Each schedule states UTC or names a business-local time requirement with its IANA time zone.

### Define Business-Local Clock Behavior

**ID:** `litespec/ext/jobs#define-business-local-clock-behavior`

**Statement:** A business-local schedule MUST record an IANA time-zone identifier and define skipped and repeated local-time behavior.

**Rationale:** Daylight-saving transitions omit one local hour and repeat another. A business-local schedule records its IANA time-zone identifier and defines what happens to occurrences in the skipped and repeated hours. Without that definition, each transition silently drops or doubles the local work.

**Reviewed for:** Each business-local schedule records its IANA zone with skipped and repeated local-time behavior.

### Test Time Transitions

**ID:** `litespec/ext/jobs#test-time-transitions`

**Statement:** A scheduled-job test MUST use an injected clock and representative time-zone transitions.

**Rationale:** An injected clock lets tests move time to any transition on demand. Representative time-zone transitions then run deterministically instead of once a year in production. Tests on the real clock can only wait for the transition to arrive.

**Reviewed for:** Job tests inject the clock and cover representative time-zone transitions.

### Exclude Machine-Local Time

**ID:** `litespec/ext/jobs#exclude-machine-local-time`

**Statement:** Scheduled-job production code MUST NOT depend on machine-local time.

**Rationale:** Machine-local time follows each host's configuration. Production code that reads it schedules differently on a developer machine, in staging, and in production. Schedules read their time from the injected clock and the declared zone instead.

**Reviewed for:** Production job sources read no machine-local clock.

## Conventions

### Place Handlers with Owned Behavior

**ID:** `litespec/ext/jobs#place-handlers-with-owned-behavior`

**Default:** Create one job handler under the module that owns its behavior.

**Replacement:** A project keeps job handlers in one shared jobs folder.

**Rationale:** One job handler lives under the module that owns its behavior. The module owns both the business action and what its schedule means. A handlers folder far from the behavior separates each schedule from its reason.

**Reviewed for:** Each job handler sits under the module that owns its behavior, or the project keeps its handlers in one shared jobs folder.

### Use Execution Scopes

**ID:** `litespec/ext/jobs#use-execution-scopes`

**Default:** Resolve scoped dependencies inside an execution scope and dispose the scope after the Command completes.

**Replacement:** A project names another scope boundary for occurrence dependencies in its documentation.

**Rationale:** Each occurrence resolves its scoped dependencies inside one execution scope and disposes it after the Command completes. The scope isolates one occurrence's dependencies from the next. A shared scope would leak one occurrence's state into another.

**Reviewed for:** Each occurrence resolves scoped dependencies in one disposed execution scope, or the project names another scope boundary in its documentation.

### Separate Schedule Definitions

**ID:** `litespec/ext/jobs#separate-schedule-definitions`

**Default:** Keep schedule definitions separate from Command behavior.

**Replacement:** A project keeps schedule definitions beside their Command behavior.

**Rationale:** Timing policy changes when operations needs a new rhythm, and Command behavior changes when the business needs a new outcome. Keeping schedule definitions separate from Command behavior lets each change alone. A schedule buried in the handler makes every timing change a code change.

**Reviewed for:** Schedule definitions sit apart from Command behavior, or the project keeps them beside it.

## Dependencies

Packages: `None.`

Related extensions: `outbox`.

Guides: `None.`
