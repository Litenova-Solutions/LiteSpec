# Privacy and Retention

**Stack:** any
**Concern:** records

Privacy and Retention states how the product handles personal data: each data class declares its erasure position and its retention. Agents write these records from Intent, and code carries them out. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value, and record name is written.

## Concepts

### Retention on the Data Class

Each aggregate data class declares how long its fields are kept. The declaration holds the `period` with the date it counts from in `periodStartsAt`, the `basis` for keeping the fields, the `purpose`, the `enforcedBy` naming what carries out the erasure, any `exceptions`, and any `holds` pausing deletion, as the [Domain Model page](domain-model.md) states.

The book renders the retention schedule from the data classes: one row per class with its purpose, basis, period, enforcement, and the requirement behind personal or sensitive keeping. The schedule states the time limits of a record of processing; the deleting job and the evidence are LiteSpec extensions to that record, and the project's counsel confirms the wording.

An external system names the aggregates and data classes it processes in `processes`, as the [Edges and Screens page](edges.md) states.

### Extended Erasure Positions

The `onErasure` field carries one of nine positions. The five common positions are `erase`, `pseudonymize`, `anonymize`, `retain`, and `exempt`. The four extended positions are `tombstone`, which keeps the row id and the erasure date where references outlive erasure; `discard`, which deletes the row where no marker duty exists; `cascade-only`, which erases only through a named parent; and `transferred`, which hands the data to a named receiver with its handover record.

Erasure that answers a subject request uses the `requested` reach and touches only the data the request names. A class that must export before erasure carries `exportFirst`, and erasure under `unchangedSinceExport` waits for a fresh export when the data changed since the last export. A restore runbook names the sweep job that re-applies erasure after the restore.

## Standards

### Classify Every Aggregate Data Field

**ID:** `litespec/core/privacy#classify-every-aggregate-data-field`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST list its data fields with their meaning, classification, and data class in `data`.

**Rationale:** Without a classification, personal facts sit beside public ones with nothing marking which is which, and every handler guesses. A meaning with a classification of public, internal, personal, or sensitive tells every later reader how each field is handled, and the data class ties the field to its erasure position and its retention. The classification of a field matches its data class, and the fields live in `data` on the aggregate record file.

**Checked by:** `litespec model check` reports `classification-drift` for a field whose `classification` differs from its data class.

### Declare Every Data Class with Its Erasure Position

**ID:** `litespec/core/privacy#declare-every-data-class-with-its-erasure-position`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each data class MUST declare its classification, its fields, its purpose, its erasure position, its basis, its retention period with the date it counts from, and the enforcement that carries it out.

**Rationale:** Erasure declared per field drifts: one field keeps a new position while another field in the same class keeps the old one. Declaring the position once per class, with per-field exceptions beside it, keeps one decision in one place. The position is `erase`, `pseudonymize`, `anonymize`, `retain`, `exempt`, `tombstone`, `discard`, `cascade-only`, or `transferred`. The `basis` names the constraint, requirement, and decision identifiers behind the retention; the period is an ISO 8601 duration or `{ "mode": "none", "reason": "..." }` with its start in `periodStartsAt`; the enforcement names the `job/` or `use-case/` identifiers carrying out the erasure. A pseudonymized class stays personal with its basis, and an anonymized class can no longer identify anyone. The `erasure-undeclared` check reports a personal or sensitive field with no declared position, and `retention-enforcement-missing` reports a class nothing enforces.

**Example:** In Claimwick, the submitter-contact class declares `personal`, `pseudonymize`, its purpose, and its accepting requirement, while the expense-ledger class declares `internal`, `retain`, a statutory seven years from the financial year end, and the finance expiry job.

**Checked by:** `litespec model check` reports `erasure-undeclared` for a personal or sensitive field with no declared position, `retention-enforcement-missing` for a class nothing enforces, and `data-class-fields-drift` for a data class `fields` list differing from the fields naming the class.

### Pause Deletion While a Hold Stands

