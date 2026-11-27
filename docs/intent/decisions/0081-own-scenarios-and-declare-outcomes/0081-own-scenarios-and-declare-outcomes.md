# Own Scenarios and Declare Outcomes

An author proving one behavior writes a scenario, and a reaction or job ends each run in one state. This decision keeps each scenario with the behavior it exercises, declares the outcomes of every reaction and job, and runs each effect on its own.

## Context

A scenario's parent was its result, so scenarios of reactions and jobs sat under use-case results they never touched.

An automation scenario had to end in a run use case's result. A send-only reaction or job runs no use case, so authors invented commands to end its scenarios.

Effects of one reaction shared a run, so one slow effect blocked the rest and a retry reran finished work.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Scenarios with their behavior; declared outcomes; lone effects | One parent per scenario; send-only ends named; lone retries. | Scenarios move; outcomes declared. |
| Keep scenarios under results with run-result ends | Nothing moves. | Send-only automation invents commands. |
| Name outcomes, share one effect run | Fewer fields. | Slow effects block; retries rerun finished work. |

## Decision

A scenario's parent is its use case, reaction, or job, stored in that record's file. It names its expected result, or for a reaction or job one of its outcomes, in an explicit link. A scenario's parent is never its result.

A reaction or job declares its run outcomes from a closed set: `completed`, `skipped`, `suppressed`, `failed`, `retry-scheduled`, `nothing-selected`. Its scenarios end in one of them or in a run use case's result, so a send-only reaction or job needs no invented command. A job's name takes its own verb when it runs no use case.

Each effect of a reaction or job runs, retries, and deduplicates on its own under its own idempotency. An author orders effects by chaining facts, never by assuming an order.

## Consequences

The book groups a use case's scenarios by result.

A use-case scenario keeps `result`; a reaction or job scenario names either `result` or `outcome`. A send-only reaction ends in `completed` with no command behind it.

## Evidence

[research/behavior-containment-evidence](research/behavior-containment-evidence) found scenarios finding their parent only through file placement, which the stored-with-parent rule replaces. [research/lower-layers-practice](research/lower-layers-practice) surveyed the automation practice the closed outcome set follows. The send-only ends were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a scenario proves behavior across two operations inseparably. It also reopens when two effects need one shared run.
