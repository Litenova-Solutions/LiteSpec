# Privacy and Retention

**Stack:** any
**Layer:** records

## Intent

Privacy and Retention states how the product handles personal data: the data classes with their erasure positions, the retention schedule, and the extended erasure needs of referenced rows, child rows, handed-over data, requested reach, export guards, conflicts, and restore sweeps. Agents write these records from Intent, and code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Retention Schedule

The retention schedule is one page holding one row per data class with its purpose, basis, period, enforcement, and the requirement behind personal or sensitive keeping. It states the time limits of a record of processing; the deleting job and the evidence are LiteSpec extensions to that record. The project's counsel confirms the wording. Processors and conflicts sit on the row: where the data forwards, and which demand wins when keeping for audit and erasing for the subject collide, with who records the exception.

| Field | Meaning |
|:---|:---|
| `dataClass`, `covers` | The class and the aggregates holding it. |
| `purpose` | Why the data is kept. |
| `basis`, `period`, `enforcedBy` | The statute, business, or engineering basis, the ISO 8601 duration period or `{ "mode": "none", "reason": "..." }`, and the enforcement. |
| `cites` | The requirement behind personal or sensitive keeping. |
| `processors` | `{ system, forwarding, basedOn }` entries. |
| `conflicts` | Which demand wins on a keeping-versus-erasing collision and who records it. |

### Extended Erasure Positions

The `onErasure` field carries one of nine positions. Five cover the common fates: `erase`, `pseudonymize`, `anonymize`, `retain`, and `exempt`. Four cover the extended needs: `tombstone` keeps the row id and the erasure date where references outlive erasure, `discard` deletes the row where no marker duty exists, `cascade-only` erases only through a named parent, and `transferred` hands the data to a named receiver with its handover record. Erasure answering a subject request uses the `requested` reach and touches only the data the request names. A class that must export before erasure carries `exportFirst`, and erasure under `unchangedSinceExport` waits for a fresh export when the data changed since the last export. A restore runbook names the sweep job that re-applies erasure after the restore.

## Standards

### Classify Every Aggregate Data Field

**ID:** `litespec/core/privacy#classify-every-aggregate-data-field`

**Strength:** rule

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST list its data fields with their meaning, classification, and data class in `data`.

**Rationale:** Data without a classification is handled by guess: personal facts sit beside public ones with nothing marking which is which. A meaning with a classification of public, internal, personal, or sensitive tells every later reader how each field is handled, and the data class ties the field to its erasure position and its retention row. The fields live in `data` on the aggregate record file.

**Reviewed for:** Which aggregate data fields lack a meaning, a classification, or a data class? Look at `data` on each aggregate page.

### Declare Every Data Class with Its Erasure Position

**ID:** `litespec/core/privacy#declare-every-data-class-with-its-erasure-position`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** Each data class MUST declare its classification, its fields, its purpose, its erasure position, its basis, its retention period, and the enforcement that carries it out.

**Rationale:** Erasure declared per field drifts: one field keeps a new position while its twin keeps the old one. Declaring the position once per class, with per-field exceptions beside it, keeps one decision in one place. The position is `erase`, `pseudonymize`, `anonymize`, `retain`, `exempt`, `tombstone`, `discard`, `cascade-only`, or `transferred`; the basis is `statute`, `business`, or `engineering`; the period is an ISO 8601 duration or `{ "mode": "none", "reason": "..." }`; the enforcement is `reaction/...`, `job/...`, `record-expiry`, `store-ttl`, or `query-filter`. A pseudonymized class stays personal with its basis, and an anonymized class can no longer identify anyone. The `erasure-undeclared` check reports a personal or sensitive field with no declared position, and `retention-enforcement-missing` reports a class nothing enforces.

**Example:** The submitter-contact class declares `personal`, `pseudonymize`, its purpose, and its accepting requirement, while the expense-ledger class declares `internal`, `retain`, a statutory seven years, and the finance expiry job.

**Checked by:** `litespec model check` reports `erasure-undeclared` for a personal or sensitive field with no declared position and `retention-enforcement-missing` for a class nothing enforces.

### Accept Every Retained Personal Data Class in Intent

**ID:** `litespec/core/privacy#accept-every-retained-personal-data-class-in-intent`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** Each personal or sensitive data class kept with `pseudonymize`, `retain`, or `exempt` MUST name the Intent requirements or decisions accepting it in `basedOn`.

**Rationale:** Keeping personal data past erasure is a business choice, not a technical default: the statute or the business accepts it, with the reason on record. Pseudonymized data stays personal data, so it needs the same acceptance as retained data. Naming those requirements or decisions in `basedOn` keeps the keeping reviewable where the promise lives.

**Reviewed for:** Which pseudonymized, retained, or exempt personal or sensitive classes name no accepting requirement or decision? Look at `basedOn` on each data class.

### Route Every External Erasure Through an Operation or Event

**ID:** `litespec/core/privacy#route-every-external-erasure-through-an-operation-or-event`

