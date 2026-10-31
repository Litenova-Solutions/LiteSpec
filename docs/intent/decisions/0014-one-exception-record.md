# One Exception Record

## Context

Exceptions are uneven and too wide: a draft known gap nobody approved silences a check level G finding, a deviation silences every finding on its subject instead of its provision, and deferrals with the frozen phase switch checks off with no owner or expiry.

## Decision

One `exception` record replaces deviation, known gap, `deferredProvisions` and `check.phase: frozen`, with `kind` (`departure` or `gap`), `covers` (a provision or finding codes), `subject` or `paths`, `reason`, `owner`, `status` and `expiresOn`. It covers only what it names, applies once approved, where one person may approve, and `exception-expired` reports it after its date.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D26, high confidence.
