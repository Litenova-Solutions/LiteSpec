---
{
  "type": "decision",
  "id": "decision/0018-one-record-language",
  "title": "One Record Language",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "One language page; authoring keeps page prose",
      "pros": "Each language rule lives once, every layer cites one page, and LiteSpec's own prose rules stay with the pages they govern.",
      "cons": "The field provision moves pages, and every citation of it moves in the same change."
    },
    {
      "name": "Keep the grammar on the model page",
      "pros": "No page moves and no citation sweep is needed.",
      "cons": "The model page owns structure, not language, and a grammar serving every layer does not belong to one."
    },
    {
      "name": "One page per layer",
      "pros": "Each layer reads its language without leaving its pages.",
      "cons": "One grammar restated four times drifts into four grammars."
    },
    {
      "name": "Fold page prose into the language page",
      "pros": "One page would hold every writing rule.",
      "cons": "LiteSpec's own prose rules differ from project record language, so one page would speak to two readers at once."
    }
  ],
  "choice": "One language page; authoring keeps page prose",
  "reopenedWhen": "A record layer appears whose language the page cannot state.",
  "basedOn": [
    "research/naming-grammar-evidence"
  ]
}
---

# One Record Language

## Context

The field grammar, value style, identifier names, finding codes and tool names are scattered across the model, authoring and intent pages, so one rule lives in several places and no page answers what language a record speaks.

## Decision

One language standard for every layer lives on a new core page, `docs/core/language.md` ("Record Language"), holding the field grammar, the value style, identifier names, finding codes and messages, and tool names as provisions. `docs/core/authoring.md` keeps the prose of LiteSpec's own pages and links to it; each rule lives on one of the two pages, never both. `decision/0011-one-grammar-for-names-and-values` holds the grammar choice itself; this decision places it.

## Consequences

`litespec/core/language#name-each-field-by-its-word-class`, `litespec/core/language#write-each-value-in-the-value-style`, `litespec/core/language#name-each-record-by-its-types-name-form`, `litespec/core/language#name-each-finding-code-by-subject-and-problem`, `litespec/core/language#write-each-finding-message-as-a-fact` and `litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set` hold the standard, and `litespec/core/authoring#use-one-term-for-one-concept` is one prose rule authoring keeps.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the field inventory and the word classes this page standardizes, read from the schemas and measured over the largest consumer, high confidence.
