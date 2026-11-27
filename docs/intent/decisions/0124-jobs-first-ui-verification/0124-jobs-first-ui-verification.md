# Organize UI Around Jobs and Visual Verification

Each screen does one job, takes one of six known shapes, and proves itself in a browser. Paper structure alone never settled whether a page works.

## Context

Decision 0028 limited each pattern to a Default and two options. Decision 0117 stated composition as checked structure over ASCII wireframes.

Both judged structure on paper. Neither opened a route in a browser, and the listed screen faults kept shipping: pages doing several jobs, cards inside cards, and tables clipping at narrow widths.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Organize screens around jobs with locked archetypes and rendered-page proof | Each screen serves one job with a known shape. The audit opens every route and reports what the rendered page shows. | Six archetypes to keep, and a browser audit to run per product. |
| Keep the pattern catalog with three options per pattern | Owners keep a choice per pattern, and no new tooling is needed. | Guidance the agent never meets on a route file cannot shape the screen it builds. |
| Keep ASCII wireframes as the checked blueprint | Wireframes stay cheap text files with a structure check. | Twelve thousand lines of ASCII describe structure no reader can see, and the rendered page still goes unopened. |

## Decision

Screens organize around jobs with six locked page archetypes: `HomePage`, `ListPage`, `WorkPage`, `RecordPage`, `SettingsPage`, and `FormPage`.

Records name the `job` and the `archetype` for each screen. A Claimwick expense list shows the shape: a `ListPage` for the job of scanning reports, with each row opening one `RecordPage`.

## Consequences

The command `litespec ui audit` opens each route at phone, laptop, and wide widths and reports the rendered-page codes. Geometry and multimodal review confirm spacing and grouping from screenshots.

This decision supersedes `decision/0028-one-default-and-two-pattern-options` for screen choice and `decision/0117-compose-screens-from-checked-structure` for paper blueprints: where they judged structure on paper, the audit and the review judge the rendered page.

## Revisit Conditions

Reopen this when a product job fits none of the six shapes with evidence, or when rendered-page proof duplicates route-suite assertions closely enough that one of the two stops earning its keep.