**Strength:** rule

**Applies to:** artifact: record:external-system

**Statement:** Each external system MUST declare its erasure as `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "impossible", "reason": "..." }` with its acceptance.

**Rationale:** Personal data forwarded to a processor without an erasure route stays there past erasure: the product forgets while the processor remembers. Declaring `{ "mode": "call", "operation": "external-system/<name>#<operation>" }` naming the operation, `{ "mode": "event", "event": "integration-event/<name>" }` naming the forwarding integration event, or `{ "mode": "impossible", "reason": "..." }` with its reason and its Intent acceptance states the route beside the system.

**Reviewed for:** Which external systems declare no erasure call, forwarding event, or impossible reason with acceptance? Look at the external systems beside their erasure routes.

### Schedule Every Data Class for Retention

**ID:** `litespec/core/privacy#schedule-every-data-class-for-retention`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** Each data class MUST appear on the retention schedule with its purpose, basis, period, enforcement, and the requirement it cites where personal or sensitive.

**Rationale:** Retention decided per aggregate scatters: one module erases in days what another keeps for years, and no page shows the whole. Listing every data class on one schedule with its purpose, basis, period, enforcement, processors with their forwarding and acceptance, and the conflict rule where keeping and erasing collide states the whole as rows a reviewer judges together. The schedule states the time limits of a record of processing. Personal or sensitive keeping cites its Intent requirement. The `retention-enforcement-missing` check reports a row nothing enforces.

**Checked by:** `litespec model check` reports `retention-enforcement-missing` for a data class nothing enforces.

### Keep a Tombstone Where Deletion Stays Visible

**ID:** `litespec/core/privacy#keep-a-tombstone-where-deletion-stays-visible`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** A data class whose rows outlive erasure as references MUST use `onErasure: tombstone` with the fields its marker keeps.

**Rationale:** Some rows are referenced after they are gone: remote servers hold copies, audit trails cite row ids, and support tools link to deleted accounts. Erasing the row breaks those references, while keeping it breaks the erasure duty. A tombstone keeps the row id and the erasure date and drops every other field, so references resolve to a marker that says the subject is gone.

**Example:** ZITADEL deletes a user by moving it to a terminal state and deleting its read-model rows while the events stay for audit, and Sentry writes a tombstone for a discarded issue so that later events for it are dropped at intake instead of stored again.

**Reviewed for:** The aggregate pages mark referenced-after-erasure classes tombstone with the marker fields each keeps.

### Discard Rows That Leave No Trace

**ID:** `litespec/core/privacy#discard-rows-that-leave-no-trace`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** A data class with no duty to keep a marker MUST use `onErasure: discard`.

**Rationale:** A tombstone is still a row: it occupies storage, appears in counts, and invites readers to treat a gone subject as present. Where no reference and no audit needs the marker, deleting the row outright keeps the store honest.

**Example:** Lago discards a customer: the profile is hidden and its relations end, while finalized invoices and credit notes stay as the ledger.

**Reviewed for:** The `onErasure` entries on the aggregate pages carry discard where no marker duty exists.

### Remove Child Rows Through Their Parent Only

**ID:** `litespec/core/privacy#remove-child-rows-through-their-parent-only`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** A data class erased only as a cascade from a parent erasure MUST use `onErasure: cascade-only` with the parent it follows.

**Rationale:** Some rows have no subject of their own: line items belong to an invoice, receipt files belong to an expense, usage rows belong to a customer. A direct erasure request names the parent, never the child, so the child follows the parent's erasure and refuses a request addressed to itself.

**Example:** Cal.com deletes a user by removing the account and letting the database cascade remove the rows that hang off it, with no retention schedule; a product at this rung declares cascade-only and names the cascade path.

**Reviewed for:** The child class entries name cascade-only with the parent class, and the parent erasure tests clear the children.

### Move Data to Its Next Controller Instead of Erasing

**ID:** `litespec/core/privacy#move-data-to-its-next-controller-instead-of-erasing`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** A data class handed to another controller MUST use `onErasure: transferred` with the receiver and the handover record.

**Rationale:** Erasure is not the only end of custody: a branch closes and a successor takes its readers, a company is acquired, a contract moves to a new processor. The data survives under a new controller, so the record names the receiver and the handover both sides signed instead of a deletion that never happened.

**Example:** A support tool sends a loan summary and the reader message to a hosted AI model to draft a reply; the position names the model provider, its region and its retention, and the product stores none of it.

**Reviewed for:** The handed-over class entries name transferred with the receiver and the handover record.

### Scope Erasure to What the Subject Requested

**ID:** `litespec/core/privacy#scope-erasure-to-what-the-subject-requested`

**Strength:** rule

**Applies to:** artifact: record:use-case

**Statement:** Erasure that answers a subject request MUST use `reach: requested` and touch only the data the request names.

