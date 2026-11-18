# Give Product Commitments a Home

A product carries permanent commitments that no offer holds, such as tenant isolation, audit integrity, accessibility, and recovery. This decision gives each of them a home at product level, with the reason it answers cited beside it.

## Context

Every requirement needed a feature, so a permanent commitment with no offer had no home. Authors filed tenant isolation, audit integrity, and retention of the books under invented features, or left them unwritten.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Product requirements at product level, citing their reason | Commitments gain a home with no invented feature. | Invented features split into product requirements. |
| Keep commitments under features | No home is added. | Commitments still wait on an offer. |
| Leave commitments unwritten | Nothing is written for what no offer carries. | No check reads them and no Behavior fulfills them. |

## Decision

A requirement with no offer sits at `intent/requirements/<name>/` under the product, citing the constraint, goal, or problem it comes from.

## Consequences

`litespec new requirement <name>` writes the product-level home. The standing examples are tenant isolation, audit integrity, accessibility, and recovery.

## Evidence

[research/established-traceability-practice](research/established-traceability-practice) requires every requirement to name an origin, which the product-level `cites` carries. The homeless commitments were read from live records with high confidence.

## Revisit Conditions

This decision reopens when a product commitment needs an offer to be built.
