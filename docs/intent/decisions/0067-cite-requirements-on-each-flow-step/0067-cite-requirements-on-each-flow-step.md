# Cite Requirements on Each Flow Step

A step is one act in an ordered story. A requirement is a promise the product keeps. This decision puts the citations on the step that carries each promise, so a reader sees what each step owes without opening another record.

## Context

A flow cited its requirements as a whole, in `cites` on the flow, so nothing recorded which step carried which promise. Steps restated requirements instead of pointing at them. Requirements attached to a flow through steps off its path.

In one consumer, a sign-in flow showed both defects. The first step's failure text restated the code validity rule, so a rule change needed two edits. Two steps stood off the path from opening the product to closing the session: linking a second sign-in method and reading the audit trail. They existed only so the flow could cite their requirements.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Each step cites its requirements | Each promise is cited where it applies, and rules are stated once. | Citations move onto steps by hand, and failures are reworded. |
| Keep cites on the flow | No record changes. | No step names its promise, and steps restate requirements. |
| Cite from the serving use cases only | Each requirement is cited once. | The flow loses which step carries the promise. |

## Decision

A step names the requirements it carries in its own `cites`. The flow keeps no `cites` field. The model computes the flow's citations as the union of its steps, so each citation is written once.

A step's `statement` and `failure` state what the person does and experiences. Numbers, limits, and conditions stay in the cited requirement. A failure states what the person sees and can do next, never the rule behind it. With a wrong code, no session starts and the person asks for a new code.

A step names a person in `actor` or an external system in `system`. Jobs and reactions never appear in a step. They serve the step upward like use cases. A step stays in product terms while jobs and reactions serve it. An external system in a step is a relationship the flow states on purpose.

A check tying citations to servers lands only if it proves cheap and sure on real records. Otherwise the rule stays reviewed rather than checked.

## Consequences

The Links table row for `cites` moves from the flow to the step. A feature's in-scope flows are the flows citing its requirements through their steps. The book lists each cited requirement under its step, with its statement and its `except` and fallback inline, instead of one block for the flow.

Steps that stand off the path leave the flow. Their requirements stay fulfilled by their use cases, which serve no step for them. Authors write a flow of their own only when they hold a start and a finish worth stating.

## Evidence

[research/intent-hierarchy-evidence](research/intent-hierarchy-evidence) found authors following the page rather than the parent, so the promise belongs on the step the author writes. [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence) found the Behavior links below dense and checked, which is where the serving use cases already point. The sign-in flow defects were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a requirement applies to a flow as a whole and to no single step. It also reopens when a step needs a server the `serves` link cannot name.
