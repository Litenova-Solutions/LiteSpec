# Waivers

**Stack:** any
**Concern:** records

## Intent

A waiver is the one record that sets a provision, a page or a finding code aside for one project, with its reason. This page states what a waiver holds, when to record one, how it covers a finding, what no waiver covers, and when it expires. The [model page](model.md) states the record types and the checks, and the [scope page](scope.md) states the record layers a project claims.

## Concepts

### What a Waiver Is

A waiver is a record in `.litespec/governance/waivers/`, or in `.litespec/governance/modules/<module>/waivers/` when it covers one module. Each waiver sits in its own folder named for it, holding its record file `<name>.yaml` and an optional explanation `<name>.md` beside it. It states what the project departs from, why, and where the waiver applies. It binds nothing outside the records it names.

| Field | Required | Holds |
|:---|:---|:---|
| `type` | yes | `waiver` |
| `id` | yes | `waiver/<name>` |
| `title` | yes | What is set aside, in one line. |
| `covers` | yes | A list. Each item is a provision ID (`litespec/<page>#<slug>`), a page (`litespec/<page>`) or a finding code. |
| `reason` | yes | Why the project sets the rule aside. |
| `subject` or `paths` | one of them | The records, or the file globs from the repository root, that the waiver covers. |
| `expiresOn` | no | The date a gap closes. Absent for a lasting choice. |
| `exit` | when `expiresOn` is set | How the gap closes. |
| `consequences` | no | What the project accepts by setting the rule aside. |

### When to Record One

A waiver takes one of two forms. A lasting choice has no `expiresOn`: the project keeps the waiver for as long as its reason holds. A gap has an `expiresOn` date and an `exit`: the team means to close the work, and the waiver holds until that date.

### How a Waiver Covers a Finding

A waiver covers a finding when all three of these hold:

- Its `covers` names the finding's code, or names a provision or a page that governs the code. A provision governs the codes its **Checked by** line names.
- The finding is on one of its `subject` records, or in one of its `paths` files.
- The waiver is live: it has no `expiresOn`, or its `expiresOn` date has not passed.

A waiver that names a use case also covers the findings about the scenarios and results that use case holds. A covered finding has the outcome `covered`: it prints, and the run passes.

### What No Waiver Covers

Some findings stop the model from loading, and every other result depends on them. No waiver covers these codes: `record-invalid`, `folder-invalid`, `record-placement-invalid`, `identifier-invalid`, `identifier-duplicate` and `reference-unknown`. A waiver whose `covers` names one of them reports `waiver-invalid`. A waiver reports `waiver-invalid` as well when a `covers` item names a finding code no check emits, or a provision or page the installed release does not ship. A `waiver-invalid` finding is never covered.

### When a Waiver Expires

A waiver with an `expiresOn` date holds through that date. After it, the findings it covered report again, and `waiver-expired` reports the waiver, which blocks the run. The fix is to close the finding or to write a new `expiresOn` date in the record.

### How the Analyzers Match a Waiver

The analyzers run inside the compiler and read no provision index, so they match a waiver's `covers` against their own LiteSpec rule IDs only. A project that sets aside a provision an analyzer enforces names that rule's LiteSpec rule ID in `covers`. The model check reads the provision, the page or the finding code, so a lowering recorded for the provision also holds in the check.

### Replaced Defaults Are Not Waivers

A provision of Strength `default` is replaced by the project's own choice, recorded where the provision's Replacement names. That record is not a waiver. [Provision Scopes](scopes.md) states the rule for each replaced default.

## Standards

### Record Accepted Gaps as Waivers

**ID:** `litespec/core/waivers#record-accepted-gaps-as-waivers`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** An accepted gap MUST live as a waiver with an `expiresOn` date and an `exit`.

**Rationale:** Missing work with no date is invisible: nothing reports it. A waiver with an `expiresOn` date turns the same missing work into a dated promise. Its `covers` findings are covered until the date, the `exit` says how the gap closes, and `waiver-expired` reports the waiver after the date. Settled by [decision/0014-one-waiver-record](decision/0014-one-waiver-record) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as a waiver in `.litespec/governance/waivers/export-expenses-unimplemented/export-expenses-unimplemented.yaml`:

```yaml
type: waiver
id: waiver/export-expenses-unimplemented
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

**Checked by:** `litespec model check` reports `waiver-expired` (level G) for a waiver past its `expiresOn` date, and `waiver-invalid` (level G) for a waiver whose `covers` names a finding code no check emits, a code the model needs in order to load, or a provision or page the installed release does not ship.

### Record a Project-Wide Lowering as a Waiver

**ID:** `litespec/core/waivers#record-a-project-wide-lowering-as-a-waiver`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** A project MUST record a waiver for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** A project-wide lowering in `.editorconfig` is a waiver from the standard rather than a setting. A waiver names the provision, the reason and the expiry date, so the lowering is dated and ends instead of staying silent.

**Example:** A project that lowers LS5001 to a suggestion in its root `.editorconfig` records a waiver whose `covers` names `litespec/core/system#point-code-and-tests-up-to-records`, the provision the rule enforces.

**Checked by:** `litespec model check` reports `analyzer-lowering-unrecorded` (level S) for a project-wide lowering in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable` or `[SuppressMessage]` that no live waiver covers.

### Deviate from Advice Without a Record

**ID:** `litespec/core/waivers#deviate-from-advice-without-a-record`

**Strength:** advisory

**Applies to:** artifact: record:waiver

**Statement:** A project MAY deviate from a provision of Strength advisory without recording a waiver, since an advisory provision binds no project.

**Rationale:** A provision of Strength advisory is experience applied with judgement, and [Provision Scopes](scopes.md) says it records nothing. A waiver from it would name a rule the provision never made. A provision whose Statement says `SHOULD` or `SHOULD NOT` takes this strength for the same reason: the project sets the advice aside within the bounds its Deviation states and records nothing. Only a provision of Strength required is departed from through a waiver. Settled by [decision/0082-give-every-rule-one-home-and-one-strength](decision/0082-give-every-rule-one-home-and-one-strength).

**Reviewed for:** Which waivers name a provision of Strength advisory in their `covers`? Look at `covers` in each file under `.litespec/governance/waivers/`.

## Conventions

None.
