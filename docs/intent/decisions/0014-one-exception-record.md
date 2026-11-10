# One Departure Record

## Context

Waivers were uneven and too wide: a known gap silenced a check level G finding, a deviation silenced every finding on its subject instead of its provision, and deferrals with the frozen phase switched checks off with no expiry.

## Decision

One `departure` record replaces the deviation, the known gap, `deferredProvisions` and `check.phase: frozen`. It holds `covers`, a list of provision IDs, page IDs or finding codes; `subject` or `paths`; `reason`; and, for a gap the team means to close, `expiresOn` with `exit`. A departure without `expiresOn` is a lasting choice and does not expire. It covers only what it names, and it reports `departure-expired` after its date. A departure covers a finding when it names the finding's code, when it names the provision whose Checked by line names that code, or when it names that provision's page, and in each case the finding sits on the departure's `subject` or `paths`. The record lives in `.litespec/governance/departures/`. Replaced defaults are not departures, because their Replacement names them.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind `decision/0014-one-exception-record`, high confidence.
