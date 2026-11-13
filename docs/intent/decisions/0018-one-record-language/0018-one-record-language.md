# One Record Language

LiteSpec's record language is the set of rules that every record and every finding follows. The rules cover how fields are named, how values are written, how identifiers and finding codes are named, and how commands are named. This decision sets where those rules live, so that each layer cites one page instead of restating the rules.

## Context

The field grammar, the value style, identifier names, finding codes and tool names are spread across the model, authoring and intent pages. One rule lives in several places. No page answers what language a record speaks.

## Options

LiteSpec compares 4 places for the record language.

| Option | Strengths | Drawbacks |
|---|---|---|
| One language page; authoring keeps page prose | Each language rule lives once, every layer cites one page, and LiteSpec's own prose rules stay with the pages they govern | The field provision moves pages, and every citation of it moves in the same change |
| Keep the grammar on the model page | No page moves, and no citation sweep is needed | The model page owns structure, not language, and a grammar for every layer does not belong to one page |
| One page per layer | Each layer reads its language without leaving its pages | One grammar restated 4 times drifts into 4 grammars |
| Fold page prose into the language page | One page would hold every writing rule | LiteSpec's own prose rules differ from record language, so one page would speak to two readers at once |

The chosen option gives the language its own page. LiteSpec's prose rules stay on the authoring page, because they speak to a different reader.

## Decision

One language standard for every layer lives on a new core page, `docs/core/language.md`, titled Record Language. The page holds the field grammar, the value style, identifier names, finding codes and messages, and tool names, each as a provision.

`docs/core/authoring.md` keeps the prose rules for LiteSpec's own pages and links to the language page. Each rule lives on one of the two pages, never on both. `decision/0011-one-grammar-for-names-and-values` holds the grammar choice itself, and this decision places it.

## Consequences

Six provisions on the language page hold the standard. They cover fields, values, record names, finding codes, finding messages and commands. The provision `litespec/core/authoring#use-one-term-for-one-concept` is the one prose rule that authoring keeps.

## Evidence

`research/naming-grammar-evidence` holds the field inventory and the word classes that this page standardizes. It is read from the schemas and measured over the largest consumer, with high confidence.

## Revisit Conditions

This decision reopens when a record layer appears whose language the page cannot state.
