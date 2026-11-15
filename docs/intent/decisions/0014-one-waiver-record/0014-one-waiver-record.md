# One Waiver Record

A waiver is a written exception that a project records when it sets a rule aside. LiteSpec uses one waiver record for every kind of exception: a deviation, a known gap, a deferral and a frozen phase. Each waiver names what it covers and gives a reason. A gap that the team means to close also carries an expiry date. This decision explains why one shape is enough.

## Context

Waivers are uneven and too wide. A known gap silences a check at level G, the gating level. A deviation silences every finding on its subject, not only the finding for its provision. A deferral with the frozen phase switches checks off with no expiry.

## Options

LiteSpec compares 4 ways to record an exception.

| Option | Strengths | Drawbacks |
|---|---|---|
| One waiver record with scope and expiry | One shape carries every waiver with its reason and scope, and a gap also has an end date | Deviations, known gaps, deferrals and the frozen phase all move to the one record in one change |
| Keep 4 mechanisms and tighten each | Each mechanism keeps its readers | 4 waiver shapes and 4 expiry rules keep the uneven coverage that caused the finding |
| Exceptions as project settings only | One file holds every waiver | Settings carry no reason and no per-item expiry, so waivers never return for review |
| Every finding blocks | No waiver, so no waiver abuse | One-person teams stall on advisory findings |

The chosen option keeps one shape for every waiver. The other options keep 4 shapes, drop the reasons, or block on every finding.

## Decision

One `waiver` record replaces the deviation, the known gap, `deferredProvisions` and `check.phase: frozen`. A waiver names what it covers in `covers`, which is a list of provision IDs, page IDs or finding codes. It also names its `subject` or `paths`, and its `reason`. A gap that the team means to close also names `expiresOn` and `exit`.

A waiver without `expiresOn` is a lasting choice, and it never expires. The record lives in `.litespec/governance/waivers/`. A replaced default is not a waiver, because the default's `Replacement` names it.

A waiver covers only what it names. It reports `waiver-expired` after its date. It covers a finding in three cases: it names the finding's code, it names the provision whose Checked by line names that code, or it names that provision's page. In each case, the finding sits on the waiver's `subject` or `paths`.

## Consequences

The waiver record becomes the one shape for every waiver. The field `kind` is removed. The field `covers` names a provision, a page or finding codes. The field `expiresOn` is set only for a gap that the team means to close.

Three provisions in the waivers and release pages state the uses that this decision unifies. The decision also fixes the analyzer rule: an analyzer matches a waiver by comparing its `covers` with its own `LS` rule ID.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts measured over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when a waiver appears that covers no provision, no page and no named finding code.
