# Jobs and Queues

**Stack:** any
**Layer:** application

## Intent

Work that outlives a request runs in a Worker: scheduled jobs from a timetable and reports too heavy for a request, while people clear queued items through claimed listings. This page states how each kind records its work, runs it once, and recovers. A project selects it for recurring, delayed, heavy, or human-cleared work.

## Activation

Activation scope: `local`.

Applicable specification kinds: `job`, `use-case`, `journey`.

The consumer enables `jobs` for recurring, delayed, or calendar-based work. Each schedule lives on a job record with its `kind`, `schedule`, and run fields, as the [automation page](../core/automation.md) states. Selecting it replaces `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request` for scheduled work: a Worker runs the scheduler.

For human queues, a project selects `jobs` for a use case when the use case lists work items people claim or clears items in bulk. Ordinary reads and single-item commands do not select it.

For reports, a use case selects `jobs` for one documented query or export that the normal request path cannot serve: complex relational SQL, a large result, long processing, output in object storage, or a latency budget beyond a normal request. The use case page carries the measurements that prove the normal path falls short.

## Baseline Relationship

This extension replaces `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`. The baseline lets a project declare either the WebApi process or a Worker project as the execution host for work that outlives a request. A project selecting `jobs` runs scheduled work in a Worker outside WebApi request handling, under `litespec/ext/jobs#run-scheduled-jobs-in-worker`.

The job, setting, and runbook records belong to the core. Schedules bound their retention with setting records and operators recover through runbook records. The request budget and each report bound live in setting records.

A listing query states its `claim` mode and its `score`, while a bulk money command states its `preview` and its `confirmBy` code. Ordinary queries stay under `litespec/backend/persistence#query-through-the-read-session`; the report provisions govern only the reads whose cost or duration exceeds that path.

## Standards

### Carry the Schedule on the Job Record

**ID:** `litespec/ext/jobs#carry-the-schedule-on-the-job-record`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST carry its schedule in structured form with its timezone on its job record.

**Rationale:** The job record states when the work runs: `every` or `cron` with its timezone, so tools read the rhythm instead of parsing prose. A schedule kept outside the record runs with no recorded moment attached, and misfire behavior has no stated time to measure against. The [automation page](../core/automation.md) states the record fields, and the `schedule-invalid` check reports a schedule no tool can read.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a job schedule that is neither an `every` duration nor a `cron` expression; each scheduled job record states its timezone.

### Run One Command per Item in Selection

**ID:** `litespec/ext/jobs#run-one-command-per-item-in-selection`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A job MUST run one command per item in its `selection` within its `batchSize`, each in its own unit of work.

**Rationale:** The selection states what each run selects, the batch size bounds how many items one batch takes, and `runs` names the command run per item. Each item runs as one command in its own unit of work, as `litespec/backend/application#issue-per-record-work-from-a-job` requires: a failed item replays without replaying its batch.

**Reviewed for:** Each job run runs one command per item in its selection within its batch size, each in its own unit of work.

### Resume Driven Work Through the Cursor

**ID:** `litespec/ext/jobs#resume-driven-work-through-the-cursor`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A job that works through items in batches MUST name where resume state lives in its `cursor`.

**Rationale:** A batch that fails halfway resumes instead of restarting: the cursor holds where the run stopped. Without it, the next run replays finished items or skips unfinished ones, and a scenario covering the remainder resumes has no stated position to assert.

**Reviewed for:** Each batch job names its resume position in its cursor, with a scenario covering the remainder after a halfway failure.

### Run Manual Jobs Under Their Guard

**ID:** `litespec/ext/jobs#run-manual-jobs-under-their-guard`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A manual job run MUST stay inside its `runsWhere` under its `guard`.

**Rationale:** Manual work is interactive platform work with no aggregate, such as a rehearsal harness, so no model bounds where it runs or who runs it. `runsWhere` names the deployment scope it may touch and the guard names who may run it and when. The [automation page](../core/automation.md) states the record fields, and the `job-guard-missing` check reports a manual job with no guard.