**ID:** `litespec/core/privacy#pause-deletion-while-a-hold-stands`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class MUST pause deletion while a retention hold stands, with each hold's reason and the date the hold starts on record.

**Rationale:** A tax audit, an objection, or a dispute stops deletion: a product that deletes records during an audit breaks the law even though it followed its retention period exactly. The hold states its reason with the date it starts in `heldOn`, and deletion resumes only when the hold lifts with its date in `liftedOn`. The enforcement named in `enforcedBy` skips held rows, so the hold pauses every erasure path of the class. Settled by [decision/0100-retention-holds-and-rounded-starts](decision/0100-retention-holds-and-rounded-starts).

**Example:** Claimwick's finance team places a hold on the expense ledger while the tax office audits the year, and the expiry job leaves held rows until the hold lifts.

**Reviewed for:** Does every data class under audit, objection, or dispute carry its hold with its reason and start date, with deletion paused until the hold lifts? Check `holds` on each data class beside the enforcement runs.

### Round an Event Start to a Calendar End

**ID:** `litespec/core/privacy#round-an-event-start-to-a-calendar-end`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A retention period that rounds an event start up to a calendar end MUST name its event and its rounding in `periodStartsAt`.

**Rationale:** The law starts counting when a record stops being current: an asset invoice when the asset leaves, a lease when it ends. The start then rounds up to the end of the year, so every record of one year shares one expiry. Naming the event with its rounding states both halves: the event that starts the count and the calendar end it rounds to. A start naming only the event expires each record on its own date. Settled by [decision/0100-retention-holds-and-rounded-starts](decision/0100-retention-holds-and-rounded-starts).

**Example:** Claimwick keeps a receipt seven years from the end of the year its expense was reimbursed: `periodStartsAt` names `domain-event/expense.reimbursed` with `rounding: financial-year-end`.

**Reviewed for:** Does every retention period counted from a rounded event name its event and rounding? Check `periodStartsAt` on each data class.

### Accept Every Retained Personal Data Class in Intent

**ID:** `litespec/core/privacy#accept-every-retained-personal-data-class-in-intent`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each personal or sensitive data class kept with `pseudonymize`, `retain`, or `exempt` MUST name the Intent requirements or decisions accepting it in `basedOn`.

**Rationale:** Keeping personal data past erasure is a business choice, not a technical default: the statute or the business accepts it, with the reason on record. Pseudonymized data stays personal data, so it needs the same acceptance as retained data. Naming the accepting requirements or decisions in `basedOn` keeps the choice reviewable where the promise lives.

**Reviewed for:** Does every pseudonymized, retained, or exempt personal or sensitive class name its accepting requirement or decision? Check `basedOn` on each data class.

### Route Every External Erasure Through an Operation or Event

**ID:** `litespec/core/privacy#route-every-external-erasure-through-an-operation-or-event`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** Each external system MUST declare its erasure as `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "unsupported", "reason": "..." }`, `{ "mode": "manual", "reason": "..." }`, or `{ "mode": "not-applicable", "reason": "..." }`, each with its acceptance.

**Rationale:** Personal data forwarded to a processor without an erasure route stays there past erasure: the product erases its copy while the processor keeps its copy. Declaring the call operation, the forwarding integration event, or one of the three reason modes with its reason and its Intent acceptance states the route beside the system.

**Reviewed for:** Does every external system declare its erasure call, forwarding event, or reason mode with acceptance? Check the external systems beside their erasure routes.

### Keep a Tombstone Where Deletion Stays Visible

**ID:** `litespec/core/privacy#keep-a-tombstone-where-deletion-stays-visible`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class whose rows outlive erasure as references MUST use `onErasure: tombstone` with the fields its marker keeps.

**Rationale:** Some rows stay referenced after they are gone: remote servers hold copies, audit trails cite row ids, and support tools link to deleted accounts. Erasing the row breaks those references, while keeping it breaks the erasure duty. A tombstone keeps the row id and the erasure date and drops every other field, so references resolve to a marker that says the subject is gone.

