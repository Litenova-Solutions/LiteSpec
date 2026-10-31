# Privacy and Retention

## Intent

Privacy and Retention states how the product handles personal data: the data classes with their erasure positions and the retention schedule. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Retention Schedule

The retention schedule is one page listing every data class with its purpose, basis, period, enforcement, and the requirement behind personal or sensitive keeping. It states the time limits of a record of processing; the deleting automation and the evidence are LiteSpec extensions to that record. The project's counsel confirms the wording. Processors and conflicts sit on the row: where the data forwards, and which demand wins when keeping for audit and erasing for the subject collide, with who records the exception. The rows render from the data classes as a generated region.

| Field | Meaning |
|:---|:---|
| `dataClass`, `covers` | The class and the aggregates holding it. |
| `purpose` | Why the data is kept. |
| `basis`, `period`, `enforcedBy` | The statute, business, or engineering basis, the ISO 8601 duration period or `{ "mode": "none", "reason": "..." }`, and the enforcement. |
| `cites` | The requirement behind personal or sensitive keeping. |
| `processors` | `{ system, forwarding, acceptedBy }` entries. |
| `conflicts` | Which demand wins on a keeping-versus-erasing collision and who records it. |

## Standards

### Classify Every Aggregate Data Field

**ID:** `litespec/core/privacy#classify-every-aggregate-data-field`

**Statement:** Each aggregate MUST list its data fields with their meaning, classification, and data class in its Data table.

**Rationale:** Data without a classification is handled by guess: personal facts sit beside public ones with nothing marking which is which. A meaning with a classification of public, internal, personal, or sensitive tells every later reader how each field is handled, and the data class ties the field to its erasure position and its retention row. The fields live in frontmatter and the Data table renders them as a generated region.

**Reviewed for:** Which aggregate data fields lack a meaning, a classification, or a data class? Look at the Data table on each aggregate page.

### Declare Every Data Class with Its Erasure Position

**ID:** `litespec/core/privacy#declare-every-data-class-with-its-erasure-position`

**Statement:** Each data class MUST declare its classification, its fields, its purpose, its erasure position, its basis, its retention period, and the enforcement that carries it out.

**Rationale:** Erasure declared per field drifts: one field keeps a new position while its twin keeps the old one. Declaring the position once per class, with per-field exceptions beside it, keeps one decision in one place. The position is `erase`, `pseudonymize`, `anonymize`, `retain`, or `exempt`; the basis is `statute`, `business`, or `engineering`; the period is an ISO 8601 duration or `{ "mode": "none", "reason": "..." }`; the enforcement is `automation/...`, `record-expiry`, `store-ttl`, or `query-filter`. A pseudonymized class stays personal with its basis, and an anonymized class can no longer identify anyone. The `erasure-undeclared` check reports a personal or sensitive field with no declared position, and `retention-enforcement-missing` reports a class nothing enforces.

**Example:** The submitter-contact class declares `personal`, `pseudonymize`, its purpose, and its approving requirement, while the expense-ledger class declares `internal`, `retain`, a statutory seven years, and the finance expiry automation.

**Checked by:** `litespec model check` reports `erasure-undeclared` for a personal or sensitive field with no declared position and `retention-enforcement-missing` for a class nothing enforces.

### Accept Every Retained Personal Data Class in Intent

**ID:** `litespec/core/privacy#accept-every-retained-personal-data-class-in-intent`

**Statement:** Each personal or sensitive data class kept with `pseudonymize`, `retain`, or `exempt` MUST name the Intent requirement or decision accepting it in `acceptedBy`.

**Rationale:** Keeping personal data past erasure is a business choice, not a technical default: the statute or the business accepts it, with the reason on record. Pseudonymized data stays personal data, so it needs the same acceptance as retained data. Naming that requirement or decision in `acceptedBy` keeps the keeping reviewable where the promise lives. The `risk-acceptance-missing` check reports a pseudonymized, retained, or exempt personal or sensitive class with no Intent acceptance.

**Checked by:** `litespec model check` reports `risk-acceptance-missing` for a pseudonymized, retained, or exempt personal or sensitive class with no Intent acceptance.

### Review Every Erasure Exemption Before Its Date

**ID:** `litespec/core/privacy#review-every-erasure-exemption-before-its-date`

**Statement:** Each exempt data class MUST carry its reason and a future `reviewBy` date.

**Rationale:** An exemption without a date is a permanent exception wearing temporary clothes: nobody reopens it, so it holds forever. A reason with a future review date makes the exemption a dated choice the owner rejudges on time. The `exemption-expired` check reports an exemption past its date.

**Checked by:** `litespec model check` reports `exemption-expired` for an exemption past its `reviewBy` date.

### Route Every External Erasure Through an Operation or Event

**ID:** `litespec/core/privacy#route-every-external-erasure-through-an-operation-or-event`

**Statement:** Each external system MUST declare its erasure as `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "impossible", "reason": "..." }` with its acceptance.

**Rationale:** Personal data forwarded to a processor without an erasure route stays there past erasure: the product forgets while the processor remembers. Declaring `{ "mode": "call", "operation": "external-system/<name>#<operation>" }` naming the operation, `{ "mode": "event", "event": "integration-event/<name>" }` naming the forwarding integration event, or `{ "mode": "impossible", "reason": "..." }` with its reason and its Intent acceptance states the route beside the system.

**Reviewed for:** Which external systems declare no erasure call, forwarding event, or impossible reason with acceptance? Look at the external systems beside their erasure routes.

### Schedule Every Data Class for Retention

**ID:** `litespec/core/privacy#schedule-every-data-class-for-retention`

**Statement:** Each data class MUST appear on the retention schedule with its purpose, basis, period, enforcement, and the requirement it cites where personal or sensitive.

**Rationale:** Retention decided per aggregate scatters: one module erases in days what another keeps for years, and no page shows the whole. Listing every data class on one schedule with its purpose, basis, period, enforcement, processors with their forwarding and acceptance, and the conflict rule where keeping and erasing collide states the whole as rows a reviewer judges together. The schedule states the time limits of a record of processing, and the rows render from the data classes as a generated region. Personal or sensitive keeping cites its Intent requirement. The `retention-enforcement-missing` check reports a row nothing enforces.

**Checked by:** `litespec model check` reports `retention-enforcement-missing` for a data class nothing enforces.

## Conventions

None.