**Checked by:** `litespec model check` reports `job-guard-missing` (level G) for a manual job with no `guard`; each manual run stays inside its `runsWhere`.

### Run Scheduled Jobs in Worker

**ID:** `litespec/ext/jobs#run-scheduled-jobs-in-worker`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST run in Worker outside WebApi request handling.

**Rationale:** Scheduled work runs in a Worker, outside WebApi request handling. The Worker owns the schedule's lifetime and shuts its replicas down in a controlled order. Request handling has neither a scheduler nor a shutdown order for background work.

**Reviewed for:** Worker sources hold every scheduled job, and WebApi request handling holds none.

### Observe Job Cancellation

**ID:** `litespec/ext/jobs#observe-job-cancellation`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST observe cancellation.

**Rationale:** Each job observes cancellation, so an occurrence ends its current step instead of running past the moment an operator asked it to stop.

**Reviewed for:** Each job handler threads cancellation into its waits and stops its current step when cancellation fires.

### Bound Job Duration

**ID:** `litespec/ext/jobs#bound-job-duration`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST complete within a bounded duration.

**Rationale:** Each job bounds its duration, so a Worker stops without stranding work it can no longer watch.

**Reviewed for:** Each job handler carries a duration bound, and the occurrence store shows completions inside it.

### Stop Jobs During Graceful Shutdown

**ID:** `litespec/ext/jobs#stop-jobs-during-graceful-shutdown`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST stop accepting new work during graceful shutdown.

**Rationale:** Each job stops accepting new work during graceful shutdown. An unbounded job turns every deploy into a wait for work that never ends.

**Reviewed for:** The Worker shutdown path stops schedule polling before it drains running occurrences.

### Store Execution Leases

**ID:** `litespec/ext/jobs#store-execution-leases`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST record lease owner, expiry, and fencing value in PostgreSQL.

**Rationale:** Each planned occurrence carries a lease with its owner, expiry, and fencing value in PostgreSQL. The lease names the one replica that may complete that occurrence. The `lease` field on the job record names the lock each run claims. The outbox extension cites this definition for its own claims.

**Reviewed for:** The lease rows in PostgreSQL carry owner, expiry, and fencing value for each occurrence.

### Renew Only Owned Leases

**ID:** `litespec/ext/jobs#renew-only-owned-leases`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A Worker replica MUST renew only its active lease and stop when renewal fails.

**Rationale:** A replica renews only the lease it holds and only while it stays active. A failed renewal means another replica owns the occurrence now. The loser stops instead of completing work it no longer owns.

**Reviewed for:** Replica logs show no renewal after a failed renewal, and the loser stops its occurrence.

### Prevent Duplicate Completion

**ID:** `litespec/ext/jobs#prevent-duplicate-completion`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job lease MUST prevent two replicas from completing the same occurrence.

**Rationale:** Overlapping replicas must not complete the same occurrence twice. The lease admits one completion per occurrence. Two completions would bill, notify, or reconcile the same planned work twice.

**Reviewed for:** The lease admits one completion per occurrence across overlapping replicas.

### Generate Monotonic Fencing Values

**ID:** `litespec/ext/jobs#generate-monotonic-fencing-values`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST generate each fencing value monotonically per occurrence.

**Rationale:** Each fencing value rises monotonically per occurrence, so a later claim always presents a higher value than an earlier one. A Worker that lost its lease presents a stale value, and `litespec/ext/jobs#restrict-completion-by-fencing-value` refuses completion on a stale value.

**Reviewed for:** Fencing values in the store rise per occurrence, and a stale value refuses completion.

### Scope Occurrences and Leases by Tenant

**ID:** `litespec/ext/jobs#scope-occurrences-and-leases-by-tenant`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job occurrence and its lease MUST carry tenant scope when the product isolates tenants.

**Rationale:** Products that isolate tenants scope each occurrence and its lease to one tenant. One tenant's schedules then never trigger, block, or expose another tenant's work. Unscoped leases would let a replica mix tenants inside one occurrence.

**Reviewed for:** Occurrence and lease rows carry tenant scope in products that isolate tenants.

