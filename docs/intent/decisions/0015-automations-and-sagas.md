---
{
  "type": "decision",
  "id": "decision/0015-automations-and-sagas",
  "title": "Automations and Sagas",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "One automation record, sagas, doors, bound atomicity",
      "pros": "One trigger shape ends the reaction and job split, saga names the orchestration honestly, doors end the auth split, and bound groups keep atomic writes visible.",
      "cons": "Reactions, jobs, processes and entry points all migrate to the new shapes in one change."
    },
    {
      "name": "Keep reaction and job apart",
      "pros": "Each trigger keeps its own page and checks.",
      "cons": "One shape under two names means two check sets proving the same fields."
    },
    {
      "name": "Atomic writes for one aggregate only",
      "pros": "The aggregate boundary stays absolute, as the outside practice advises.",
      "cons": "The largest consumer's place-order writes seven aggregates atomically today, so the ban would force a saga where the business settles at once."
    },
    {
      "name": "Keep process as the saga name",
      "pros": "No aggregate kind migrates.",
      "cons": "A lifecycle name for orchestration leaves lifecycles without their plain word and teaches newcomers the wrong shape."
    }
  ],
  "choice": "One automation record, sagas, doors, bound atomicity",
  "reopenedWhen": "An automation appears that no trigger of event, schedule or manual can start.",
  "basedOn": [
    "research/lower-layers-practice"
  ]
}
---

# Automations and Sagas

## Context

Jobs and reactions share one shape under two names, the `process` aggregate kind is a saga under a lifecycle name, entry points cover doors, triggers and content at once, and atomic writes across aggregates contradict the page's own aggregate definition.

## Decision

One `automation` record replaces reaction and job, with `triggeredBy` of `event`, `schedule` or `manual`. The aggregate kind `process` becomes `saga`: a coordinator whose steps run use cases on other aggregates, each with its compensation. A record with only a lifecycle is an `entity`, with "process manager" as the glossary alias. An entry point is a door from outside only (`browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`): the door states how a caller authenticates in `auth`, and the use case states who may act in `grant` and `tenant`. An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates; the use case's `changes` must equal that group; a group above 4 aggregates reports at check level A as a saga candidate; `best-effort` is removed.

## Consequences

`litespec/core/domain-model#declare-the-kind-of-every-aggregate` carries the saga and entity kinds, `litespec/core/edges#give-each-entry-point-a-kind` carries doors only, and `litespec/core/use-cases#state-the-consistency-of-every-use-case` carries bound atomicity. `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind` keeps auth on the door, and `litespec/core/access#grant-every-use-case-through-a-declared-grant` keeps the grant on the use case.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D27 to D30, high confidence.
