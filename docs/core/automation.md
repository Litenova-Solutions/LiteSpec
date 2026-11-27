# Automation

**Stack:** any
**Concern:** application

This page states the work running without a request: reactions answering events, jobs running on schedule or once, module events carrying facts between modules, and read models a module keeps current from the module events another module publishes. It is for agents who write automation records from Intent and for reviewers who check that every background effect is declared. An undeclared effect runs where no record says it does.

## Concepts

### Reactions

A reaction answers an event. When a domain event, a module event, or an integration event arrives, the reaction runs. It carries the identifier `reaction/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/reactions/<name>/<name>.yaml`. It is filed with the module whose own records the reaction changes, with the sending module when it changes another module only by sending that module's public command, or with the module that owns the notification when the reaction only sends one.

A reaction changes aggregates only through the use case it runs. Where a change is needed, the reaction runs a small command making it. The use case carries the access, consistency, results, and scenarios, so the write path stays behind a reviewed command.

The name follows `<verb>-<what>-on-<fact>`. The record carries `summary`, and the explanation file beside it describes the trigger and effect under Trigger and Effect, and the failure handling under Failure Handling, with an optional Example.

| Field | Meaning |
|:---|:---|
| `triggeredBy` | The `domain-event/...`, `module-event/...`, or `integration-event/...` ids the reaction answers, as one id or a list: a domain event of its own module, a module event of an upstream module of the product, or an inbound integration event. |
| `skippedWhen` | The skip condition in domain words, completing "when ...", optional. |
| `compensates` | The `use-case/...` id the reaction undoes, for a reaction answering a later step's failure, optional. |
| `effects` | `{ runs / sends / calls / queries / commands, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states, and at most one `runs` entry. A reaction leaves `timing` out for `async`. Each effect runs on its own. |
| `outcomes` | The run endings: `{ name, meaning }` entries with `name` from `completed`, `skipped`, `suppressed`, `failed`, `retry-scheduled`, `nothing-selected`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads` | The requirements carried, the flow steps served, the failure handling, the idempotency, and the aggregates and settings read, as on a use case. |
| `onFailure` | `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`: `mode` is `retry`, `skip-and-log`, or `discard`; `retry` is `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `ifExhausted` is `dead-letter`; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. `retry` and `ifExhausted` come with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the triggering event fields, which the record leaves unlisted. |
| `tracksState` | `{ aggregate, pending, done, refused }`: the aggregate the run use case writes with the pending state the run leaves, the done state it enters, and the refused state a terminal refusal enters, where the reaction runs a use case writing an aggregate; `refused` stays out where another attempt returns the run to pending. |

Scenarios sit under the reaction as child entries, and each one ends in a result of a use case the reaction runs or in one of the reaction's own outcomes. The event handler carrying it out marks itself with `implements reaction/<module>.<name>`.

An event with several outcomes needs one reaction per outcome, because a reaction runs at most one use case. In Claimwick, the expense decision ends approved or refused: `domain-event/expense.approved` triggers the reaction running `use-case/spending.reimburse-expense`, while `domain-event/expense.refused` triggers the reaction sending the refusal notice. There is no shared decided event with one reaction choosing between the two.

### Module Events

A module event is a fact one module publishes for the other modules of the same product. It is a contract between modules: the publishing module states the fact once, and each consuming module answers it from a reaction or copies it into a read model. The event travels in process through the transactional outbox, with no external transport. A module event stays inside the bounded context. A fact for readers outside the product is an integration event instead, as the [Edges and Screens page](edges.md) states.

The event carries the identifier `module-event/<module>.<name>`, such as `module-event/spending.expense-approved`, and lives at `.litespec/behavior/modules/<module>/aggregates/<aggregates>/module-events/<name>/<name>.yaml`, under the aggregate whose domain event it translates. The record carries `summary` and `compatibility`, and the explanation file beside it restates Compatibility from `compatibility` and carries Consumers.

| Field | Meaning |
|:---|:---|
| `translates` | The domain event of its own module it carries; the event publishers are the use cases raising it, so each module event has exactly one publishing module. |
| `fields` | The names of the fields of the translated domain event that the module event publishes. |
| `compatibility` | How the event stays compatible with its consumers. |
| `terms` | The glossary terms used, optional. |

A module event carries no `direction`, no CloudEvents identity, no `trust`, and no `via`: it travels one way between known modules, and nothing outside the product reads it. It keeps its record and name as the published contract between modules, and it selects in `fields` the fields of the domain event it translates, publishing only `fields` that event carries. The book shows each selected field with the domain event type and meaning. No record declares the publishing as an effect, and no user code publishes the event. The pipeline step staging raised domain events also stages each module event translating them, built from the domain event alone, in the same commit.

In Claimwick, the spending module publishes `module-event/spending.expense-approved` when an expense is approved, and the payouts module answers it from a reaction paying the reimbursement.

### Jobs

A job runs on a schedule or once, with no event to answer. It carries the identifier `job/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/jobs/<name>/<name>.yaml`. It is filed with the module whose own records it changes, with the sending module when it changes another module only by sending that module's public command, or with the module that owns the notification when it only sends one.

Work a person starts by hand is a command, not a job, and its entry point and access state who starts it.

A job changes aggregates only through the use case it runs. Where a change is needed, the job runs a small command making it. The use case carries the access, consistency, results, and scenarios, so the write path stays behind a reviewed command.

The name follows `<verb>-<what>`, using the verb of the use case it runs, or its own verb where it runs no use case. The record carries `summary`, and the explanation file beside it describes how each run starts and what it covers under Invocation, and the failure handling under Failure Handling, with an optional Example.

| Field | Meaning |
|:---|:---|
| `kind` | `scheduled` or `one-off`. |
| `schedule` | The schedule, with `scheduled`: `{ "every": "PT1M" }`, `{ "cron": "0 3 * * *", "timeZone": "..." }`, or `{ "at": "startup" }` for a run once at deployment start. The time zone is a literal IANA zone, or `{ "setting": "setting/<module>.<name>" }` or `{ "field": "aggregate/<module>.<aggregate>#<field>" }` naming where the zone is stored. `every` accepts a `settingId` naming a setting. |
| `selection`, `batchSize`, `lease`, `cursor`, `runbook`, `runsWhen`, `serviceLevel`, `evidence` | What each run selects, how many items per batch, the lock with its duration, where resume state lives, the runbook answering the run, the condition it runs under, the promised service level, and the evidence the run leaves. |
| `effects` | `{ runs / sends / calls / queries / commands, timing }` entries with exactly one verb, as the [use-cases page](use-cases.md) states, and at most one `runs` entry. A job states `timing` on each entry. Each effect runs on its own. |
| `outcomes` | The run endings: `{ name, meaning }` entries with `name` from `completed`, `skipped`, `suppressed`, `failed`, `retry-scheduled`, `nothing-selected`. |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads` | The requirements carried, the flow steps served, the failure handling, the idempotency, and the aggregates and settings read, as on a use case. |
| `onFailure` | `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`, shaped as on a reaction. Delays and `after` are ISO 8601 durations. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the run window fields, which the record leaves unlisted. |
| `tracksState` | `{ aggregate, pending, done, refused }`, shaped as on a reaction, where the job runs a use case writing an aggregate. |

