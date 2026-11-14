# Every Hand-Written File in YAML

Maintainers edit the files that a LiteSpec project keeps, and reviewers read them before they merge. This decision says which syntax each file uses. Every file a person edits becomes YAML, which allows comments, and every file a program exchanges stays JSON.

## Context

The release manifest, the project file, the pattern catalog, the UI source lock and the schemas are JSON. Record pages are YAML, as `decision/0020-records-in-yaml` sets. A person edits 2 syntaxes in one project, and JSON gives the reviewers no comments and no place for a reason beside a value.

## Options

LiteSpec compared 3 options for the syntax of its hand-written files. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| YAML for every file a person edits, JSON for machine output | One syntax with comments for each reviewed file, and JSON stays for the files programs exchange. | The project file, manifest, catalog, lock and schemas convert in one change, and the site adds a YAML parser package. |
| YAML for the release manifest only | The file a maintainer writes most converts, and every other file keeps its shape. | A project keeps 2 syntaxes, and the project file and catalog stay in JSON. |
| JSON for every file | No conversion, no parser package and no change to the readers. | JSON cannot carry comments, so a reviewer has no place for a reason beside a value. |

## Decision

Every file a person edits is YAML, and every file a program exchanges stays JSON. The conversion covers the project file, the release manifest, the pattern catalog, the UI source lock, the presets, the schemas and the check-result schema.

Each converted file keeps its shape and states its `schemaVersion`. `decision/0020-records-in-yaml` holds the record files, and this decision extends the same rule to every hand-written file.

## Consequences

Each hand-written LiteSpec file states its `schemaVersion` and names its schema in a first-line `yaml-language-server` comment. Each file reads through `YamlText` into the tree that the evaluator already validates.

Keys that are not plain identifiers are double-quoted, and regular expressions are single-quoted. The site reads the manifest and the schemas with the `yaml` package that the owner approved. The shell scripts that run without Node read the 2 version keys with one `sed` line each.

Check results, the provision index, the book data, `--format json` output and the reader protocol stay JSON. The design contract takes `schemaVersion: 2`, because its `kind` and `shell` fields leave.

## Evidence

`research/record-format-evidence` holds the survey of 18 formats, which found no format that beats the strict YAML subset, with high confidence. `research/yaml-format-evidence` shows that an editor checks a YAML file against the schema that its first-line comment names, and that the language server validates draft 2020-12, which every LiteSpec schema declares. That is why each converted file names its schema.

The record reports, with high confidence, that each converted schema and the manifest were read through the YAML reader and compared with its JSON original, and that all 8 trees matched. The cited research does not record that comparison, so the claim rests on this record alone.

## Revisit Conditions

This decision reopens when a person-written file needs a construct that the YAML reader does not carry, or when a format appears that people write as readily as YAML and that has a stable .NET reader.

The YAML reader refuses the constructs that the record schema does not allow, such as duplicate keys and tabs in indentation. A file that needs one of those constructs is the case that would change the choice.
