# Reference Data Tables

Dated business values live in tables the rules cite, not in rule prose. This decision adds the reference-data record type: one dated table per value set under its module, with each row carrying its value, its effective dates, and the record behind it.

## Context

Behavior rules stated dated values in their statements: amounts, deadlines, and time zones written into prose. No record held the tables behind those values. A setting holds one operator value with its default and range, and limits are prose, so neither holds a table of dated values that rules look up.

Each statement then carried its own copy of each value. Changing a value meant hunting through statements, and no check saw which statements held it. A reader could not build a rate table or a deadline map from the records, because no record said what any table contains.

## Options

LiteSpec compared 2 options for dated business values. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Hold dated values in reference-data tables cited by key | One table holds each value with its dates and source, and rules cite it by key, so the model checks every cited table exists and no two rows overlap. | Each table is one more record to write beside the rules citing it. |
| Flag literal values with a judgment provision only | No new record type, and reviewers catch literals by eye. | Values stay in prose with no home, so each change still hunts through statements. |

## Decision

Dated business values live in reference-data tables under their module. Each table carries `reference-data/<module>.<name>` with dated rows, and each row carries its value, the `effectiveFrom` date it takes effect, the `effectiveTo` date it stops applying, and the `source` naming the constraint, research, or decision behind the value.

Rules cite the table instead of stating its values, and use cases name it in `reads`. The model checks that every cited table exists and that no two rows overlap in time.

## Consequences

Dated business values live in reference-data tables under their module: `reference-data/<module>.<name>` with dated rows, each row carrying its value, its effective dates, and the constraint, research, or decision behind it. Rules cite the table instead of stating its values, and use cases name it in `reads`.

The new provision is `litespec/core/model#cite-reference-data-instead-of-literal-values`, beside the Reference Data section of the domain model page.

## Revisit Conditions

This decision reopens when a value needs semantics a dated table fails to cover, such as a value computed from live state.

That case means rows no longer hold what the rules need. The team then decides again where such values live.