Scenarios sit under the job as child entries, and each one ends in a result of a use case the job runs or in one of the job's own outcomes. The job host marks itself with `implements job/<module>.<name>`.

### Read Models

A read model is a module's own copy of facts it needs from another module's aggregate. The owning module publishes module events, and the reading module keeps its copy current from them instead of calling the owning module. The record carries the identifier `read-model/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/read-models/<name>/<name>.yaml`. It is filed with the reading module.

The name is a singular noun phrase for the thing the model keeps one row per, such as `reimbursed-expense`. A use case lists the read model in `reads`, the same way it lists an aggregate, and reads its own copy. The record carries `summary`, and the explanation file beside it describes the sources under Sources and the staleness under Freshness, with an optional Example.

| Field | Meaning |
|:---|:---|
| `keyedBy` | The `aggregate/...` of another module the read model keeps one row per. |
| `fields` | `{ name, meaning, type, identifies, items, from, accumulate, classification, erasedBy }` entries. `from` names the published field as `module-event/<module>.<name>#<field>` or `integration-event/<name>#<field>`; `accumulate` is `latest` (the default), `sum`, or `count`. A list field uses `type: list` with `items:`. |
| `erasedBy` | On a `personal` or `sensitive` field, the `module-event/...` or `integration-event/...` whose arrival erases the field. |
| `removedBy` | The `module-event/...` or `integration-event/...` ids whose arrival removes the row, optional. |
| `freshness` | How stale a row reads when a reader sees it: `{ mode, within, decidedBy }` with `mode` of `immediate`, `snapshot`, or `eventual`, `within` an ISO 8601 duration, and `decidedBy` naming the command use case or aggregate deciding the row at write time. |
| `terms` | The glossary terms used, optional. |

