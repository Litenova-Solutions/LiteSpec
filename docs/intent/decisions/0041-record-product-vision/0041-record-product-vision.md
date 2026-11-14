# Record Product Vision as a Vision Record

A product vision is the longer destination that a product heads toward, beyond the features it promises now. This decision adds a `vision` record, which holds that destination at product level in a summary and a statement, with no date. Decisions and requirements can cite a vision, so a choice made to keep a destination open names its reason in the records.

## Context

A consumer starting its first release knows where the product should go later: private and business books side by side, staff with their own permissions, and many currencies and countries. In that release, 2 choices are made for this destination. Every record carries the business it belongs to, and amounts are stored in a home currency rather than in euros.

Neither decision can name its reason, because a decision's `basedOn` takes constraints, research and requirements. A destination is none of these. Both decisions therefore show an empty `basedOn`, and the reason sits in the product explanation file. No record can cite that file, and no check reads it.

The existing record types do not fit a destination. A goal needs a measure and a journey or quality requirement that moves it, so an unmeasured destination raises `goal-mover-missing`. A feature needs a problem with observed or reported evidence, which a destination often lacks. A departure parks planned-Intent findings until an expiry date, and a destination has no date. The backlog outside the repository holds order and timing well, but agents and decisions inside the repository never see it.

## Options

LiteSpec compared 4 options for recording a product vision. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| A `vision` record at product level, cited by decisions and requirements | A decision names its reason as a record, and the book shows the vision beside the choices that keep it open. | One more record type to learn, and a vision can grow into a wish list if dates creep in. |
| Keep the vision in the product explanation file | No new record type, and the prose reads well. | No record can cite prose, so a decision shows an empty `basedOn`, and nothing notices when the prose changes. |
| Write the vision as planned goals and features, parked by departures | It uses existing record types and checks. | A goal needs a measure and a mover, a feature needs evidence, and a departure needs an expiry date, so each one is invented or misused. |
| Keep the vision outside the repository, on the backlog | Planning stays with planning, and the records hold only promises. | Agents and decisions in the repository never see it, so choices made for it read as over-engineering. |

## Decision

LiteSpec adds the `vision` record. A vision names where the product is heading beyond what it promises now, at product level. It has a `summary` and a `statement`, and no date, priority or status.

A decision made so that a vision stays reachable names it in `basedOn`. A requirement that takes a step toward the vision names it in `cites`.

## Consequences

A vision reaches no behavior by design, so no reach check reads it. An agent builds nothing from a vision until a requirement cites it. A vision may justify the shape of a record or a model, but never machinery.

A `vision` record lives at `.litespec/intent/visions/<name>/<name>.yaml`, with `summary` and `statement`. An explanation file sits beside it when the project lists `intent` in `explanations.layers`. A decision names a vision in `basedOn`, and a requirement names one in `cites`. Nothing else links to a vision.

Four provisions on the intent page state the fields of a vision. They keep dates and priorities out, forbid building from a vision, and require a decision kept open for a vision to cite it. `litespec/core/intent#reach-every-intent-record-with-behavior` excepts the vision. `litespec/core/principles#require-current-complexity-activation` states that a vision activates no machinery.

`litespec new vision <name>` scaffolds the record. The book lists visions beside goals. Claimwick carries `vision/contractor-expenses`, the destination where contractors submit expenses as employees do.

## Revisit Conditions

This decision reopens when visions start to carry dates or priorities in practice, or when no consumer cites a vision from a decision within 2 releases.

Either case means the vision record no longer holds what the decision gave it. The team would then need to decide again whether a vision stays a separate record.
