# Solve Problems with Standalone Features

An author files a feature as an offer, and one offer can answer more than one problem. This decision stands each feature alone with an explicit `solves` link, and gives a product-wide commitment a home with no offer.

## Context

Each feature sat in the folder of the one problem it answered. A feature whose content spanned two problems did not belong to either, yet the folder filed it under one. Splitting such features cost more than sharing them.

A permanent commitment with no offer had no home either, because every requirement needed a feature. Authors filed tenant isolation, audit integrity, and retention of the books under invented features, or left them unwritten.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Standalone features with solves; product requirements | Each offer written once; commitments gain a home. | Features move; invented features split. |
| Keep features under their problem | Nothing moves. | Spanning offers misfile; commitments stay homeless. |
| Standalone features, commitments under features | Features move once. | Commitments still wait on an offer. |

## Decision

Features move to `intent/features/<name>/` with their requirements under them, and name the problems they answer in `solves` with one or more identifiers. This applies the placement provision: a feature whose content spans two problems belongs to neither. The book groups features under each problem they solve.

A requirement with no offer sits at `intent/requirements/<name>/` under the product, citing the constraint, goal, or problem it comes from. The standing examples are tenant isolation, audit integrity, accessibility, and recovery.

## Consequences

The typed-folder consequences naming features under problems are rewritten in place. `litespec new feature` writes the standalone home and takes the solved problems.

## Evidence

[research/intent-hierarchy-evidence](research/intent-hierarchy-evidence) found outside tools grouping each promise with the unit its author writes, and authors following the page rather than the parent. [research/established-traceability-practice](research/established-traceability-practice) requires every requirement to name an origin, which the product-level `cites` carries. The spanning features and homeless commitments were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a feature answers problems so unrelated that one offer misleads. It also reopens when a product commitment needs an offer to be built.
