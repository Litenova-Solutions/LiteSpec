# Name Each Identifier from Its Title

A record stored as its own file takes its folder name and file name from its identifier. This decision takes every page record's identifier name from its title, so readers who browse the folders see what the record says.

## Context

A writer retitles a record and keeps the identifier, and the folder and file names stop matching what the record says. Readers who browse the folders see the old name, and nothing reports it.

A provision that fixed the full title in kebab case as the identifier would keep the two in step, but requirement sentences would become long identifiers that no citation writes by hand. A short name that says what the record says needs a rule that allows leaving words out while adding or changing none.

## Options

LiteSpec compared 3 options for how an identifier name relates to its title. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| The identifier name is a word subsequence of the title | A retitle keeps a short identifier while the folder still says what the record says. | A name may leave title words out, so near titles may take near names. |
| The identifier name equals the title in kebab case | The identifier states the whole title, so the two cannot differ. | Requirement sentences become long identifiers. |
| No rule | No record is renamed and no check is added. | Folders drift from content with nothing reporting it. |

## Decision

Every page record takes its identifier name from the words of its title in title order. The name words appear in the title words in the same order, with title words allowed in between, so words may be left out but none added or changed.

The owner prefix and the decision number name the home and the sequence rather than the record, so neither faces the title. The record file carries the identifier name or the fixed file name of its type, and the folder check already keeps each named folder on its file's stem.

## Consequences

`litespec model check` reports `identifier-title-invalid` for a page record outside the rule. Claimwick renames five requirements to take their names from their titles, so the example stays a perfect example of the rule.

## Revisit Conditions

This decision holds until titles grow words no identifier keeps, so names stop saying what records say. It also holds until a record type needs a name outside its title. Either report reopens the choice.
