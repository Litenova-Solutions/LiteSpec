# Set One Agent Authority Boundary

Agents do engineering work and people keep decision authority. This decision states the one boundary between the two, so every agent knows what it may do unasked and what needs approval.

## Context

No page stated what an agent may do on its own. Agents approved their own product-policy proposals, widened scope past the task, invented business values no record holds, and took consequential outside actions. Reviews caught some of it after the fact. Most of it shipped in records nobody questioned.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| One stated boundary | Agents know their acts; policy stays owned. | Agents stop at the boundary and ask. |
| Trust the task text alone | No boundary to learn. | Scope widens silently; values get invented. |
| Approve every agent act | Nothing happens unreviewed. | Owners approve each draft; agents wait. |

## Decision

An agent may apply an approved standard or decision, draft records and proposed decisions, implement within the task, and report findings, including a failed check or a conflict between provisions. It may not approve its own product-policy proposal, widen scope, invent business values, or take a consequential outside action beyond the task.

An explicit task instruction that names the action and its bounds is the approval.

A project may grant wider authority in its `AGENTS.md`.

## Consequences

A product-policy proposal from an agent waits on the owner like any draft. The agent reports a failed check or a conflict between provisions, never works around it.

The agent protocol carries the boundary, so each task starts inside it.

## Evidence

[research/agent-testing-evidence](research/agent-testing-evidence) measured how agent-written tests fail and what guards them: the boundary is the guard for acts beyond tests. [research/comparable-rationale-practice](research/comparable-rationale-practice) found the specification remaining the only normative surface, which agents apply while owners keep decision authority. The unapproved acts were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a task needs an act the boundary forbids and no instruction can approve. It also reopens when the boundary blocks work the owner always allows.
