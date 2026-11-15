# Jobs and Queues

**Stack:** any
**Concern:** application

Work that outlives a request runs in a Worker: scheduled jobs from a timetable and reports too heavy for a request, while people clear queued items through claimed listings. This page states how each kind records its work, runs it once, and recovers. A project selects it for recurring, delayed, heavy, or human-cleared work.

## Activation

Activation scope: `local`.

Applicable specification kinds: `job`, `use-case`, `flow`.

Enable `jobs` for recurring, delayed, or calendar-based work. Each schedule lives on a job record with its `kind`, `schedule`, and run fields, as the [automation page](../core/automation.md) states. Selecting the extension replaces `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request` for scheduled work: a Worker runs the scheduler.

For human queues, select `jobs` for a use case when the use case lists work items people claim, or clears items in bulk. Ordinary reads and single-item commands don't select it.

For reports, select `jobs` on a use case for one documented query or export that the normal request path can't serve. The use case qualifies when any of the following holds:

- Complex relational SQL.
- A large result.
- Long processing.
- Output in object storage.
- A latency budget beyond a normal request.

The use case page carries the measurements that prove the normal path falls short.

## Baseline Relationship

The baseline lets a project declare either the WebApi process or a Worker project as the execution host for work that outlives a request. This extension replaces that choice for scheduled work with one rule: a project selecting `jobs` runs its scheduled work in a Worker outside WebApi request handling, under `litespec/ext/jobs#run-scheduled-jobs-in-worker`. It replaces one baseline provision, `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request`.

The job, setting, and runbook records belong to the core, not to this extension. Schedules bound their retention with setting records, operators recover through runbook records, and the request budget and each report bound live in setting records.

A listing query states its `claim` mode and its `score`, while a bulk money command states its `preview` and its `confirmBy` code. Ordinary queries stay under `litespec/backend/persistence#query-through-the-read-session`; the report provisions govern only the reads whose cost or duration exceeds that path.

## Standards

### Record the Schedule on the Job Record

**ID:** `litespec/ext/jobs#record-the-schedule-on-the-job-record`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST record its schedule in structured form with its timezone on its job record.

**Rationale:** Tools can't parse prose for a schedule. The job record states when the work runs as `every` or `cron` with its timezone, which tools read directly. A schedule kept outside the record runs with no recorded moment attached, so misfire behavior has no stated time to measure against. The [automation page](../core/automation.md) states the record fields, and the `schedule-invalid` check reports a schedule no tool can read.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a job schedule that is neither an `every` duration nor a `cron` expression; each scheduled job record states its timezone.

### Run One Command per Selected Item

**ID:** `litespec/ext/jobs#run-one-command-per-selected-item`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A job MUST run one command per item in its `selection` within its `batchSize`, each in its own unit of work.

**Rationale:** The selection states what each run selects, the batch size bounds how many items one batch takes, and `runs` names the command run per item. Each item runs as one command in its own unit of work, as `litespec/backend/application#issue-per-record-work-from-a-job` requires, so a failed item replays without replaying its batch.

**Reviewed for:** Does every job run one command per item in its `selection` within its `batchSize`, each in its own unit of work? Check the job record selection and the handler code.

### Resume Batch Work from Its Cursor

**ID:** `litespec/ext/jobs#resume-batch-work-from-its-cursor`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A job that works through items in batches MUST name where resume state lives in its `cursor`.

**Rationale:** A batch that fails halfway resumes instead of restarting. The cursor holds where the run stopped. Without it, the next run replays finished items or skips unfinished ones, and a scenario covering the remainder has no stated position to assert.

**Reviewed for:** Does every batch job name its resume position in its cursor, with a scenario for the remainder after a halfway failure? Check the job records and the scenario tests.

### Run Scheduled Jobs in Worker

**ID:** `litespec/ext/jobs#run-scheduled-jobs-in-worker`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST run in Worker outside WebApi request handling.

**Rationale:** Scheduled work needs two things request handling doesn't have: a scheduler that owns each schedule's lifetime, and a shutdown order that stops replicas in a controlled way. That is why scheduled work runs in a Worker, outside WebApi request handling.

**Reviewed for:** Does every scheduled job run in the Worker sources, outside WebApi request handling? Check the Worker project and the WebApi endpoints.

### Observe Job Cancellation

