# Automation

**Stack:** any
**Concern:** application

## Intent

Automation states the work that runs without a request: the reactions answering events, the jobs running on schedule or once, and the read models a module keeps current from another module's published events. Agents write these records from Intent, and code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Reactions

A reaction is work that runs when a domain event or integration event arrives. It carries the identifier `reaction/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/reactions/<name>/<name>.yaml`, filed with the module whose records it changes, or with the module owning the notification it sends when it only sends. A reaction changes nothing directly; a reaction needing a change runs a small command. A reaction is named `<verb>-<what>-on-<fact>`, as the [record language page](language.md) states. It carries `summary`, and its trigger and effect live under Trigger and Effect in the explanation file beside the record file, with Failure Handling and an optional Example.

| Field | Meaning |
|:---|:---|
| `triggeredBy` | The list of `domain-event/...` or `integration-event/...` ids the reaction answers, each a domain event of its own module or an integration event. |
| `skippedWhen` | The skip condition in domain words, completing "when ...", optional. |
| `compensates` | The `use-case/...` id the reaction undoes, for a reaction answering a later step's failure, optional. |
| `effects` | `{ runs / sends / calls, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states, with at most one `runs` entry; a reaction may omit `timing`, which reads as `async`. Each effect runs on its own, as [Run Each Effect on Its Own](#run-each-effect-on-its-own) states. |
| `outcomes` | The run endings, required: `{ name, meaning }` entries with `name` from `completed`, `skipped`, `suppressed`, `failed`, `retry-scheduled`, `nothing-selected`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads` | The requirements carried, the flow steps served, the failure handling, the idempotency, and the aggregates and settings read, as on a use case. |
| `onFailure` | `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `ifExhausted` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `ifExhausted` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, required, usually `state`; a `key` templates `{field}` placeholders over the triggering event fields, which the record does not list. |

A reaction holds scenario child records ending in a result of a use case its effects run or in one of its outcomes. An event handler carries `implements reaction/<module>.<name>`.

When one event carries several outcomes, one reaction cannot serve them all: a reaction runs at most one use case, as [Run at Most One Use Case from a Reaction or Job](#run-at-most-one-use-case-from-a-reaction-or-job) states. Split the dispatching reaction into one reaction per outcome, each triggered by its own event. Claimwick example: the expense decision ends approved or refused, so `domain-event/expense.approved` triggers the reaction running `use-case/spending.reimburse-expense`, and `domain-event/expense.refused` triggers the reaction sending the refusal notice, instead of one reaction answering a shared decided event and choosing between the two.

### Jobs

A job is work that runs on a schedule or once. It carries the identifier `job/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/jobs/<name>/<name>.yaml`, filed with the module whose records it changes, or with the module owning the notification it sends when it only sends. A job changes nothing directly; a job needing a change runs a small command. A job is named `<verb>-<what>` with the verb of the use case it runs, or with its own verb when it runs no use case, as the [record language page](language.md) states. It carries `summary`, and its trigger and effect live under Trigger and Effect in the explanation file beside the record file, with Failure Handling and an optional Example.

| Field | Meaning |
|:---|:---|
| `kind` | `scheduled` or `one-off`. |
| `schedule` | The schedule, required with `scheduled`: `{ "every": "PT1M" }`, `{ "cron": "0 3 * * *", "timezone": "..." }`, or `{ "at": "startup" }` for a run once at deployment start. |
| `selection`, `batchSize`, `lease`, `cursor`, `runbook`, `runsWhen`, `serviceLevel`, `evidence` | What each run selects, how many items per batch, the lock with its duration, where resume state lives, the runbook answering the run, the condition it runs under, the promised service level, and the evidence the run leaves. |
| `effects` | `{ runs / sends / calls, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states, with at most one `runs` entry; a job states `timing` on each entry. Each effect runs on its own, as [Run Each Effect on Its Own](#run-each-effect-on-its-own) states. |
| `outcomes` | The run endings, required: `{ name, meaning }` entries with `name` from `completed`, `skipped`, `suppressed`, `failed`, `retry-scheduled`, `nothing-selected`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads` | The requirements carried, the flow steps served, the failure handling, the idempotency, and the aggregates and settings read, as on a use case. |
| `onFailure` | `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `ifExhausted` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `ifExhausted` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, required, usually `state`; a `key` templates `{field}` placeholders over the run window fields, which the record does not list. |

A job holds scenario child records ending in a result of a use case its effects run or in one of its outcomes. A job host carries `implements job/<module>.<name>`. Work a person starts by hand is a command whose entry point and access name who starts it, not a job.

### Read Models

A read model is one module's own copy of what it needs to know about another module's aggregate, kept current from the integration events that module publishes. It carries the identifier `read-model/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/read-models/<name>/<name>.yaml`, filed with the module that reads it. A read model is named for the thing it keeps one row per, as a singular noun phrase such as `reimbursed-expense`. A use case names the read model in `reads`, as it names an aggregate, and reads its own copy instead of calling the owning module. It carries `summary`, and Sources, Freshness and an optional Example live in the explanation file beside the record file.

| Field | Meaning |
|:---|:---|
| `keyedBy` | The `aggregate/...` of another module the read model keeps one row per. |
| `fields` | `{ name, meaning, type, of, many, from, accumulate, classification, erasedBy }` entries. `from` names the published field as `integration-event/<name>#<field>`; `accumulate` is `latest` (the default), `sum`, or `count`; `type`, `of`, `many` and `classification` read as on use-case inputs. |
| `erasedBy` | On a `personal` or `sensitive` field, the `integration-event/...` whose arrival erases the field, required. |
| `removedBy` | The `integration-event/...` ids whose arrival removes the row, optional. |
| `freshness` | How stale a reader may see a row: `{ mode, within, decidedBy }` with `mode` of `immediate`, `snapshot`, or `eventual`, `within` an ISO 8601 duration, and `decidedBy` naming the command use case or aggregate that decides the row at write time, as on query output groups. |
| `terms` | The glossary terms used, optional. |

A read model's code is a set of projections, one per source integration event, each writing only its read model, and the reader its module's use cases call. Each carries `implements read-model/<module>.<name>`. Every source event carries an identity, and a redelivery never counts twice. `latest` compares source versions, not arrival order. Corrections arrive as events. A rebuild replays retained events from the start. Staleness is measured from the source event's time. An erased key keeps a tombstone that a rebuild applies, so no rebuild restores erased personal data. A decision that cannot accept the read model's `freshness` is decided in the owning module instead, as [Decide Current Truth in the Owning Module](#decide-current-truth-in-the-owning-module) states. With the outbox extension, each integration event a read model names is kept for as long as a read model may rebuild from it.

### Processes

A long-running process is states, reactions and timed transitions, not a record of its own. An aggregate holds the current state. A reaction answers each fact and runs the command moving to the next state. A transition that time moves carries `after` with the duration from entering its source state and names the moving job in `movedBy`, as [Move a State by Time with a Timed Transition](#move-a-state-by-time-with-a-timed-transition) states. Work spanning modules raises its completion fact only after every module reports done, as [Raise Completion Only When Every Module Is Done](#raise-completion-only-when-every-module-is-done) states.

Library example: a hold moves Placed, Ready, Collected, Expired. Placing the hold starts the pickup window. A job named in `movedBy` with `after` of `P7D` moves an uncollected Ready hold to Expired. The expire-holds command runs once per hold, and the hold-placed and hold-expired domain events announce each move.

Compensation undoes a committed step when a later step fails. Each step of a multi-step process commits before the next runs, so a failure cannot roll earlier steps back; the process answers the failure with a compensating reaction, a reaction that runs the undoing command and names the undone step's use case in `compensates`. This is the compensating transaction pattern. Library example: reserving a copy commits the reservation, and when the later charge fails, a reaction answers the charge failure, runs the release command, and names the reserve use case in `compensates`.

### Exports

A file export is a query producing a file of many rows. It cites the row and size settings bounding it and keeps its resume mark as aggregate state, so an interrupted export resumes instead of restarting. The export query declares its output as a file with its content type and size limit, and each run writes the last exported key to the aggregate before returning the file.

### Reading Another Module

A use case reads another module's state in one of three ways. The table states when each applies.

| Need | Way |
|:---|:---|
| The reading module accepts a lag up to a stated freshness | A read model its own module keeps, named in `reads` |
| The decision needs the owner's current value | A decision port the command calls, with the use case stating why the lag is not enough |
| A person sees figures from several owners on one page | A screen composing each owner's query, with no module reading another |

A read model is the normal case. A decision port answers one question with the owner's current value and nothing else. A composed screen joins each owner's query at display time.

### Validity and Storage

Validity and storage are two separate facts. A credential or session is invalid from its expiry, checked when used. A cleanup job removes expired records within a stated interval. The expiry refuses the use; the job reclaims the rows. Neither replaces the other, as [Keep Validity Apart from Storage](#keep-validity-apart-from-storage) states.

### Runbook

A runbook is the steps to run or recover the product. It carries the identifier `runbook/<name>` and lives in Implementation, at product or module level, named verb-first for what the reader does, as the [record language page](language.md) states. Its `trigger` names when to open it. A job names its runbook in `runbook`, and any record or effect names its recovery in `onFailure.runbook`. It keeps its `answers` link to the job and the alert label. Its fields are `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, and `escalation`.

## Standards

### Trigger Every Reaction from Its Declared Events

**ID:** `litespec/core/automation#trigger-every-reaction-from-its-declared-events`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** Each reaction MUST declare in `triggeredBy` the domain events of its own module or the integration events it answers.

**Rationale:** A reaction without a trigger never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the events states when the reaction fires: the domain events of its own module or the integration events it answers, one id or a list. A trigger on another module's domain event couples the reaction to state its module does not own; the owning module publishes an integration event instead. A `skippedWhen` clause states when it skips.

**Example:** The send-due-notice reaction answers the loan-borrowed domain event and runs the send-due-notice command.

**Checked by:** `litespec model check` reports `reaction-trigger-unknown` (level G) for a trigger naming an unknown event, `reaction-trigger-foreign` (level G) for a trigger naming another module's domain event, and fails a reaction without a `triggeredBy` list.

### Run Every Job from Its Declared Kind

**ID:** `litespec/core/automation#run-every-job-from-its-declared-kind`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Each job MUST declare its `kind` of `scheduled` or `one-off` with its `schedule` where scheduled.

**Rationale:** A job without a kind never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the kind states when the job runs: the schedule with its `every` duration, `cron` expression and timezone, or `at: startup` for a run once at deployment start, or `one-off` for work that runs once.

**Example:** The expire-lapsed-holds job runs every six hours over open holds past their window.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a schedule neither an `every` duration, a `cron` expression, nor `at: startup`, and fails a job without its `kind` or a scheduled job without `schedule`.

### Declare Every Reaction and Job Edge in Effects

**ID:** `litespec/core/automation#declare-every-reaction-and-job-edge-in-effects`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** A reaction or job with no declared edge observes and never acts, while an edge outside `effects` hides behavior the checks never see. Declaring each edge in `effects` with exactly one verb of `runs`, `sends`, or `calls` keeps every consequence of the reaction or job beside its trigger, under one grammar for every record that has edges, as the [use-cases page](use-cases.md) states.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job with no `effects` or with a top-level effect verb.

### Declare the Outcomes of Each Reaction and Job

**ID:** `litespec/core/automation#declare-the-outcomes-of-each-reaction-and-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its run endings in `outcomes` from the closed set.

**Rationale:** A run without declared endings leaves each reader to guess what a run means: a skipped item reads as a failure, a suppressed send reads as a bug. Declaring each ending with its meaning states what a run produces: `completed` for work done, `skipped` for work set aside by its skip condition, `suppressed` for a send held by suppression, `failed` for work ending in refusal or error, `retry-scheduled` for work waiting on another attempt, and `nothing-selected` for a run finding no item. Scenarios end in one of these endings or in a run use case result, so every run has a proof. Settled by [decision/0081-own-scenarios-and-declare-outcomes](decision/0081-own-scenarios-and-declare-outcomes).

**Example:** The send-due-notice reaction declares `completed` for a sent notice and `suppressed` for a notice held because the reader opted out.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job without `outcomes` or with a name outside the closed set.

### Write No Aggregate Directly from a Reaction or Job

**ID:** `litespec/core/automation#write-no-aggregate-directly-from-a-reaction-or-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** A reaction or job MUST NOT write an aggregate except through the use case it runs.

**Rationale:** A reaction or job writing directly splits the write path: the same aggregate changes through a reviewed command and through an unreviewed reaction or job, each with its own guards. Running a small command keeps every write behind a use case with its access, consistency, results, and scenarios.

**Reviewed for:** Which reactions or jobs write an aggregate outside the use case they run? Look at the reaction and job effects beside the use cases.

### File Every Reaction and Job with the Module It Changes

**ID:** `litespec/core/automation#file-every-reaction-and-job-with-the-module-it-changes`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST live with the module whose records it changes, or with the module owning the notification it sends when it only sends.

**Rationale:** Reactions and jobs filed by trigger scatter: one module behavior hides on a dozen trigger pages, and nobody sees the module whole. Filing each reaction and job with the module it changes keeps each module behavior in one place. Its event handler or job class lives in that module's code with what it runs: in the folder of the use case it runs, or with the notification it sends or the system it calls (`litespec/workspace/naming#place-each-trigger-with-what-it-runs`). The trigger page lists the reactions answering its event, derived from records.

**Reviewed for:** Which reaction or job pages sit outside the module they change? Look at the reactions and jobs beside the modules whose records they change.

### Run at Most One Use Case from a Reaction or Job

**ID:** `litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** A reaction or job MUST run at most one use case, a job running it once per selected item.

**Rationale:** A reaction or job is an entry point: it turns one fact or one selected item into one message, as an endpoint turns one request into one message. A reaction that ran two use cases would decide their order and their joint failure in code no record states, and the second would fail after the first committed. Two follow-on acts are two reactions to the same fact, each with its own failure handling. A reaction or job that only sends or calls runs no use case. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** The erase-expired-personal-data job selects decided expenses past retention and runs `use-case/spending.erase-personal-data` once for each.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job with more than one `runs` entry.

### Run Each Effect on Its Own

**ID:** `litespec/core/automation#run-each-effect-on-its-own`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each effect of a reaction or job MUST run, retry and deduplicate on its own, with any order expressed by chaining facts.

**Rationale:** Effects sharing one retry decide their joint failure in code no record states: the second send waits on the first, a retry doubles the first while fixing the second. Running each effect on its own keeps each consequence independent, with its own idempotency and its own failure handling. An order between effects is a fact the first produces and the second answers, never an assumed sequence.

**Example:** The loan-borrowed reaction sends the due notice and calls the statistics system as two independent effects; a retry of the call never resends the notice.

**Reviewed for:** Which reactions or jobs assume an order between effects or share one retry across effects? Look at the effects beside their failure handling and idempotency.

### Declare Each Read Model Field from a Published Field

**ID:** `litespec/core/automation#declare-each-read-model-field-from-a-published-field`

**Strength:** required

**Applies to:** artifact: record:read-model

**Statement:** Each read model MUST name in `keyedBy` an aggregate of another module and, on each field, name in `from` the outbound integration event and the payload field the field comes from.

**Rationale:** A read model is a copy, and a copy with no stated source drifts: nobody can tell which event fills a field, so a renamed payload field breaks the read model without a finding. Naming the event and its field makes every crossing of the module boundary a declared edge the check reads, the same way an effect names its target. A read model keyed by its own module's aggregate copies state the module already reads directly. A use case names the read model in `reads`, and a read model no use case reads is kept current for no reader. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model). Settled by [decision/0084-give-read-models-their-semantics](decision/0084-give-read-models-their-semantics).

**Example:** `read-model/reporting.reimbursed-expense` is keyed by `aggregate/spending.expense`, and its `amount` field comes from `integration-event/expense-reimbursed#amount`.

**Checked by:** `litespec model check` reports `read-model-source-unknown` (level G) for a `from`, `erasedBy` or `removedBy` naming no outbound integration event or a payload field the event does not carry, `read-model-key-invalid` (level G) for a `keyedBy` naming an aggregate of the read model's own module, and `read-model-unused` (level G) for a read model no use case reads.

### Erase a Read Model Personal Field with Its Owner

**ID:** `litespec/core/automation#erase-a-read-model-personal-field-with-its-owner`

**Strength:** required

**Applies to:** artifact: record:read-model

**Statement:** Each `personal` or `sensitive` read model field MUST name in `erasedBy` the integration event whose arrival erases it.

**Rationale:** A read model copies data out of the module that owns its erasure, so a copy without an erasure trigger outlives the owner's erasure and keeps personal data the product promised to remove. Naming the erasing event ties the copy to the owner's erasure position, and the projection for that event clears the field. The erased key keeps a tombstone that a rebuild applies, so no rebuild restores the erased field. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model).

**Example:** The `employeeId` field of `read-model/reporting.reimbursed-expense` is personal, so it names `integration-event/expense-personal-data-erased` in `erasedBy`, and the projection for that event clears it.

**Checked by:** `litespec model check` reports `read-model-erasure-missing` (level G) for a `personal` or `sensitive` read model field with no `erasedBy`.

### Decide Current Truth in the Owning Module

**ID:** `litespec/core/automation#decide-current-truth-in-the-owning-module`

**Strength:** required

**Applies to:** artifact: record:use-case, record:read-model

**Statement:** A decision that needs current truth MUST be decided in the owning module, never from a read model.

**Rationale:** A read model lags by its freshness: revoked permissions, available funds, reservations and period locks move after the copy was taken. Guarding such a decision from a copy accepts a stale answer where the business needs the current one. The owning module decides through a decision port the command calls and revalidates within its own commit, or by its own invariant. A read model serves reads that accept the lag.

**Example:** The library refuses a hold on a lent copy by asking the lending module for current availability instead of reading a reporting copy.

**Reviewed for:** Which use cases guard a decision needing current truth from a read model instead of the owning module? Look at the reads beside the decision ports.

### Move a State by Time with a Timed Transition

**ID:** `litespec/core/automation#move-a-state-by-time-with-a-timed-transition`

**Strength:** required

**Applies to:** artifact: record:aggregate

**Statement:** Each transition that time moves MUST name its duration from entering its source state in `after` and the moving job in `movedBy`.

**Rationale:** A deadline with no stated duration and no moving job expires whenever its author remembered: holds linger, reservations never release. Naming the duration and the job states the timed move beside the lifecycle: the job selects instances past their window and runs the moving command once per instance. The process pattern of states, reactions and timed transitions covers long-running work with no process record.

**Example:** The hold transition from Ready to Expired carries `after: P7D` and `movedBy: job/lending.expire-lapsed-holds`, and the job runs the expire-holds command once per lapsed hold.

**Checked by:** `litespec model check` reports `transition-unknown` (level G) for a timed transition naming a missing state or a job no record declares.

### Raise Completion Only When Every Module Is Done

**ID:** `litespec/core/automation#raise-completion-only-when-every-module-is-done`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Work that spans modules MUST raise its completion fact only after every module reports its part done.

**Rationale:** A completion raised early declares done while parts remain: erased data returns, closed work reopens. Each module's reaction erases or closes its part and reports done, and the completion is raised only when every report arrived. Progress lives in a record outliving the deleted data, and the requesting state blocks new writes until completion.

**Example:** Erasing a departed employee's personal data enters Erasing, which blocks new writes. The spending reaction erases expenses and the access reaction erases sessions, each reporting done. The employee becomes Erased only when both report, and the erasure record keeps the fact and date outside the erased data.

**Reviewed for:** Which spanning work raises completion before every module reports done, or keeps progress in data the work deletes? Look at the completion reactions beside the module reports.

### Name the Step a Compensating Reaction Undoes

**ID:** `litespec/core/automation#name-the-step-a-compensating-reaction-undoes`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** A reaction that undoes a committed step of a multi-step process MUST name the undone use case in `compensates`, run a use case other than the one it names, and answer an event the undone use case does not raise.

**Rationale:** A multi-step process commits each step before the next runs, so a later failure cannot roll the earlier steps back: each committed step needs an undo that runs only when a later step fails. Without the link, a reader sees a reaction running an undoing command and cannot tell which committed step it reverses. Running the named use case again redoes the step instead of undoing it, and answering an event the undone step raises undoes every success. Naming the undone step in `compensates` states the undo beside the reaction, with its own trigger and failure handling.

**Example:** The library reserves a copy with `use-case/lending.reserve-copy`, which commits the reservation. When the later charge fails, `use-case/lending.record-charge-failure` raises `domain-event/charge.charge-failed`. The reaction `reaction/lending.release-copy-on-charge-failed` answers that event, runs `use-case/lending.release-copy`, and carries `compensates: use-case/lending.reserve-copy`, so a reader sees which committed step the release undoes.

**Checked by:** `litespec model check` reports `compensation-target-invalid` (level G) for a `compensates` naming a use case that is not a command or the same use case the reaction runs, `compensation-trigger-invalid` (level G) for a compensating reaction answering an event the compensated use case lists in `raises`, and `reference-unknown` (level G) for a `compensates` naming no written record. The trigger check is a minimum: a reviewer confirms the trigger reports a later step's failure.

### Keep Validity Apart from Storage

**ID:** `litespec/core/automation#keep-validity-apart-from-storage`

**Strength:** required

**Applies to:** artifact: record:aggregate, record:job

**Statement:** Each expiring credential or session MUST refuse its use from its expiry and be removed by a cleanup job within a stated interval.

**Rationale:** An expiry that only deletes leaves a window where an expired credential still works; a check without cleanup leaves dead rows forever. Refusing the use from the expiry keeps validity at the moment of use, and a cleanup job removing expired records within a stated interval reclaims storage. The two are separate: the check refuses, the job removes.

**Example:** A library session past its expiry is refused at its next use, and the expire-sessions job removes expired sessions within one day.

**Reviewed for:** Which expiring credentials or sessions lack a use-time refusal from expiry or a cleanup job with a stated removal interval? Look at the expiry rules beside the cleanup jobs.

### Name Each Reaction for Its Act and Its Trigger

**ID:** `litespec/core/automation#name-each-reaction-for-its-act-and-its-trigger`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** Each reaction MUST be named `<verb>-<what>-on-<fact>` with a declared verb.

**Rationale:** A reaction runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` could fire on a borrow, a hold, or a reminder run. Naming the act and the fact lets a reader find every consequence of a fact by its name, as `litespec/core/language#name-each-record-by-its-types-name-form` states.

**Example:** `reaction/lending.send-due-notice-on-loan-borrowed` answers `domain-event/loan.loan-borrowed`.

**Reviewed for:** Which reaction names hide their act or their trigger? Look at the reaction names beside their triggering events.

### Name Each Job for the Use Case It Runs

**ID:** `litespec/core/automation#name-each-job-for-the-use-case-it-runs`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Each job MUST be named `<verb>-<what>` with the verb of the use case it runs, or with its own verb when it runs no use case.

**Rationale:** A job selects items and runs one command for each, so naming it for that command lets a reader go from the schedule to the behavior in one step, as `litespec/core/language#name-each-record-by-its-types-name-form` states. A job that only sends or calls takes its own verb, since no use case verb names it.

**Example:** `job/lending.expire-lapsed-holds` runs `use-case/lending.expire-holds`.

**Checked by:** `litespec model check` reports `job-name-invalid` (level G) for a job outside `<verb>-<what>` with the verb of the use case it runs or its own verb when it runs none.

### State the Failure Handling of Every Reaction and Job

**ID:** `litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its `onFailure` in the one failure shape and its idempotency.

**Rationale:** A failed reaction or job without handling retries forever or drops silently: notices double-send, holds never clear. Declaring `onFailure` states the failure shape beside the trigger: `mode` of `retry`, `skip-and-log`, or `discard`; `retry` with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `ifExhausted` of `dead-letter`; `deadLetter` carrying `after` with the action a person takes; `surfacesIn` naming the entry point showing the failure and `runbook` the recovery procedure. `retry` and `ifExhausted` are required with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations, and prose handling is refused. The same shape serves every record and every effect entry. The idempotency mode, usually `state`, states how repeats collapse.

**Checked by:** `litespec model check` fails a reaction or job with `onFailure` outside the one failure shape, and reports `record-invalid` for prose handling, a missing or malformed retry, and a malformed duration.

### Point Reaction and Job Scenarios at the Run Result

**ID:** `litespec/core/automation#point-reaction-and-job-scenarios-at-the-run-result`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each scenario on a reaction or job MUST end in a result of a use case its effects run or in one of its outcomes.

**Rationale:** A reaction or job scenario ending anywhere else proves another behavior story: it walks the trigger and asserts an ending no run use case declares and no outcome names. Ending in a run use case result or in a declared outcome keeps the scenario a proof of the reaction or job consequence. A send-only reaction or job ends its scenarios in outcomes, with no invented command.

**Checked by:** `litespec model check` reports `scenario-result-unknown` for a reaction or job scenario ending outside the run use case results and the declared outcomes.

### Keep Job Fields off Reactions

**ID:** `litespec/core/automation#keep-job-fields-off-reactions`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** A reaction MUST NOT carry `kind`, `schedule`, `runbook`, `runsWhen`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

**Rationale:** Run fields describe work over time: the kind, the schedule, what each run selects, how many items per batch, the lock, where resume state lives, the condition it runs under, the promised service level, and the evidence the run leaves. A reaction answers one event and carries none of that, so a run field on it claims a schedule the trigger never states. Settled by [decision/0015-automations-and-sagas](decision/0015-automations-and-sagas) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** The send-due-notice reaction answers its event with no `selection` and no `schedule`.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction carrying `kind`, `schedule`, `runbook`, `runsWhen`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

### Write Every Runbook to Run or Recover

**ID:** `litespec/core/automation#write-every-runbook-to-run-or-recover`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Each runbook MUST state its `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, and `escalation`.

**Rationale:** A runbook named by a job, a failure policy, or an alert is the answer a person opens under pressure: `impact` says what breaks, `prerequisites` say what to hold, `procedure` says each step, `verification` says how to confirm, `recovery` says how to undo, and `escalation` says who to call. A runbook missing one leaves the reader guessing at the worst moment.

**Example:** The clear-stuck-reimbursements runbook states its impact on waiting expenses, its prerequisites, its procedure, its verification query, its recovery, and its escalation to the spending team.

**Checked by:** `litespec model check` reports `record-invalid` for a runbook page without `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, or `escalation`.

## Conventions

None.
