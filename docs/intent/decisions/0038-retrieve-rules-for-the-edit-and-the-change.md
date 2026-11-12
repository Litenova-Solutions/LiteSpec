# Retrieve Rules for the Edit and the Change

## Context

An agent that asked what governs one command handler received 137 provisions, 65 of them needing judgment: query-only and upload provisions, provisions about issuing document numbers and erasing personal data, and about 35 provisions about how the record file is written. A provision could not be looked up by its ID. An agent that asked what governs a class following a provider decision received 11 lines, all about writing decision records, and nothing of the choice itself.

## Decision

`rules` answers for the file being edited and for the change set, and only with what applies. On a code path, the provisions about a marked record collapse to one line per record, and conditional provisions return only when the marked record meets their `when` condition. `rules --since` prints the report skeleton the completion report answers, and `rules` on one provision or page ID prints that provision or list from the carried index. A `Decided` group prints first on a code path, so the choice behind the code stands beside the code that follows it, and the stage and audit adoption provisions apply to the project and the named adoption decision instead of to every decision.
