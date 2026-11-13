# Bind the Docs to the Tools and the Example

LiteSpec's documentation states facts about its tools, such as finding codes, templates and the record IDs that the example uses. This decision makes `validate` check each such fact against the data the tools hold. It also generates the reference tables from that data, so a doc fact that drifts from the tools fails the check instead of misleading a reader.

## Context

The docs state tool behavior that the tools do not have, such as a `validate` option and a `new decision` number form. In a sample, 5 of 6 record IDs in the example did not exist. Nothing checks either kind of statement.

## Options

LiteSpec compared 3 options for keeping the docs in step with the tools. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Validate each tool fact the docs cite, and generate the reference tables | Each check is a lookup in data the tools hold, so a drifting citation fails `validate`. | A page that cites a code, template, ID or pattern option needs that entry to exist, and generated pages change with their data. |
| Review the docs by hand | No checks to maintain. | In the sample, 13 statements about tool behavior had drifted, and 5 of 6 sampled IDs were missing. |
| Generate every docs page | No page can drift from the data. | Explanatory prose cannot come from data, so the pages would lose their explanation. |

## Decision

`validate` checks each citation of a tool fact against data that the tools hold. The reference tables are generated from that data.

Explanatory prose stays hand-written.

## Consequences

`validate` reports these problems:

- A cited finding code that the finding table lacks.
- A `Checked by` line that names no finding code.
- A cited template that does not exist.
- An example record ID that the example lacks.
- A page that the index omits.
- A pattern with more options than its budget allows.

The finding table, the folder reference and the manifest pin table are generated from the data that the tools hold. The provision tables are written by `provisions`.

## Revisit Conditions

This decision reopens when a documented claim about a tool has no lookup that the tools can make, or when a generated page grows past what a reader can review.

A claim with no lookup cannot be checked by `validate`, so it drifts without a failure. A generated page that a reader cannot review would hide the drift it was meant to show.