**Example:** ZITADEL deletes a user by moving it to a terminal state and deleting its read-model rows while the events stay for audit, and Sentry writes a tombstone for a discarded issue so that later events for it are dropped at intake instead of stored again.

**Reviewed for:** Does every class referenced after erasure carry a tombstone marker with its kept fields? Check the marker entries on the aggregate pages.

### Discard Rows That Leave No Trace

**ID:** `litespec/core/privacy#discard-rows-that-leave-no-trace`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class with no duty to keep a marker MUST use `onErasure: discard`.

**Rationale:** A tombstone is still a row: it takes storage, appears in counts, and invites readers to treat a gone subject as present. Where no reference and no audit needs the marker, deleting the row outright leaves nothing behind.

**Example:** Lago discards a customer: the profile is hidden and its relations end, while finalized invoices and credit notes stay as the ledger.

**Reviewed for:** Does any `onErasure` entry keep a marker where no marker duty exists? Check the entries on the aggregate pages.

### Remove Child Rows Through Their Parent Only

**ID:** `litespec/core/privacy#remove-child-rows-through-their-parent-only`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class erased only as a cascade from a parent erasure MUST use `onErasure: cascade-only` with the parent it follows.

**Rationale:** Some rows have no subject of their own: line items belong to an invoice, receipt files belong to an expense, and usage rows belong to a customer. A direct erasure request names the parent, never the child, so the child follows the parent erasure and refuses a request addressed to itself.

**Example:** Cal.com deletes a user by removing the account and letting the database cascade remove the rows that hang off it, with no retention schedule. A product following this rule declares cascade-only and names the cascade path.

**Reviewed for:** Does every child class entry carry cascade-only with its parent class, with parent erasure tests that clear the children? Check the entries and tests beside each child class.

### Move Data to Its Next Controller Instead of Erasing

**ID:** `litespec/core/privacy#move-data-to-its-next-controller-instead-of-erasing`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class handed to another controller MUST use `onErasure: transferred` with the receiver and the handover record.

**Rationale:** Custody does not always end in erasure: a branch closes and a successor takes its readers, a company is acquired, or a contract moves to a new processor. The data survives under a new controller, so the record names the receiver and the handover both sides signed instead of a deletion that never happened.

**Example:** A library support tool sends a loan summary and the reader message to a hosted AI model to draft a reply; the position names the model provider, its region and its retention, and the product stores none of it.

**Reviewed for:** Does every handed-over class entry name its receiver and handover record? Check the entries on the aggregate pages.

### Scope Erasure to What the Subject Requested

**ID:** `litespec/core/privacy#scope-erasure-to-what-the-subject-requested`

**Strength:** required

**Applies to:** artifact: record:use-case; when: erasure

**Statement:** Erasure that answers a subject request MUST use `reach: requested` and touch only the data the request names.

**Rationale:** A subject who asks to delete one order does not ask to delete the account, and a subject who asks to delete the account names everything. The requested reach ties the erasure to the request it answers, so a narrow request cannot widen into a broad deletion and a broad request cannot stop at one row.

**Example:** A request naming one hold erases that hold and leaves the reader's other loans untouched.

**Reviewed for:** Does every erasure use case cite the request it answers and touch only the data in it? Check the erasure use cases beside their requests.

### State the Erasure Sentence in Intent

**ID:** `litespec/core/privacy#state-the-erasure-sentence-in-intent`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** The literal sentence the subject reads about an erasure MUST stand in Intent before the erasure ships.

**Rationale:** The sentence on the confirmation screen is the promise: what goes, what stays, and for how long. Only the product team decides what the product promises, so Behavior cannot word it. Stating it in Intent puts the exact sentence readers see beside the requirement it fulfills.

**Example:** Mastodon shows on its account deletion form that content other servers have cached may persist, next to the promise that the account and its posts are removed; that sentence is the user-facing copy for a requested reach.

