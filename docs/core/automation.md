# Automation

## Intent

Automation states the work that runs without a request: the reactions answering events and the jobs running on schedule, once, or by hand. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Reactions

A reaction is work that runs when a domain or integration event arrives. It carries the identifier `reaction/<module>.<name>` and lives at `docs/behavior/modules/<module>/reactions/<name>.md`, filed with the module whose records it changes, or with the module owning the notification it sends when it only sends. A reaction changes nothing directly; a reaction needing a change runs a small command. A reaction is named `<verb>-<what>-on-<fact>`, as the [record language page](language.md) states.

| Field | Meaning |
|:---|:---|
| `triggeredBy` | The list of `event/...` or `integration-event/...` ids the reaction answers. |
| `skippedWhen` | The skip condition in domain words, completing "when ...", optional. |
| `effects` | `{ runs / sends / emits / calls, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states; a reaction may omit `timing`, which reads as `async`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads`, `changes` | The requirements carried, the journey steps served, the failure handling, the idempotency, and the aggregates read and changed, as on a use case. |
| `onFailure` | `{ mode, retry, then, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `then` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the triggering event fields, which the record does not list. |

A reaction may hold scenario child records ending in a result of a use case its effects run. An event handler carries `implements reaction/<module>.<name>`.

### Jobs

A job is work that runs on a schedule, once, or by hand. It carries the identifier `job/<module>.<name>` and lives at `docs/behavior/modules/<module>/jobs/<name>.md`, filed with the module whose records it changes, or with the module owning the notification it sends when it only sends. A job changes nothing directly; a job needing a change runs a small command. A job is named `<verb>-<what>` with the verb of the use case it runs, as the [record language page](language.md) states.

| Field | Meaning |
|:---|:---|
| `kind` | `scheduled`, `one-off`, or `manual`. |
| `schedule` | The schedule, required with `scheduled`: `{ "every": "PT1M" }`, `{ "cron": "0 3 * * *", "timezone": "..." }`, or `{ "at": "startup" }` for a run once at deployment start. |
| `selection`, `batchSize`, `lease`, `cursor`, `runbook`, `runsWhere`, `guard`, `serviceLevel`, `evidence` | What each run selects, how many items per batch, the lock with its duration, where resume state lives, the runbook answering the run, where it runs, the guard it runs under, the promised service level, and the evidence the run leaves. `guard` is required with `manual`. |
| `effects` | `{ runs / sends / emits / calls, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states; a job states `timing` on each entry. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads`, `changes` | The requirements carried, the journey steps served, the failure handling, the idempotency, and the aggregates read and changed, as on a use case. |
| `onFailure` | `{ mode, retry, then, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `then` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the run window fields, which the record does not list. |

A job may hold scenario child records ending in a result of a use case its effects run. A job host carries `implements job/<module>.<name>`.

### Runbook

A runbook is the steps to run or recover the product. It carries the identifier `runbook/<name>` and lives at `docs/behavior/runbooks/<name>.md`, named verb-first for what the reader does, as the [record language page](language.md) states. Its `trigger` names when to open it. A job names its runbook in `runbook`, a saga driven by a person names its runbook in `drivenBy`, and any record or effect names its recovery in `onFailure.runbook`. Its sections are Impact, Prerequisites, Procedure, Verification, Rollback or Recovery, and Escalation.

## Standards

### Trigger Every Reaction from Its Declared Events

**ID:** `litespec/core/automation#trigger-every-reaction-from-its-declared-events`

**Statement:** Each reaction MUST declare the events it answers in `triggeredBy` as a list of `event/...` or `integration-event/...` ids.

**Rationale:** A reaction without a trigger never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the events states when the reaction fires: the domain or integration events it answers, one id or a list. A `skippedWhen` clause states when it skips.

**Example:** The send-due-notice reaction answers the loan-borrowed domain event and runs the send-due-notice command.

**Checked by:** `litespec model check` reports `reaction-trigger-unknown` (level G) for a trigger naming an unknown event; `litespec consumer` fails a reaction without a `triggeredBy` list.

### Run Every Job from Its Declared Kind

**ID:** `litespec/core/automation#run-every-job-from-its-declared-kind`

**Statement:** Each job MUST declare its `kind` of `scheduled`, `one-off`, or `manual` with its `schedule` where scheduled.

**Rationale:** A job without a kind never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the kind states when the job runs: the schedule with its `every` duration, `cron` expression and timezone, or `at: startup` for a run once at deployment start, `one-off` for work that runs once, or `manual` for work a person starts.

**Example:** The expire-lapsed-holds job runs every six hours over open holds past their window.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a schedule neither an `every` duration, a `cron` expression, nor `at: startup`; `litespec consumer` fails a job without its `kind` or a scheduled job without `schedule`.

### Declare Every Reaction and Job Edge in Effects

**ID:** `litespec/core/automation#declare-every-reaction-and-job-edge-in-effects`

**Statement:** Each reaction and each job MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** A reaction or job with no declared edge observes and never acts, while an edge outside `effects` hides behavior the checks never see. Declaring each edge in `effects` with exactly one verb of `runs`, `sends`, `emits`, or `calls` keeps every consequence of the reaction or job beside its trigger, under one grammar for every record that has edges, as the [use-cases page](use-cases.md) states.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job with no `effects` or with a top-level effect verb.

### Write No Aggregate Directly from a Reaction or Job

**ID:** `litespec/core/automation#write-no-aggregate-directly-from-a-reaction-or-job`

**Statement:** A reaction or job MUST NOT write an aggregate except through the use case it runs.

**Rationale:** A reaction or job writing directly splits the write path: the same aggregate changes through a reviewed command and through an unreviewed reaction or job, each with its own guards. Running a small command keeps every write behind a use case with its access, consistency, results, and scenarios.

**Reviewed for:** Which reactions or jobs write an aggregate outside the use case they run? Look at the reaction and job effects beside the use cases.

### File Every Reaction and Job with the Module It Changes

**ID:** `litespec/core/automation#file-every-reaction-and-job-with-the-module-it-changes`

**Statement:** Each reaction and each job MUST live with the module whose records it changes, or with the module owning the notification it sends when it only sends.

**Rationale:** Reactions and jobs filed by trigger scatter: one module behavior hides on a dozen trigger pages, and nobody sees the module whole. Filing each reaction and job with the module it changes keeps each module behavior in one place. Its event handler lives in that module's code under the `On{Fact}` folder (`litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`). The trigger page shows reactions answering its event as a view derived from records.

**Reviewed for:** Which reaction or job pages sit outside the module they change? Look at the reactions and jobs beside the modules whose records they change.

### Name Each Reaction for Its Act and Its Trigger

**ID:** `litespec/core/automation#name-each-reaction-for-its-act-and-its-trigger`

**Statement:** Each reaction MUST be named `<verb>-<what>-on-<fact>` with a declared verb.

**Rationale:** A reaction runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` could fire on a borrow, a hold, or a reminder run. Naming the act and the fact lets a reader find every consequence of a fact by its name, as `litespec/core/language#name-each-record-by-its-types-name-form` states.

**Example:** `reaction/lending.send-due-notice-on-loan-borrowed` answers `event/loan.loan-borrowed`.

**Checked by:** `litespec model check` reports `reaction-name-invalid` (level G) for a reaction outside `<verb>-<what>-on-<fact>` with a declared verb.

### Name Each Job for the Use Case It Runs

**ID:** `litespec/core/automation#name-each-job-for-the-use-case-it-runs`

**Statement:** Each job MUST be named `<verb>-<what>` with the verb of a use case it runs.

**Rationale:** A job selects items and runs one command for each, so naming it for that command lets a reader go from the schedule to the behavior in one step, as `litespec/core/language#name-each-record-by-its-types-name-form` states.

**Example:** `job/lending.expire-lapsed-holds` runs `use-case/lending.expire-holds`.

**Checked by:** `litespec model check` reports `job-name-invalid` (level G) for a job outside `<verb>-<what>` with the verb of a use case it runs.

### State the Failure Handling of Every Reaction and Job

**ID:** `litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job`

**Statement:** Each reaction and each job MUST declare its `onFailure` in the one failure shape and its idempotency.

**Rationale:** A failed reaction or job without handling retries forever or drops silently: notices double-send, holds never clear. Declaring `onFailure` states the failure shape beside the trigger: `mode` of `retry`, `skip-and-log`, or `discard`; `retry` with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `then` of `dead-letter`; `deadLetter` carrying `after` with the action a person takes; `surfacesIn` naming the entry point showing the failure and `runbook` the recovery procedure. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations, and prose handling is refused. The same shape serves every record and every effect entry. The idempotency mode, usually `state`, states how repeats collapse.

**Checked by:** `litespec consumer` fails a reaction or job with `onFailure` outside the one failure shape; `litespec model check` reports `record-invalid` for prose handling, a missing or malformed retry, and a malformed duration.

### Point Reaction and Job Scenarios at the Run Result

**ID:** `litespec/core/automation#point-reaction-and-job-scenarios-at-the-run-result`

**Statement:** Each scenario on a reaction or job MUST end in a result of a use case its effects run.

**Rationale:** A reaction or job scenario ending anywhere else proves another behavior story: it walks the trigger and asserts an ending no run use case declares. Ending in a run use case result keeps the scenario a proof of the reaction or job consequence.

**Checked by:** `litespec model check` reports `scenario-result-unknown` for a reaction or job scenario ending outside the run use case results.

### Keep Job Fields off Reactions

**ID:** `litespec/core/automation#keep-job-fields-off-reactions`

**Statement:** A reaction MUST NOT carry `kind`, `schedule`, `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

**Rationale:** Run fields describe work over time: the kind, the schedule, what each run selects, how many items per batch, the lock, where resume state lives, where it runs, the guard it runs under, the promised service level, and the evidence the run leaves. A reaction answers one event and carries none of that, so a run field on it claims a schedule the trigger never states.

**Example:** The send-due-notice reaction answers its event with no `selection` and no `guard`.

**Checked by:** `litespec consumer` fails a reaction carrying `kind`, `schedule`, `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

### Guard Every Manual Job

**ID:** `litespec/core/automation#guard-every-manual-job`

**Statement:** Each job with `kind: manual` MUST state the guard it runs under.

**Rationale:** Work a person starts without a guard runs anywhere by anyone: a rehearsal harness touches production, a manual sweep runs twice. Stating the guard keeps person-started work inside its fence, beside where it runs in `runsWhere`.

**Example:** The rehearse-payout job runs under the platform guard on the staging deployment.

**Checked by:** `litespec model check` reports `job-guard-missing` (level G) for a manual job without `guard`.

### Write Every Runbook to Run or Recover

**ID:** `litespec/core/automation#write-every-runbook-to-run-or-recover`

**Statement:** Each runbook MUST state its impact, prerequisites, procedure, verification, rollback or recovery, and escalation.

**Rationale:** A runbook named by a job, a saga, a failure policy, or an alert is the answer a person opens under pressure: impact says what breaks, prerequisites say what to hold, procedure says each step, verification says how to confirm, rollback or recovery says how to undo, and escalation says who to call. A runbook missing one leaves the reader guessing at the worst moment.

**Example:** The clear-stuck-reimbursements runbook states its impact on waiting expenses, its prerequisites, its procedure, its verification query, its rollback, and its escalation to the spending owner.

**Checked by:** `litespec consumer` fails a runbook page without Impact, Prerequisites, Procedure, Verification, Rollback or Recovery, or Escalation.

## Conventions

None.
