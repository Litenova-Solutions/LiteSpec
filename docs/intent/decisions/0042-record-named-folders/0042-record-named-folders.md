# Keep Each Named Record in Its Own Folder

## Context

A named record used to sit in a flat file beside every other record of its type, with its explanation file beside it under the same name. A consumer reading one record opened a folder full of unrelated files, and a check that paired a record with its explanation had to match two names inside one shared folder. A record that needed a second file had no folder to join, so the only way to add one was another flat file with a related name.

Three options were weighed. Flat files keep every record of a type in one list, but a reader finds a pair only by its name, and a tool cannot tell which files belong to one record without matching names. One folder per record type, with each record as a pair of files named by a scheme, keeps the folder count low, but the pair shares its folder with every other record of the type, and a second file for one record has no folder to join. One folder per named record puts the record file and its explanation in a folder named for the record. The pair is easy to find, and the folder holds the record, its explanation and nothing else. Its cost is one more folder and path segment for each record, and a check that matches each folder name to its file stem.

## Decision

LiteSpec places each named record in its own folder, `<container>/<name>/<name>.yaml`, with its explanation file `<name>.md` beside it in that folder. The folder name equals the file stem, and a named record whose folder name differs from its file stem is a `record-folder-drift` finding. A named record placed flat is a `record-placement-invalid` finding. Fixed-name files such as `product.yaml` and `aggregate.yaml`, list files, and child records that sit in their parent's file keep the places the placement table gives. `litespec new` writes the folder layout, and `litespec rename` moves a record with its folder. No migration tool and no compatibility path ship with this release, so each consumer moves its own records.