### Dispatch Application Behavior

**ID:** `litespec/ext/jobs#dispatch-application-behavior`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST dispatch an Application Command or a declared Application port.

**Rationale:** The job record names the use cases it runs, and the use case boundary lives in Application. Each job therefore dispatches an Application Command or a declared Application port. The schedule stays a trigger while the behavior stays owned.

**Reviewed for:** Each job handler calls an Application Command or a declared Application port.

### Exclude Direct Storage and HTTP Execution

**ID:** `litespec/ext/jobs#exclude-direct-storage-and-http-execution`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST NOT mutate a Marten session or DbContext directly, bypass Domain behavior, or call WebApi endpoints.

**Rationale:** A job that mutates a Marten session or DbContext directly, bypasses Domain behavior, or calls WebApi endpoints skips the command boundary. That boundary carries authorization, transaction, and validation the job would then lack. Jobs enter through Commands like every other caller.

**Reviewed for:** Job sources mutate no Marten session or DbContext and call no WebApi endpoint.

### Bound Job Retries

**ID:** `litespec/ext/jobs#bound-job-retries`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use bounded attempts, cancellation-aware backoff, and a poison state for permanent failure.

**Rationale:** Bounded attempts with cancellation-aware backoff cap what one failed occurrence consumes. A poison state parks the permanently failing occurrence instead of retrying it forever. Without the bound, one poison occurrence eats the Worker's whole capacity.

**Reviewed for:** Each schedule declares bounded attempts with cancellation-aware backoff and a poison state.

### Tolerate Duplicate Execution

**ID:** `litespec/ext/jobs#tolerate-duplicate-execution`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job Command and side effect MUST tolerate duplicate execution after lease or process failure.

**Rationale:** A lease or process failure can replay a job whose completion stayed uncertain. The job Command and its side effects tolerate that duplicate execution. Work that assumes exactly-once breaks on the first uncertain failure.

**Reviewed for:** Job Commands and side effects show one accepted effect after a replayed occurrence.

### Treat Unavailable Schedule Stores as Outages

**ID:** `litespec/ext/jobs#treat-unavailable-schedule-stores-as-outages`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A Worker MUST treat an unavailable scheduler store as the store outage `litespec/ext/outbox#distinguish-dependency-outage` defines, with backed-off, rate-limited logging.

**Rationale:** An unavailable scheduler store is an outage of the dependency, not a failed occurrence. The Worker treats it as the store outage `litespec/ext/outbox#distinguish-dependency-outage` defines, with backed-off, rate-limited logging. Retry and poison outcomes belong to claimed occurrences, not to a store nobody can reach.

**Reviewed for:** Worker logs during a store outage show backed-off, rate-limited entries and no poisoned occurrences.

### Persist Occurrence Recovery Points

**ID:** `litespec/ext/jobs#persist-occurrence-recovery-points`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST persist each occurrence's last scheduled, started, and completed time.

**Rationale:** Last scheduled, started, and completed times persist per occurrence. After a restart, those points separate missed work from completed work. Memory alone cannot make that split, because the restart wipes it.

**Reviewed for:** Each occurrence row carries last scheduled, started, and completed times.

### Define Misfire Recovery

**ID:** `litespec/ext/jobs#define-misfire-recovery`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job specification MUST define skipped, once-replayed, or interval-replayed behavior for missed occurrences.

**Rationale:** Downtime and delay leave occurrences past their time. The specification states whether each missed occurrence is skipped, replayed once, or replayed per interval. The choice controls the business effect: a skipped reminder stays silent while a replayed one arrives late.

**Reviewed for:** Each schedule states skipped, once-replayed, or interval-replayed behavior for missed occurrences.

### Provide Operational Recovery Procedures

**ID:** `litespec/ext/jobs#provide-operational-recovery-procedures`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** Each schedule MUST have Runbook records for inspection, manual replay, and disable.

**Rationale:** Stalled, failed, or unsafe schedules need controlled operator actions: inspection, manual replay, and disable. Runbook records carry those procedures. An operator without them improvises against the store during the incident.

