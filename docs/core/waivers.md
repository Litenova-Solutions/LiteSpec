# Waivers

**Stack:** any

**Concern:** records

A waiver sets a provision, a page, or a finding code aside for one project, with its reason. It is the only record doing this: no other record sets a rule aside. This page states where waivers live, what each one holds, and how each one covers a finding. It is for the team recording accepted debt or a lasting choice, and for the reviewer checking the gap ends. The [model page](model.md) states the record types and the checks, and the [scope page](scope.md) states the record layers a project claims.

## Concepts

### What a Waiver Is

A waiver lives in `.litespec/governance/waivers/`, or in `.litespec/governance/modules/<module>/waivers/` when it covers one module. Each waiver sits in its own folder named for it, holding its record file `<name>.yaml` with an optional explanation `<name>.md` beside it.

It states what the project sets aside, why, and where the waiver applies. It binds nothing outside the records it names.

| Field | Required | Holds |
|:---|:---|:---|
| `type` | yes | `waiver` |
| `id` | yes | `waiver/<name>` |
| `title` | yes | What is set aside, in one line. |
| `kind` | yes | `gap` for work the team means to close, `lasting` for a choice the project keeps. |
| `waives` | yes | A list. Each item is a provision ID (`litespec/<page>#<slug>`), a page (`litespec/<page>`), or a finding code. |
| `reason` | yes | Why the project sets the rule aside. |
| `appliesTo` or `paths` | one of them | The records, or the file globs from the repository root, that the waiver covers. |
| `expiresOn` | on a gap | The date the gap closes. A lasting waiver carries no date. |
| `exitWhen` | on a gap | How the gap closes. A lasting waiver carries no exitWhen. |
| `closedBy` | on a gap | The records whose change closes the gap. A lasting waiver carries no closing records. |
| `consequences` | on a lasting waiver | What the project accepts by setting the rule aside. |

### When to Record One

The kind follows from whether the work ends. A gap waiver is work the team means to close: it carries `kind: gap` with an `expiresOn` date, an `exitWhen`, and `closedBy`, and holds until that date. A lasting waiver is a choice the project keeps: it carries `kind: lasting` with `consequences` and no end date, and holds for as long as its reason holds.

### How a Waiver Covers a Finding

A waiver covers a finding when all three of these hold:

- Its `waives` names the finding's code, or names a provision or a page governing the code. A provision governs the codes its **Checked by** line names.
- The finding is on one of its `appliesTo` records, or in one of its `paths` files.
- The waiver is live: a lasting waiver, or a gap waiver whose `expiresOn` date has not passed.

A waiver naming a use case also covers the findings about the scenarios and results that use case holds. A covered finding carries the outcome `covered`: it is still reported, and the run passes.

### One Waiver per Finding

When two live waivers cover the same finding code, provision, or page on the same appliesTo record or the same path, the check reports `waiver-duplicate`. The project keeps one waiver and removes the other, so each covered finding traces to exactly one waiver.

### What No Waiver Covers

Some findings stop the model from loading, and every other result depends on them. No waiver covers these codes: `record-invalid`, `record-placement-invalid`, `identifier-invalid`, `identifier-duplicate`, and `reference-unknown`.

A waiver whose `waives` names one of them reports `waiver-invalid`. The same finding reports when a `waives` item names a finding code no check emits, or a provision or page the installed release does not ship. A `waiver-invalid` finding is covered at no point.

### When a Waiver Expires

A gap waiver holds through its `expiresOn` date. After that date, the findings it covered report again, and the check reports `waiver-expired` on the waiver, which blocks the run. To clear it, close the finding or write a new `expiresOn` date in the record. A lasting waiver carries no `expiresOn` date and holds for as long as its reason holds.

### How the Analyzers Match a Waiver

The analyzers run inside the compiler and read no provision index, so they match a waiver's `waives` against their own LiteSpec rule IDs alone. A project setting aside a provision an analyzer enforces names that rule's LiteSpec rule ID in `waives`.

The model check reads the provision, the page, or the finding code from `waives`, so the same waiver also covers the finding there.

### Replaced Defaults Are Not Waivers

A provision of Strength `default` gives way to the project's own choice, recorded where the provision's Replacement names. That record is not a waiver. [Provision Scopes](scopes.md) states the rule for each replaced default. A provision of Strength advisory asks for no record either: the project passes over it with judgment inside the bounds its text states.

## Standards

### Record Accepted Gaps as Waivers

**ID:** `litespec/core/waivers#record-accepted-gaps-as-waivers`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** An accepted gap MUST live as a gap waiver with an `expiresOn` date, an `exitWhen`, and `closedBy`.

