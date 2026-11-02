# Records in YAML

## Context

A record page kept as a Markdown file with JSON frontmatter between `---` fences and a Markdown body lets one fact live in three places: the frontmatter, a body table the builder merges into the record, or a body section a check reads by heading. Measured over the largest consumer on 2026-10-31, `.litespec/behavior` holds 2,450 pages with 403k lines of JSON frontmatter against 32k non-blank body lines; aggregate transitions and data live only as body tables, the approval hash covers the frontmatter and the merged tables but not the body prose, Behavior bodies hold 1,551 links to `.md` file paths, and research articles cite 584 URLs with 486 missing from the dated `sources` list. `research/record-format-evidence` holds the full counts and the format survey.

## Decision

Each record page is one YAML mapping in a record file, with an optional explanation file beside it under the same name; anything a check reads or a reviewer approves is a YAML field, and anything that only helps someone understand goes in the explanation file. Article types require their explanation file inside the approval hash, prose cites records by identifier, research sources carry keys, and the tool writes every record file in canonical form.

## Evidence with Confidence

`research/record-format-evidence` holds the consumer counts and the eighteen-format survey behind this choice, high confidence. The counts were measured over the largest consumer on 2026-10-31; the survey found no format beating the strict YAML subset, with the closest match in beta and without a .NET implementation.