In code, a read model is a set of projections with one reader: one projection per source event, each writing only its own read model. The module's use cases read through the reader. Each projection and the reader mark themselves with `implements read-model/<module>.<name>`.

The copy stays sound under retry and rebuild. Every source event carries an identity, so a redelivery counts nothing twice. The `latest` accumulation compares source versions, not arrival order, and corrections arrive as events. A rebuild replays retained events from the start, and staleness is measured from the source event's time.

An erased key keeps a tombstone that a rebuild applies, so no rebuild restores erased personal data. A decision turning away from the read model `freshness` is decided in the owning module instead. With the outbox extension, each event a read model names is kept for as long as a read model rebuilds from it.

### Ways to Read Another Module

A use case needing state from another module has three options:

| Need | Use |
|:---|:---|
| The decision needs the owner current value | The owner public query, sent with the behavior declaring the edge in `effects.queries` |
| The reading module needs the data reshaped and kept locally | A read model its own module keeps, named in `reads` |
| A person sees figures from several owners on one page | A screen composing each owner query, with no module reading another |

The public query is the normal choice. The provider's public query answers with the owner current value and nothing else. The read model stays for data the consumer reshapes and keeps locally. A composed screen joins each owner query at display time.

### Long-Running Processes

A long-running process has no record of its own. It is built from states, reactions, and timed transitions working together: the aggregate carries the current state, and a reaction answers each fact by running the command moving to the next state.

A transition moved by time carries `after` with the duration measured from entering its source state, and names the moving job in `movedBy`. Work spanning modules raises its completion fact only after every module reports its part done.

In the library, a hold moves through Placed, Ready, Collected, and Expired. Placing the hold starts the pickup window. A job named in `movedBy` carries `after` of `P7D` and moves an uncollected Ready hold to Expired. The expire-holds command runs once per hold, and the hold-placed and hold-expired domain events announce each move.

Compensation undoes a committed step where a later step fails. Each step of a multi-step process commits before the next runs, so a failure rolls the earlier steps back nowhere. The process answers the failure with a compensating reaction: a reaction running the undoing command and naming the undone step's use case in `compensates`. This is the compensating transaction pattern.

In the library, reserving a copy commits the reservation. Where the later charge fails, a reaction answers the charge failure, runs the release command, and names the reserve use case in `compensates`.

### File Exports

A file export is a query producing a file of many rows. The export query declares its output as a file with its content type and size limit, and cites the row and size settings bounding it. Each run writes its resume mark, the last exported key, to the aggregate before returning the file, so an interrupted export resumes instead of restarting.

### Expiry Checks and Cleanup Jobs

Validity and storage are two separate facts. A credential or session becomes invalid at its expiry, and the check at use time refuses it from then on. A cleanup job removes expired records within a stated interval, reclaiming the rows. Neither replaces the other.

### Runbooks

A runbook lists the steps running or recovering the product: what breaks, what to hold, each step, how to confirm, how to undo, and who to call. It carries the identifier `runbook/<name>` and lives in Implementation, at product or module level.

The name starts with a verb for what the reader does. The `trigger` field names when to open it.

A job names its runbook in `runbook`, and a reaction or job names its recovery in `onFailure.runbook` on the record, while a use-case effect names its recovery on the effect entry. The runbook keeps its `answers` link to the job and the alert label. Its fields are `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, and `escalation`.

## Standards

### Trigger Every Reaction from Its Declared Events

**ID:** `litespec/core/automation#trigger-every-reaction-from-its-declared-events`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** Each reaction MUST declare in `triggeredBy` the domain events of its own module, the module events of upstream modules of the product, or the inbound integration events it answers.

**Rationale:** A reaction without a trigger has no defined moment to run. Naming the events states when it fires, as one id or a list. The reaction answers the domain events of its own module, upstream module events, or inbound integration events, and answers no other module domain event and no downstream module fact. Answering another module domain event couples it to state its module owns poorly. The owning module publishes a module event instead.

**Example:** In the library, `reaction/lending.send-due-notice-on-loan-borrowed` answers `domain-event/loan.loan-borrowed` and runs the send-due-notice command.