Each procedure names the interface that performs it. A procedure that says "disable the schedule" without naming a command, an endpoint, or a configuration value remains undiscovered until the incident it was written for. Avoid an interface that writes to the schedule store directly, because it bypasses the occurrence identity `litespec/ext/jobs#derive-stable-occurrence-identity` depends on.

**Example:** A disable procedure names the configuration value or the administrative endpoint that stops the schedule. It states what happens to an occurrence already claimed.

**Reviewed for:** Runbook records cover inspection, manual replay, and disable for each schedule.

### Derive Stable Occurrence Identity

**ID:** `litespec/ext/jobs#derive-stable-occurrence-identity`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST derive each occurrence identity from its schedule ID and scheduled instant.

**Rationale:** Schedule ID plus scheduled instant gives each planned time one stable identity across retries and replicas. Retries of one occurrence then share their idempotency scope, and two replicas recognize the same occurrence.

**Reviewed for:** Occurrence identities in the store combine schedule ID with scheduled instant.

### Remove Occurrences Past Retention

**ID:** `litespec/ext/jobs#remove-occurrences-past-retention`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST remove occurrences older than the schedule's retention setting record.

**Rationale:** Occurrences older than the retention setting record leave the store so the key space stays bounded.

The key space grows with the schedule's frequency and its retention. A per-second schedule kept for a year holds more than 30 million rows. [Quartz states the same practice](https://quartz-scheduler.net/documentation/best-practices.html) for a persistent job store.

**Example:** A daily schedule that retains occurrences for 90 days holds 90 rows. A minute schedule with the same retention holds more than 100,000 rows. That is a different storage decision.

**Reviewed for:** The store holds no occurrence older than its schedule's retention setting record.

### Persist Occurrence State

**ID:** `litespec/ext/jobs#persist-occurrence-state`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST persist scheduled, processing, completed, and dead-letter state with safe execution details.

**Rationale:** Scheduled, processing, completed, and dead-letter states persist with attempt, lease, start, completion, and safe error data. The persisted state tells operators and recovery code where each occurrence stands. A store without states cannot separate work in flight from work never started.

**Reviewed for:** Each occurrence row carries scheduled, processing, completed, or dead-letter state with safe execution details.

### Scope Command Idempotency by Occurrence

**ID:** `litespec/ext/jobs#scope-command-idempotency-by-occurrence`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use its occurrence identity as the command idempotency scope.

**Rationale:** The occurrence identity scopes the command idempotency key. Every retry of one occurrence then replays the same scheduled business action. A key scoped any wider would merge separate occurrences into one.

**Reviewed for:** Retries of one occurrence share the idempotency scope of its occurrence identity.

### Restrict Completion by Fencing Value

**ID:** `litespec/ext/jobs#restrict-completion-by-fencing-value`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A Worker MUST mark completion only while its fencing value remains current.

**Rationale:** The Worker marks completion only while its fencing value stays current. A stale value means another replica owns the occurrence now. Completing past that point would write one replica's result over another's ownership.

**Reviewed for:** Completion writes fail on a stale fencing value in the store.

### Audit Manual Replay

**ID:** `litespec/ext/jobs#audit-manual-replay`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A manual replay MUST create a new attempt for the same occurrence identity: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A manual replay creates a new attempt under the same occurrence identity: an audit record where audit is selected, a standalone record otherwise. The replay keeps the original scheduled instant and its history. A replay recorded as a new occurrence would rewrite when the work was due.

**Reviewed for:** Each manual replay adds an audited attempt under its original occurrence identity.

### Prefer UTC Schedules

**ID:** `litespec/ext/jobs#prefer-utc-schedules`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use a UTC schedule unless its specification names a business-local time requirement and its IANA time zone.

**Rationale:** A local schedule runs twice on one date each year and skips an hour on another. A daylight-saving transition does that to a wall clock. That behavior is correct for a job whose meaning is local, such as a nightly statement at 9 AM for a reader. It is a defect for every other job. Naming the time zone in the specification states the transition behavior for a later reader.

