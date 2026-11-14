# Cite a Source for Each Constraint Rule

A constraint states an outside rule that binds the product, such as a tax or privacy law. This decision puts the sources of each rule on the constraint itself, so a reader sees the source beside the rule it states. Research stays the home for a question that needs investigating or for observed provider behavior.

## Context

A constraint kept one read date for the whole record, and its rules cited no source of their own. A reader who checked one rule had to find the source through prose, and a rule with two sources had no place to show which one held it.

A law that states a rule on one public page is its own source. Requiring a research record to hold one URL added a record per constraint and held no question that needed investigating.

Constraint explanations also mixed the outside obligation with the product's response. A reviewer could not tell which sentence an outside party wrote and which one a team decided.

## Options

LiteSpec compared 3 options for where a constraint's sources live. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Sources on the constraint, with an optional research link | A law needs no extra record, and each rule names its source key in its own file. | A source cited by two constraints is listed in both. |
| Every constraint cites a research record | Every outside fact has one shape, and each source is checked once. | A single public page needs a research record, which holds no question. |
| Keep authority and readOn, with review questions | No field changes. | A rule cannot be traced to a source key, and a reviewer checks each citation by hand. |

## Decision

A constraint carries `sources`, required, in the research shape. Each source has a key, a link and a read date, and a publication date where the source shows one. A constraint that rests on observed provider behavior also names its research in `basedOn`.

A constraint's Rules section states one rule per paragraph. Each rule keeps its condition and cites one of the constraint's sources with a reference link. Scope states what the rule covers and what it leaves out. Effect on the Product names each requirement that holds an effect of the rule. Open Points holds only questions about how a verified rule applies to the product.

## Consequences

The record-level `readOn` is removed, and `effectiveFrom` states the date a rule starts to bind when it starts on a date. The source checks run on constraint files as they run on research files. `model render` writes the source links into each explanation file that cites a source, and `model check` reports drift when that region differs from what `render` writes.

`litespec/core/intent#cite-a-source-for-each-constraint-rule` is required. `litespec/core/intent#state-each-constraint-rule-with-its-condition` is required and reviewed. `constraint-effect-unlisted` and `constraint-effect-uncited` keep each citing requirement and each Effect on the Product entry in step. The book renders sources and the effective date.

## Revisit Conditions

This decision holds until consumers report that several constraints cite one source often enough that a shared record would be cheaper. It also holds until a constraint's source list grows past what a reader can scan. Either report reopens the choice.