**Checked by:** `litespec model check` reports `reference-unknown` (level G) for a trigger naming an unknown event, `reaction-trigger-foreign` (level G) for a trigger naming another module's domain event, and fails a reaction without a `triggeredBy` list.

### Publish Every Module Event from the Event It Translates

**ID:** `litespec/core/automation#publish-every-module-event-from-the-event-it-translates`

**Strength:** required

**Applies to:** artifact: record:module-event, record:use-case

**Statement:** Each module event MUST name in `translates` the domain event of its own module it carries, and is published only from that domain event, by the pipeline step that stages raised events.

**Rationale:** Where a handler builds a module event from whatever the aggregate carries after the act, it publishes state the act announced nowhere, and a second handler publishes the same event with different fields. Deriving the event from the one domain event it translates keeps every published field a field of the announced fact. The publishing use cases then follow from `raises`, with no second declaration to drift. A publish call in user code is an edge no record declares, so the stack reader reports it.

**Example:** Claimwick's `module-event/spending.expense-approved` translates `domain-event/expense.approved`, so `use-case/spending.approve-expense`, raising that event, publishes it, and its handler publishes nothing. The payouts module answers the module event from a reaction.

**Checked by:** `litespec model check` reports `record-invalid` for a module event without `translates` or `fields`, `event-field-uncarried` (level G) for a field name its translated domain event does not carry, `event-publisher-invalid` for a module event with a second publishing module, and `effect-undeclared` for an event a handler, event handler or job publishes itself; `dotnet build` reports `LS2022` for an event published by user code.

### Subscribe to Every Module Event

**ID:** `litespec/core/automation#subscribe-to-every-module-event`

**Strength:** required

**Applies to:** artifact: record:module-event

**Statement:** Each module event MUST be named by a reaction, a read model projection or another consumer that answers or copies it.

**Rationale:** A module event is a contract between modules. Publishing one nobody consumes stages an event on every commit that no work reads, and a reader tells poorly whether the fact matters or the consumer was forgotten. Naming the consumer makes the crossing complete: the reaction answers the fact, or the projection copies it into the reading module's own store.

**Example:** Claimwick's `module-event/spending.expense-approved` is named by the payouts reaction paying the reimbursement. A second event with no reaction or read model is reported until its consumer is written or the event is removed.

**Checked by:** `litespec model check` reports `module-event-unused` (level G) for a module event that no reaction in `triggeredBy`, no read model field in `from`, `erasedBy` or `removedBy`, and no other consumer names.

### Run Every Job from Its Declared Kind

**ID:** `litespec/core/automation#run-every-job-from-its-declared-kind`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Each job MUST declare its `kind` of `scheduled` or `one-off` with its `schedule` where scheduled.

**Rationale:** A job without a kind has no defined moment to run. Naming the kind states when it runs. A scheduled job carries its `schedule`, with an `every` duration, a `cron` expression and timezone, or `at: startup` for a run once at deployment start. A `one-off` job runs once.

**Example:** `job/lending.expire-lapsed-holds` runs every six hours over open holds past their window.

**Checked by:** `litespec model check` reports `schedule-invalid` (level G) for a schedule neither an `every` duration, a `cron` expression, nor `at: startup`, and fails a job without its `kind` or a scheduled job without `schedule`.

### Declare Every Reaction and Job Edge in Effects

**ID:** `litespec/core/automation#declare-every-reaction-and-job-edge-in-effects`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** A reaction or job with no declared edge observes and acts nowhere, and an edge written outside `effects` hides behavior the checks see poorly. Declaring each edge in `effects`, with exactly one verb of `runs`, `sends`, `calls`, `queries`, or `commands`, keeps every consequence beside the trigger or schedule, in the one grammar every record with edges shares.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job with no `effects` or with a top-level effect verb.

### Declare the Outcomes of Each Reaction and Job

**ID:** `litespec/core/automation#declare-the-outcomes-of-each-reaction-and-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its run endings in `outcomes` from the closed set.

**Rationale:** A run without declared endings leaves each reader guessing: a skipped item reads as a failure, and a suppressed send reads as a bug. Declaring each ending with its meaning states what a run produces: `completed` for work done, `skipped` for work set aside by its skip condition, `suppressed` for a send held by suppression, `failed` for work ending in refusal or error, `retry-scheduled` for work waiting on another attempt, and `nothing-selected` for a run finding no item. Scenarios end in one of these endings or in a run use case result, so every run has a proof. Settled by [decision/0081-own-scenarios-and-declare-outcomes](decision/0081-own-scenarios-and-declare-outcomes).

**Example:** The send-due-notice reaction declares `completed` for a sent notice and `suppressed` for a notice held because the reader opted out.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job without `outcomes` or with a name outside the closed set.

### File Every Reaction and Job with the Module It Changes

**ID:** `litespec/core/automation#file-every-reaction-and-job-with-the-module-it-changes`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST live with the module whose own records it changes, with the sending module when it changes another module only by sending that module's public command, or with the module owning the notification when it only sends one.

