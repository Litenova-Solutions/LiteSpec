# Write Features for the Builder and the Tester

A feature and its requirements are read by someone who has to build or test what they say. Until now, a feature's explanation could leave its edges unstated, promise outcomes it cannot bring about, and describe behavior no requirement holds. This decision requires a Boundaries section and states how success criteria, requirement statements and rationales are written, so a developer can tell what to build and a tester can tell what passes.

## Context

A review of a feature written to the old sections found faults that plain wording hid rather than fixed. A sentence such as "each one must be in the books as it is at the bank" sounded simple but left open what had to match. Loose verbs such as touches, holds and lands named no operation.

A requirement compared a book balance with a statement balance without naming the date of either, so correct books could show a difference. A success criterion promised that the two balances match after an import, which the feature cannot bring about. A rationale called a difference "the quickest sign" of a missing entry, naming one cause and an unmeasured comparison.

The feature also read as answering its whole problem. Nothing said that importing statements and matching payments belong to another feature, or that the date of an opening balance and transfers between currencies were still open questions.

## Options

LiteSpec compared 3 options for how a feature is explained. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep the sections, an optional Limits and the existing rules | No feature explanation is rewritten. | Edges stay unstated, criteria overpromise, and behavior lives in prose. |
| Required Boundaries, with rules for criteria, statements, rationales, prose and wording | A developer can tell what to build and a tester what passes. | Every feature gains a section, and reviewers judge more provisions. |
| A free Notes section | Authors write what each feature needs. | Edges sit in a different place on each feature, and reviewers cannot tell what the section holds. |

## Decision

A feature explanation carries Capabilities, Success Criteria, Boundaries and an optional Example. Boundaries states what the feature leaves to other features, the features it depends on, and the open questions the owner has not decided.

Success criteria state only outcomes the feature brings about on its own and a tester can check. A requirement statement states one behavior with each condition, date and amount its result depends on. A rationale states why the requirement exists, adds no behavior, and claims no more than its reason supports.

An explanation describes only behavior the requirements state. Every record names the operation it describes, never a loose verb, and states the behavior or the measured target in place of an intensifier.

## Consequences

`explanation-missing` reports a feature explanation without Boundaries. Five new provisions on the intent page and one on the writing page state the rules, and the literal-words provision also refuses loose verbs. A requirement title names the specific behavior or fact it requires.

The feature template and the Claimwick approval feature follow the new sections. Each consumer adds Boundaries to its feature explanations, as the changelog's upgrade step describes.

## Revisit Conditions

This decision holds until consumers report that most features have nothing to say under Boundaries. It also holds until the judgment provisions cost more review time than the defects they catch. Either report reopens the choice.