**ID:** `litespec/ext/jobs#observe-job-cancellation`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST observe cancellation.

**Rationale:** Each job observes cancellation, so an occurrence ends its current step instead of running past the moment an operator asked it to stop.

**Reviewed for:** Does every job handler wait with cancellation and end its current step when cancellation fires? Check the handler code and its cancellation tests.

### Limit Job Duration

**ID:** `litespec/ext/jobs#limit-job-duration`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST complete within a bounded duration.

**Rationale:** Each job bounds its duration, so a Worker stops without stranding work it can no longer watch.

**Reviewed for:** Does every job declare a duration bound, with completions recorded inside it? Check the handler code and the occurrence store.

### Stop Jobs During Graceful Shutdown

**ID:** `litespec/ext/jobs#stop-jobs-during-graceful-shutdown`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST stop accepting new work during graceful shutdown.

**Rationale:** A job that keeps accepting work during shutdown turns every deploy into a wait for work that never ends. Stopping intake lets the running occurrences drain, and the Worker stop.

**Reviewed for:** Does every Worker shutdown path drain running occurrences before stopping schedule polling? Check the shutdown code in the Worker.

### Store Execution Leases

**ID:** `litespec/ext/jobs#store-execution-leases`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST record lease owner, expiry, and fencing value in PostgreSQL.

**Rationale:** Each planned occurrence carries a lease with its owner, expiry, and fencing value in PostgreSQL. The lease names the one replica that may complete that occurrence. The `lease` field on the job record names the lock each run claims. The outbox extension cites this definition for its own claims.

**Reviewed for:** Does every lease row in PostgreSQL carry an owner, an expiry, and a fencing value? Check the lease table schema.

### Renew Only Owned Leases

**ID:** `litespec/ext/jobs#renew-only-owned-leases`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A Worker replica MUST renew only its active lease and stop when renewal fails.

**Rationale:** A failed renewal means another replica owns the occurrence now. A replica that kept going would complete work it no longer owns. The loser stops instead, so each replica renews only the lease it holds, and only while it stays active.

**Reviewed for:** Does any replica renew a lease after a failed renewal, or keep running an occurrence it lost? Check the replica logs and the renewal tests.

### Prevent Duplicate Completion

**ID:** `litespec/ext/jobs#prevent-duplicate-completion`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job lease MUST prevent two replicas from completing the same occurrence.

**Rationale:** Overlapping replicas must not complete the same occurrence twice. The lease admits one completion per occurrence. Two completions would bill, notify, or reconcile the same planned work twice.

**Reviewed for:** Does any lease path admit more than one completion per occurrence across overlapping replicas? Check the lease claim code and its concurrency tests.

### Generate Monotonic Fencing Values

**ID:** `litespec/ext/jobs#generate-monotonic-fencing-values`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST generate each fencing value monotonically per occurrence.

**Rationale:** Each fencing value rises monotonically per occurrence, so a later claim always presents a higher value than an earlier one. A Worker that lost its lease presents a stale value, and `litespec/ext/jobs#allow-completion-only-with-a-current-fencing-value` refuses completion on a stale value.

**Reviewed for:** Does every fencing value in the store rise per occurrence, with no stale value completing? Check the store write path and its fencing tests.

### Scope Occurrences and Leases by Tenant

**ID:** `litespec/ext/jobs#scope-occurrences-and-leases-by-tenant`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job occurrence and its lease MUST carry tenant scope when the product isolates tenants.

**Rationale:** In a product that isolates tenants, one tenant's schedules must trigger, block, and expose only that tenant's work. Scoping each occurrence and its lease to one tenant keeps that boundary. Unscoped leases would let a replica mix tenants inside one occurrence.

**Reviewed for:** Does every occurrence and lease row carry tenant scope in a product that isolates tenants? Check the occurrence and lease table schemas.

### Dispatch Jobs Through Application Commands or Ports

**ID:** `litespec/ext/jobs#dispatch-jobs-through-application-commands-or-ports`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST dispatch an Application Command or a declared Application port.

**Rationale:** The job record names the use case it runs, and the use case boundary lives in Application. Each job therefore dispatches an Application Command or a declared Application port. The schedule stays a trigger while the behavior stays owned.

**Reviewed for:** Does every job handler call only an Application Command or a declared Application port? Check the handler calls.

### Keep Jobs off Direct Storage and Web Endpoints