**Rationale:** Filing by trigger spreads one module's work across many trigger pages, so no page shows the module whole. Filing each reaction and job with the module whose records it changes keeps each module's behavior in one place. A module changes another module only through that module's public command, sent from a reaction or job. The sending module holds that fact and its reaction, so the reaction sits with the sender.

**Reviewed for:** Does every reaction and job sit with the module whose own records it changes, or with the sending module when it changes another module only through that module's public command? Check the reactions and jobs beside the modules whose records they change and the public commands they send.

### Run at Most One Use Case from a Reaction or Job

**ID:** `litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** A reaction or job MUST run at most one use case, a job running it once per selected item.

**Rationale:** A reaction or job is an entry point: it turns one fact or one selected item into one message, as an endpoint turns one request into one message. A reaction running two use cases decides their order and their joint failure in code no record states, and the second fails after the first committed. Two follow-on acts are two reactions to the same fact, each with its own failure handling. A reaction or job only sending or calling runs no use case.

**Example:** The erase-expired-personal-data job selects decided expenses past retention and runs `use-case/spending.erase-personal-data` once for each.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction or job with more than one `runs` entry.

### Run Each Effect on Its Own

**ID:** `litespec/core/automation#run-each-effect-on-its-own`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each effect of a reaction or job MUST run, retry and deduplicate on its own, with any order expressed by chaining facts.

**Rationale:** Effects sharing one retry decide their joint failure in code no record states: the second send waits on the first, and a retry doubles the first while fixing the second. Running each effect on its own keeps each consequence independent, with its own idempotency and its own failure handling. An order between effects is a fact the first produces and the second answers, not an assumed sequence.

**Example:** The loan-borrowed reaction sends the due notice and calls the statistics system as two independent effects. A retry of the call resends no notice.

**Reviewed for:** Does every effect of each reaction and job run, retry, and deduplicate on its own? Check the effects beside their failure handling and idempotency.

### Settle Every Async Outcome in Visible States

**ID:** `litespec/core/automation#settle-every-async-outcome-in-visible-states`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job whose outcome a reader waits on MUST name in `settles` the aggregate it writes with its pending state and its done state, and its refused state except where another attempt returns the run to pending.

**Rationale:** A result produced after the commit arrives later: the command commits, and a reaction or job settles the outcome afterwards. A reader meeting the record meanwhile sees neither done nor refused, and reads the absence as whichever ending fits the screen: a payout still travelling reads as refused, or a refused payout reads as still travelling. Naming the pending state the run leaves, the done state it enters, and the refused state a terminal refusal enters keeps the three endings apart on the written aggregate. Where every failure earns another attempt, the run returns to pending instead, and the declared retry states that no terminal refusal exists.

**Example:** Claimwick's erase job runs the erase-personal-data command over decided expenses past retention. It settles the expense from pending Reimbursed to done Erased, and lists no refused state: its retry returns each failed expense to Reimbursed for the next nightly run.

**Checked by:** `litespec model check` reports `async-pending-missing` (level G) for a `settles` with a pending or done state that names no state or names one state twice, or with a pending state that no earlier transition reaches, `async-refused-unrecorded` (level G) for a run with no refused state and no declared retry, or a refused state that repeats pending or done, and `reference-unknown` (level G) for a `settles` naming no written aggregate or state. A run with no `settles` states nothing, because the model tells poorly whether a reader waits on it. A reviewer confirms each query reading the settled aggregate returns its state.

### Declare Each Read Model Field from a Published Field

**ID:** `litespec/core/automation#declare-each-read-model-field-from-a-published-field`

**Strength:** required

**Applies to:** artifact: record:read-model

**Statement:** Each read model MUST name in `keyedBy` an aggregate of another module and, on each field, name in `from` the module event or integration event and the published field the field comes from.

