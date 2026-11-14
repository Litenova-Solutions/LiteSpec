# Keep Each Named Record in Its Own Folder

LiteSpec stores each record as a file. A named record, such as a use case or a rule set, is a record with its own name, as opposed to a fixed-name file such as `product.yaml`. This decision places each named record in a folder of its own, with its record file and its explanation file inside that folder. A reader opens one folder to read a whole record, and a tool finds the pair from the folder name.

## Context

Each named record sits in a flat file beside every other record of its type. Its explanation file sits beside it under the same name. A reader of one record opens a folder full of unrelated files.

A check that pairs a record with its explanation must match two names inside one shared folder. A record that needs a second file has no folder to join, so the only way to add one is another flat file with a related name.

## Options

LiteSpec compared 3 layouts for named records. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One flat file per named record, with its explanation beside it in the type's folder | Every record of a type sits in one list, and a new record needs no folder. | A reader finds a pair only by its name, and a tool cannot tell a record's own files from another record's files. |
| One folder per named record, with the record file and its explanation inside | A reader opens one folder to read a whole record, and a tool finds the pair from the folder name. | Each record adds a folder and one more path level, and a check matches each folder name to its file stem. |
| One folder per record type, with each record as a named pair of files | One folder per type holds every record of the type, and no record adds a folder. | The pair shares its folder with every other record of the type, and a second file for a record has no folder to join. |

## Decision

LiteSpec places each named record in its own folder, `<container>/<name>/<name>.yaml`. Its explanation file `<name>.md` sits beside it in that folder. The folder name equals the file stem.

A named record whose folder name differs from its file stem is a `record-folder-drift` finding. A named record placed flat is a `record-placement-invalid` finding.

Fixed-name files, such as `product.yaml` and `aggregate.yaml`, keep the places that the placement table gives. So do list files, and child records that sit in their parent's file.

## Consequences

`litespec new` writes the folder layout. `litespec rename` moves a record together with its folder.

`litespec/core/model#explain-each-record-beside-its-record-file` and the placement table state the layout.

No migration tool and no compatibility path ship with this release. Each consumer moves its own records.

## Revisit Conditions

This decision reopens when a named record needs more than its record file and one explanation file, such as a second explanation beside the same record, or a record split across files.

A second explanation or a split record would need a place that the one-folder layout does not give. The layout would then need a second level or a different rule.
