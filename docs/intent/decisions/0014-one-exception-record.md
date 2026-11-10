# One Exception Record

## Context

Exceptions are uneven and too wide: a known gap silences a check level G finding, a deviation silences every finding on its subject instead of its provision, and deferrals with the frozen phase switch checks off with no expiry.

## Decision

One `exception` record replaces deviation, known gap, `deferredProvisions` and `check.phase: frozen`, with `kind` (`departure` or `gap`), `covers` (a provision or finding codes), `subject` or `paths`, `reason`, `exit` and `expiresOn`. It covers only what it names and reports when its date has passed.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind `decision/0014-one-exception-record`, high confidence.
