# Agents Never Weaken Proving Tests

## Context

Agents edit tests to make them pass when a test and the specification disagree. A proof that changes with the implementation stops proving the decided behavior and starts restating the code.

## Decision

An agent never weakens a proving test. The rule lives on the agent page of the standard. A proving test that changed while its scenario did not is reported by `model check --since` as an advisory finding, so the reviewer sees it without ordinary work stopping.

## Evidence with Confidence

`research/agent-testing-evidence` reports that agents take shortcuts to pass a test that conflicts with the specification. The research gives no rate for a named model, so the confidence that agents do this often is low, and the rule rests on the direction of the evidence.
