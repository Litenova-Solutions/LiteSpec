# Privacy and Erasure

## Intent

Some personal data needs more than erasure or retention alone: rows other systems reference, rows erased only with a parent, data handed to another controller, and erasure scoped to what the subject asked. This page states the erasure positions, reach, export guards, conflict rules, and restore sweeps a project uses when the four core positions do not cover its duties. A project selects it when its retention schedule names any of those needs.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `privacy` when its retention schedule names a tombstone, discard, cascade-only, or transferred position, a requested reach, an export-first guard, a retention conflict, or a restore that can bring erased data back. A project whose data classes use only erase, pseudonymise, retain, and exempt does not select it: the core positions already cover that project.

## Baseline Relationship

This extension replaces no baseline provision. It switches on no record type: it adds fields to records the core already defines.

It adds four `onErasure` positions to data classes: `tombstone`, `discard`, `cascade-only`, and `transferred`. It adds the `requested` reach, the `exportFirst` and `unchangedSinceExport` guards, conflict detail on retention schedule rows, and the sweep job on restore runbooks.

## Standards

### Keep a Tombstone Where Deletion Stays Visible

**ID:** `litespec/ext/privacy#keep-a-tombstone-where-deletion-stays-visible`

**Statement:** A data class whose rows outlive erasure as references MUST use `onErasure: tombstone` with the fields its marker keeps.

**Rationale:** Some rows are referenced after they are gone: remote servers hold copies, audit trails cite row ids, and support tools link to deleted accounts. Erasing the row breaks those references, while keeping it breaks the erasure duty. A tombstone keeps the row id and the erasure date and drops every other field, so references resolve to a marker that says the subject is gone.

**Example:** ZITADEL deletes a user by moving it to a terminal state and deleting its read-model rows while the events stay for audit, and Sentry writes a tombstone for a discarded issue so that later events for it are dropped at intake instead of stored again.

**Reviewed for:** The aggregate pages mark referenced-after-erasure classes tombstone with the marker fields each keeps.

### Discard Rows That Leave No Trace

**ID:** `litespec/ext/privacy#discard-rows-that-leave-no-trace`

**Statement:** A data class with no duty to keep a marker MUST use `onErasure: discard`.

**Rationale:** A tombstone is still a row: it occupies storage, appears in counts, and invites readers to treat a gone subject as present. Where no reference and no audit needs the marker, deleting the row outright keeps the store honest.

**Example:** Lago discards a customer: the profile is hidden and its relations end, while finalized invoices and credit notes stay as the ledger.

**Reviewed for:** The `onErasure` entries on the aggregate pages carry discard where no marker duty exists.

### Remove Child Rows Through Their Parent Only

**ID:** `litespec/ext/privacy#remove-child-rows-through-their-parent-only`

**Statement:** A data class erased only as a cascade from a parent erasure MUST use `onErasure: cascade-only` with the parent it follows.

**Rationale:** Some rows have no subject of their own: line items belong to an invoice, receipt files belong to an expense, usage rows belong to a customer. A direct erasure request names the parent, never the child, so the child follows the parent's erasure and refuses a request addressed to itself.

**Example:** Cal.com deletes a user by removing the account and letting the database cascade remove the rows that hang off it, with no retention schedule; a product at this rung declares cascade-only and names the cascade path.

**Reviewed for:** The child class entries name cascade-only with the parent class, and the parent erasure tests clear the children.

### Move Data to Its Next Controller Instead of Erasing

**ID:** `litespec/ext/privacy#move-data-to-its-next-controller-instead-of-erasing`

**Statement:** A data class handed to another controller MUST use `onErasure: transferred` with the receiver and the handover record.

**Rationale:** Erasure is not the only end of custody: a branch closes and a successor takes its readers, a company is acquired, a contract moves to a new processor. The data survives under a new controller, so the record names the receiver and the handover both sides signed instead of a deletion that never happened.

**Example:** A support tool sends a loan summary and the reader message to a hosted AI model to draft a reply; the position names the model provider, its region and its retention, and the product stores none of it.

**Reviewed for:** The handed-over class entries name transferred with the receiver and the handover record.

### Scope Erasure to What the Subject Requested

**ID:** `litespec/ext/privacy#scope-erasure-to-what-the-subject-requested`

**Statement:** Erasure that answers a subject request MUST use `reach: requested` and touch only the data the request names.

**Rationale:** A subject who asks to delete one order does not ask to delete the account, and a subject who asks to delete the account names everything. The requested reach ties the erasure to the request it answers, so a narrow request cannot widen into a broad deletion and a broad request cannot stop at one row.

**Example:** A request naming one hold erases that hold and leaves the reader's other loans untouched.

**Reviewed for:** The erasure use cases cite the request they answer and touch only the data it names.

