# Automation

## Intent

Automation states the work that runs without a request: the automations answering events and the scheduled and manual runs. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Automations

An automation is work that runs without a request: on a domain or integration event, on a schedule, or by hand. It carries the identifier `automation/<module>.<name>` and lives at `docs/behavior/modules/<module>/automations/<name>.md`, filed with the module whose records it changes, or with the module owning the notification it sends when it only sends. An automation changes nothing directly; an automation needing a change runs a small command. An event automation is named `<verb>-<what>-on-<fact>`, and a schedule or manual automation is named `<verb>-<what>` with the verb of the use case it runs, as the [record language page](language.md) states.

| Field | Meaning |
|:---|:---|
| `triggeredBy` | Exactly one of `{ "event": "event/<...>" }`, `{ "event": ["event/<...>", ...] }`, or the `integration-event/...` forms, `{ "schedule": { "every": "PT1M" } }`, `{ "schedule": { "cron": "0 3 * * *", "timezone": "..." } }`, `{ "schedule": { "at": "startup" } }`, `{ "manual": true }`. |
| `skippedWhen` | The skip condition in domain words, completing "when ...", optional. |
| `effects` | `{ runs / sends / emits / calls, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states; a schedule or manual automation states `timing` on each entry, and an event automation may omit it, which reads as `async`. |
| `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, `evidence` | Schedule or manual triggers only: the runbook answering the run, where it runs, the guard it runs under, where resume state lives, what each run selects, the lock with its duration, how many items per batch, the promised service level, and the evidence the run leaves. `guard` is required with `manual`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads`, `changes` | The requirements carried, the journey steps served, the failure handling, the idempotency, and the aggregates read and changed, as on a use case. |
| `onFailure` | `{ mode, retry, then, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `then` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the triggering event or run window fields, which the record does not list. |

An automation may hold scenario child records ending in a result of a use case its effects run. A code element serving an automation carries `implements automation/<module>.<name>`.

### Runbook

A runbook is the steps to run or recover the product. It carries the identifier `runbook/<name>` and lives at `docs/behavior/runbooks/<name>.md`, named verb-first for what the reader does, as the [record language page](language.md) states. Its `trigger` names when to open it. An automation names its runbook in `runbook`, a saga driven by a person names its runbook in `drivenBy`, and any record or effect names its recovery in `onFailure.runbook`. Its sections are Impact, Prerequisites, Procedure, Verification, Rollback or Recovery, and Escalation.

## Standards

### Trigger Every Automation from Its Declared Trigger

**ID:** `litespec/core/automation#trigger-every-automation-from-its-declared-trigger`

**Statement:** Each automation MUST declare exactly one trigger in `triggeredBy`: an event, a schedule, or manual.

**Rationale:** An automation without a trigger never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the trigger states when the behavior fires: the domain or integration events it answers, one id or a list, the schedule with its `every` duration, `cron` expression and timezone, or `at: startup` for a run once at deployment start, or `manual` for work a person starts. A `skippedWhen` clause states when it skips.

**Example:** The send-due-notice automation answers the loan-borrowed domain event and runs the send-due-notice command, while the expire-lapsed-holds automation runs every six hours over open holds past their window.

**Checked by:** `litespec model check` reports `automation-trigger-unknown` (level G) for a trigger naming an unknown event and `schedule-invalid` (level G) for a schedule neither an `every` duration, a `cron` expression, nor `at: startup`; `litespec consumer` fails an automation without exactly one trigger.

### Declare Every Automation Edge in Effects

**ID:** `litespec/core/automation#declare-every-automation-edge-in-effects`

**Statement:** Each automation MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** An automation with no declared edge observes and never acts, while an edge outside `effects` hides behavior the checks never see. Declaring each edge in `effects` with exactly one verb of `runs`, `sends`, `emits`, or `calls` keeps every consequence of the automation beside its trigger, under one grammar for every record that has edges, as the [use-cases page](use-cases.md) states.

**Checked by:** `litespec model check` reports `record-invalid` for an automation with no `effects` or with a top-level effect verb.

### Write No Aggregate Directly from an Automation

**ID:** `litespec/core/automation#write-no-aggregate-directly-from-an-automation`

**Statement:** An automation MUST NOT write an aggregate except through the use case it runs.

**Rationale:** An automation writing directly splits the write path: the same aggregate changes through a reviewed command and through an unreviewed automation, each with its own guards. Running a small command keeps every write behind a use case with its access, consistency, results, and scenarios.

**Reviewed for:** Which automations write an aggregate outside the use case they run? Look at the automation effects beside the use cases.

### File Every Automation with the Module It Changes

**ID:** `litespec/core/automation#file-every-automation-with-the-module-it-changes`

**Statement:** Each automation MUST live with the module whose records it changes, or with the module owning the notification it sends when it only sends.

**Rationale:** Automations filed by trigger scatter: one module behavior hides on a dozen trigger pages, and nobody sees the module whole. Filing each automation with the module it changes keeps each module behavior in one place. Its event handler lives in that module's code under the `On{Fact}` folder (`litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`). The trigger page shows automations answering its event as a view derived from records.

**Reviewed for:** Which automation pages sit outside the module they change? Look at the automations beside the modules whose records they change.

### Name Each Automation for Its Act and Its Trigger

**ID:** `litespec/core/automation#name-each-automation-for-its-act-and-its-trigger`

**Statement:** Each automation MUST be named `<verb>-<what>-on-<fact>` with a declared verb for an event trigger, or `<verb>-<what>` with the verb of a use case it runs for a schedule or manual trigger.

**Rationale:** An event automation runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` could fire on a borrow, a renewal, or a reminder run. Naming the act and the fact lets a reader find every consequence of a fact by its name. A schedule or manual automation selects items and runs one command for each, so naming it for that command lets a reader go from the schedule to the behavior in one step, as `litespec/core/language#name-each-record-by-its-types-name-form` states.

**Example:** `automation/lending.send-due-notice-on-loan-borrowed` answers `event/loan.loan-borrowed`; `automation/lending.expire-lapsed-holds` runs `use-case/lending.expire-holds`.

**Checked by:** `litespec model check` reports `automation-name-invalid` (level G) for an event automation outside `<verb>-<what>-on-<fact>` with a declared verb, or a schedule or manual automation outside `<verb>-<what>` with the verb of a use case it runs.

### State the Failure Handling of Every Automation

**ID:** `litespec/core/automation#state-the-failure-handling-of-every-automation`

**Statement:** Each automation MUST declare its `onFailure` in the one failure shape and its idempotency.

**Rationale:** A failed automation without handling retries forever or drops silently: notices double-send, holds never clear. Declaring `onFailure` states the failure shape beside the trigger: `mode` of `retry`, `skip-and-log`, or `discard`; `retry` with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `then` of `dead-letter`; `deadLetter` carrying `after` with the action a person takes; `surfacesIn` naming the entry point showing the failure and `runbook` the recovery procedure. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations, and prose handling is refused. The same shape serves every record and every effect entry. The idempotency mode, usually `state`, states how repeats collapse.

**Checked by:** `litespec consumer` fails an automation with `onFailure` outside the one failure shape; `litespec model check` reports `record-invalid` for prose handling, a missing or malformed retry, and a malformed duration.

### Point Automation Scenarios at the Run Result

**ID:** `litespec/core/automation#point-automation-scenarios-at-the-run-result`

**Statement:** Each scenario on an automation MUST end in a result of a use case its effects run.

**Rationale:** An automation scenario ending anywhere else proves another behavior story: it walks the trigger and asserts an ending no run use case declares. Ending in a run use case result keeps the scenario a proof of the automation consequence.

**Checked by:** `litespec model check` reports `scenario-result-unknown` for an automation scenario ending outside the run use case results.

### Keep Scheduled Fields off Event Automations

**ID:** `litespec/core/automation#keep-scheduled-fields-off-event-automations`

**Statement:** An automation with an event trigger MUST NOT carry `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

**Rationale:** Run fields describe work over time: what each run selects, how many items per batch, the lock, where resume state lives, where it runs, the guard it runs under, the promised service level, and the evidence the run leaves. An event automation answers one event and carries none of that, so a run field on it claims a schedule the trigger never states.

**Example:** The send-due-notice automation answers its event with no `selection` and no `guard`.

**Checked by:** `litespec consumer` fails an event automation carrying `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

### Guard Every Manual Automation

**ID:** `litespec/core/automation#guard-every-manual-automation`

**Statement:** Each automation with a manual trigger MUST state the guard it runs under.

**Rationale:** Work a person starts without a guard runs anywhere by anyone: a rehearsal harness touches production, a manual sweep runs twice. Stating the guard keeps person-started work inside its fence, beside where it runs in `runsWhere`.

**Example:** The rehearse-payout automation runs under the platform guard on the staging deployment.

**Checked by:** `litespec model check` reports `automation-guard-missing` (level G) for a manual automation without `guard`.

### Write Every Runbook to Run or Recover

**ID:** `litespec/core/automation#write-every-runbook-to-run-or-recover`

**Statement:** Each runbook MUST state its impact, prerequisites, procedure, verification, rollback or recovery, and escalation.

**Rationale:** A runbook named by an automation, a saga, a failure policy, or an alert is the answer a person opens under pressure: impact says what breaks, prerequisites say what to hold, procedure says each step, verification says how to confirm, rollback or recovery says how to undo, and escalation says who to call. A runbook missing one leaves the reader guessing at the worst moment.

**Example:** The clear-stuck-reimbursements runbook states its impact on waiting expenses, its prerequisites, its procedure, its verification query, its rollback, and its escalation to the spending owner.

**Checked by:** `litespec consumer` fails a runbook page without Impact, Prerequisites, Procedure, Verification, Rollback or Recovery, or Escalation.

## Conventions

None.
