# Automations and Sagas

LiteSpec models automated work in two record types. A reaction runs when an event happens. A job runs on a schedule, once, or on demand. A saga is a flow that changes several aggregates in steps, and an aggregate is one entity or one store. This decision sets how the model names these records, and how a write across several aggregates is handled. It matters because one name for two kinds of work hides which kind a team is reading.

## Context

Entry points cover doors, triggers and content at once. An atomic write across aggregates contradicts the aggregate definition. One automation name hides whether the work answers an event or runs on a schedule.

## Options

LiteSpec compares 4 designs for automation and multi-aggregate writes.

| Option | Strengths | Drawbacks |
|---|---|---|
| Reactions and jobs in core, atomic writes through a bound rule | Reaction and job name the two shapes, doors end the auth split, and bound groups keep atomic writes visible | Reactions, jobs and entry points all move to the new shapes in one change |
| Keep one automation record | One trigger shape ends the reaction and job split | One name hides whether the work answers an event or runs on a schedule |
| Atomic writes for one aggregate only | The aggregate boundary stays absolute, as the outside practice advises | The largest consumer's place-order write changes 7 aggregates atomically today, so the ban would force coordination the business settles all at once |
| Coordination as a core aggregate kind | Multi-aggregate flows sit beside entities in core | No consumer uses them in core, and the record schema would need a saga kind that no check reads |

The chosen option keeps two record types and an atomic write rule. The other options each merge the two work types, ban atomic writes, or add a kind that no consumer uses.

## Decision

A reaction and a job are two record types. A reaction carries the identifier `reaction/<module>.<name>`. It names the events it answers in `triggeredBy`, and its skip condition in `skippedWhen`. Its name follows `<verb>-<what>-on-<fact>`.

A job carries the identifier `job/<module>.<name>`. It names its `kind`, which is `scheduled`, `one-off` or `manual`, and its `schedule`, `selection`, `lease`, `cursor` and `guard`. Its name follows `<verb>-<what>`, using the verb of the use case that it runs.

An aggregate is an entity or a store. Coordination across aggregates is not a record kind. A use case states its consistency as `atomic`, `per-record` or `compensated`. An atomic write names its group in an `atomicity` rule.

No workflow-engine extension exists. `decision/0030-limit-the-jobs-extension` removes the workflow-engine subjects that the jobs extension held.

An entry point is a door from outside only. The doors are `browser`, `api`, `webhook`, `device`, `cli`, `mcp` and `served-content`. The door states how a caller authenticates, in `auth`. The use case states who may act, in `grant` and `tenant`. An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates, and the use case's `changes` must equal that group.

## Consequences

The provision `litespec/core/domain-model#declare-the-kind-of-every-aggregate` carries the entity and store kinds, and no coordination kind exists. The provision `litespec/core/edges#give-each-entry-point-a-kind` carries doors only.

The provision `litespec/core/use-cases#state-the-consistency-of-every-use-case` carries the 3 consistency modes. An atomic write across aggregates cites its bound rule. A compensated write names the undo of each of its writes.

The provision `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind` keeps auth on the door. The provision `litespec/core/access#grant-every-use-case-through-a-declared-grant` keeps the grant on the use case. Five provisions in the automation page carry the reaction trigger, the job kind, the two name forms and the guard on manual jobs.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts measured over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when work appears that neither a reaction answering events nor a scheduled or manual job can carry. It also reopens for a multi-aggregate flow that neither an atomic write nor a compensated use case can state.
