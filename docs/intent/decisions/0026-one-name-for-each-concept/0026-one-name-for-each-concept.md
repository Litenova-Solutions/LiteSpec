# One Name for Each Concept

LiteSpec's standard, its tools and its book use one vocabulary for its concepts, such as check levels, completion stages and record identifiers. This decision gives each concept one name in every layer, so a word never means two things. A reader who meets a word in one place can rely on it in the others.

## Context

Several words in the standard and the tools name two things. `level` names both the conformance level and the check level. `rule` names both a record and a provision strength. `implemented` and `met` are both proposed as completion stages. A record has both a short identifier and a qualified one. Readers meet each overlap in the book and in the tool output.

## Options

LiteSpec compared 3 options for the overlapping names. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One name per concept, renaming only the words that name another concept | Each word means one thing in every layer, and familiar names stay. | Readers who knew `level` as the conformance level must learn it again for the check level. |
| Rename every overloaded concept to a new word | Every name changes once, and no reader keeps an old meaning. | The new names collide with `gate`, which already names the CI gates. |
| Keep every name and document each overlap | No name changes and no citation moves. | Readers keep meeting 2 meanings for one word. |

## Decision

Each concept has one name in every layer. The check level keeps its name, because the conformance level that shared the word is removed by `decision/0027-projects-claim-record-layers`.

The stage, strength and identifier names hold in the standard, the tools and the book. No stage, strength or identifier carries a second name.

## Consequences

The check level keeps its name, with the values `G`, `S` and `A`. The duplicate `severity` field goes.

A finding's `outcome` carries the result, with the values `blocking`, `reported` or `covered`. `covered` replaces `known`.

The completion stages are `unfulfilled`, `specified`, `coded` and `verified`. `implemented` becomes `coded`, and `met` is not a stage. A requirement reaches `verified` when every fulfiller is verified and its measure passes.

Provision strength is `required`, `default` or `advisory`, replacing `rule`, `default` and `advice`. So `rule` names only the record. The qualified identifier goes, and a record's placement shows as its folder path and its `module`.

## Revisit Conditions

This decision reopens when a concept appears that no existing word names without a second meaning.

A new concept that a current word would name only with a second meaning is the case that would change the choice.