**Reviewed for:** Each schedule states UTC or names a business-local time requirement with its IANA time zone.

### Define Business-Local Clock Behavior

**ID:** `litespec/ext/jobs#define-business-local-clock-behavior`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A business-local schedule MUST record an IANA time-zone identifier and define skipped and repeated local-time behavior.

**Rationale:** Daylight-saving transitions omit one local hour and repeat another. A business-local schedule records its IANA time-zone identifier and defines what happens to occurrences in the skipped and repeated hours. Without that definition, each transition silently drops or doubles the local work.

**Reviewed for:** Each business-local schedule records its IANA zone with skipped and repeated local-time behavior.

### Test Time Transitions

**ID:** `litespec/ext/jobs#test-time-transitions`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** A scheduled-job test MUST use an injected clock and representative time-zone transitions.

**Rationale:** An injected clock lets tests move time to any transition on demand. Representative time-zone transitions then run deterministically instead of once a year in production. Tests on the real clock can only wait for the transition to arrive.

**Reviewed for:** Job tests inject the clock and cover representative time-zone transitions.

### Exclude Machine-Local Time

**ID:** `litespec/ext/jobs#exclude-machine-local-time`

**Strength:** rule

**Applies to:** artifact: job

**Statement:** Scheduled-job production code MUST NOT depend on machine-local time.

**Rationale:** Machine-local time follows each host's configuration. Production code that reads it schedules differently on a developer machine, in staging, and in production. Schedules read their time from the injected clock and the declared zone instead.

**Reviewed for:** Production job sources read no machine-local clock.

### State the Claim Mode

**ID:** `litespec/ext/jobs#state-the-claim-mode`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST state its claim mode as disabled, optional, or required.

**Rationale:** Two people clearing the same item double-pay, double-approve, or double-refuse. The claim mode says whether items need no claim, accept an optional hold, or require one before action, so the queue never leaves two hands on one item.

**Example:** The payout queue requires a claim before release, while the review queue leaves claiming optional.

**Reviewed for:** The queue use cases state disabled, optional, or required on their claim field.

### Order Queue Items by Score

**ID:** `litespec/ext/jobs#order-queue-items-by-score`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST order its items by a score that cites a calculation rule.

**Rationale:** A queue without an order is cleared by habit: newest first, loudest first, easiest first. A score citing a calculation rule pins the order to declared inputs, so the riskiest or most valuable item surfaces first by rule instead of by accident.

**Example:** The dispute queue orders by `rule/dispute.urgency-score`, which weighs amount, age, and evidence gaps.

**Reviewed for:** The queue listings sort by a score citing the calculation rule behind it.

### Show the Score Beside Each Item

**ID:** `litespec/ext/jobs#show-the-score-beside-each-item`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A queue listing MUST show each item score with the inputs that set it.

**Rationale:** A hidden score is an oracle: people work around an order they cannot see or trust. Showing the score with its inputs lets each person check the order and challenge the inputs instead of the queue.

**Example:** Each dispute row shows `urgency 82: EUR 1,900, 6 days old, evidence missing`.

**Reviewed for:** The queue rows display each score with the inputs that set it.

### Release Claims Past Their Lease

**ID:** `litespec/ext/jobs#release-claims-past-their-lease`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A claimed item MUST return to the queue when its claim lease expires.

**Rationale:** Claims are held by people who go home: a claim with no expiry parks the item forever. The lease returns stale claims to the queue, so an abandoned item waits for the next person instead of for nobody.

**Example:** A payout claimed Friday and untouched returns to the queue when its one-hour lease expires.

**Reviewed for:** The claim tests show untouched items returning after their lease expires.

### Show a Preview Before Bulk Money Actions

**ID:** `litespec/ext/jobs#show-a-preview-before-bulk-money-actions`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST render a preview of every affected item before acting.

**Rationale:** Bulk money moves on many items at once, and one wrong filter multiplies one mistake into hundreds. The preview lists every affected item with its amount, so the person confirms the set they meant instead of the set they typed.

**Example:** The bulk release previews forty-two payouts totaling EUR 18,400 before asking for confirmation.

