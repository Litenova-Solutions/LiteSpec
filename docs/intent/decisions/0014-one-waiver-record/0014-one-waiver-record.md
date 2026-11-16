# One Waiver Record

Project teams need one record for every exception they set aside: a deviation, a known gap, a deferral, and a frozen phase. A waiver is that written exception. Each waiver states its kind, names what it covers, and gives a reason, and a gap that the team means to close also carries an expiry date and the records whose change closes it. This decision sets that one shape.

## Context

Waivers are uneven and too wide. A known gap suppresses a check at level G, the gating level. A deviation suppresses every finding on its subject, not only the finding for its provision. A deferral with the frozen phase disables checks with no expiry.

## Options

LiteSpec compares 4 ways to record an exception.

| Option | Strengths | Drawbacks |
|---|---|---|
| One waiver record with kind, scope and closing records | One shape carries every waiver with its kind, reason, and scope, closing records for a gap, and consequences for a lasting choice | Deviations, known gaps, deferrals and the frozen phase all move to the one record in one change |
| Keep four mechanisms and tighten each | Each mechanism keeps its readers | 4 waiver shapes and 4 expiry rules keep the uneven coverage that caused the finding |
| Exceptions as project settings only | One file holds every waiver | Settings carry no reason and no per-item expiry, so waivers never return for review |
| Every finding blocks | No waiver, so no waiver abuse | One-person teams stall on advisory findings |

The chosen option keeps one shape for every waiver. The other options keep 4 shapes, drop the reasons, or block on every finding.

## Decision

One `waiver` record replaces the deviation, the known gap, `deferredProvisions`, and `check.phase: frozen`. A waiver states its kind: `gap` for work the team means to close, `lasting` for a choice the project keeps. Whether a waiver ends is a fact the reader needs on the record, so the kind is explicit rather than read from the presence of an expiry date.

A waiver names what it covers in `covers`, which is a list of provision IDs, page IDs, or finding codes. It also names its `subject` or `paths`, and its `reason`. A gap also names `expiresOn`, `exit`, and `closedBy`, the records whose change closes the gap. A lasting waiver names `consequences` and carries no end date.

A lasting waiver never expires. The record lives in `.litespec/governance/waivers/`. A replaced default is not a waiver, because the default's `Replacement` names it.

A waiver covers only what it names. It reports `waiver-expired` after its date. It covers a finding in three cases: it names the finding's code, it names the provision whose Checked by line names that code, or it names that provision's page. In each case, the finding sits on the waiver's `subject` or `paths`.

## Consequences

The waiver record is the one shape for every waiver. The field `kind` states whether the waiver is a gap or a lasting choice. The field `covers` names a provision, a page, or finding codes. A gap carries `expiresOn`, `exit`, and the typed `closedBy` records. A lasting waiver carries `consequences` and no end date.

Three provisions in the waivers and release pages state the uses that this decision unifies. The decision also fixes the analyzer rule: an analyzer matches a waiver by comparing its `covers` with its own `LS` rule ID.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts from measurements over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when a waiver appears that covers no provision, no page and no named finding code.
