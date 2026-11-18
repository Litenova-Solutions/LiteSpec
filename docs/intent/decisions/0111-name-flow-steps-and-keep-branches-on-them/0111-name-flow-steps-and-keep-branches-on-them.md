# Name Flow Steps and Keep Branches on Them

Each flow step carries a name for its act, and each branch sits on the step it leaves. This decision gives every step an identifier that reads on its own and puts every path where it leaves, so citations, checks, queries, and the book all read the step without opening the flow.

## Context

A flow's steps carried position ids, `s1`, `s2`, and so on. A use case citing `flow/submit-and-reimburse#s3` could not be read without opening the flow, and every inserted step renumbered the steps after it.

Branches stood in one list at the bottom of the file, linked back by those ids. The distance hid modeling slips: a branch `s1 -> s2` that really meant instead of step 1, or one branch to `end` packing two conditions where one of them only skipped a step.

## Options

LiteSpec compared 2 options for flow step ids and branch placement. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Name each step `step/<flow>.<act>` and keep each branch on the step it leaves | Every step citation reads without opening the flow. Inserting a step renames nothing. Each branch reads beside the step it leaves, with one condition and one ending. | Every flow file, step citation, branch check, query, and book flow page changes in one change. |
| Keep position ids and the flow-level branch list | Every flow file, step citation, check, and book page stays as written. | Citations stay unreadable without the flow, insertions keep renumbering steps, and the branch list keeps hiding modeling slips. |

## Decision

Each step carries its id as `step/<flow>.<act>`, where `<flow>` is the flow's name and `<act>` is the step's act as a verb phrase in kebab case. List order is the main path, and no position lives in an id. The flow-level `branches` list is removed.

Each branch sits on the step it leaves, in that step's `branches`. A branch states one `when` condition, and exactly one of `then`, naming the step of the same flow where the path continues, or `ends`, stating the outcome when the flow ends there. A branch that does work of its own states that work in `instead`, with its own `carries` and `system` where the work needs them.

A step id is a real identifier in the record index, resolved by every link check, by `model query`, and by search. Screens and use cases cite `step/<flow>.<act>` in `serves`. Code never marks a step: code points one layer up, to the behavior serving it, and the trace walks from there.

## Consequences

`model query` on a step id returns the step with its `statement`, `failure`, and branches, the screens and use cases serving it, the requirements it carries with the use cases fulfilling them, and for each serving or fulfilling record the code files whose marks name it and the tests proving its scenarios. The book shows each step with its branches indented under it and its trace.

The checks report a `then` naming no step of the flow, a branch with both or neither of `then` and `ends`, a `when` joining two conditions with "or", a step id outside the form or naming another flow, a step no path reaches, and a step citation naming no step. The existing step checks keep working on the new ids, beside a finding for a step whose serving use cases have no implementing code.

The rewritten provisions are `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for`, `litespec/core/intent#write-flow-steps-as-sentences-with-ids`, `litespec/core/intent#branch-a-flow-only-between-its-steps`, and `litespec/core/model#serve-every-flow-step`, with the new `litespec/core/intent#reach-every-flow-step-by-a-path`.

## Revisit Conditions

This decision reopens when a product shows steps that no verb phrase names, or branches that belong to the whole flow rather than to one step.

Either case means the step no longer names one act, or the path leaves no one step. The team then decides again how steps and branches are identified.
