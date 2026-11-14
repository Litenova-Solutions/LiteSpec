# Keep Release Reasoning as Records

LiteSpec states its rules as provisions. Each provision is one obligation that a project must meet. The reasons for those rules, the research behind them and the decisions that chose them need a home too. This record decides that the reasoning lives in records beside the standard, and it names the folders where those records sit. It concerns the people who maintain LiteSpec and write its records.

## Context

Provisions state obligations. They hold no question, no dated source and no conclusion that a later change can cite. When a change asks why a rule exists, the answer has nowhere to live.

LiteSpec needs a place for that reasoning that its tools read and that git tracks. The research and decision record shapes already exist, and the site already ingests records under `docs/intent`.

## Options

LiteSpec compares 4 places that could hold the reasoning.

| Option | Strengths | Drawbacks |
|---|---|---|
| Consumer homes under `docs/intent` | Comparable projects keep rationale in numbered records beside the normative text | The maintainer validator and the site each need a record pass |
| A root records folder | Reasoning stays visibly apart from the documented contract | No site ingestion or authoring precedent; the validator reads its pages as stray Markdown |
| Stay in `.agents` | No change to the contract, validator or site | Git ignores the folder, so no later change can cite the reasoning |
| `docs/reference` | Sits beside the other exact material that readers consult | The reference style states exact values and owns no records |

The first option is the one that already has a record shape and site ingestion. The record shows no such support for the other three options.

## Decision

LiteSpec keeps its research and decisions as records under `docs/intent/research/` and `docs/intent/decisions/`. The maintainer validator, `litespec-maintain validate`, checks these records.

The site shows these records as a non-applied area, separate from the provisions. The records use the consumer shapes for research and decisions.

## Consequences

The provision `litespec/core/maintaining#record-the-reasoning-behind-each-release` states the obligation to record the reasoning behind each release. Its Checked by line names the 5 `RECORD_*` finding codes.

Research and decisions inform and explain the standard. Only that provision creates an obligation.

## Evidence

`research/comparable-rationale-practice` is read in full from 5 project templates, with high confidence. `research/established-traceability-practice` rests on vendor restatements for the regulated regimes and on a second-hand glossary quotation.

Its claims on those points carry medium confidence.

## Revisit Conditions

This decision reopens if LiteSpec stops publishing its provisions as its specification.
