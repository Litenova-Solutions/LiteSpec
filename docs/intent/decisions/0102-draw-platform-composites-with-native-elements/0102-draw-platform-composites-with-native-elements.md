# Draw Platform Composites with Native Elements

The shared package installs the pinned component baseline once, and the web platform provides its own dialog, popover, and details elements. This decision draws those platform composites with native elements and takes every other composite from the baseline.

## Context

The shared package held every composite, and the pinned baseline was its source. The web platform provides a dialog element with focus handling, dismissal, and layering, a popover element with anchoring and light dismissal, and a details element with disclosure. Nothing said whether a dialog composite builds on the platform dialog or on the baseline dialog, so one team left the baseline unused while following platform guidance, and the next shipped baseline dialogs built on generic elements that the same guidance refuses. Each team answered the question its own way, and the answers disagreed on the same page.

## Options

LiteSpec compared 3 options for where composites come from. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Draw platform composites with native elements and take the rest from the baseline | Each composite comes from the source that owns its behavior: the platform dialog, popover, and details carry focus handling, dismissal, anchoring, and disclosure, while the baseline covers what the platform lacks, such as a combobox and a date picker. | The shared package holds two sourcing rules instead of one, and each native element must meet the declared browser support tier or carry a fallback beside it. |
| Take every composite from the pinned baseline | One source holds every composite, and no team judges a platform element against the browser support tier. | Dialogs, popovers, and disclosures rebuild platform behavior on generic elements, each worse than the element it copies, and the product carries two implementations of one behavior. |
| Draw every composite with native elements | No baseline to install, pin, or keep current. | The platform has no combobox, date picker, or data table, so each team builds the missing composites by hand and every product ships its own set. |

## Decision

A dialog, popover, or details composite builds on its native element. Every other composite comes from the pinned baseline, or from a justified behavior package rendering through baseline tokens.

Each native element meets the declared browser support tier, or carries a fallback beside it where it falls outside that tier. Icons stay per-icon imports under the existing per-route download rule, which already refuses whole icon sets.

## Consequences

A dialog, popover, or details composite builds on its native element, and every other composite comes from the pinned baseline or a justified behavior package rendering through baseline tokens. Each native element meets the declared browser support tier or carries a fallback beside it. Icons stay per-icon imports under the existing per-route download rule.

## Revisit Conditions

This decision reopens when the platform gains a composite the baseline owns today, or when a native element leaves the declared browser support tier with no workable fallback.

Either case means the split no longer follows what each source owns. The team then decides again which composites stay native.