**ID:** `litespec/ext/jobs#keep-jobs-off-direct-storage-and-web-endpoints`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST NOT mutate a Marten session or DbContext directly, bypass Domain behavior, or call WebApi endpoints.

**Rationale:** A job that mutates a Marten session or DbContext directly, bypasses Domain behavior, or calls WebApi endpoints skips the command boundary. That boundary carries the authorization, transaction, and validation the job needs. Jobs enter through use case messages like every other caller.

**Reviewed for:** Is every job source free of direct Marten session or DbContext mutations and WebApi endpoint calls? Check the job sources and their imports.

### Limit Job Retries with Backoff and a Poison State

**ID:** `litespec/ext/jobs#limit-job-retries-with-backoff-and-a-poison-state`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use bounded attempts, cancellation-aware backoff, and a poison state for permanent failure.

**Rationale:** Without the bound, one poison occurrence eats the Worker's whole capacity. Bounded attempts with cancellation-aware backoff cap what one failed occurrence consumes, and a poison state parks the permanently failing occurrence instead of retrying it forever.

**Reviewed for:** Does every schedule declare bounded attempts, cancellation-aware backoff, and a poison state? Check the schedule records and the retry code.

### Tolerate Duplicate Execution

**ID:** `litespec/ext/jobs#tolerate-duplicate-execution`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job Command and side effect MUST tolerate duplicate execution after lease or process failure.

**Rationale:** A lease or process failure can replay a job whose completion stayed uncertain. The job Command and its side effects tolerate that duplicate execution. Work that assumes exactly-once execution breaks on the first uncertain failure.

**Reviewed for:** Does any job Command or side effect record more than one accepted effect after a replayed occurrence? Check the replay tests.

### Treat Unavailable Schedule Stores as Outages

**ID:** `litespec/ext/jobs#treat-unavailable-schedule-stores-as-outages`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A Worker MUST treat an unavailable scheduler store as the store outage `litespec/ext/outbox#report-store-outages-separately-from-delivery-failures` defines, with backed-off, rate-limited logging.

**Rationale:** An unavailable scheduler store is an outage of the dependency, not a failed occurrence. The Worker treats it as the store outage `litespec/ext/outbox#report-store-outages-separately-from-delivery-failures` defines, with backed-off, rate-limited logging. Retry and poison outcomes belong to claimed occurrences, not to a store nobody can reach.

**Reviewed for:** During a store outage, is every Worker log entry backed off and rate-limited, with no occurrence poisoned? Check the Worker logs and the outage tests.

### Persist Occurrence Recovery Points

**ID:** `litespec/ext/jobs#persist-occurrence-recovery-points`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST persist each occurrence's last scheduled, started, and completed time.

**Rationale:** After a restart, the Worker must tell missed work from completed work. Last scheduled, started, and completed times persist per occurrence to draw that line. Memory alone can't hold it, because the restart wipes it.

**Reviewed for:** Does every occurrence row carry a last scheduled, started, and completed time? Check the occurrence table schema.

### State How Missed Occurrences Recover

**ID:** `litespec/ext/jobs#state-how-missed-occurrences-recover`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job specification MUST define skipped, once-replayed, or interval-replayed behavior for missed occurrences.

**Rationale:** Downtime and delay leave occurrences past their time. The specification states whether each missed occurrence is skipped, replayed once, or replayed per interval. The choice controls the business effect: a skipped reminder stays silent, while a replayed one arrives late.

**Reviewed for:** Does every schedule state skipped, once-replayed, or interval-replayed behavior for missed occurrences? Check each schedule record.

### Write Runbooks for Inspection, Replay, and Disable

**ID:** `litespec/ext/jobs#write-runbooks-for-inspection-replay-and-disable`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Each schedule MUST have Runbook records for inspection, manual replay, and disable.

**Rationale:** Stalled, failed, or unsafe schedules need controlled operator actions: inspection, manual replay, and disable. Runbook records carry those procedures, because an operator without them improvises against the store during the incident.

Each procedure names the interface that performs it: a command, an endpoint, or a configuration value. A procedure that says "disable the schedule" without naming the switch stays undiscovered until the incident it was written for. Writing straight to the schedule store instead would bypass the occurrence identity `litespec/ext/jobs#derive-stable-occurrence-identity` depends on.

