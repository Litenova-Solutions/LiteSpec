# Narrow Rules Output per File and Change Set

`litespec rules` prints the provisions, the numbered rules of the standard, that govern a file an agent is about to edit. This decision makes the output show only what applies to that file and to the whole change. An agent can then judge each provision that needs judgment and report it. The full listing that it replaces was too long to read or answer.

## Context

An agent that asks what governs one command handler receives 137 provisions, and 65 of them need judgment. They include query-only and upload provisions, provisions about issuing document numbers and erasing personal data, and about 35 provisions on how the record file is written. A provision cannot be looked up by its ID.

An agent that asks what governs a class that follows a provider decision receives 11 lines. All 11 are about writing decision records, and none is about the choice itself.

## Options

LiteSpec compared 3 options for the rules output. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Answer per file and per change set, with collapsed records, a skeleton and lookup by ID | An agent sees only the judgment provisions that its file and change set need, and it reads the decision behind the code. | `rules` gains modes and a condition vocabulary, and each provision author must apply it. |
| Keep the full output and rank it by relevance | One output mode, and nothing new to learn. | 137 provisions stay unread in any order, and the completion report still cannot name what was judged. |
| One command per target: code, records and change sets | Each output stays small. | 3 commands to learn and keep, and the agent runs the wrong one for the edit it makes. |

## Decision

`rules` answers for the file being edited and for the change set, and it prints only what applies. On a code path, the provisions about a marked record collapse to one line per record. A conditional provision returns only when the marked record meets its `when` condition.

`rules --since` prints the report skeleton that the completion report answers. `rules` with one provision or page ID prints that provision or list from the carried index.

A `Decided` group prints first on a code path. The choice behind the code appears beside the code. The stage provision and the audit adoption provision apply to the project and to the named adoption decision, not to every decision.

## Consequences

On a code path, provisions that match only through a marked record print as one line per record. A record path or a record ID still prints them in full.

`rules --since <ref>` reads the files changed since the revision. It prints a report skeleton with one `How judged:` line for each judgment provision. `rules <provision-id>` prints one provision in full, and `rules <page-id>` prints the page list. Both read the carried provisions file.

A provision states its condition as a `when` key on its `Applies to` line. The keys are `query`, `command`, `upload`, `document-number`, `erasure`, `project` and `audit-adoption`. A code path returns a conditional provision only when the marked record meets its condition.

`litespec/core/agent#load-applicable-provisions-before-editing` runs `rules <path>` before each edit, and it answers each skeleton provision before reporting done. The `Decided` group prints each followed decision's `choice` and `reopenedWhen`, and each covering departure's `reason` and `expiresOn`. The site serves the provision index as JSON, and the published URLs page states the ID-to-URL rule.

## Revisit Conditions

This decision reopens when a code path again returns provisions that its file cannot judge, or when an agent cannot answer the change-set skeleton from the output alone.

Either case means the output stops matching what an agent can judge. The narrowed output then fails its purpose.