### Approve the Erasure Sentence in Intent

**ID:** `litespec/ext/privacy#approve-the-erasure-sentence-in-intent`

**Statement:** The literal sentence the subject reads about an erasure MUST be approved in Intent before the erasure ships.

**Rationale:** The sentence on the confirmation screen is the promise: what goes, what stays, and for how long. Behavior cannot word it, because only the product owner decides what the product promises. Approving it in Intent puts the exact sentence readers see under the same review as the requirement it fulfils.

**Example:** Mastodon shows on its account deletion form that content other servers have cached may persist, next to the promise that the account and its posts are removed; that sentence is the approved user-facing copy for a requested reach.

**Reviewed for:** The confirmation sentence in the Intent requirement carries an approval before the release that shows it.

### Complete the Subject Export Before Erasure

**ID:** `litespec/ext/privacy#complete-the-subject-export-before-erasure`

**Statement:** A data class with `exportFirst` MUST complete the subject export before its erasure runs.

**Rationale:** Erasure destroys the only copy of data the subject may still want: identity documents, message history, purchase records. Completing the export first gives the subject their data while it still exists, so erasure never destroys what the subject asked to keep elsewhere.

**Example:** Claimwick makes finance download the tax-relevant data before a shredder runs, and refuses to shred when anything changed after that download.

**Reviewed for:** The export-first classes show a completed export before each erasure run in the run evidence.

### Require a Fresh Export After Changes

**ID:** `litespec/ext/privacy#require-a-fresh-export-after-changes`

**Statement:** Erasure under `unchangedSinceExport` MUST stop and ask for a fresh export when the data changed since the last export.

**Rationale:** An export taken last month does not cover rows written yesterday. When the data changed since the last export, erasing on the stale copy destroys rows the subject never received. Stopping for a fresh export keeps the export-first promise true at the moment of erasure.

**Example:** A reader who corrected their address after exporting it gets a fresh export first, and the erasure waits for it.

**Reviewed for:** The erasure runs after mid-window edits show a fresh export completed first.

### Purge Erased Data from Restored Backups

**ID:** `litespec/ext/privacy#purge-erased-data-from-restored-backups`

**Statement:** A restore runbook MUST name the sweep job that re-applies erasure after the restore.

**Rationale:** A backup holds data as it was, including rows erased since: restoring it resurrects subjects the product promised to forget. The sweep job re-applies every erasure decided before the restore, so the restore brings the system back without bringing the erased data back with it.

**Example:** The database restore runbook names `job/privacy.sweep-restored-erasure`, which runs before traffic returns and deletes every row erased since the backup.

**Reviewed for:** Which restore runbooks name no sweep job? Look at the restore runbooks.

### Record Which Duty Wins Each Conflict

**ID:** `litespec/ext/privacy#record-which-duty-wins-each-conflict`

**Statement:** Each retention conflict MUST name the winning duty, its basis, the owner recording the exception, and the review date.

**Rationale:** Keeping for audit and erasing for the subject collide on the same row: the invoice the statute keeps sits beside the name the subject erases. Silence lets each implementer pick a winner. Naming the winning duty with its basis, the owner who records the exception, and the date the choice is reviewed keeps one decision where every reader finds it.

**Example:** Claimwick keeps the expense ledger for seven years under tax law while erasing the submitter contact, and the retention row names the finance owner and the next review date. ZITADEL keeps its audit events while erasing the user they describe, under the same rule.

**Reviewed for:** The retention schedule rows name the winning duty with its basis, recording owner, and review date.

## Conventions

### Keep Tombstone Fields Minimal

**ID:** `litespec/ext/privacy#keep-tombstone-fields-minimal`

**Default:** A tombstone keeps the row id and the erasure date, and no other field.

**Replacement:** A project lists wider tombstone fields with the rule that needs each one.

**Rationale:** Every field on a tombstone is data kept past erasure. The row id and the date satisfy references and audits, and anything wider needs its own rule.

**Reviewed for:** The tombstone entries keep the row id and the erasure date, or the project lists wider fields with the rule behind each.

### Run One Sweep Job per Data Class

**ID:** `litespec/ext/privacy#run-one-sweep-job-per-data-class`

**Default:** Each sweep job covers one data class.

**Replacement:** A project names the data classes each sweep job covers.

**Rationale:** One sweep per class keeps the blast radius small: a broken sweep delays one class while the others complete. A shared sweep is one failure away from resurrecting every class at once.

**Reviewed for:** Each sweep job page covers one data class, or the project names the classes each job covers.

## Dependencies

Packages: None.

Related extensions: `audit` for tamper-resistant evidence of erasure runs, and `tenancy` for erasure that crosses tenant lines.

Guides: None.
