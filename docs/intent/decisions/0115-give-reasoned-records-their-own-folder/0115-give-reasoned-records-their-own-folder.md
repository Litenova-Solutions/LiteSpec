# Give Reasoned Records Their Own Folder

Each rule, setting and domain event lives in its own folder with its own explanation file. Closed lists whose entries carry no reasoning of their own, such as permissions, refusal codes, results, scenarios and cast entries, stay as entries.

## Context

Rules sat together in rule-set files, settings in setting-set files, and domain events as entries of their aggregate file. One explanation file covered every rule of a set. A rule's reasons, sources and examples sat in a long shared page that drifted from the rules it described, and many rules had no section at all.

A consumer's set grew past two thousand lines. A change to one rule rewrote the whole set, and splitting rules by condition made the sets grow further. Requirements had already left their feature file for the same reasons, in `decision/0045-keep-each-requirement-in-its-own-folder`.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep rules, settings and domain events in sets and aggregate files | One file shows all the rules of an aggregate, and nothing moves. | A rule has no explanation of its own, a set grows with every rule, and each new field lengthens every set. |
| Give every record type its own folder | One rule for every type. | A refusal code or a permission carries no reasoning, so a folder per entry adds files without information. |
| Give each record with reasoning of its own its own folder, and keep closed lists as entries | Each rule, setting and domain event has one file, one explanation and its own history, and a new field changes one file. | Every consumer moves these records into folders, and a hand edit opens more files. |

## Decision

A rule lives at `rules/<name>/<name>.yaml` under its aggregate, its module or `behavior/`. A setting lives at `settings/<name>/<name>.yaml` under its module, and a domain event at `domain-events/<name>/<name>.yaml` under its aggregate. The `rule-set` and `setting-set` page styles are removed, and an aggregate file holds no domain events.

A rule's explanation holds Rationale, then an optional Example and Limits. A setting's holds Defaults and an optional Changes, and a domain event's holds an optional Example.

## Consequences

`litespec new rule`, `litespec new setting` and `litespec new domain-event` write the folder, and `litespec new entry` takes permissions only. The provision on module rules becomes `litespec/core/model#hold-module-rules-in-rule-folders`. Each consumer moves its records under the changelog's upgrade step.

## Revisit Conditions

Reopen this when consumers report that many small rule files cost more than the separate explanations and history gain, or that readers need a combined view of an aggregate's rules the book does not give them.
