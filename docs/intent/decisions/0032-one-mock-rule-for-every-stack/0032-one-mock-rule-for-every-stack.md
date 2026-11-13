# One Mock Rule for Every Stack

## Context

The backend and the frontend stacks stated different mock rules. One backend rule required a test to substitute the project's own repositories and assert what a handler loaded, called and staged. Agents write that kind of test often, and they rewrite it with each refactor.

## Decision

One rule holds for every stack: a test substitutes only the outside providers and the non-deterministic values that the project does not control. The coordination test rule is removed, because scenario proof through the real host already proves coordination.

## Evidence with Confidence

`research/agent-testing-evidence` reports that coding agents add mocks in 36 percent of their commits, against 26 percent for other authors. The findings are rates from observed commits rather than controlled trials, so the confidence is medium.