**Rationale:** Missing work with no date is reported by nothing. A gap waiver with an `expiresOn` date turns the same missing work into a dated promise: its `waives` findings stay covered until the date, the `exitWhen` says how the gap closes, `closedBy` names the records whose change closes it, and `waiver-expired` reports the waiver after the date. See also decision/0014-one-waiver-record.

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as a waiver in `.litespec/governance/waivers/export-expenses-unimplemented/export-expenses-unimplemented.yaml`:

```yaml
type: waiver
id: waiver/export-expenses-unimplemented
title: Export Expenses Unimplemented
summary: Export waits for the finance file format.
kind: gap
waives:
  - use-case-unimplemented
appliesTo:
  - use-case/spending.export-expenses
reason: Export waits for the finance file format.
exitWhen: Exporting expenses has no handler yet. The gap closes when the finance file format lands and the export use case gains its handler.
expiresOn: 2027-06-01
closedBy:
  - use-case/spending.export-expenses
```

Until 2027-06-01 the `use-case-unimplemented` finding is covered and the run passes, which satisfies this provision.

**Checked by:** `litespec model check` reports `waiver-expired` (level G) for a waiver past its `expiresOn` date, and `waiver-invalid` (level G) for a waiver whose `waives` names a finding code no check emits, a code the model needs in order to load, or a provision or page the installed release does not ship.

### Record a Project-Wide Lowering as a Waiver

**ID:** `litespec/core/waivers#record-a-project-wide-lowering-as-a-waiver`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** A project MUST record a waiver for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** A project-wide lowering in `.editorconfig` sets the standard aside. Recording it as a waiver names the provision, the reason, and the expiry date, so the lowering stays dated and ends on its date instead of staying without notice.

**Example:** A project lowering LS5001 to a suggestion in its root `.editorconfig` records a waiver whose `waives` names `litespec/core/system#point-code-and-tests-up-to-records`, the provision the rule enforces.

**Checked by:** `litespec model check` reports `analyzer-lowering-unrecorded` (level S) for a project-wide lowering in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable`, or `[SuppressMessage]` that no live waiver covers.

### State the Kind of Every Waiver

**ID:** `litespec/core/waivers#state-the-kind-of-every-waiver`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** Every waiver MUST state its kind as `gap` or `lasting`.

**Rationale:** Whether a waiver ends is a fact the reader needs on the record: a gap ends on its `expiresOn` date, and a lasting choice holds for as long as its reason holds. Reading the kind from the presence of `expiresOn` hides that fact in an absence, and a reader fails to tell a gap with no date yet from a choice that ends at no point. See also decision/0014-one-waiver-record.

**Example:** The Claimwick waiver for the unimplemented export states `kind: gap` with its `expiresOn` date, so a reader sees the export gap ending on 2027-06-01.

**Checked by:** `litespec model check` reports `record-invalid` (level G) for a waiver with no `kind` or a `kind` outside `gap` and `lasting`.

### Name the Records That Close a Gap

**ID:** `litespec/core/waivers#name-the-records-that-close-a-gap`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** Every gap waiver MUST name the records whose change closes the gap in `closedBy`.

**Rationale:** An `exitWhen` says how a gap closes in prose, but prose points at no record a check verifies. Naming the closing records in `closedBy` shows the reader what to change, and lets the check report an entry pointing at no written record. See also decision/0014-one-waiver-record.

**Example:** The Claimwick waiver for the unimplemented export names `use-case/spending.export-expenses` in `closedBy`, so the gap closes when the export use case gains its handler.

**Checked by:** `litespec model check` reports `record-invalid` (level G) for a gap waiver with no `closedBy`, and `reference-unknown` (level G) for a `closedBy` entry naming no written record.

### Cover Each Finding with One Waiver

**ID:** `litespec/core/waivers#cover-each-finding-with-one-waiver`

**Strength:** required

**Applies to:** artifact: record:waiver

**Statement:** Two live waivers MUST NOT cover the same code, provision, or page on the same appliesTo record or the same path.

**Rationale:** Two waivers on one finding split the reason and the end date across two records: one expires while the other still holds, and no reader tells which waiver to close. One waiver per finding keeps each covered finding traced to exactly one reason and one end.

**Example:** Claimwick covers the unimplemented export with the one waiver `waiver/export-expenses-unimplemented`; a second waiver naming `use-case-unimplemented` on `use-case/spending.export-expenses` reports `waiver-duplicate`.

**Checked by:** `litespec model check` reports `waiver-duplicate` (level G) for two live waivers covering the same finding on the same appliesTo record or path.

## Conventions

None.
