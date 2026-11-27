# Rule Sources

Each rule states whether it restates the law, a contract, or a product choice. This decision adds the `source` field on the rule, with a `cites` link naming the restated record when the source is law.

## Context

A rule that restates the law reads like a rule that makes a product choice: both state what holds with no word on where the statement comes from. The `basedOn` links behind a rule name constraints and research, but they never say whether the statement restates them or adds a value of its own.

The two kinds then get mistaken for each other. A future maintainer reads a product simplification that claims legal backing and will not change it, even when the owner could. Or they change a real legal rule, believing it is a product choice.

## Options

LiteSpec compared 2 options for rule sources. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| State the source on the rule, with a cites link when it is law | One field states the kind, and the cites link names the restated record, so each law rule traces to its constraint or research. | Rules written before the field gain it only as they are touched. |
| State the basis on each basedOn entry | Each link carries its own restates, interprets, or chooses. | Every link gains a judgment, and a rule with several links states its kind several times. |

## Decision

Each rule states `source` as `law`, `contract`, or `product-choice`. A rule with `source: law` names the restated constraint or research record in `cites`, so each law claim traces to the record it carries.

One field states the kind for the whole rule. Per-link judgments would repeat the kind on every link and still leave the statement itself unstated.

## Consequences

Each rule states source as law, contract, or product-choice, and a rule with `source: law` names the restated constraint or research record in `cites`.

The new provisions are `litespec/core/domain-model#state-each-rules-source` and `litespec/core/domain-model#name-the-record-each-law-rule-restates`.

## Revisit Conditions

This decision reopens when a rule mixes restating and choosing in one statement in a way one source fails to state.

That case means one field no longer covers what the rule does. The team then decides again how rules state their sources.
