# State the Start and Branches of Each Journey

A journey shows one actor reaching one goal through an ordered set of steps. This decision gives each journey a start written as an observable state and a structured list of branches, so the path a journey may take off its main order is a field that a check reads.

## Context

A journey's start sat in a Preconditions section, and its alternate paths sat in a field named `betweenSteps` and in a Variations section. The field held prose, so an action could appear in a branch and never reach a step. A reader could not tell which step a branch left, or where the journey went next.

The journey that served as the reference example also served a step that belonged to another journey. A use case that fulfilled a requirement in a journey with a different goal was hard to find, because the order of the journey did not show which steps it held.

## Options

LiteSpec compared 3 options for how a journey states its start and its branches. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Add `startedWhen` and a structured `branches` list | A check reads each branch, and an action in a branch cannot be written. | Every journey is rewritten, and the book renders each branch. |
| Keep `betweenSteps` as prose, with review questions | No field changes. | A reviewer reads the prose for each branch, and no check sees a hidden action. |
| Drop `betweenSteps` and keep branches in the Variations prose | The fields shrink. | No check sees the order of the steps, and no reader sees the branches without the explanation. |

## Decision

A journey carries `startedWhen`, one sentence stating the observable state in which the journey starts. Its finish stays in `finishedWhen`.

A journey leaves the order of its steps only through `branches`. Each branch names the step where the path differs, the condition under which it differs in product words, and the step the journey continues at, or `end`. An action is always a step, never a branch.

A journey's Example starts from `startedWhen` and performs its steps in order. Each clause of `finishedWhen` is reached by a step, and each blocker is the `failure` of the step it blocks.

## Consequences

`record-invalid` reports a journey without `startedWhen`, a `betweenSteps` field, or a branch that lacks a field. `field-invalid` reports a branch whose step names no step of the journey. The book renders the start before the steps and each branch as a sentence after them.

`litespec/core/intent#state-when-each-journey-starts` and `litespec/core/intent#branch-a-journey-only-between-its-steps` are required. `litespec/core/intent#reach-each-finish-condition-through-a-step` and `litespec/core/intent#state-each-journey-blocker-as-a-step-failure` are reviewed for. The order provision names `branches` in place of `betweenSteps`.

## Revisit Conditions

This decision holds until consumers report that branch lists repeat the steps so often that the field costs more review time than the section it replaced. It also holds until a branch needs more than one condition to state its path. Either report reopens the choice.
