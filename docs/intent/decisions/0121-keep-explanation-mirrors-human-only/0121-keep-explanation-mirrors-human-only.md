# Keep Explanation Mirrors Human Only

Records carry every fact a build needs. Explanation files help people read those facts, and no check reads them.

## Context

Decision 0095 made every explanation file restate its record so agents could build from YAML alone, and it coupled the mirrors to checks.

In practice authors updated one copy and forgot the other. Checks read prose they should never have judged, so authors wrote to satisfy the checker instead of the reader.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Records carry the specification, mirrors stay human-only and unchecked | Agents build from fields alone. People keep context and examples. No check reads prose. | Two copies to update in one change, and drift shows only on review. |
| Keep mirrors coupled to checks | Drift fails fast in the toolchain. | Checks judge prose style, and authors write to satisfy the checker instead of the reader. |
| Drop explanation files and keep only the records | One copy of every fact and nothing to drift. | People lose the context, reasons and examples that make a specification readable cold. |

## Decision

The records state the whole specification in fields an agent and a check can read.

Each explanation file restates the same specification for people, adds no fact, and changes in the same commit as its record. A Claimwick expense rule shows the shape: the YAML holds the condition and the outcome, and the beside it explains why the rule exists.

## Consequences

No check reads a `.md` mirror. `validate` and `model check` read YAML fields only, so prose quality stays a human review concern.

This decision supersedes `decision/0095-keep-the-whole-specification-in-the-records` for the mirrors: where that decision coupled explanations to checks, this one keeps them human-only.

## Revisit Conditions

Reopen this when authors routinely leave mirrors out of step with their records, or when a fact proves impossible to state in a field.
