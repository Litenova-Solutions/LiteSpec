# One Record Language

## Context

The field grammar, value style, identifier names, finding codes and tool names are scattered across the model, authoring and intent pages, so one rule lives in several places and no page answers what language a record speaks.

## Decision

One language standard for every layer lives on a new core page, `docs/core/language.md` ("Record Language"), holding the field grammar, the value style, identifier names, finding codes and messages, and tool names as provisions. `docs/core/authoring.md` keeps the prose of LiteSpec's own pages and links to it; each rule lives on one of the two pages, never both. `decision/0011-one-grammar-for-names-and-values` holds the grammar choice itself; this decision places it.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the field inventory and the word classes this page standardizes, read from the schemas and measured over the largest consumer, high confidence.