**Rationale:** A read model is a copy, and a copy with no stated source drifts: nobody tells which event fills a field, so a renamed published field breaks the read model without a finding. Naming the event and its field makes every crossing of the module boundary a declared edge the check reads, the same way an effect names its target. A read model keyed by its own module's aggregate copies state the module already reads directly. A use case names the read model in `reads`, and a read model no use case reads is kept current for no reader. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model) and [decision/0084-give-read-models-their-semantics](decision/0084-give-read-models-their-semantics).

**Example:** `read-model/reporting.reimbursed-expense` is keyed by `aggregate/spending.expense`, and its `amount` field comes from `module-event/spending.expense-reimbursed#amount`.

**Checked by:** `litespec model check` reports `reference-unknown` (level G) for a `from`, `erasedBy` or `removedBy` naming no module event or integration event, or a field the event does not carry, `read-model-key-invalid` (level G) for a `keyedBy` naming an aggregate of the read model's own module, and `read-model-unused` (level G) for a read model no use case reads.

### Erase a Read Model Personal Field with Its Owner

**ID:** `litespec/core/automation#erase-a-read-model-personal-field-with-its-owner`

**Strength:** required

**Applies to:** artifact: record:read-model

**Statement:** Each `personal` or `sensitive` read model field MUST name in `erasedBy` the module event or integration event whose arrival erases it.

**Rationale:** A read model copies data out of the module owning its erasure. A copy without an erasure trigger outlives the owner erasure and keeps personal data the product promised to remove. Naming the erasing event ties the copy to the owner erasure position, and the projection for that event clears the field. The erased key keeps a tombstone that a rebuild applies, so no rebuild restores the erased field. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model).

**Example:** The `employeeId` field of `read-model/reporting.reimbursed-expense` is personal, so it names `module-event/spending.expense-personal-data-erased` in `erasedBy`, and the projection for that event clears it.

**Checked by:** `litespec model check` reports `read-model-erasure-missing` (level G) for a `personal` or `sensitive` read model field with no `erasedBy`.

### Decide Current Truth in the Owning Module

**ID:** `litespec/core/automation#decide-current-truth-in-the-owning-module`

**Strength:** required

**Applies to:** artifact: record:use-case, record:read-model

**Statement:** A decision that needs current truth MUST be decided in the owning module, never from a read model.

**Rationale:** A read model lags by its freshness. Revoked permissions, available funds, reservations, and period locks move after the copy was taken, so deciding from a copy accepts a stale answer where the business needs the current one. The owning module decides instead, through its public query the command sends with a declared `queries` edge, or by its own invariant. A read model serves reads the consumer reshapes and keeps locally.

**Example:** The library refuses a hold on a lent copy by asking the lending module for current availability through its public query instead of reading a reporting copy.

**Reviewed for:** Is every decision that needs current truth decided in the owning module? Check the reads beside the declared `queries` edges.

### Move a State by Time with a Timed Transition

**ID:** `litespec/core/automation#move-a-state-by-time-with-a-timed-transition`

**Strength:** required

**Applies to:** artifact: record:aggregate

**Statement:** Each transition that time moves MUST name its duration from entering its source state in `after` and the moving job in `movedBy`.

**Rationale:** A deadline with no stated duration and no moving job has no defined moment to expire: holds linger, and reservations release nowhere. Naming the duration and the job states the timed move beside the lifecycle. The job selects instances past their window and runs the moving command once per instance. States, reactions, and timed transitions cover long-running work with no process record. Settled by decision/0074-declare-access-settings-and-deadlines.

**Example:** The hold transition from Ready to Expired carries `after: P7D` and `movedBy: job/lending.expire-lapsed-holds`, and the job runs the expire-holds command once per lapsed hold.

**Checked by:** `litespec model check` reports `reference-unknown` (level G) for a timed transition naming a missing state or a job no record declares.

### Raise Completion Only When Every Module Is Done

**ID:** `litespec/core/automation#raise-completion-only-when-every-module-is-done`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Work that spans modules MUST raise its completion fact only after every module reports its part done.

**Rationale:** A completion raised early declares done while parts remain: erased data returns, and closed work reopens. Each module's reaction erases or closes its part and reports done, and the completion is raised only where every report arrived. A report to an upstream module travels as its public command that the reporting module calls. Progress lives in a record outliving the deleted data, and the requesting state blocks new writes until completion. Settled by decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** Erasing a departed employee personal data enters Erasing, which blocks new writes. The spending reaction erases expenses and the access reaction erases sessions, each reporting done. The employee turns Erased only where both report, and the erasure record keeps the fact and date outside the erased data.

