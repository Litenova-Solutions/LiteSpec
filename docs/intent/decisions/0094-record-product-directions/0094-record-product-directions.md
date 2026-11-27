# Record Product Directions as Light Records

Each product holds wishes for how it may grow that nobody promises yet, and those wishes need a record light enough to stay a wish. This decision records each wish as a `direction` record with only `statement` required and a free-form explanation file, so a choice made to keep one open names its reason in the records.

## Context

Decision 0041 added the `vision` record at product level, with `summary` and `statement` and an explanation file carrying Direction, Effect on Current Design, and Trigger. Decisions and requirements cite a vision, no reach check reads it, and nothing builds from it.

In use, the record proved heavier than a wish needs. Vision names one statement for a whole product in common usage, while LiteSpec holds many small wishes that may never happen. The three fixed sections made consumers repeat the statement across them and fill Effect on Current Design with design facts copied from the records, which then drifted from their sources.

The fixed sections also listed by hand what the model already knows: the decisions that keep a direction open are computed from `basedOn` and shown by the book, so prose need not list them.

## Options

LiteSpec compared 3 options for recording a product wish. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| A `direction` record with only `statement` required and a free-form explanation file | The name fits many small wishes; one required field keeps a wish light; the book shows the keeping decisions from `basedOn`. | One more rename across records, provisions, and the book; a free-form file can stay empty. |
| Keep the `vision` record with its three fixed sections | No rename, and fixed sections prompt every record for the same points. | The name fits one statement, not many wishes; consumers repeat the statement and copy design facts. |
| Keep wishes on the backlog only | The records hold only promises, and planning stays with planning. | Agents and decisions never see a wish, so choices made for it read as over-engineering. |

## Decision

LiteSpec records each product wish as a `direction` record. A direction states a way the product might grow beyond what it promises now, at product level. It states `statement`, takes `summary` only when the owner wants it, and holds no date, release, priority, status, or order among directions. Its explanation file is free-form.

A decision made so that a direction stays reachable names it in `basedOn`. A requirement that takes a step toward the direction names it in `cites`.

## Consequences

A direction reaches no behavior by design, so no reach check reads it and no finding reports an uncited one. An agent builds nothing from a direction until a requirement cites it. A direction may justify the shape of a record or a model, but never machinery.

A `direction` record lives at `.litespec/intent/directions/<name>/<name>.yaml`, with required `statement` and optional `summary`. A decision names a direction in `basedOn`, and a requirement names one in `cites`. Nothing else links to a direction. The product explanation loses its optional Direction section.

Four provisions on the intent page require the statement of a direction, keep dates and priorities out, forbid building from a direction, and require a decision kept open for a direction to cite it. `litespec/core/intent#reach-every-intent-record-with-behavior` excepts the direction. `litespec/core/principles#add-new-machinery-only-for-a-current-requirement` states that a direction activates no machinery.

Claimwick carries `direction/contractor-expenses`, the way of growing where contractors submit expenses as employees do. This decision supersedes `decision/0041-record-product-vision`.

## Revisit Conditions

This decision reopens when directions start to carry dates or priorities in practice, or when no consumer cites a direction from a decision within 2 releases.

Either case means the direction record no longer holds what the decision gave it. The team then decides again whether a wish stays a separate record.
