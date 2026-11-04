# Automations and Sagas

## Context

Entry points cover doors, triggers and content at once, and atomic writes across aggregates contradict the aggregate definition. One automation name hides whether the work answers an event or runs on a schedule.

## Decision

Reaction and job are two record types. A reaction carries `reaction/<module>.<name>`, names the events it answers in `triggeredBy` with its skip condition in `skippedWhen`, and is named `<verb>-<what>-on-<fact>`. A job carries `job/<module>.<name>`, names its `kind` of `scheduled`, `one-off`, or `manual` with its `schedule`, `selection`, `lease`, `cursor`, and `guard`, and is named `<verb>-<what>` with the verb of the use case it runs. An aggregate is an entity or a store. Coordination across aggregates belongs to the workflow-engine extension. An entry point is a door from outside only (`browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`): the door states how a caller authenticates in `auth`, and the use case states who may act in `grant` and `tenant`. An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates; the use case's `changes` must equal that group.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D28 to D30, high confidence.