**Reviewed for:** Does spanning work raise completion only after every module reports done, with progress in a record the work does not delete? Check the completion reactions beside the module reports.

### Name the Step a Compensating Reaction Undoes

**ID:** `litespec/core/automation#name-the-step-a-compensating-reaction-undoes`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** A reaction that undoes a committed step of a multi-step process MUST name the undone use case in `compensates`, run a use case other than the one it names, and answer an event the undone use case does not raise.

**Rationale:** A multi-step process commits each step before the next runs, so a later failure rolls the earlier steps back nowhere, and each committed step needs an undo running only where a later step fails. Without the link, a reader sees a reaction running an undoing command and tells poorly which committed step it reverses. Running the named use case again redoes the step instead of undoing it, and answering an event the undone step raises undoes every success. Naming the undone step in `compensates` states the undo beside the reaction, with its own trigger and failure handling.

**Example:** The library reserves a copy with `use-case/lending.reserve-copy`, which commits the reservation. Where the later charge fails, `use-case/lending.record-charge-failure` raises `domain-event/charge.charge-failed`. The reaction `reaction/lending.release-copy-on-charge-failed` answers that event, runs `use-case/lending.release-copy`, and carries `compensates: use-case/lending.reserve-copy`, so a reader sees which committed step the release undoes.

**Checked by:** `litespec model check` reports `compensation-target-invalid` (level G) for a `compensates` naming a use case that is not a command or the same use case the reaction runs, `compensation-trigger-invalid` (level G) for a compensating reaction answering an event the compensated use case lists in `raises`, and `reference-unknown` (level G) for a `compensates` naming no written record. The trigger check is a minimum: a reviewer confirms the trigger reports a later step's failure.

### Keep Validity Apart from Storage

**ID:** `litespec/core/automation#keep-validity-apart-from-storage`

**Strength:** required

**Applies to:** artifact: record:aggregate, record:job

**Statement:** Each expiring credential or session MUST refuse its use from its expiry and be removed by a cleanup job within a stated interval.

**Rationale:** An expiry only deleting leaves a window where an expired credential still works, and a check without cleanup leaves dead rows in place. Refusing the use from the expiry keeps validity at the moment of use, and a cleanup job removing expired records within a stated interval reclaims storage. The two stay separate: the check refuses, and the job removes. Settled by decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** A library session past its expiry is refused at its next use, and the expire-sessions job removes expired sessions within one day.

**Reviewed for:** Does every expiring credential and session refuse use from expiry, with a cleanup job removing expired records within a stated interval? Check the expiry rules beside the cleanup jobs.

### Name Each Reaction for Its Act and Its Trigger

**ID:** `litespec/core/automation#name-each-reaction-for-its-act-and-its-trigger`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** Each reaction MUST be named `<verb>-<what>-on-<fact>` with a declared verb.

**Rationale:** A reaction runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` fires on a borrow, a hold, or a reminder run. Naming the act and the fact lets a reader find every consequence of a fact by its name.

**Example:** `reaction/lending.send-due-notice-on-loan-borrowed` answers `domain-event/loan.loan-borrowed`.

**Reviewed for:** Does every reaction name state its act and its trigger? Check the reaction names beside their triggering events.

### Name Each Job for the Use Case It Runs

**ID:** `litespec/core/automation#name-each-job-for-the-use-case-it-runs`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Each job MUST be named `<verb>-<what>` with the verb of the use case it runs, or with its own verb when it runs no use case.

**Rationale:** A job selects items and runs one command for each. Naming it for that command lets a reader go from the schedule to the behavior in one step. A job only sending or calling takes its own verb, since no use case verb names it.

**Example:** `job/lending.expire-lapsed-holds` runs `use-case/lending.expire-holds`.

**Checked by:** `litespec model check` reports `job-name-invalid` (level G) for a job outside `<verb>-<what>` with the verb of the use case it runs or its own verb when it runs none.

### State the Failure Handling of Every Reaction and Job

