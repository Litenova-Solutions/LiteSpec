# Features Advance Goals

A feature in LiteSpec is a capability that the product offers. Each feature solves a problem, and it advances a goal, which is an outcome that the product aims for. This decision renames the link from a feature to a goal, so the word matches the meaning. It matters because a reader who starts at a goal should find the features that move it.

## Context

A feature links to a goal with the word `serves`. The other records that use `serves` link to journey steps, not to goals. The same word therefore names two different kinds of target.

## Options

LiteSpec compares 4 options for the link words.

| Option | Strengths | Drawbacks |
|---|---|---|
| Advances for goals, solves for problems | One verb for each meaning; every existing feature already solves exactly one problem | Features, provisions, checks and the book all learn the new verb in one change |
| Keep `serves` for features | No field renames and no citation sweep | Three target sets and two meanings remain, and authors must guess which targets are allowed |
| Goals optional for features | With few goals, forcing one goal per feature risks vague claims that say less than the feature's offering | A reader who starts at most planned features reaches no goal, and only a handful of current requirements trace to a goal |
| One shared link for goals and problems | A feature carries one list instead of two | A goal states an outcome and a problem states a pain, so one list hides which one the feature moves |

The chosen option gives each link its own verb. A feature advances goals and solves problems, and the two verbs never mean the same thing.

## Decision

A feature's link to a goal is renamed from `serves` to `advances`. A feature's link to a problem is `solves`. Every feature names at least one problem in `solves`, and the first problem listed there is the feature's parent. Every feature names at least one goal in `advances`.

## Consequences

The provision `litespec/core/intent#state-each-feature-with-its-offering` states the renamed fields. The word `serves` keeps one meaning for each record that uses it. A use case, a reaction, a job or a screen serves journey steps. A journey serves one goal.

## Evidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, with high confidence. `research/established-traceability-practice` carries the limit that its claims on the regulated regimes rest on vendor restatements.

## Revisit Conditions

This decision reopens when a feature kind appears that advances no goal and solves no problem.
