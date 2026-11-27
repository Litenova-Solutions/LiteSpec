# Privacy and Retention

**Stack:** any

**Concern:** records

Privacy and Retention states how the product handles personal data: each data class declares its erasure position and its retention. Agents write these records from Intent, and code carries them out. This page is for teams declaring data classes on aggregates and for developers running the erasure and sweep jobs. A wrong position keeps data the law says to erase, or erases data the law says to keep.

## Concepts

### How Long Each Data Class Is Kept

Each aggregate data class declares how long its fields are kept. The declaration holds the `period` with the date it counts from in `periodStartsAt`, the `basedOn` authority for keeping the fields, the `purpose`, the `enforcedBy` naming what carries the erasure out, any `exceptions`, and any `retentionHolds` pausing deletion. The book renders the retention schedule from the data classes: one row per class with its purpose, authority, period, enforcement, and the requirement behind personal or sensitive keeping.

Keeping for audit and erasing for the subject collide on the same row: the invoice the statute keeps sits beside the name the subject erases. Without a recorded winner, each implementer picks one. Each conflict therefore names the winning duty with its authority in the data class `exceptions`, and each retained, pseudonymized, or exempt personal class names the Intent requirements or decisions accepting it in `basedOn`.

A tax audit, an objection, or a dispute pauses deletion through a hold carrying its reason with its start date in `heldOn`. The enforcement skips held rows, and deletion resumes with the lift date in `liftedOn`. An external system names the aggregates and data classes it processes in `processes`, with its erasure route beside it: the call operation, the forwarding event, or the reason mode with its Intent acceptance.

### How Each Erasure Position Works

The `onErasure` field carries one of nine positions. The five common positions are `erase`, `pseudonymize`, `anonymize`, `retain`, and `exempt`. The four extended positions are `tombstone` keeping the row id and the erasure date where references outlive erasure, `discard` deleting the row where no marker duty exists, `cascade-only` erasing through a named parent, and `transferred` handing the data to a named receiver with its handover record.

A pseudonymized class stays personal with its authority, and an anonymized class identifies no one. A tombstone keeps the row id and the erasure date and drops each other field, so references resolve to a marker showing the subject is gone. A child row with no subject of its own follows the parent erasure and takes no request addressed to itself, while handed-over data survives under a new controller, with the record naming the receiver and the handover both sides signed.

Erasure answering a subject request uses the `requested` reach and touches the data the request names alone. The literal sentence the subject reads about the erasure stands in Intent beside the requirement it fulfills. A class carrying `exportFirst` completes the subject export before its erasure runs, and erasure under `unchangedSinceExport` stops for a fresh export where the data changed since the last export.

A restore brings back rows erased since the backup, so the restore runbook names the sweep job re-applying erasure after the restore. The sweep runs before traffic returns. The book schedule, the export evidence, and the runbook thus show the whole retention story from declaration to proof.

## Standards

### Classify Every Aggregate Data Field

**ID:** `litespec/core/privacy#classify-every-aggregate-data-field`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST list its data fields with their meaning, classification, and data class in `data`.

**Rationale:** Without a classification, personal facts sit beside public ones with nothing marking which is which. Each handler then guesses. A meaning with a classification of public, internal, personal, or sensitive tells each later reader how each field is handled, and the data class ties the field to its erasure position and its retention.

**Checked by:** `litespec model check` reports `classification-drift` for a field whose `classification` differs from its data class.

### Declare Every Data Class with Its Erasure Position

**ID:** `litespec/core/privacy#declare-every-data-class-with-its-erasure-position`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each data class MUST declare its classification, its `fields`, its purpose, its erasure position, its `basedOn` authority, its retention period with the date it counts from, and the enforcement that carries it out.

**Rationale:** Erasure declared per field drifts: one field keeps a new position while another field in the same class keeps the old one. Declaring the position once per class, with per-field exceptions beside it, keeps one decision in one place. The `basedOn` names the authority behind the retention, and the enforcement names the job or use case carrying it out. Settled by decision/0016-typed-contracts-and-privacy.

**Example:** In Claimwick, the submitter-contact class declares `personal` with `pseudonymize`, its purpose, and its accepting requirement. The expense-ledger class declares `internal` with `retain`, a statutory seven years from the financial year end, and the finance expiry job.

