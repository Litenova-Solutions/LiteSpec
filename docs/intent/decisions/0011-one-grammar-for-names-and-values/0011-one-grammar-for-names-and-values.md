# One Grammar for Names and Values

## Context

One field name carries two concepts, and one concept carries two names, across the record types, while values arrive as free text with colliding mini-languages that checks must parse instead of read.

## Decision

One naming grammar of eight word classes names every field: fixed identity words; active present-tense verbs for links to many records; singular role nouns for links to one record; `<participle>By` where the target acts on the record; closed-set nouns with `kind` for the record's own subtype; prose nouns for text; `<condition>When` for named conditions; and `<participle>Because` for reasons. The grammar carries no exemption list: only names an outside standard fixes are exempt, Gherkin `given`, `when` and `then` with the CloudEvents attributes. `cites` is the one verb for a reason link everywhere. A boolean is a present-tense predicate that is true about the record when `true`, with no `is` or `has` prefix. String mini-languages become objects. Spelling is American. A use case names the settings it reads in `reads`, decided in `decision/0012-use-cases-read-their-settings`. `when` keeps only its Gherkin meaning, and the value style is a set of provisions with its mechanical half checked.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the 248-field inventory, the collisions and the word classes behind `decision/0011-one-grammar-for-names-and-values`, `decision/0012-use-cases-read-their-settings` and `decision/0018-one-record-language`, read from the schemas and measured over the largest consumer, high confidence.
