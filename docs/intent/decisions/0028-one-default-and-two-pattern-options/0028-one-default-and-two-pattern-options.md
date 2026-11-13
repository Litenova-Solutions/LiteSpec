# One Default and Two Options per Pattern

LiteSpec's pattern catalog gives one recommended answer for each decision that a project faces. That answer is the Default, and a pattern may add options beside it. This decision caps each pattern at the Default and 2 options, so a reader compares 3 answers at most.

## Context

Pattern pages offered long option lists for one decision. The pattern pages listed 107 options before the cut, and a reader cannot compare that many. An agent that builds a page has no basis to choose among them.

## Options

LiteSpec compared 3 options for the number of pattern options. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| The Default and at most 2 options, enforced by `validate` | A reader compares 3 options at most, and `validate` stops a fourth from entering the catalog. | A team that wants a fourth option proposes it to the pattern page instead of adding it locally. |
| Every option the pattern pages list | No option is dropped, and no team loses a choice it used. | The pattern pages list 107 options, which a reader cannot compare. |
| The Default only | The catalog holds one answer per pattern. | An owner with a real choice has no option to pick, so each departure becomes a pattern outside the catalog. |

## Decision

Each pattern offers its Default and at most 2 options. A proposal for a third option goes to the pattern page. The owner accepts it there before any catalog entry uses it.

## Consequences

`validate` reports a pattern that offers more than its Default and 2 options, under `PATTERN_OPTION_COUNT`.

The pattern pages were cut to the Default and the 2 options that their rationale points to most. Page order breaks a tie between options.

## Revisit Conditions

This decision reopens when an owner needs a third option that a proposal to the pattern page cannot settle.

A proposal for a third option that the pattern page cannot settle leaves the owner with no way to add it. That gap is the case that would change the choice.