**Reviewed for:** The bulk money commands render every affected item with its amount before acting.

### Confirm Bulk Money Actions Out of Band

**ID:** `litespec/ext/jobs#confirm-bulk-money-actions-out-of-band`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST require its `confirmBy` code from outside the session.

**Rationale:** A preview confirms the set, but the session itself may be hijacked, shared, or left open. An out-of-band code arrives on a channel the session does not control, so the confirmation proves the person, not just the browser.

**Example:** The bulk release asks for the six-digit code sent to the finance phone before moving any money.

**Reviewed for:** The bulk money commands ask for the out-of-band code before moving money.

### Record Reporting Need

**ID:** `litespec/ext/jobs#record-reporting-need`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A reporting proposal MUST record the report, representative data volume, plan or limitation, latency target, maximum result, and operator.

**Rationale:** Reporting infrastructure costs a Worker, storage, and operating attention, so the team writes down the report, a representative data volume, the plan or limitation, the latency target, the maximum result, and the operator before building any of it. The use case page carries that record, and it shows why a normal request projection cannot meet the boundary.

**Reviewed for:** The use case page holds the report, volume, plan, target, maximum, and operator.

### Avoid Speculative Reporting Systems

**ID:** `litespec/ext/jobs#avoid-speculative-reporting-systems`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A project MUST NOT create reporting infrastructure for possible future analytics.

**Rationale:** A reporting system built for analytics the product may never need charges its full operating cost from day one while serving no accepted query. Each report or export earns its infrastructure with a current cost boundary, not a guess about future analysis.

**Reviewed for:** Each reporting system traces to an accepted current query or export.

### Parameterize Report SQL

**ID:** `litespec/ext/jobs#parameterize-report-sql`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A reporting query MUST parameterize every actor, tenant, filter, range, sort, and pagination value.

**Rationale:** A report concatenates actor, tenant, filters, ranges, sorting, and pagination into SQL that reads across the database. Binding every one of those values as a parameter keeps tenant and actor isolation intact and closes the injection hole string concatenation opens.

**Reviewed for:** Each report query binds actor, tenant, filter, range, sort, and pagination values as parameters in code.

### Keep Raw SQL in Infrastructure

**ID:** `litespec/ext/jobs#keep-raw-sql-in-infrastructure`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** Infrastructure MUST own raw reporting SQL and connection handling.

**Rationale:** Raw SQL names tables, dialects, and connection handling that belong to one provider. Keeping it in Infrastructure lets Application and Domain state what the report means without depending on how one database phrases it.

**Reviewed for:** Raw report SQL and connection handling sit in Infrastructure code.

### Review Report Plans

**ID:** `litespec/ext/jobs#review-report-plans`

**Strength:** advice

**Applies to:** artifact: record:use-case

**Statement:** Each report SHOULD have its query plans and indexes reviewed against representative data.

**Deviation:** A report over a small fixed data set justifies skipping the review.

**Rationale:** A query that flies over a hundred local rows can scan millions in production. The team reviews query plans and indexes against representative data, so the true cost shows before the report ships rather than after it slows the database.

**Reviewed for:** The review record shows query plans and indexes checked against representative data.

### Apply Item-Read Authorization

**ID:** `litespec/ext/jobs#apply-item-read-authorization`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A report or export MUST enforce actor, role, ownership, tenant, and sensitive-field rules that apply to item reads.

**Rationale:** A report that reads ten thousand rows at once crosses the same actor, role, ownership, tenant, and sensitive-field boundaries as ten thousand single reads. Enforcing the item-read rules on the bulk path stops the report from exposing rows its reader may not open one by one.

**Reviewed for:** Each report and export enforces the item-read rules in the authorization tests.

### Run Budget-Exceeding Exports in Worker

**ID:** `litespec/ext/jobs#run-budget-exceeding-exports-in-worker`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export exceeding its request budget MUST run through Worker, with the budget a setting record the export's use case names.

**Rationale:** An export that outlasts its request budget holds an HTTP connection open while the reader waits and the server counts threads. Running it in a Worker returns the request immediately, and the budget in its setting record tells the team exactly when an export outgrows the request path.

