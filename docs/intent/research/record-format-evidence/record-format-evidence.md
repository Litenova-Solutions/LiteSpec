# Record Format Evidence

A LiteSpec record is one file per page, and checks and people both read it. This research record asks which file format holds each fact once and validates against the record schema. It compares the candidate formats and measures the largest consumer's current pages, which are Markdown files with JSON frontmatter. A check reads the frontmatter and the tables, but not the prose. It matters because a format that holds each fact once keeps the checked data and the explanation apart.

## Findings

A strict subset of YAML holds each fact once and validates against the unchanged record schema. No format in the survey beats it. The closest alternative, KSON, is in beta and has no .NET implementation.

On 31 October 2026, the largest consumer's `.litespec/behavior` folder held 2,450 pages. Those pages carry 403 thousand lines of JSON frontmatter, against 32 thousand non-blank body lines. Every body uses fixed sections: Goal (1,273), Example (1,211), Risk (1,022), What Happens (984), Notes (476) and Screen Summary (380). The record gives no confidence for this measurement.

Aggregate transitions and data live only in body tables, with 164 and 179 tables. So a Markdown table is the source of truth for every aggregate state set. Checks read the frontmatter and the merged tables, but not the body prose. So Goal, Risk and What Happens can change without any check noticing. The record gives no confidence for this claim.

Behavior bodies hold 1,551 Markdown links to `.md` file paths. Research pages are articles. They have 181 distinct headings, 160 subsections and 85 tables. They cite 584 distinct URLs, and 486 of those URLs are missing from the dated `sources` list. The record gives no confidence for these measurements.

A rough converter puts YAML at 78 percent of the JSON lines and 81 percent of its characters. The gain is mainly structural, not a gain in size. The record gives no confidence for this claim.

The survey compared 18 formats: KDL, TOML, JSON5, JSONC, KSON, HUML, NestedText, MAML, TOON, Ziggy, RON, EDN, CUE, Pkl, Nickel, Dhall, Jsonnet and HCL. None beats a strict YAML subset. KSON is the closest by design, but it is in beta and has no .NET implementation. KSON is a JSON superset with JSON Schema support. So a later conversion from strict YAML to KSON would be mechanical. The record gives no confidence for this claim.

The reader is YamlDotNet. It accepts the YAML that YamlDotNet reads, except that it refuses an empty document. It also refuses a document that does not start at the first column, document markers, tabs in indentation, folded blocks and duplicate keys. Each refusal names the line where it sits. Scalars resolve by the YAML 1.2 core schema. Key order comes from the record schema, which validates the parsed YAML unchanged. The record gives no confidence for these claims.

A scalar that would resolve to a non-string, where a string is meant, is quoted. Multi-line text uses `|` block scalars. The record gives no confidence for this rule.

Anything that a check reads is a YAML field. Anything that only helps a person understand goes in the explanation file. Research and decision records require their explanation file. Prose cites records by identifier, and each research source carries a key that the article cites. The record gives no confidence for these rules.

## Source Notes

The record read 3 sources on 31 October 2026. The first is the YAML 1.2 specification, read for the core schema resolution rules behind the strict subset. The second is the JSON Schema site, read for the validation claim that the subset relies on, and for the KSON superset claim behind a later conversion. The third is the shipped record schema (`schemas/record.schema.yaml`), read as the validation target and for the key order.

The 18-format survey and the consumer counts are the researcher's own comparison and measurement. The record names no disagreement between sources. It names no claim that it could not confirm.
