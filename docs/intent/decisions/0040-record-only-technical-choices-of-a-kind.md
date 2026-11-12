# Record Only Technical Choices of a Kind

## Context

A review of one large consumer kept 45 of 74 decision records: 16 restated a provision that already made the choice, 6 described the inside of one class, 6 folded into the decision that absorbs them, and 1 was a business choice that belongs in a requirement. The same consumer kept 32 provision-shaped conventions outside `.litespec/governance/conventions/`, so `rules` returned none of them. The book listed decisions as one flat list, with no index by kind or concern.

## Decision

A technical decision records only an expensive-to-reverse choice of one of six kinds: provider, package, format, boundary, topology, or replacement. A choice of no kind states its reason in the class documentation, and a standing rule every change follows is a convention under `.litespec/governance/conventions/`, citable from a `follows` mark. Categories live in fields and generated indexes: the concern is computed from the followers, `model render` writes the decision and departure indexes, and the book groups decisions by kind, then by concern. Claimwick renders each receipt as a PDF file, so `decision/0005-render-receipts-as-pdf` carries `kind: package`.
