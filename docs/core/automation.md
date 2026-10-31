# Automation

## Intent

Automation states the work that runs without a request: the reactions answering events and the scheduled, one-off, and operator jobs. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Reactions

A reaction is a when-X-do-Y automation: on a domain or integration event, it runs the edges its `effects` declare. A reaction changes nothing directly; a reaction needing a change issues a small command. It is filed with the module whose records it changes, or with the notification module when it only sends.

| Field | Meaning |
|:---|:---|
| `on` | The domain or integration events answered. |
| `if` | The skip condition in domain words, optional. |
| `effects` | `{ issues / sends / publishes / calls, timing }` entries with exactly one verb, the same entries a use case uses; a reaction may omit `timing`, which reads as `async`. There are no top-level `issues` or `sends`. |
| `forEach`, `reads` | The optional scope and the aggregates read. |
| `fulfills`, `serves` | The requirements carried and the journey steps served. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the triggering event or run window fields, which the record does not list. |
| `onFailure` | `{ retry, deadLetter, surfacesIn, runbook }`: `retry` is `none`, `next-run`, or `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. Delays and `after` are ISO 8601 durations. |

A reaction may hold scenario child records ending in a result of a use case its effects issue.

### Jobs

A job is scheduled, one-off, or operator work running without a request. A scheduled job runs on its structured schedule; a one-off job runs once; an operator job is interactive platform work with no aggregate, such as a rehearsal harness. Its `effects` run once per item in its `selection`.

| Field | Meaning |
|:---|:---|
| `kind` | `scheduled`, `one-off`, or `operator`. |
| `schedule` | `{ every | cron, timezone }` in structured form. |
| `selection`, `batchSize`, `lease` | What each run selects, how many items per batch, and the lock with its duration. |
| `effects`, `reads` | The `{ issues / sends / publishes / calls, timing }` entries run per item and the aggregates read. There is no top-level `issues`. |
| `serviceLevel`, `evidence` | The promise citing its requirement, and where proof of a run lands. |
| `runbook`, `runsWhere` | The runbook answering the job and where it runs. |
| `fulfills`, `serves`, `onFailure`, `idempotency` | As on a use case and a reaction. |
| `guard` | The guard an operator job runs under. |
| `cursor` | Where resume state lives on a driven process. |

## Standards

### Trigger Every Reaction from a Declared Event

**ID:** `litespec/core/automation#trigger-every-reaction-from-a-declared-event`

**Statement:** Each reaction MUST name the domain or integration events it answers in `on`.

**Rationale:** A reaction without a trigger never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the answered events states when the behavior fires, and an `if` condition states when it skips. The `reaction-trigger-unknown` check reports a trigger naming an unknown event.

**Example:** The send-due-notice reaction answers the loan loan-borrowed domain event and issues the send-due-notice command.

**Checked by:** `litespec model check` reports `reaction-trigger-unknown` for a trigger naming an unknown event.

### Issue or Send from Every Reaction

**ID:** `litespec/core/automation#issue-or-send-from-every-reaction`

**Statement:** Each reaction MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** A reaction with no declared edge observes and never acts, while an edge outside `effects` hides behavior the checks never see. Declaring each edge in `effects` with exactly one verb of `issues`, `sends`, `publishes`, or `calls` keeps every consequence of the automation beside its trigger, under one grammar for every record that has edges. The `record-invalid` check reports a reaction with no `effects` and a reaction holding top-level `issues` or `sends`.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction with no `effects` and for top-level `issues` or `sends`.

### Write No Aggregate Directly from a Reaction

**ID:** `litespec/core/automation#write-no-aggregate-directly-from-a-reaction`

**Statement:** A reaction MUST NOT write an aggregate except through the use case it issues.

**Rationale:** A reaction writing directly splits the write path: the same aggregate changes through a reviewed command and through an unreviewed automation, each with its own guards. Issuing a small command keeps every write behind a use case with its access, consistency, results, and scenarios.

**Reviewed for:** Each reaction changes aggregates only through the use case it issues.

### File Every Reaction with the Module It Changes

**ID:** `litespec/core/automation#file-every-reaction-with-the-module-it-changes`

**Statement:** Each reaction MUST live with the module whose records it changes, or with the notification module when it only sends.

**Rationale:** Reactions filed by trigger scatter: one module behavior hides on a dozen trigger pages, and nobody sees the module whole. Filing each reaction with the module it changes keeps each module behavior in one place. Its event handler lives in that module's code under the `On{Fact}` folder (`litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`). The trigger page shows reactions to its event as a view derived from records.

**Reviewed for:** Each reaction page sits with the module it changes, or with the notification module when it only sends, and its event handler lives in that module's code under the `On{Fact}` folder.

### Name Each Reaction for Its Act and Its Trigger

