# Records in YAML

LiteSpec stores each record as one YAML file, and the tools read that file to check the product. Anything a check reads is a field in the file, and anything that only helps a person understand goes in an explanation file beside it. Contributors who write records and agents that read them both depend on this format.

## Context

Each record page is a Markdown file with JSON frontmatter between `---` fences, followed by a Markdown body. That layout lets one fact live in three places: the frontmatter, a body table that the builder merges into the record, or a body section that a check reads by its heading.

The largest consumer shows the cost. Measured on 2026-10-31, `.litespec/behavior` holds 2,450 pages, with 403k lines of JSON frontmatter and 32k non-blank body lines. Aggregate transitions and data live only in body tables, and checks read the frontmatter and the merged tables, not the body prose. The Behavior bodies hold 1,551 links to `.md` file paths. The research articles cite 584 URLs, and 486 of those URLs are missing from the dated `sources` list. `research/record-format-evidence` holds the full counts.

## Options

LiteSpec compared 4 options for the format of its records. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Strict YAML records with explanation files beside them | Each fact lives once, and the tool owns the canonical layout. | Every record page is rewritten once, and the reader follows a strict YAML subset. |
| Markdown pages with JSON frontmatter | No page is rewritten, and no reader is written. | One fact lives in three places. |
| JSON records with explanation files beside them | Records stay in the notation the schema already validates. | Records stay verbose, and prose stays in quoted strings. |
| A newer format such as KSON or TOML | A closer match by design removes edge cases that YAML keeps. | The closest match, KSON, is in beta and has no .NET implementation. |

## Decision

LiteSpec writes each record as one YAML mapping in its own record file. An explanation file sits beside the record under the same name. The research and decision types require that explanation file.

Prose cites other records by identifier, and each research source carries a key that the articles cite. The tool writes every record file in canonical form.

## Consequences

Each record page becomes one YAML mapping per file, and the tool reads each file through a YAML reader. The record schema then validates the parsed YAML.

The reader refuses some YAML. It refuses an empty document, a document that does not start at the first column, document markers, tabs in indentation, folded blocks and duplicate keys. It reports the line of each refusal. Authors quote any value that would resolve to a non-string where a string is meant, and they write multi-line text with `|` block scalars.

## Evidence

`research/record-format-evidence` holds the consumer counts and the survey of 18 formats behind this choice, with high confidence. The survey found no format that beats the strict YAML subset, and it found that the subset validates against the unchanged record schema.

The closest format, KSON, is in beta and has no .NET implementation. The counts were measured on 2026-10-31 over the largest consumer, and the survey is the researcher's own comparison.

## Revisit Conditions

This decision reopens when a record format appears that the strict YAML subset cannot carry, or when a surveyed format ships a stable .NET implementation.

A format that stays in beta does not reopen the decision by itself. Until one of the two conditions holds, the strict YAML subset stays.