**Reviewed for:** Each export past its request budget runs through a Worker, with the budget in its setting record.

### Protect Export Output

**ID:** `litespec/ext/jobs#protect-export-output`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export worker MUST store output in approved object storage with a named contact, an expiry, and a short-lived authorized download.

**Rationale:** An export file concentrates sensitive rows into one downloadable object that outlives the request that built it. Approved object storage with a named contact, an expiry, and a short-lived authorized download gives that object controlled access and a defined end instead of a permanent public link.

**Reviewed for:** Export output sits in approved object storage with a named contact, an expiry, and a short-lived authorized download.

### Scope Export Output to the Tenant

**ID:** `litespec/ext/jobs#scope-export-output-to-the-tenant`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export MUST store its output under a tenant-scoped object-storage path.

**Rationale:** Export addresses are predictable, and a shared folder lets one tenant guess another tenant's file name. A tenant-scoped storage path puts each tenant's exports under its own prefix, so a guessed address stays inside the guesser's own data.

**Reviewed for:** Each export stores its output under a tenant-scoped path in object storage.

### Define Report Limits

**ID:** `litespec/ext/jobs#define-report-limits`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A report specification MUST state each bound as a setting record: maximum date range, rows, execution time, concurrent jobs, output size, and storage lifetime.

**Rationale:** Date range, rows, execution time, concurrent jobs, output size, and storage lifetime each cap one dimension of report cost. Setting records hold all six, so a runaway report hits a documented bound instead of an exhausted database, a full disk, or a surprise invoice.

**Reviewed for:** Each report bound lives as a setting record beside the report.

### Declare Export Paging

**ID:** `litespec/ext/jobs#declare-export-paging`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export query MUST state `paging` with `mode` of `pages` or `none` with its reason.

**Rationale:** An export reads many rows at once, so it states how the read is bounded: `{"mode": "pages", ...}` bounds each run, and `{"mode": "none", "reason": "..."}` states why the export reads all rows at once. Without the declaration, a reviewer cannot tell a bounded export from one that grows with the table.

**Reviewed for:** Which export queries lack paging with mode pages or mode none with its reason? Look at the export query beside the report.

### Audit Export Reads

**ID:** `litespec/ext/jobs#audit-export-reads`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export query MUST state its `audit` reason and target.

**Rationale:** An export file concentrates sensitive rows into one downloadable object, so the use case states why the bulk read is audited and where the audit lands. Without the stated audit, the concentrated read leaves no trace a reviewer can follow.

**Reviewed for:** Which export queries lack a stated `audit` reason and target? Look at the export query beside the report.

### Support Report Cancellation

**ID:** `litespec/ext/jobs#support-report-cancellation`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A report implementation MUST support cancellation and remove expired outputs.

**Rationale:** A reader who closes the tab leaves the report running, and an export nobody downloads still occupies storage. Cancellation stops the abandoned work, and expiry removes the abandoned file, so neither consumes resources indefinitely.

**Reviewed for:** Report work cancels on request and expired output clears in the cancellation tests.

### Escape Formula-Leading Export Values

**ID:** `litespec/ext/jobs#escape-formula-leading-export-values`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** A CSV or spreadsheet export MUST escape formula-leading values unless the team intentionally fills a field with a formula.

**Rationale:** A value starting with `=`, `+`, `-`, or `@` becomes a live formula when the export opens in a spreadsheet, running in the opener's application with the opener's data. Escaping those values keeps data as data, except in a field the team intentionally fills with a formula.

**Reviewed for:** Exported values starting a formula are escaped, except in formula fields the team fills intentionally.

### Define Export Encoding and Columns

**ID:** `litespec/ext/jobs#define-export-encoding-and-columns`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** An export format MUST use explicit character encoding and a stable column contract.

**Rationale:** An export without a stated encoding arrives garbled on the first machine that guesses differently, and columns that shift per release break every parser built on them. Explicit encoding with a stable column contract lets consumers parse the file without guessing either.