**Example:** Claimwick's disable procedure names the configuration value or the administrative endpoint that stops the schedule, and states what happens to an occurrence already claimed.

**Reviewed for:** Does every schedule have a runbook record for inspection, manual replay, and disable? Check the runbook records beside the schedules.

### Derive Stable Occurrence Identity

**ID:** `litespec/ext/jobs#derive-stable-occurrence-identity`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST derive each occurrence identity from its schedule ID and scheduled instant.

**Rationale:** Schedule ID plus scheduled instant gives each planned time one stable identity across retries and replicas. Retries of one occurrence then share their idempotency scope, and two replicas recognize the same occurrence.

**Reviewed for:** Does every occurrence identity in the store carry the schedule ID and the scheduled instant? Check the occurrence identity column.

### Remove Occurrences Past Retention

**ID:** `litespec/ext/jobs#remove-occurrences-past-retention`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST remove occurrences older than the schedule's retention setting record.

**Rationale:** Occurrences older than the retention setting record leave the store so the key space stays bounded.

The key space grows with the schedule's frequency and its retention: a per-second schedule kept for a year holds more than 30 million rows. [Quartz states the same practice](https://quartz-scheduler.net/documentation/best-practices.html) for a persistent job store.

**Example:** A daily schedule that retains occurrences for 90 days holds 90 rows. A minute schedule with the same retention holds more than 100,000 rows. That is a different storage decision.

**Reviewed for:** Is every stored occurrence within its schedule's retention setting record? Check the store rows beside the setting records.

### Persist Occurrence State

**ID:** `litespec/ext/jobs#persist-occurrence-state`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job store MUST persist scheduled, processing, completed, and dead-letter state with safe execution details.

**Rationale:** Operators and recovery code need to see where each occurrence stands. The four states persist with attempt, lease, start, completion, and safe error data: scheduled, processing, completed, and dead-letter. Without them, work in flight is indistinguishable from work not yet started.

**Reviewed for:** Does every occurrence row carry a scheduled, processing, completed, or dead-letter state, with only safe execution details? Check the occurrence rows.

### Scope Command Idempotency by Occurrence

**ID:** `litespec/ext/jobs#scope-command-idempotency-by-occurrence`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use its occurrence identity as the command idempotency scope.

**Rationale:** Every retry of one occurrence replays the same scheduled business action. The occurrence identity scopes the command idempotency key to make that hold. A key scoped any wider would merge separate occurrences into one.

**Reviewed for:** Does every retry of one occurrence use its occurrence identity as the idempotency scope? Check the command idempotency keys.

### Allow Completion Only with a Current Fencing Value

**ID:** `litespec/ext/jobs#allow-completion-only-with-a-current-fencing-value`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A Worker MUST mark completion only while its fencing value remains current.

**Rationale:** A stale fencing value means another replica owns the occurrence now. Completing past that point would write one replica's result over another's ownership. The Worker marks completion only while its value stays current.

**Reviewed for:** Does any completion write succeed on a stale fencing value? Check the store write path and its fencing tests.

### Audit Manual Replay

**ID:** `litespec/ext/jobs#audit-manual-replay`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A manual replay MUST create a new attempt for the same occurrence identity: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A manual replay creates a new attempt under the same occurrence identity: an audit record where audit is selected, a standalone record otherwise. The replay keeps the original scheduled instant and its history. A replay recorded as a new occurrence would rewrite when the work was due.

**Reviewed for:** Does every manual replay add an audited attempt under the same occurrence identity? Check the replay command and the audit rows.

### Prefer UTC Schedules

**ID:** `litespec/ext/jobs#prefer-utc-schedules`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled job MUST use a UTC schedule unless its specification names a business-local time requirement and its IANA time zone.

**Rationale:** A daylight-saving transition makes a wall clock run one hour twice and skip another. A local schedule therefore runs twice on one date each year and skips an hour on another. That behavior is correct for a job whose meaning is local, such as a nightly statement at 9 AM for a user, and a defect for every other job. Naming the time zone in the specification states the transition behavior for a later reader.

**Reviewed for:** Does every schedule state UTC or a business-local time requirement with its IANA time zone? Check each schedule record.

### State Clock Behavior for Business-Local Schedules

**ID:** `litespec/ext/jobs#state-clock-behavior-for-business-local-schedules`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A business-local schedule MUST record an IANA time-zone identifier and define skipped and repeated local-time behavior.

