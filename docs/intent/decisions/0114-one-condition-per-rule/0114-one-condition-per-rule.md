# State One Condition per Rule

A rule states one condition, and review judges that. A statement over 600 characters is flagged as a signal, never as a block, and a value that varies by date, jurisdiction, or tenant belongs in reference data.

## Context

The domain-model page said to state one rule per condition, but only in its concepts text, so no review asked for it and no check signalled a breach. Rules grew into paragraphs. One rule held a dozen cases, each with its own dates or refusal, and every scenario step, refusal, and code mark that cited it cited all of them at once.

Nothing told a consumer how much one rule should hold, or where a value that changes by date or jurisdiction belongs.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| A required one-condition provision, an advisory length signal, and advisory guidance on varying values | Review judges the real criterion, while a cheap check points at the statements most likely to break it and never blocks a long statement that holds one condition. Varying values move toward dated reference data. | Existing projects split their long rules and re-point the citations, and the threshold is a heuristic. |
| A required length limit checked by the model | Fully mechanical. | Length is not the criterion: a short statement can hold two conditions, and a long calculation over a closed list holds one. A blocking limit forces splits that make the model worse. |
| No guidance | No change for consumers. | Citations stay vague, and a change to one condition reopens every record that cites the rest. |

## Decision

The required provision `litespec/core/domain-model#state-one-condition-per-rule` asks for one condition per rule: the first sentence states it, and any further sentence only narrows its scope, its unit, or an exception without a refusal of its own. A case with its own refusal, derived value, dates, or scenario is a rule of its own. Review reads each sentence after the first.

The advisory provision `litespec/core/domain-model#keep-each-rule-statement-short` keeps a statement within 600 characters. The model check reports `rule-statement-wide` at level A past that length, which never blocks. The advisory provision `litespec/core/domain-model#cite-varying-values-from-reference-data` asks a rule to cite the reference-data table holding a rate, threshold, or deadline rather than write the value into its statement.

## Consequences

Projects split rules that hold several conditions. The original identifier stays on the first condition, so citations that rest on it need no change, and each other citation is re-pointed to the rule it rests on.

## Revisit Conditions

Reopen this when the length signal flags mostly statements that hold one condition, or when projects need a rule kind that holds several conditions by design.
