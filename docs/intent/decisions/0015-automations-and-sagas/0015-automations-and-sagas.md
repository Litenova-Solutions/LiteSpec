# Automations and Sagas

Teams need one name for each kind of automated work: a reaction runs when an event happens, and a job runs on a schedule or once. A saga is a flow that changes several aggregates in steps, and an aggregate is one entity or one store. This decision sets how the model names these records and how a write across several aggregates is handled. One name for two kinds of work hides which kind a team reads.

## Context

One entry-point shape covers outside callers, triggers, and content. An atomic write across aggregates contradicts the aggregate definition. One automation name hides whether the work answers an event or runs on a schedule.

## Options

LiteSpec compares 4 designs for automation and multi-aggregate writes.

| Option | Strengths | Drawbacks |
|---|---|---|
| Reactions and jobs in core, atomic writes through a bound rule | Reaction and job name the two shapes, entry-point kinds end the auth split, and bound groups keep atomic writes visible | Reactions, jobs, and entry points all move to the new shapes in one change |
| Keep one automation record | One trigger shape ends the reaction and job split | One name hides whether the work answers an event or runs on a schedule |
| Atomic writes for one aggregate only | The aggregate boundary stays absolute, as the outside practice advises | The largest consumer's place-order write changes 7 aggregates atomically today, so the ban would force coordination where the business settles in one step |
| Coordination as a core aggregate kind | Multi-aggregate flows sit beside entities in core | No consumer uses them in core, and the record schema would need a saga kind that no check reads |

The chosen option keeps two record types and an atomic write rule. The other options each merge the two work types, ban atomic writes, or add a kind that no consumer uses.

## Decision

A reaction and a job are two record types. A reaction carries the identifier `reaction/<module>.<name>`. It names the events it answers in `triggeredBy`, and its skip condition in `skippedWhen`. Its name follows `<verb>-<what>-on-<fact>`.

A job carries the identifier `job/<module>.<name>`. It names its `kind`, which is `scheduled` or `one-off`, and its `schedule`, `selection`, `lease`, and `cursor`. Its name follows `<verb>-<what>`, using the verb of the use case that it runs. Work a person starts by hand is a command whose entry point and access name who starts it, not a job.

An aggregate is an entity or a store. Coordination across aggregates is not a record kind. A use case states its consistency as `atomic` or `per-record`. An atomic write names its group in an `atomicity` rule. Compensation is a reaction with `compensates`: a reaction that undoes a committed step of a multi-step process names the step's use case in `compensates`.

No workflow-engine extension exists. `decision/0030-limit-the-jobs-extension` removes the workflow-engine subjects that the jobs extension held.

An entry point leads from outside only. The kinds are `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, and `served-content`. The entry point states how a caller authenticates, in `auth`. The use case states who may act, in `grant` and `tenant`. An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates, and the use case's `writes` must equal that group. The write also needs a decision record naming the use case and the invariant that needs both in one transaction.

## Consequences

An aggregate carries no kind, a store is its own record type under `litespec/core/domain-model#keep-technical-state-out-of-behavior`, and no coordination kind exists. The provision `litespec/core/edges#give-each-entry-point-a-kind` carries the kinds only.

The provision `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands` carries the 2 consistency modes. An atomic write across aggregates cites its bound rule and its decision record. A reaction that undoes a committed step names the step in `compensates`, as `litespec/core/automation#name-the-step-a-compensating-reaction-undoes` states.

The provision `litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind` keeps auth on the entry point. The provision `litespec/core/access#authorize-every-use-case-through-a-declared-grant` keeps the grant on the use case. Four provisions in the automation page carry the reaction trigger, the job kind, and the two name forms.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts from measurements over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when work appears that neither a reaction answering events nor a scheduled job can carry. It also reopens for a multi-step process that states and reactions with `compensates` cannot express. The multi-step workflow case of `decision/0030-limit-the-jobs-extension` also reopens this decision.