**Checked by:** `litespec model check` reports `erasure-undeclared` for a personal or sensitive field with no declared position, `retention-enforcement-missing` for a class nothing enforces, and `data-class-fields-drift` for a data class `fields` list differing from the fields naming the class.

### Enforce a Data Class Through Its Aggregate

**ID:** `litespec/core/privacy#enforce-a-data-class-through-its-aggregate`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** When a data class names a job in `enforcedBy`, that job MUST run or send a use case writing the aggregate holding the data class.

**Rationale:** An enforcement job reaching no aggregate leaves the retention promise unkept. The class states a period and an erasure position, and no command in the run path touches the rows the period covers. Following the job `runs` and `commands` effects to the use cases they send and those use cases `writes` shows whether the job reaches the class it enforces.

**Example:** Claimwick expense-ledger data class names the finance expiry job. That job runs the erase-expired-expenses command, whose `writes` names `aggregate/spending.expense`, so the job reaches the class it enforces.

**Checked by:** `litespec model check` reports `enforcement-job-unreached` (level G) for a data class whose `enforcedBy` names a job that runs or sends no use case writing the aggregate holding the class.

### Pause Deletion While a Hold Stands

**ID:** `litespec/core/privacy#pause-deletion-while-a-hold-stands`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A data class MUST pause deletion while a retention hold stands, with each hold's reason and the date the hold starts on record.

**Rationale:** A tax audit, an objection, or a dispute stops deletion. A product deleting records during an audit breaks the law even while following its retention period exactly. The hold carries its reason with its start date in `heldOn`, and deletion resumes with the lift date in `liftedOn`. This follows decision/0100-retention-holds-and-rounded-starts.

**Example:** Claimwick finance team places a hold on the expense ledger while the tax office audits the year. The expiry job leaves held rows until the hold lifts.

**Reviewed for:** Does every data class under audit, objection, or dispute carry its hold with its reason and start date, with deletion paused until the hold lifts? Check `retentionHolds` on each data class beside the enforcement runs.

### Round an Event Start to a Calendar End

**ID:** `litespec/core/privacy#round-an-event-start-to-a-calendar-end`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A retention period that rounds an event start up to a calendar end MUST name its event and its rounding in `periodStartsAt`.

**Rationale:** The law starts counting where a record stops being current. The start then rounds up to the end of the year, so each record of one year shares one expiry. Naming the event with its rounding states both halves: the event starting the count and the calendar end it rounds to. This follows decision/0100-retention-holds-and-rounded-starts.

**Example:** Claimwick keeps a receipt seven years from the end of the year its expense was reimbursed. `periodStartsAt` names `domain-event/expense.reimbursed` with `rounding: financial-year-end`.

**Reviewed for:** Does every retention period counted from a rounded event name its event and rounding? Check `periodStartsAt` on each data class.

### Scope Erasure to What the Subject Requested

**ID:** `litespec/core/privacy#scope-erasure-to-what-the-subject-requested`

**Strength:** required

**Applies to:** artifact: record:use-case; when: erasure

**Statement:** Erasure that answers a subject request MUST use `reach: requested` and touch only the data the request names.

**Rationale:** A subject asking to delete one order asks nothing about the account. The requested reach ties the erasure to the request it answers. A narrow request thus stays narrow, and a broad request names everything.

**Example:** A reader request naming one hold erases that hold. The reader other loans stay untouched.

**Reviewed for:** Does every erasure use case cite the request it answers and touch only the data in it? Check the erasure use cases beside their requests.

### Purge Erased Data from Restored Backups

**ID:** `litespec/core/privacy#purge-erased-data-from-restored-backups`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** A restore runbook MUST name the sweep job that re-applies erasure after the restore.

**Rationale:** A backup holds data as it was, including rows erased since. Restoring it brings back rows the product promised to erase. The sweep job re-applies each erasure decided before the restore, so the restore brings the system back without bringing the erased data back with it.

**Example:** The database restore runbook names `job/privacy.sweep-restored-erasure`. The job runs before traffic returns and deletes each row erased since the backup.

**Reviewed for:** Does every restore runbook name its sweep job? Check the restore runbooks.

## Conventions

None.
