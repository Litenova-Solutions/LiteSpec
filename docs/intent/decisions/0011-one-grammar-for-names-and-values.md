---
{
  "type": "decision",
  "id": "decision/0011-one-grammar-for-names-and-values",
  "title": "One Grammar for Names and Values",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "State the grammar and rename every field",
      "pros": "The collisions already produce invalid and misleading data, and a stated grammar lets the validator check the mechanical half on every run.",
      "cons": "Every renamed field moves schema, templates, provisions, checks, and consumer records in one change."
    },
    {
      "name": "Define without renaming",
      "pros": "No record migrates and no citation sweep is needed.",
      "cons": "Definitions fix readers but not search or tooling, and the invalid values keep arriving under the colliding names."
    },
    {
      "name": "Rename fields but skip the value style",
      "pros": "The change stays smaller and values keep their current freedom.",
      "cons": "Free-text values keep their mini-languages, so checks still parse prose instead of reading objects."
    },
    {
      "name": "Adopt external names wholesale",
      "pros": "One outside standard would settle every naming dispute at once.",
      "cons": "No outside standard covers record fields, and external names are kept only where a standard fixes them, such as Gherkin and CloudEvents."
    }
  ],
  "choice": "State the grammar and rename every field",
  "reopenedWhen": "A new field appears that no word class can name.",
  "basedOn": [
    "research/naming-grammar-evidence"
  ],
  "replaces": "decision/0006-one-field-naming-grammar"
}
---

# One Grammar for Names and Values

## Context

One field name carries two concepts, and one concept carries two names, across the record types, while values arrive as free text with colliding mini-languages that checks must parse instead of read.

## Decision

One naming grammar of eight word classes names every field: fixed identity words; active present-tense verbs for links to many records; singular role nouns for links to one record; `<participle>By` where the target acts on the record; closed-set nouns with `kind` for the record's own subtype; prose nouns for text; `<condition>When` for named conditions; and `<participle>Because` for reasons. The grammar carries no exceptions list: only names an outside standard fixes are exempt, Gherkin `given`, `when` and `then` with the CloudEvents attributes. `cites` is the one verb for a reason link everywhere. A boolean is a present-tense predicate that is true about the record when `true`, with no `is` or `has` prefix. String mini-languages become objects. Spelling is American. `ballot` stays with its glossary meaning. A use case names the settings it reads in `reads`, decided in `decision/0012-use-cases-read-their-settings`. Questions carry the one lifecycle shape, `when` keeps only its Gherkin meaning, and the value style is a set of provisions with its mechanical half checked.

## Consequences

`litespec/core/language#name-each-field-by-its-word-class` states the grammar, with `litespec/core/language#name-a-boolean-as-a-predicate`, `litespec/core/language#name-each-date-by-what-it-marks`, `litespec/core/language#write-each-value-in-the-value-style`, `litespec/core/language#write-in-american-english` and `litespec/core/language#keep-names-an-outside-standard-fixes` beside it. `litespec/core/model#answer-each-question-on-its-record` carries the question lifecycle. The model.md field provision gives way to the language page in this release.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the 248-field inventory, the collisions and the word classes behind D9 to D18, read from the schemas and measured over the largest consumer, high confidence.
