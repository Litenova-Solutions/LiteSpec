# Declare Access Settings and Deadlines

Sensitive acts need a fresh sign-in, running values need readers and dates, and states need deadlines. This decision declares all three in fields, so no window, default, or deadline lives only in prose.

## Context

Each sensitive use case wrote its own sign-in window, so one consumer held four copies of the same window. Settings named no readers, so nothing reported a setting nothing read. A setting's new value could silently change a posted record. Deadlines hid in job prose with no link to the state they moved.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A recent-sign-in setting, readable dated settings, timed transitions | One window, stated readers and dates, deadlines on transitions. | The product names its window by hand. |
| Keep guards, prose windows, scheduled prose | Nothing moves. | Windows drift, settings change posted records, deadlines hide. |
| A central policy record | One place holds every bound. | The policy duplicates each record's fields. |

## Decision

`access` takes `recentSignIn`, naming the setting that holds the window, and the refusal derives like `forbidden`. The window is one product setting instead of a copy in every use case.

`reads` on a use case, reaction, or job may name settings, and a setting no record reads is reported. A setting states with `dated: true` that each value applies from a date and never changes a posted record. Defaults take the setting's value type: an ISO duration, an amount with its currency, a number, or one of its `choices`.

A state transition may be moved by time: `movedBy` names the job and `after` names the duration from entering the state. Long-running processes stay out of the core, as [decision/0015-automations-and-sagas](decision/0015-automations-and-sagas) decided; the automation page shows the pattern of states, reactions, and timed transitions that covers them, with a worked example.

## Consequences

Claimwick carries a dated setting with a typed default and a timed transition, so each shape has one instance the docs cite. The four copied windows in the consumer collapse into the one setting.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) confirmed uneven exceptions and proof words that overclaim below Intent, where windows and deadlines sat in prose. The four copied windows were counted in live records with high confidence.

## Revisit Conditions

This decision reopens when a sensitive act needs a window the product setting cannot state. It also reopens when a deadline needs more than a duration and a job.