**Rationale:** Daylight-saving transitions omit one local hour and repeat another. A business-local schedule records its IANA time-zone identifier and defines what happens to occurrences in the skipped and repeated hours. Without that definition, each transition silently drops or doubles the local work.

**Reviewed for:** Does every business-local schedule record its IANA zone and the skipped and repeated local-time behavior? Check each schedule record.

### Test Time Transitions

**ID:** `litespec/ext/jobs#test-time-transitions`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled-job test MUST use an injected clock and representative time-zone transitions.

**Rationale:** Tests on the real clock can only wait for the next transition to arrive. An injected clock moves time to any transition on demand, so representative time-zone transitions run deterministically instead of once a year in production.

**Reviewed for:** Does every job test use an injected clock and cover representative time-zone transitions? Check the job test sources.

### Exclude Machine-Local Time

**ID:** `litespec/ext/jobs#exclude-machine-local-time`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Scheduled-job production code MUST NOT depend on machine-local time.

**Rationale:** Machine-local time follows each host's configuration, so production code that reads it schedules differently on a developer machine, in staging, and in production. Schedules read their time from the injected clock and the declared zone instead.

**Reviewed for:** Is every production job source free of machine-local clock reads? Check the job sources for local time calls.

### State the Claim Mode

**ID:** `litespec/ext/jobs#state-the-claim-mode`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST state its claim mode as disabled, optional, or required.

**Rationale:** Two people clearing the same item double-pay, double-approve, or double-refuse. The claim mode keeps two hands off one item: items need no claim, accept an optional hold, or require one before action.

**Example:** Claimwick's payout queue requires a claim before release, while its review queue leaves claiming optional.

**Reviewed for:** Does every queue use case state its claim field as disabled, optional, or required? Check the use case pages.

### Order Queue Items by Score

**ID:** `litespec/ext/jobs#order-queue-items-by-score`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST order its items by a score that cites a calculation rule.

**Rationale:** A queue without an order gets cleared by habit: newest first, loudest first, easiest first. A score citing a calculation rule pins the order to declared inputs, so the riskiest or most valuable item surfaces first by rule instead of by accident.

**Example:** Claimwick's dispute queue orders by `rule/dispute.urgency-score`, which weighs amount, age, and evidence gaps.

**Reviewed for:** Does every queue listing sort by a score that cites its calculation rule? Check the listing queries.

### Show the Score Beside Each Item

**ID:** `litespec/ext/jobs#show-the-score-beside-each-item`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A queue listing MUST show each item score with the inputs that set it.

**Rationale:** A hidden score is an oracle: people work around an order they can't see or trust. Showing the score with its inputs lets each person check the order and challenge the inputs instead of the queue.

**Example:** Each Claimwick dispute row shows `urgency 82: EUR 1,900, 6 days old, evidence missing`.

**Reviewed for:** Does every queue row show its score with the inputs that set it? Check the queue rows in the interface.

### Release Claims Past Their Lease

**ID:** `litespec/ext/jobs#release-claims-past-their-lease`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A claimed item MUST return to the queue when its claim lease expires.

**Rationale:** Claims are held by people who go home. A claim with no expiry parks the item forever. The lease returns stale claims to the queue, so an abandoned item waits for the next person instead of for nobody.

**Example:** A Claimwick payout claimed Friday and untouched returns to the queue when its one-hour lease expires.

**Reviewed for:** Does every claim test show an untouched item returning to the queue after its lease expires? Check the claim tests.

### Show a Preview Before Bulk Money Actions

**ID:** `litespec/ext/jobs#show-a-preview-before-bulk-money-actions`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST render a preview of every affected item before acting.

**Rationale:** One wrong filter multiplies one mistake into hundreds when money moves on many items at once. The preview lists every affected item with its amount, so the person confirms the set they meant instead of the set they typed.

**Example:** Claimwick's bulk release previews forty-two payouts totaling EUR 18,400 before asking for confirmation.

**Reviewed for:** Does every bulk money command render every affected item with its amount before acting? Check the preview step of each command.

### Confirm Bulk Money Actions Out of Band

**ID:** `litespec/ext/jobs#confirm-bulk-money-actions-out-of-band`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST require its `confirmBy` code from outside the session.

