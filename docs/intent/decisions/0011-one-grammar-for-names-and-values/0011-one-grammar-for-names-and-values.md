# One Grammar for Names and Values

LiteSpec records hold fields, and each field has a name and a value. This decision sets one grammar for both. It matters because a checker can read a value directly once its format is fixed, and a name always means one concept.

## Context

One field name carries two concepts, and one concept carries two names, across the record types. Values arrive as free text, and several small notations of their own. Checks must parse that text instead of reading the values.

## Options

LiteSpec compares 4 options for the grammar.

| Option | Strengths | Drawbacks |
|---|---|---|
| State the grammar and rename every field | The collisions already produce invalid and misleading data, and the validator checks the mechanical half on every run | Every renamed field changes the schema, templates, provisions, checks and consumer records in one change |
| Define without renaming | No record migrates, and no citation sweep is needed | Definitions guide readers but not search or tools, and invalid values keep the colliding names |
| Rename fields but skip the value style | A smaller change, and values keep their freedom | Free-text values keep their notations, so checks still parse prose |
| Adopt external names wholesale | One outside standard would settle every naming dispute | No outside standard covers record fields; external names survive only where a standard fixes them, such as Gherkin and CloudEvents |

The chosen option renames every field and states the grammar. Each of the other options leaves the collisions or the free-text values in place.

## Decision

LiteSpec names every field with one grammar of 8 word classes:

- Fixed identity words.
- Active present-tense verbs for links to many records.
- Singular role nouns for links to one record.
- `<participle>By` where the target acts on the record.
- Closed-set nouns with `kind` for the record's own subtype.
- Prose nouns for text.
- `<condition>When` for named conditions.
- `<participle>Because` for reasons.

The grammar has no exemption list. Only names that an outside standard fixes are exempt: Gherkin's `given`, `when` and `then`, and the CloudEvents attributes. The verb `cites` is the one verb for a reason link everywhere.

A boolean is a present-tense predicate that is `true` when it holds about the record, with no `is` or `has` prefix. String mini-languages become objects, and spelling follows American English. A use case names the settings it reads in `reads`, as decided in `decision/0012-use-cases-read-their-settings`. The word `when` keeps only its Gherkin meaning. The value style is a set of provisions, and its mechanical half is checked.

## Consequences

The provision `litespec/core/language#name-each-field-by-its-word-class` states the grammar. Five more provisions on the language page cover booleans, dates, the value style, American spelling, and the names that an outside standard fixes.

## Evidence

`research/naming-grammar-evidence` holds the 248-field inventory, the collisions and the word classes behind this decision, `decision/0012-use-cases-read-their-settings` and `decision/0018-one-record-language`. It is read from the schemas and measured over the largest consumer, with high confidence.

## Revisit Conditions

This decision reopens when a new field appears that no word class can name.
