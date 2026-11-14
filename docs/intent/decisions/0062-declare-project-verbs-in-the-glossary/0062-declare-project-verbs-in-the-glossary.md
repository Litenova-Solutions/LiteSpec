# Declare Project Verbs in the Glossary

A use-case name starts with a verb, and LiteSpec ships the baseline vocabulary the verb table lists. This decision puts each project verb beyond that vocabulary in the glossary as a verb term, so the model check reads every verb a use case or permission opens with.

## Context

A project added its verbs beyond the baseline in a Markdown convention page, which no tool reads. The use-cases page admitted that no tool checks the verb, so an undeclared verb passed every check.

The permission check could not accept project verbs either: the list was prose, so a permission opening with a project verb was reported even where the project had declared it.

A verb list is data, and the glossary already holds the product words as data.

## Options

LiteSpec compared 3 options for where project verbs live. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Glossary verb entries | One home for product language, and the check reads the verbs it declares. | Each project verb moves from its convention page into the glossary. |
| A separate typed verb list file | The verbs are checked data without touching the glossary. | Product words split across two files. |
| Keep the Markdown convention | No record moves and no check is written. | The verb list stays prose no tool reads. |

## Decision

A project declares each use-case verb beyond the baseline vocabulary as a verb term in its glossary. The term names the verb, `verbKind` names the kind it opens, and `meaning` states the act in one sentence.

## Consequences

The model check reports `use-case-verb-unknown` for a use-case name opening with a verb neither list declares, `use-case-verb-invalid` for a use case opening with a verb of the other kind, and `term-verb-duplicate` for a verb term restating a baseline verb. The permission check accepts a glossary verb of kind `command` or `either`.

A convention states a judgment rule, never a list of words. Each project verb moves from its convention page into the glossary as a verb term, and the convention page is deleted.

## Revisit Conditions

This decision holds until a verb needs a shape the glossary term cannot carry. It also holds until a project needs verbs no check should read. Either report reopens the choice.