**Rationale:** A preview confirms the set, but the session itself can be hijacked, shared, or left open. An out-of-band code arrives on a channel the session doesn't control, so the confirmation proves the person, not just the browser.

**Example:** Claimwick's bulk release asks for the six-digit code sent to the finance phone before moving any money.

**Reviewed for:** Does every bulk money command ask for the out-of-band code before moving money? Check the command handlers.

### Record Why Each Report Needs Its Infrastructure

**ID:** `litespec/ext/jobs#record-why-each-report-needs-its-infrastructure`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A reporting proposal MUST record the report, representative data volume, plan or limitation, latency target, maximum result, and operator.

**Rationale:** Reporting infrastructure costs a Worker, storage, and operating attention. Before building any of it, the team writes down the report, a representative data volume, the plan or limitation, the latency target, the maximum result, and the operator. The use case page carries that record, with why a normal request projection falls short of the boundary.

**Reviewed for:** Does every report use case page record the report, volume, plan, target, maximum, and operator? Check each use case page.

### Avoid Speculative Reporting Systems

**ID:** `litespec/ext/jobs#avoid-speculative-reporting-systems`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A project MUST NOT create reporting infrastructure for possible future analytics.

**Rationale:** A reporting system built for analytics the product might never need charges its full operating cost from day one while serving no accepted query. Each report or export earns its infrastructure with a current cost boundary, not a guess about future analysis.

**Reviewed for:** Does every reporting system trace to an accepted current query or export? Check the reporting code and the use case pages.

### Parameterize Report SQL

**ID:** `litespec/ext/jobs#parameterize-report-sql`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A reporting query MUST parameterize every actor, tenant, filter, range, sort, and pagination value.

**Rationale:** A report feeds actor, tenant, filters, ranges, sorting, and pagination into SQL that reads across the database. Binding each of those values as a parameter keeps tenant and actor isolation intact, and closes the injection hole string concatenation opens.

**Reviewed for:** Does every report query pass actor, tenant, filter, range, sort, and pagination values as parameters instead of SQL text? Check the query code.

### Keep Raw SQL in Infrastructure

**ID:** `litespec/ext/jobs#keep-raw-sql-in-infrastructure`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Infrastructure MUST own raw reporting SQL and connection handling.

**Rationale:** Raw SQL names tables, dialects, and connection handling that belong to one provider. Keeping it in Infrastructure lets Application and Domain state what the report means, without depending on how one database phrases it.

**Reviewed for:** Does all raw report SQL and connection handling sit inside Infrastructure code? Check the report query sources.

### Review Report Plans

**ID:** `litespec/ext/jobs#review-report-plans`

**Strength:** advisory

**Applies to:** artifact: record:use-case

**Statement:** Each report SHOULD have its query plans and indexes reviewed against representative data.

**Deviation:** A report over a small fixed data set justifies skipping the review.

**Rationale:** A query that flies over a hundred local rows can scan millions in production. Reviewing query plans and indexes against representative data shows the true cost before the report ships, rather than after it slows the database.

**Reviewed for:** Does every report have a review record showing query plans and indexes checked against representative data? Check the review records.

### Apply Item-Read Authorization

**ID:** `litespec/ext/jobs#apply-item-read-authorization`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A report or export MUST enforce actor, role, ownership, tenant, and sensitive-field rules that apply to item reads.

**Rationale:** A report that reads ten thousand rows at once crosses the same actor, role, ownership, tenant, and sensitive-field boundaries as ten thousand single reads. Enforcing the item-read rules on the bulk path stops the report from exposing rows its user may not open one by one.

**Reviewed for:** Does every report or export carry an authorization test for the item-read rules? Check the authorization tests.

### Run Over-Budget Exports in the Worker

**ID:** `litespec/ext/jobs#run-over-budget-exports-in-the-worker`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export exceeding its request budget MUST run through Worker, with the budget a setting record the export's use case names.

**Rationale:** An export that outlasts its request budget holds an HTTP connection open: the user waits while the server counts threads. Running it in a Worker returns the request immediately. The budget in its setting record tells the team exactly when an export outgrows the request path.

**Reviewed for:** Does every export past its request budget run in a Worker, with the budget in a setting record? Check the export use cases and their setting records.

### Protect Export Output

**ID:** `litespec/ext/jobs#protect-export-output`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export worker MUST store output in approved object storage with a named contact, an expiry, and a short-lived authorized download.