**ID:** `litespec/core/automation#state-the-failure-handling-of-every-reaction-and-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each reaction and each job MUST declare its `onFailure` in the one failure shape and its idempotency.

**Rationale:** A failed reaction or job without handling retries without end or drops silently: notices double-send, and holds clear nowhere. Declaring `onFailure` states the failure shape beside the trigger or schedule: `mode` of `retry`, `skip-and-log`, or `discard`; `retry` with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `ifExhausted` of `dead-letter`; `deadLetter` carrying `after` with the action a person takes; `surfacesIn` naming the entry point showing the failure, and `runbook` the recovery procedure. `retry` and `ifExhausted` come with `mode: retry`; `deadLetter`, `surfacesIn`, and `runbook` are optional. Delays and `after` are ISO 8601 durations, and prose handling is refused. The one shape also serves the effect entries of a use case. A reaction or job states failure handling once on the record.

**Checked by:** `litespec model check` fails a reaction or job with `onFailure` outside the one failure shape, and reports `record-invalid` for prose handling, a missing or malformed retry, and a malformed duration. A reaction or job that states `onFailure` in an effect entry is reported as `on-failure-placement-invalid`.

### Keep Failure Handling on the Reaction or Job

**ID:** `litespec/core/automation#keep-failure-handling-on-the-reaction-or-job`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** A reaction or job MUST state its failure handling once, in `onFailure` on the record and never in an effect entry.

**Rationale:** A reaction or job runs one use case and owns the failure of the whole run. The record's `onFailure` states the mode, the retry and the dead-letter beside the trigger or schedule, where a reader looks for it. Repeating the shape on an effect entry leaves two places to change and two answers to the same failure, so the entry is no second home for it. A use case has no record-level `onFailure`. Its effect entries each carry their own.

**Example:** Claimwick's reaction `reaction/spending.release-hold-on-rejection` states its retry and dead-letter in `onFailure` on the reaction record. Copying that object into its `runs` effect entry is reported.

**Checked by:** `litespec model check` reports `on-failure-placement-invalid` (level G) for a reaction or job whose effect entry carries `onFailure`.

### End Reaction and Job Scenarios in the Run Result

**ID:** `litespec/core/automation#end-reaction-and-job-scenarios-in-the-run-result`

**Strength:** required

**Applies to:** artifact: record:reaction, record:job

**Statement:** Each scenario on a reaction or job MUST end in a result of a use case its effects run or in one of its outcomes.

**Rationale:** A reaction or job scenario ending anywhere else proves another behavior story: it covers the trigger or schedule and asserts an ending no run use case declares and no outcome names. Ending in a run use case result or in a declared outcome keeps the scenario a proof of the reaction or job consequence. A send-only reaction or job ends its scenarios in outcomes, with no invented command.

**Checked by:** `litespec model check` reports `reference-unknown` for a reaction or job scenario ending outside the run use case results and the declared outcomes.

### Keep Job Fields off Reactions

**ID:** `litespec/core/automation#keep-job-fields-off-reactions`

**Strength:** required

**Applies to:** artifact: record:reaction

**Statement:** A reaction MUST NOT carry `kind`, `schedule`, `runbook`, `runsWhen`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

**Rationale:** Run fields describe work over time: the kind, the schedule, what each run selects, how many items per batch, the lock, where resume state lives, the condition it runs under, the promised service level, and the evidence the run leaves. A reaction answers one event and carries none of that. A run field on it claims a schedule the trigger states nowhere. Settled by [decision/0015-automations-and-sagas](decision/0015-automations-and-sagas).

**Example:** The send-due-notice reaction answers its event with no `selection` and no `schedule`.

**Checked by:** `litespec model check` reports `record-invalid` for a reaction carrying `kind`, `schedule`, `runbook`, `runsWhen`, `cursor`, `selection`, `lease`, `batchSize`, `serviceLevel`, or `evidence`.

### Write Every Runbook to Run or Recover

**ID:** `litespec/core/automation#write-every-runbook-to-run-or-recover`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Each runbook MUST state its `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, and `escalation`.

**Rationale:** A runbook named by a job, a failure policy, or an alert is the answer a person opens under pressure. Its `impact` says what breaks, `prerequisites` say what to hold, `procedure` says each step, `verification` says how to confirm, `recovery` says how to undo, and `escalation` says who to call. A runbook missing one leaves the reader guessing at the worst moment.

**Example:** The clear-stuck-reimbursements runbook states its impact on waiting expenses, its prerequisites, its procedure, its verification query, its recovery, and its escalation to the spending team.

**Checked by:** `litespec model check` reports `record-invalid` for a runbook page without `impact`, `prerequisites`, `procedure`, `verification`, `recovery`, or `escalation`.

## Conventions

None.
