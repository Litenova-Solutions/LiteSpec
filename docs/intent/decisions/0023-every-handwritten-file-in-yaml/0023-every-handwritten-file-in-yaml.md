# Every Hand-Written File in YAML

## Context

The release manifest, the project file, the pattern catalog, the UI source lock and the schemas were JSON, while record pages were YAML since `decision/0020-records-in-yaml`. A person edited two syntaxes in one project, and JSON gave the people who review these files no comments and no place for a reason beside a value.

## Decision

Every file a person edits is YAML, and every file a program exchanges stays JSON. The conversion covers the project file, the release manifest, the pattern catalog, the UI source lock, the presets, the schemas and the check-result schema. Each converted file keeps its shape and states its `schemaVersion`. `decision/0020-records-in-yaml` holds the record files, and this decision extends the same rule to every hand-written file.

## Evidence with Confidence

`research/record-format-evidence` holds the survey of eighteen formats, which found no format beating the strict YAML subset, with high confidence. `research/yaml-format-evidence` shows that an editor checks a YAML file against the schema its first-line comment names, which is how each converted file names its schema. Each converted schema and the manifest were read through the YAML reader and compared with the JSON original, and all eight trees matched, with high confidence.
