# Departures

**Stack:** any
**Concern:** records

## Intent

A departure is the one record that sets a provision, a page or a finding code aside for one project, with its reason. This page states what a departure holds, when to record one, how it covers a finding, what no departure covers, and when it expires. The [model page](model.md) states the record types and the checks, and the [scope page](scope.md) states the record layers a project claims.

## Concepts

### What a Departure Is

A departure is a record in `.litespec/governance/departures/`, or in `.litespec/governance/modules/<module>/departures/` when it covers one module. Each departure is one file. It states what the project departs from, why, and where the departure applies. It binds nothing outside the records it names.

| Field | Required | Holds |
|:---|:---|:---|
| `type` | yes | `departure` |
| `id` | yes | `departure/<name>` |
| `title` | yes | What is departed from, in one line. |
| `covers` | yes | A list. Each item is a provision ID (`litespec/<page>#<slug>`), a page (`litespec/<page>`) or a finding code. |
| `reason` | yes | Why the project departs. |
| `subject` or `paths` | one of them | The records, or the file globs from the repository root, that the departure covers. |
| `expiresOn` | no | The date a gap closes. Absent for a lasting choice. |
| `exit` | when `expiresOn` is set | How the gap closes. |
| `consequences` | no | What the project accepts by departing. |

### When to Record One

A departure takes one of two forms. A lasting choice has no `expiresOn`: the project keeps the departure for as long as its reason holds. A gap has an `expiresOn` date and an `exit`: the team means to close the work, and the departure holds until that date.

### How a Departure Covers a Finding

A departure covers a finding when all three of these hold:

- Its `covers` names the finding's code, or names a provision or a page that governs the code. A provision governs the codes its **Checked by** line names.
- The finding is on one of its `subject` records, or in one of its `paths` files.
- The departure is live: it has no `expiresOn`, or its `expiresOn` date has not passed.

A departure that names a use case also covers the findings about the scenarios and results that use case holds. A covered finding has the outcome `covered`: it prints, and the run passes.

### What No Departure Covers

Some findings stop the model from loading, and every other result depends on them. No departure covers these codes: `record-invalid`, `folder-invalid`, `record-placement-invalid`, `identifier-invalid`, `identifier-duplicate` and `reference-unknown`. A departure whose `covers` names one of them reports `departure-invalid`. A departure reports `departure-invalid` as well when a `covers` item names a finding code no check emits, or a provision or page the installed release does not ship. A `departure-invalid` finding is never covered.

### When a Departure Expires

A departure with an `expiresOn` date holds through that date. After it, the findings it covered report again, and `departure-expired` reports the departure, which blocks the run. The fix is to close the finding or to write a new `expiresOn` date in the record.

### How the Analyzers Match a Departure

The analyzers run inside the compiler and read no provision index, so they match a departure's `covers` against their own LiteSpec rule IDs only. A project that departs from a provision an analyzer enforces names that rule's LiteSpec rule ID in `covers`. The model check reads the provision, the page or the finding code, so a lowering recorded for the provision also holds in the check.

### Replaced Defaults Are Not Departures

A provision of Strength `default` is replaced by the project's own choice, recorded where the provision's Replacement names. That record is not a departure. [Provision Scopes](scopes.md) states the rule for each replaced default.

## Standards

### Record Accepted Gaps as Departures

**ID:** `litespec/core/departures#record-accepted-gaps-as-departures`

**Strength:** required

**Applies to:** artifact: record:departure

**Statement:** An accepted gap MUST live as a departure with an `expiresOn` date and an `exit`.

**Rationale:** Missing work with no date is invisible: nothing reports it. A departure with an `expiresOn` date turns the same missing work into a dated promise. Its `covers` findings are covered until the date, the `exit` says how the gap closes, and `departure-expired` reports the departure after the date. Settled by [decision/0014-one-exception-record](decision/0014-one-exception-record) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as a departure in `.litespec/governance/departures/export-expenses-unimplemented.yaml`:

```yaml
type: departure
id: departure/export-expenses-unimplemented
title: Export Expenses Unimplemented
summary: Export waits for the finance file format.
covers:
  - use-case-unimplemented
subject:
  - use-case/spending.export-expenses
reason: Export waits for the finance file format.
exit: Exporting expenses has no handler yet. The gap closes when the finance file format lands and the export use case gains its handler.
expiresOn: 2027-06-01
```

The `use-case-unimplemented` finding is covered, and the run passes, until 2027-06-01, which satisfies this provision.

**Checked by:** `litespec model check` reports `departure-expired` (level G) for a departure past its `expiresOn` date, and `departure-invalid` (level G) for a departure whose `covers` names a finding code no check emits, a code the model needs in order to load, or a provision or page the installed release does not ship.

### Record a Project-Wide Lowering as a Departure

**ID:** `litespec/core/departures#record-a-project-wide-lowering-as-a-departure`

**Strength:** required

**Applies to:** artifact: record:departure

**Statement:** A project MUST record a departure for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** A project-wide lowering in `.editorconfig` is a departure from the standard rather than a setting. A departure names the provision, the reason and the expiry date, so the lowering is dated and ends instead of staying silent.

**Example:** A project that lowers LS5001 to a suggestion in its root `.editorconfig` records a departure whose `covers` names `litespec/core/system#point-code-and-tests-up-to-records`, the provision the rule enforces.

**Checked by:** `litespec model check` reports `analyzer-lowering-unrecorded` (level S) for a project-wide lowering in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable` or `[SuppressMessage]` that no live departure covers.

### Depart from Advice Without a Record

**ID:** `litespec/core/departures#depart-from-advice-without-a-record`

**Strength:** advisory

**Applies to:** artifact: record:departure

**Statement:** A project MAY depart from a provision of Strength advisory without recording a departure, since an advisory provision binds no project.

**Rationale:** A provision of Strength advisory is experience applied with judgement, and [Provision Scopes](scopes.md) says it records nothing. A departure from it would name a rule the provision never made. A provision whose Statement says should takes this strength for the same reason.

**Reviewed for:** Which departures name a provision of Strength advisory in their `covers`? Look at `covers` in each file under `.litespec/governance/departures/`.

## Conventions

None.