**ID:** `litespec/core/automation#name-each-reaction-for-its-act-and-its-trigger`

**Statement:** Each reaction MUST be named `<verb>-<what>-on-<fact>`, with a declared verb.

**Rationale:** A reaction runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` could fire on a borrow, a renewal or a reminder job. Naming the act and the fact lets a reader find every consequence of a fact by its name, and keeps two reactions to one fact from colliding. The `reaction-name-invalid` check reports a reaction name without a declared verb or without `-on-`.

**Example:** `reaction/lending.send-due-notice-on-loan-borrowed` answers `event/loan.loan-borrowed` and issues the notice; `reaction/lending.notify-borrower` is reported.

**Checked by:** `litespec model check` reports `reaction-name-invalid` for a reaction name without a declared verb or without `-on-`.

### State the Failure Handling of Every Reaction

**ID:** `litespec/core/automation#state-the-failure-handling-of-every-reaction`

**Statement:** Each reaction MUST declare its structured `onFailure` policy and its idempotency.

**Rationale:** A failed automation without handling retries forever or drops silently: notices double-send, holds never clear. Declaring the `onFailure` policy states the failure shape beside the trigger: `retry` is `none`, `next-run`, or a retry object with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `deadLetter` carries `after` with the action a person takes; `surfacesIn` names the entry point showing the failure and `runbook` the recovery procedure. Delays and `after` are ISO 8601 durations, and prose handling is refused. The same shape serves a job. The idempotency mode, usually `state`, states how repeats collapse.

**Checked by:** `litespec consumer` fails a reaction with `onFailure` outside the retry and dead-letter shape; `litespec model check` reports `record-invalid` for prose handling, a missing or malformed retry, and a malformed duration.

### Point Reaction Scenarios at the Issued Result

**ID:** `litespec/core/automation#point-reaction-scenarios-at-the-issued-result`

**Statement:** Each scenario on a reaction MUST end in a result of a use case its effects issue.

**Rationale:** A reaction scenario ending anywhere else proves another behavior story: it walks the trigger and asserts an ending no issued use case declares. Ending in an issued use case result keeps the scenario a proof of the automation consequence.

**Checked by:** `litespec model check` reports `scenario-result-unknown` for a reaction scenario ending outside the issued use case results.

### Declare Every Job with Kind and Structured Schedule

**ID:** `litespec/core/automation#declare-every-job-with-kind-and-structured-schedule`

**Statement:** Each job MUST declare its kind, selection, effects, reads, service level, evidence, and runbook, with a structured schedule when it runs on time.

**Rationale:** A job without a declared selection and promise is a blind run: nobody states what each run selects, what it runs per item, what it guarantees, or where proof lands. Declaring the kind of `scheduled`, `one-off`, or `operator`, the selection with its batch size and lease, the `effects` entries with the aggregates read, the service level citing its requirement, the evidence, and the runbook states the run beside its promise. The schedule is structured with its timezone, never prose. The `schedule-invalid` check reports a schedule no tool can read, and `record-invalid` reports a job holding top-level `issues`.

**Example:** The expire-lapsed-holds job runs every six hours over open holds past their window, issues one expiry per item, and lands its evidence in the run log.

**Checked by:** `litespec model check` reports `schedule-invalid` for a schedule no tool can read and `record-invalid` for top-level `issues`; `litespec consumer` fails a job without kind, selection, effects, reads, service level, evidence, or runbook.

### Name Each Job for the Command It Issues

**ID:** `litespec/core/automation#name-each-job-for-the-command-it-issues`

**Statement:** Each job MUST start with the verb of a use case it issues.

**Rationale:** A job decides nothing about an item; it selects the next items and issues one command for each. Naming it for that command lets a reader go from the schedule to the behavior in one step, and stops a job named `sweep-` or `process-` from hiding what it changes. The `job-name-invalid` check reports a job whose first word is not the verb of a use case it issues.

**Example:** `job/lending.expire-lapsed-holds` issues `use-case/lending.expire-holds` and passes; `job/lending.sweep-holds` issuing the same use case is reported.

**Checked by:** `litespec model check` reports `job-name-invalid` for a job whose first word is not the verb of a use case it issues.

### Guard Every Operator Job

**ID:** `litespec/core/automation#guard-every-operator-job`

**Statement:** Each operator job MUST state where it runs in `runsWhere` and the guard it runs under.

**Rationale:** Interactive platform work without a guard runs anywhere by anyone: a rehearsal harness touches production, a manual sweep runs twice. Stating where it runs and the guard it runs under keeps operator work inside its fence. The `job-guard-missing` check reports an operator job with no guard.

**Checked by:** `litespec model check` reports `job-guard-missing` for an operator job without `runsWhere` or guard.

## Conventions

None.
