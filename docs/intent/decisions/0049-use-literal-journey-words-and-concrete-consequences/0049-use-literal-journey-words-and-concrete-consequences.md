# Use Literal Journey Words and Concrete Consequences

Record guidance tells a writer how to name a journey and how to open a record. This decision replaces two phrases that leave the act or the stake unclear, so the same words read the same way in every record type.

## Context

The word walk stood for a journey in several places, and in some of them it said nothing about the act: a reader could not tell what the actor did when they walked a journey. It also broke the rule that a writer uses literal words.

The phrase why it matters produced an opening such as "It matters because the figures must be correct", which named no consequence and no party. A reader learned that the record mattered without learning what goes wrong when it does not.

The two phrases sat in the guidance for goals, actors, notifications, products and the glossary, not only for journeys and constraints. A fix in one record type would have left the same words in the others.

## Options

LiteSpec compared 3 options for how far the replacement reaches. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Replace both phrases in every provision, template and glossary entry | One replacement covers every record type. | Every file that uses the words changes, including the fixtures and one citation. |
| Change only the journey and constraint wording | Fewer files change. | The same loose words stay in four record types. |
| Keep the wording and add examples to the guides only | No record changes. | The provisions still accept loose verbs. |

## Decision

An actor takes a journey, and the Links table reads is taken by and takes. The verb replaces walk everywhere it names a journey. Where walk names a plain action, such as walking a release build, the sentence names the literal act instead.

The opening of each record and explanation file states the consequence at stake and for whom, with a concrete consequence in place of a generic reason. `litespec/core/writing#use-literal-words` gives the weak form "It matters because the figures must be correct" beside the strong form "A wrong box amount means a correction return, and a late return means a fine".

Documentation pages keep why it matters where it describes a documentation page's own Intent section, since that guidance is for LiteSpec pages and not for records. Decision and research records keep the wording they were written with.

## Consequences

The testing provision named for a journey test takes the verb covers, and each citation of it moves in the same change. `describe-every-journey` asks for the person who takes the journey, what they achieve and the consequence at stake. The fix text of the opening finding and the glossary entry for Journey use the same words.

No search of the specification outside the intent records finds the journey sense of walk, or why it matters in record guidance, once the change lands.

## Revisit Conditions

This decision holds until consumers report that take reads poorly in a sentence about a journey an actor shares with others. It also holds until the consequence phrase makes explanation openings longer than a cold reader needs. Either report reopens the choice.