**Rationale:** An export file concentrates sensitive rows into one downloadable object that outlives the request that built it. Approved object storage with a named contact, an expiry, and a short-lived authorized download gives that object controlled access and a defined end instead of a permanent public link.

**Reviewed for:** Does every export output sit in approved object storage with a named contact, an expiry, and a short-lived authorized download? Check the export storage configuration.

### Scope Export Output to the Tenant

**ID:** `litespec/ext/jobs#scope-export-output-to-the-tenant`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export MUST store its output under a tenant-scoped object-storage path.

**Rationale:** Export addresses are predictable. In a shared folder, one tenant can guess another tenant's file name. A tenant-scoped storage path puts each tenant's exports under its own prefix, so a guessed address stays inside the guesser's own data.

**Reviewed for:** Does every export store output under a tenant-scoped path in object storage? Check the export storage paths.

### Define Report Limits

**ID:** `litespec/ext/jobs#define-report-limits`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A report specification MUST state each bound as a setting record: maximum date range, rows, execution time, concurrent jobs, output size, and storage lifetime.

**Rationale:** Each report cost dimension needs its own cap: date range, rows, execution time, concurrent jobs, output size, and storage lifetime. Setting records hold all six, so a runaway report hits a documented bound instead of an exhausted database, a full disk, or a surprise invoice.

**Reviewed for:** Does every report bound hold a setting record beside the report? Check each report's setting records.

### Declare Export Paging

**ID:** `litespec/ext/jobs#declare-export-paging`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export query MUST state `paging` with `mode` of `pages` or `none` with its reason.

**Rationale:** An export reads many rows at once, so it states how the read is bounded. `{"mode": "pages", ...}` bounds each run, and `{"mode": "none", "reason": "..."}` states why the export reads all rows at once. Without the declaration, a bounded export looks the same to a reviewer as one that grows with the table.

**Reviewed for:** Does every export query state `paging` with `mode` of `pages` or `none` with its reason? Check the export query beside the report.

### Audit Export Reads

**ID:** `litespec/ext/jobs#audit-export-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export query MUST state its `audit` reason and target.

**Rationale:** An export file concentrates sensitive rows into one downloadable object. The use case states why the bulk read is audited and where the audit lands. Without the stated audit, the concentrated read leaves no trace a reviewer can follow.

**Reviewed for:** Does every export query state its `audit` reason and target? Check the export query beside the report.

### Support Report Cancellation

**ID:** `litespec/ext/jobs#support-report-cancellation`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A report implementation MUST support cancellation and remove expired outputs.

**Rationale:** Closing the tab leaves the report running, and an export nobody downloads still occupies storage. Cancellation stops the abandoned work, and expiry removes the abandoned file, so neither consumes resources indefinitely.

**Reviewed for:** Does every report honor a cancel request and remove expired output? Check the cancellation tests.

### Escape Formula-Leading Export Values

**ID:** `litespec/ext/jobs#escape-formula-leading-export-values`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A CSV or spreadsheet export MUST escape formula-leading values unless the team intentionally fills a field with a formula.

**Rationale:** A value starting with `=`, `+`, `-`, or `@` becomes a live formula when the export opens in a spreadsheet. It runs in the opener's application, with the opener's data. Escaping those values keeps data as data, except in a field the team intentionally fills with a formula.

**Reviewed for:** Does every exported value starting with a formula character go out escaped, outside the formula fields the team fills on purpose? Check the CSV and spreadsheet writers.

### Define Export Encoding and Columns

**ID:** `litespec/ext/jobs#define-export-encoding-and-columns`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An export format MUST use explicit character encoding and a stable column contract.

**Rationale:** An export without a stated encoding arrives garbled on the first machine that guesses differently. Columns that shift per release break every parser built on them. Explicit encoding with a stable column contract lets consumers parse the file without guessing either.

**Reviewed for:** Does every export format state a character encoding and stable columns in its format record? Check each format record.

## Conventions

### Place Handlers with Owned Behavior

**ID:** `litespec/ext/jobs#place-handlers-with-owned-behavior`

**Strength:** default

**Applies to:** artifact: job

**Default:** Place each job handler in the `Application/` folder of the use case it runs, or with the notification it sends or the system it calls when it runs none.

**Replacement:** A project names its own job handler layout in its documentation.

