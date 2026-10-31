# Automations and Sagas

## Context

The `process` aggregate kind is a saga under a lifecycle name, entry points cover doors, triggers and content at once, and atomic writes across aggregates contradict the page's own aggregate definition. The owner withdrew the one-automation decision D27: reaction and job read more explicitly in code than one shared name.

## Decision

Reaction and job return as two record types. A reaction carries `reaction/<module>.<name>`, names the events it answers in `triggeredBy` with its skip condition in `skippedWhen`, and is named `<verb>-<what>-on-<fact>`. A job carries `job/<module>.<name>`, names its `kind` of `scheduled`, `one-off`, or `manual` with its `schedule`, `selection`, `lease`, `cursor`, and `guard`, and is named `<verb>-<what>` with the verb of the use case it runs. The aggregate kind `process` becomes `saga`: a coordinator whose steps run use cases on other aggregates, each with its compensation. A record with only a lifecycle is an `entity`, with "process manager" as the glossary alias. An entry point is a door from outside only (`browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`): the door states how a caller authenticates in `auth`, and the use case states who may act in `grant` and `tenant`. An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates; the use case's `changes` must equal that group; a group above 4 aggregates reports at check level A as a saga candidate; `best-effort` is removed.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D28 to D30, with D27 withdrawn by the owner on 2026-10-31, high confidence.