**Rationale:** A subject who asks to delete one order does not ask to delete the account, and a subject who asks to delete the account names everything. The requested reach ties the erasure to the request it answers, so a narrow request cannot widen into a broad deletion and a broad request cannot stop at one row.

**Example:** A request naming one hold erases that hold and leaves the reader's other loans untouched.

**Reviewed for:** The erasure use cases cite the request they answer and touch only the data it names.

### State the Erasure Sentence in Intent

**ID:** `litespec/core/privacy#state-the-erasure-sentence-in-intent`

**Strength:** rule

**Applies to:** artifact: record:requirement

**Statement:** The literal sentence the subject reads about an erasure MUST stand in Intent before the erasure ships.

**Rationale:** The sentence on the confirmation screen is the promise: what goes, what stays, and for how long. Behavior cannot word it, because only the product team decides what the product promises. Stating it in Intent puts the exact sentence readers see beside the requirement it fulfils.

**Example:** Mastodon shows on its account deletion form that content other servers have cached may persist, next to the promise that the account and its posts are removed; that sentence is the user-facing copy for a requested reach.

**Reviewed for:** The confirmation sentence in the Intent requirement stands before the release that shows it.

### Complete the Subject Export Before Erasure

**ID:** `litespec/core/privacy#complete-the-subject-export-before-erasure`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** A data class with `exportFirst` MUST complete the subject export before its erasure runs.

**Rationale:** Erasure destroys the only copy of data the subject may still want: identity documents, message history, purchase records. Completing the export first gives the subject their data while it still exists, so erasure never destroys what the subject asked to keep elsewhere.

**Example:** Claimwick makes finance download the tax-relevant data before a shredder runs, and refuses to shred when anything changed after that download.

**Reviewed for:** The export-first classes show a completed export before each erasure run in the run evidence.

### Require a Fresh Export After Changes

**ID:** `litespec/core/privacy#require-a-fresh-export-after-changes`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** Erasure under `unchangedSinceExport` MUST stop and ask for a fresh export when the data changed since the last export.

**Rationale:** An export taken last month does not cover rows written yesterday. When the data changed since the last export, erasing on the stale copy destroys rows the subject never received. Stopping for a fresh export keeps the export-first promise true at the moment of erasure.

**Example:** A reader who corrected their address after exporting it gets a fresh export first, and the erasure waits for it.

**Reviewed for:** The erasure runs after mid-window edits show a fresh export completed first.

### Purge Erased Data from Restored Backups

**ID:** `litespec/core/privacy#purge-erased-data-from-restored-backups`

**Strength:** rule

**Applies to:** artifact: record:runbook

**Statement:** A restore runbook MUST name the sweep job that re-applies erasure after the restore.

**Rationale:** A backup holds data as it was, including rows erased since: restoring it resurrects subjects the product promised to forget. The sweep job re-applies every erasure decided before the restore, so the restore brings the system back without bringing the erased data back with it.

**Example:** The database restore runbook names `job/privacy.sweep-restored-erasure`, which runs before traffic returns and deletes every row erased since the backup.

**Reviewed for:** Which restore runbooks name no sweep job? Look at the restore runbooks.

### Record Which Duty Wins Each Conflict

**ID:** `litespec/core/privacy#record-which-duty-wins-each-conflict`

**Strength:** rule

**Applies to:** artifact: record:retention-schedule

**Statement:** Each retention conflict MUST name the winning duty and its basis.

**Rationale:** Keeping for audit and erasing for the subject collide on the same row: the invoice the statute keeps sits beside the name the subject erases. Silence lets each implementer pick a winner. Naming the winning duty with its basis keeps one decision where every reader finds it.

**Example:** Claimwick keeps the expense ledger for seven years under tax law while erasing the submitter contact. ZITADEL keeps its audit events while erasing the user they describe, under the same rule.

**Reviewed for:** The retention schedule rows name the winning duty with its basis.

## Conventions

### Keep Tombstone Fields Minimal

**ID:** `litespec/core/privacy#keep-tombstone-fields-minimal`

**Strength:** default

**Applies to:** artifact: record:retention-schedule

**Default:** A tombstone keeps the row id and the erasure date, and no other field.

**Replacement:** A project lists wider tombstone fields with the rule that needs each one.

**Rationale:** Every field on a tombstone is data kept past erasure. The row id and the date satisfy references and audits, and anything wider needs its own rule.

**Reviewed for:** The tombstone entries keep the row id and the erasure date, or the project lists wider fields with the rule behind each.

### Run One Sweep Job per Data Class

**ID:** `litespec/core/privacy#run-one-sweep-job-per-data-class`

**Strength:** default

**Applies to:** artifact: job

**Default:** Each sweep job covers one data class.

**Replacement:** A project names the data classes each sweep job covers.

**Rationale:** One sweep per class keeps the blast radius small: a broken sweep delays one class while the others complete. A shared sweep is one failure away from resurrecting every class at once.

**Reviewed for:** Each sweep job page covers one data class, or the project names the classes each job covers.
