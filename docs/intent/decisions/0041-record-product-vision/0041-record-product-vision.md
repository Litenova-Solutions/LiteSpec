# Record Product Vision as a Vision Record

## Context

A consumer starting its first release knew where the product should go later: private and business books side by side, staff with their own permissions, many currencies and countries. Two choices in the first release were made for that destination: every record carries the business it belongs to, and amounts are stored in a home currency rather than in euros. Neither decision could name its reason. A decision's `basedOn` takes constraints, research and requirements, and the destination was none of these, so both decisions showed an empty `basedOn` while the reason sat in the product explanation file, which no record can cite and no check reads.

The existing types did not fit. A goal needs a measure and a journey or quality requirement that moves it, so an unmeasured destination raises `goal-mover-missing`. A feature needs a problem with observed or reported evidence, which a destination often lacks. A departure parks planned-Intent findings until an expiry date, and a destination has no date. The backlog outside the repository holds order and timing well, but agents and decisions inside the repository never see it.

## Decision

LiteSpec adds the `vision` record: where the product is heading beyond what it promises now, at product level, with `summary` and `statement` and no date, priority or status. A decision made so a vision stays reachable names it in `basedOn`, and a requirement that takes a step toward it names it in `cites`. A vision reaches no Behavior by design, so no reach check reads it, and an agent builds nothing from it until a requirement cites it. A vision may justify the shape of a record or model, never machinery. Claimwick carries `vision/contractor-expenses`, the destination where contractors submit expenses as employees do.