**Reviewed for:** Each export states its character encoding and stable columns in its format record.

## Conventions

### Place Handlers with Owned Behavior

**ID:** `litespec/ext/jobs#place-handlers-with-owned-behavior`

**Strength:** default

**Applies to:** artifact: job

**Default:** Create one job handler under the module that owns its behavior.

**Replacement:** A project keeps job handlers in one shared jobs folder.

**Rationale:** One job handler lives under the module that owns its behavior. The module owns both the business action and what its schedule means. A handlers folder far from the behavior separates each schedule from its reason.

**Reviewed for:** Each job handler sits under the module that owns its behavior, or the project keeps its handlers in one shared jobs folder.

### Use Execution Scopes

**ID:** `litespec/ext/jobs#use-execution-scopes`

**Strength:** default

**Applies to:** artifact: job

**Default:** Resolve scoped dependencies inside an execution scope and dispose the scope after the Command completes.

**Replacement:** A project names another scope boundary for occurrence dependencies in its documentation.

**Rationale:** Each occurrence resolves its scoped dependencies inside one execution scope and disposes it after the Command completes. The scope isolates one occurrence's dependencies from the next. A shared scope would leak one occurrence's state into another.

**Reviewed for:** Each occurrence resolves scoped dependencies in one disposed execution scope, or the project names another scope boundary in its documentation.

### Separate Schedule Definitions

**ID:** `litespec/ext/jobs#separate-schedule-definitions`

**Strength:** default

**Applies to:** artifact: job

**Default:** Keep schedule definitions separate from Command behavior.

**Replacement:** A project keeps schedule definitions beside their Command behavior.

**Rationale:** Timing policy changes when operations needs a new rhythm, and Command behavior changes when the business needs a new outcome. Keeping schedule definitions separate from Command behavior lets each change alone. A schedule buried in the handler makes every timing change a code change.

**Reviewed for:** Schedule definitions sit apart from Command behavior, or the project keeps them beside it.

### Order Unscored Queues by Age

**ID:** `litespec/ext/jobs#order-unscored-queues-by-age`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** A queue without a score lists its oldest item first.

**Replacement:** A project names its fallback queue order in its queue documentation.

**Rationale:** Age is the honest fallback: the item waiting longest goes first. Any other fallback works when the queue states it.

**Reviewed for:** Queues without a score list oldest first, or the queue documentation names another fallback order.

### Keep Report Definitions in Application

**ID:** `litespec/ext/jobs#keep-report-definitions-in-application`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep report definitions in Application under their module.

**Replacement:** A consumer records a report-definition convention naming where its report definitions live.

**Rationale:** What a report means and who may run it are business choices. Keeping the definition in Application under its module puts those choices beside the module's other rules instead of buried in query code.

**Reviewed for:** Report definitions sit in their module, or the report-definition convention names where they live.

### Keep Report Providers in Infrastructure

**ID:** `litespec/ext/jobs#keep-report-providers-in-infrastructure`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep SQL, row mappings, and storage providers in Infrastructure.

**Replacement:** A consumer records a report-provider convention naming where its SQL, mappings, and storage code live.

**Rationale:** SQL text, row mappings, and storage calls change per provider while the report's meaning stays fixed. Keeping them in Infrastructure swaps the provider without touching the report's business rules.

**Reviewed for:** SQL and storage code sit in Infrastructure, or the report-provider convention names where they live.

### Separate Worker Orchestration

**ID:** `litespec/ext/jobs#separate-worker-orchestration`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep Worker orchestration separate from report rules.

**Replacement:** A consumer records a report-orchestration convention naming where its scheduling and progress tracking live.

**Rationale:** Scheduling, chunking, and progress tracking run the report without defining it. Keeping Worker orchestration apart from the report's rules lets either change without dragging the other along.

**Reviewed for:** Worker orchestration sits apart from report rules, or the report-orchestration convention names where it lives.

## Dependencies

Packages: This extension selects no report or object-storage package. Each provider the project uses needs a decision and a manifest pin.

Related extensions: `outbox`.

Guides: None.
