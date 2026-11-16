# Declare Access Settings and Deadlines

Teams secure sensitive acts with a fresh sign-in, track running values with readers and dates, and move states by deadlines. This decision declares all three in fields, so no window, default, or deadline stays in prose alone.

## Context

Each sensitive use case wrote its own sign-in window, so one consumer held four copies of the same window. Settings named no readers, so nothing reported a setting no record reads. A setting's new value could silently change a posted record. Deadlines stayed hidden in job prose with no link to the state they moved.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A recent-sign-in setting, readable dated settings, timed transitions | One window, stated readers and dates, deadlines on transitions. | The product names its window by hand. |
| Keep guards, prose windows, scheduled prose | Nothing moves. | Windows drift, settings change posted records, deadlines stay hidden. |
| A central policy record | One place holds every bound. | The policy duplicates each record's fields. |

## Decision

`access` takes `recentSignIn`, naming the setting that holds the window. The refusal derives like `forbidden`. The window is one product setting instead of a copy in every use case.

`reads` on a use case, reaction, or job may name settings. A setting no record reads is reported.

A setting with `dated: true` states that each value applies from a date and never changes a posted record.

Defaults take the setting's value type. The type is an ISO duration, an amount with its currency, a number, or one of its `choices`.

A state transition may be moved by time. `movedBy` names the job and `after` names the duration from entering the state.

Long-running processes stay out of the core, as [decision/0015-automations-and-sagas](decision/0015-automations-and-sagas) decided. The automation page shows the pattern of states, reactions, and timed transitions that covers them, with a worked example.

## Consequences

Claimwick carries a dated setting with a typed default and a timed transition. Each shape has one instance the docs cite.

The four copied windows in the consumer become the one setting.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) confirmed uneven exceptions and proof words that overclaim below Intent, where windows and deadlines stayed in prose alone. The four copied windows were counted in live records with high confidence.

## Revisit Conditions

This decision reopens when a sensitive act needs a window the product setting cannot state. It also reopens when a deadline needs more than a duration and a job.
