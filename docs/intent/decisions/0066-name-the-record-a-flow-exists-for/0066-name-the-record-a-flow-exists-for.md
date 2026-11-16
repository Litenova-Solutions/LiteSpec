# Name the Record a Flow Exists For

A flow states the ordered steps by which one actor reaches something the product owes. The flow moves a goal or answers a problem or constraint. This decision gives each flow exactly one reason in one of two fields. A reader who opens a flow learns why it exists before reading its steps.

## Context

Every flow served exactly one goal, because [decision/0005-flows-serve-a-goal](decision/0005-flows-serve-a-goal) chose one goal per flow. A goal carries a measure and a target. A flow that exists because every goal depends on it, or because the law requires it, moves no measure. Authors attached such flows to the nearest goal.

In one consumer, six of nine flows served a goal measuring the share of bank lines booked before a tax filing. Most of the six did not move that share. A sign-in flow and a leave-taking flow existed for auditability and for switching law, not for the measure. In the Claimwick expense product, all three flows served the decision-speed goal, including an admin flow that sets a threshold value.

The checks carried the same defect. A goal's completion depended on flows outside it. `goal-mover-missing` passed for the wrong reason. The trace stated nothing true about why a flow exists.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| One reason per flow in serves or answers | Each flow names its reason, and goal completion counts only moving flows. | Authors choose the reason for every enabling and duty flow. |
| Keep one goal per flow | No record changes. | Enabling and duty flows stay on goals they do not move, and the trace misleads. |
| One link field with four target types | One field holds every reason. | Several target sets and two meanings share one verb. |

## Decision

A flow carries exactly one upward link in one of two fields. `serves` names the goal the flow moves and is the normal case. `answers` names the problem or constraint the flow exists for.

Both hold a list of exactly one identifier (minItems 1, maxItems 1). The field grammar gives every link field a list, so one name keeps one shape. `serves` on a flow and `serves` on a use case both hold lists. The grammar rule is one name, one shape, with no exceptions list. The exactly-one rule stays in the shape. A flow with both fields, or neither, is `record-invalid`.

One verb per meaning follows [decision/0004-features-advance-goals](decision/0004-features-advance-goals), which removed a `serves` with several target types from features. `answers` is already the reads-as phrase for a feature's problem and for a constraint's requirement. A feature is no target. It states what the product offers, not why a flow exists. Step citations already connect a flow to its features.

The flow stays at product level, because its content spans several features and modules. Only `serves` counts as a goal mover, so the goal checks count only moving flows.

## Consequences

Both fields present, or neither, is `record-invalid`. `goal-mover-missing` counts only flows serving the goal. `intent-unreached` counts `answers`. A goal's in-scope flows are the flows serving it. Problems and constraints carry no completion stage.

The book renders the link with the reads-as phrase the Links table gives for its target type. The Claimwick expense product shows both fields, so the docs hold a real example of each.

## Evidence

[research/intent-hierarchy-evidence](research/intent-hierarchy-evidence) and [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence) measured the defects at the top of the trace: requirements without origins and goals without movers. [research/established-traceability-practice](research/established-traceability-practice) requires each link to name its reason. The consumer counts above were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a flow appears whose reason is neither a goal it moves nor a problem or constraint it answers.
