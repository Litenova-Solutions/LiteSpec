# Keep Each Requirement in Its Own Folder

A requirement states one commitment that Behavior fulfills. Until now, every requirement was an entry in its feature's record file. This decision gives each requirement its own folder inside its feature's folder, with an optional explanation file, so each commitment has one authoritative file that can carry the context its statement leaves out.

## Context

A feature's record file held the feature and every requirement under it. A requirement had a statement and a one-line rationale, and nowhere to explain a condition, a term or an edge case that the statement leaves out. That detail ended up in the feature's explanation, mixed with the other requirements, or it was lost.

A change to one requirement rewrote the whole feature file, so the history of one commitment was spread across the history of the feature. A feature with many requirements grew into one long file that a reader scrolled to find one commitment.

A use case already sits in its own folder inside its aggregate's folder, and the folder names the parent. A requirement can follow the same pattern.

## Options

LiteSpec compared 3 options for where a requirement lives. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| An entry in the feature's record file | One file shows the offer with all its commitments. | No room for an explanation, one requirement change rewrites the feature file, and the file grows long. |
| A separate requirements folder beside the features folder | Requirements stand apart, one file each. | A second tree has to stay in step, and the folder no longer names the parent. |
| Its own folder inside its feature's folder, with an optional explanation | One authoritative file per requirement, the folder names the feature, and the explanation has room. | Every consumer moves its requirements, and a hand edit opens more files. |

## Decision

Each requirement lives at `<feature folder>/<name>/<name>.yaml`, inside the folder of the feature it belongs to. Its identifier stays `requirement/<feature>.<name>`.

A requirement's explanation file `<name>.md` is optional on every layer. It holds an opening paragraph and an optional Example, for what the statement does not make obvious. It carries no acceptance criteria, because the Behavior scenarios that fulfill the requirement already state them.

The book brings a feature's requirements together on the feature's page, so a reader still sees the offer with all its commitments in one place.

## Consequences

A feature record file holds no requirement entries. `litespec new requirement <feature>.<name>` writes the requirement's folder inside the feature's folder, and `litespec rename` moves it.

The provisions that named a requirement entry now name a requirement, and the rule for Intent homes places each requirement in its feature's folder.

Each consumer moves its requirements out of its feature files by hand, as the changelog's upgrade step describes.

## Revisit Conditions

This decision holds until consumers report that editing many small requirement files costs more than the separate history and explanation gain. It also holds until readers need a combined feature file that the book does not give them. Either report reopens the choice.