**Reviewed for:** Does every Intent requirement carry the confirmation sentence before the release that shows it? Check the requirement beside the release.

### Complete the Subject Export Before Erasure

**ID:** `litespec/core/privacy#complete-the-subject-export-before-erasure`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class with `exportFirst` MUST complete the subject export before its erasure runs.

**Rationale:** Erasure destroys the only copy of data the subject may still want: identity documents, message history, and purchase records. Completing the export first gives the subject that data while it still exists, so erasure never destroys what the subject asked to keep elsewhere.

**Example:** Claimwick makes finance download the tax-relevant data before a shredder runs, and refuses to shred when anything changed after that download.

**Reviewed for:** Does every export-first class complete its export before an erasure run? Check the run evidence beside each class.

### Require a Fresh Export After Changes

**ID:** `litespec/core/privacy#require-a-fresh-export-after-changes`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Erasure under `unchangedSinceExport` MUST stop and ask for a fresh export when the data changed since the last export.

**Rationale:** An export taken last month does not cover rows written yesterday. Erasing on that stale copy destroys rows the subject never received. Stopping for a fresh export keeps the export-first promise true at the moment of erasure.

**Example:** A reader who corrected their address after exporting it gets a fresh export first, and the erasure waits for it.

**Reviewed for:** Does every erasure run after mid-window edits wait for a fresh export completed first? Check the run evidence beside each erasure.

### Purge Erased Data from Restored Backups

**ID:** `litespec/core/privacy#purge-erased-data-from-restored-backups`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** A restore runbook MUST name the sweep job that re-applies erasure after the restore.

**Rationale:** A backup holds data as it was, including rows erased since. Restoring it brings back rows the product promised to erase. The sweep job re-applies every erasure decided before the restore, so the restore brings the system back without bringing the erased data back with it.

**Example:** The database restore runbook names `job/privacy.sweep-restored-erasure`, which runs before traffic returns and deletes every row erased since the backup.

**Reviewed for:** Does every restore runbook name its sweep job? Check the restore runbooks.

### Record Which Duty Wins Each Conflict

**ID:** `litespec/core/privacy#record-which-duty-wins-each-conflict`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each retention conflict MUST name the winning duty and its basis in the data class `exceptions`.

**Rationale:** Keeping for audit and erasing for the subject collide on the same row: the invoice the statute keeps sits beside the name the subject erases. Without a recorded winner, each implementer picks one. Naming the winning duty with its basis in the data class `exceptions` keeps one decision where every reader finds it.

**Example:** Claimwick keeps the expense ledger for seven years under tax law while erasing the submitter contact. ZITADEL keeps its audit events while erasing the user they describe, under the same rule.

**Reviewed for:** Does every data class `exceptions` entry name its winning duty with its basis? Check the `exceptions` on each data class.

## Conventions

### Keep Tombstone Fields Minimal

**ID:** `litespec/core/privacy#keep-tombstone-fields-minimal`

**Strength:** default

**Applies to:** artifact: aggregate

**Default:** A tombstone keeps the row id and the erasure date, and no other field.

**Replacement:** A project lists wider tombstone fields with the rule that needs each one.

**Rationale:** Every field on a tombstone is data kept past erasure. The row id and the date satisfy references and audits; anything wider needs its own rule.

**Reviewed for:** Does every tombstone entry keep only the row id and the erasure date, unless the project lists wider fields with their rules? Check the entries beside the project listing.

### Run One Sweep Job per Data Class

**ID:** `litespec/core/privacy#run-one-sweep-job-per-data-class`

**Strength:** default

**Applies to:** artifact: job; when: erasure

**Default:** Each sweep job covers one data class.

**Replacement:** A project names the data classes each sweep job covers.

**Rationale:** One sweep per class keeps a failure small: a broken sweep delays one class while the others complete. A shared sweep is one failure away from resurrecting every class at once.

**Reviewed for:** Does every sweep job page cover one data class, unless the project names the classes each job covers? Check each sweep job page beside the project naming.