**Rationale:** A job handler lives in the `Application/` folder of the use case its `runs` entry names, so the use case folder shows every way the use case starts. One that runs no use case lives in the module `Notifications/` folder of the notification it sends, or the `ExternalSystems/` folder of the system it calls, under `litespec/workspace/naming#place-each-trigger-with-what-it-runs`. The module owns both the business action and what its schedule means. A handlers folder far from the behavior separates each schedule from its reason.

**Reviewed for:** Does every job handler sit in the folder of what it runs, when the project names no other job handler layout? Check the handler folders beside the project documentation.

### Resolve Dependencies Inside an Execution Scope

**ID:** `litespec/ext/jobs#resolve-dependencies-inside-an-execution-scope`

**Strength:** default

**Applies to:** artifact: job

**Default:** Resolve scoped dependencies inside an execution scope and dispose the scope after the Command completes.

**Replacement:** A project names another scope boundary for occurrence dependencies in its documentation.

**Rationale:** Each occurrence resolves its scoped dependencies inside one execution scope, disposed after the Command completes. The scope isolates one occurrence's dependencies from the next. A shared scope would leak one occurrence's state into another.

**Reviewed for:** Does every occurrence resolve scoped dependencies inside a disposed execution scope, when the project names no other scope boundary? Check the job execution code and the project documentation.

### Keep Schedule Definitions Separate from Commands

**ID:** `litespec/ext/jobs#keep-schedule-definitions-separate-from-commands`

**Strength:** default

**Applies to:** artifact: job

**Default:** Keep schedule definitions separate from Command behavior.

**Replacement:** A project keeps schedule definitions beside their Command behavior.

**Rationale:** Timing policy changes when operations needs a new rhythm; Command behavior changes when the business needs a new outcome. Keeping schedule definitions separate from Command behavior lets each change alone. A schedule buried in the handler makes every timing change a code change.

**Reviewed for:** Does any schedule definition sit inside Command behavior, when the project keeps them apart from it? Check the handler code and the schedule records.

### Order Unscored Queues by Age

**ID:** `litespec/ext/jobs#order-unscored-queues-by-age`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** A queue without a score lists its oldest item first.

**Replacement:** A project names its fallback queue order in its queue documentation.

**Rationale:** Age is the honest fallback: the item waiting longest goes first. Any other fallback works, when the queue states it.

**Reviewed for:** Does every queue without a score list oldest first, or name a fallback order in the queue documentation? Check the queue listings and the queue documentation.

### Keep Report Definitions in Application

**ID:** `litespec/ext/jobs#keep-report-definitions-in-application`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep report definitions in Application under their module.

**Replacement:** A consumer records a report-definition convention naming where its report definitions live.

**Rationale:** What a report means and who may run it are business choices. Keeping the definition in Application under its module puts those choices beside the module's other rules, instead of buried in query code.

**Reviewed for:** Does every report definition sit inside its module, when the report-definition convention names no other place? Check the report definition sources.

### Keep Report Providers in Infrastructure

**ID:** `litespec/ext/jobs#keep-report-providers-in-infrastructure`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep SQL, row mappings, and storage providers in Infrastructure.

**Replacement:** A consumer records a report-provider convention naming where its SQL, mappings, and storage code live.

**Rationale:** SQL text, row mappings, and storage calls change per provider, while the report's meaning stays fixed. Keeping them in Infrastructure swaps the provider without touching the report's business rules.

**Reviewed for:** Does all SQL and storage code sit inside Infrastructure, when the report-provider convention names no other place? Check the report provider sources.

### Keep Worker Orchestration Separate from Report Rules

**ID:** `litespec/ext/jobs#keep-worker-orchestration-separate-from-report-rules`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Keep the scheduling, chunking, and progress tracking of a report in Worker code, apart from the rules that define the report.

**Replacement:** A consumer records a report-orchestration convention naming where its scheduling and progress tracking live.

**Rationale:** Scheduling, chunking, and progress tracking run the report; they don't define it. Keeping Worker orchestration apart from the report's rules lets either change without dragging the other along.

**Reviewed for:** Does any Worker orchestration code sit inside report rules, when the report-orchestration convention names no other place? Check the report rule sources.

## Dependencies

Packages: This extension selects no report or object-storage package. Each provider the project uses needs a decision and a manifest pin.

Related extensions: `outbox`.

Guides: None.
