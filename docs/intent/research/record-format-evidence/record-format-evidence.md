# Record Format Evidence

A LiteSpec record is one file per page, and checks and people both read it. This research record asks which file format holds each fact once and validates against the record schema. It compares the candidate formats and measures the largest consumer's pages, which are Markdown files with JSON frontmatter. A check reads the frontmatter and the tables, but not the prose. It matters because a format that holds each fact once keeps the checked data and the explanation apart.

## Findings

A strict subset of YAML holds each fact once and validates against the unchanged [record schema][record-schema]. No format in the survey beats it. The closest alternative, KSON, is in beta and has no .NET implementation [JSON Schema site][json-schema].

On 31 October 2026, the largest consumer `.litespec/behavior` folder held 2,450 pages [record schema][record-schema]. Those pages carry 403 thousand lines of JSON frontmatter, against 32 thousand non-blank body lines. Every body uses fixed sections. `Goal` appears 1,273 times, `Example` 1,211 times, and `Risk` 1,022 times. `What Happens` appears 984 times, `Notes` 476 times, and `Screen Summary` 380 times.

Body tables alone hold aggregate transitions and data, in 164 and 179 tables [record schema][record-schema]. The record infers that a Markdown table is the source of truth for every aggregate state set. Checks read the frontmatter and the merged tables, but not the body prose. The record infers that `Goal`, `Risk`, and `What Happens` can change without any check noticing.

Behavior bodies hold 1,551 Markdown links to `.md` file paths [record schema][record-schema]. Research pages are articles with 181 distinct headings, 160 subsections, and 85 tables. They cite 584 distinct URLs, and 486 of those URLs are missing from the dated `sources` list.

A rough converter puts YAML at 78 percent of the JSON lines and 81 percent of its characters [record schema][record-schema]. The gain is mainly structural, not a gain in size.

The survey compared 18 formats [record schema][record-schema]. They are KDL, TOML, JSON5, JSONC, KSON, HUML, NestedText, MAML, and TOON. They also include Ziggy, RON, EDN, CUE, Pkl, Nickel, Dhall, Jsonnet, and HCL. None beats a strict YAML subset. KSON is the closest by design, but it is in beta and has no .NET implementation [JSON Schema site][json-schema]. KSON is a JSON superset with JSON Schema support. The record infers that a later conversion from strict YAML to KSON would be mechanical.

The reader is YamlDotNet [YAML 1.2 specification][yaml-spec-1-2]. It accepts the YAML that YamlDotNet reads, except that it refuses an empty document. It also refuses a document that does not start at the first column, document markers, tabs in indentation, folded blocks, and duplicate keys. Each refusal names the line where it sits. Scalars resolve by the YAML 1.2 core schema. Key order comes from the [record schema][record-schema], which validates the parsed YAML unchanged.

A scalar that would resolve to a non-string, where a string is meant, is quoted [YAML 1.2 specification][yaml-spec-1-2]. Multi-line text uses `|` block scalars.

The record infers that anything a check reads is a YAML field [record schema][record-schema]. The record infers that anything only helping a person understand goes in the explanation file. The record infers that research and decision records require their explanation file. The record infers that prose cites records by identifier, and each research source carries a key that the article cites.

## Source Notes

The record read the YAML 1.2 specification for the core schema resolution rules behind the strict subset [YAML 1.2 specification][yaml-spec-1-2].

The record read the JSON Schema site for the validation claim that the subset relies on, and for the KSON superset claim that makes a later conversion mechanical [JSON Schema site][json-schema].

The record read the shipped [record schema][record-schema] as the validation target and for the key order. The survey comparison and the consumer counts are the researcher's own measurement.

[yaml-spec-1-2]: https://yaml.org/spec/1.2/ "Read on 2026-10-31"
[json-schema]: https://json-schema.org/ "Read on 2026-10-31"
[record-schema]: ../../../../schemas/record.schema.yaml "Read on 2026-10-31"
