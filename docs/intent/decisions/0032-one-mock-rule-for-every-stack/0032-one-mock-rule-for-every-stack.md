# One Mock Rule for Every Stack

A mock is a stand-in object that a test uses in place of a real part of the system. LiteSpec's testing standard states the rule for when a test may use one. This decision applies that one rule to every stack, meaning the language and framework a project's code uses, so a team that learns the rule once knows it for all its code.

## Context

The backend and the frontend stacks state different mock rules. One backend rule requires a test to substitute the project's own repositories, and to assert what a handler loaded, called and staged. Agents write that kind of test often, and they rewrite it with each refactor.

## Options

LiteSpec compared 3 options for mock rules. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One mock rule for every stack, with the Application coordination test rule removed | Tests do not depend on how a handler loads and stages its work, and one rule holds across every stack. | A team that tested its handlers through substituted repositories loses a check it used. |
| Keep the coordination rule and the stack mock rules | Each stack keeps the rules it already has. | Tests coupled to structure are the ones agents over-produce and rewrite, and the same mistake reads differently on each stack. |
| Mock rules per stack, with no shared rule | Each stack page stands alone. | A reader who learns one stack's rule cannot tell whether another stack holds the same rule. |

## Decision

One rule holds for every stack. A test substitutes only what the code under test does not own, or what is not deterministic. A test never substitutes the code's own repositories, handlers or modules.

The Application coordination test rule is removed. Scenario proof through the real host already proves coordination.

## Consequences

`litespec/quality/testing#use-one-mock-rule-for-every-stack` holds the one rule. Each stack's mock provision applies that rule to its own libraries.

Scenario proof through the real host still proves coordination, so the removed rule loses no proof of it.

## Evidence

`research/agent-testing-evidence` reports that coding agents add mocks in 36% of their commits, against 26% for non-agent commits. Hora and Robbes measured those rates from the commits, and the research read only their abstract page, not the full text.

The record rates the confidence as medium. The rates come from observed commits and not from controlled trials.

## Revisit Conditions

This decision reopens when a coordination fault reaches a release that scenario proof through the real host did not catch.

A fault of that kind would show that scenario proof misses coordination. The removed rule would then be the check that it needs.
