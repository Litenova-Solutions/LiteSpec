# Audit the Rendered Page

LiteSpec ships a command that opens each route in a browser and reports what the rendered page shows. Records and wireframes state the structure; the audit reads the result at real widths and schemes.

## Context

Records and wireframes cannot judge what exists only in the rendered page. A field wider than its value, an action group at mixed sizes, two primary actions on one view, a tab row wrapping past one row, a table clipping at a narrow width, neighbouring layers sharing one fill, or a code where a name belongs: none of these exist before the page renders.

The route suite could assert these, but it is consumer-authored Playwright that no LiteSpec check reads, so a missing assertion fails silently. Nothing in the standard opens a route in a browser and reports what it shows.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Require the assertions in the consumer route suite | No new command; each product asserts its own pages in the suite it already runs. | A product that never writes an assertion passes as quietly as one that writes them all. |
| Ship a rendered-page audit command with ten codes | One command reports the same ten findings on every product, so a missing page and a failing page both show. | A browser audit to keep, with routes, widths, schemes and sign-in state to configure per product. |
| Judge rendered pages by human review only | No tooling to build or keep; a reviewer sees what any audit would assert. | Review effort grows with every route and width, and unreviewed routes ship unseen. |

## Decision

The command `litespec ui audit` opens each route at the given widths and schemes and reports ten codes: `surface-nesting-invalid`, `surface-contrast-failed`, `field-surface-missing`, `field-width-invalid`, `action-group-invalid`, `primary-action-duplicate`, `column-alignment-invalid`, `text-code-invalid`, `tab-label-invalid` and `table-clip-invalid`.

Each screen is still reviewed from screenshots at phone, laptop and wide widths in each declared scheme before it is reported done. The audit checks what a tool can judge; the review judges the rest.

## Consequences

The web route-suite section points at the audit and lists its codes. The composition standards name the audit codes that check them, and the token limits name the contrast code that checks the rendered layers. This decision supersedes `decision/0117-compose-screens-from-checked-structure` for the rendered page: where the checked structure judged the wireframe, the audit opens each route in a browser and reports what the rendered page shows. It also supersedes `decision/0118-layer-surfaces-by-role`: the audit holds neighbouring fills apart where the token table did, with `surface-contrast-failed` and `surface-nesting-invalid`.

## Revisit Conditions

Reopen this when the audit cannot reach a product's routes, or when its findings duplicate route-suite assertions closely enough that one of the two stops earning its keep.
