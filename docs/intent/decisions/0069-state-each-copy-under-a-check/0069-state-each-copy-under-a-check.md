# State Each Copy Under a Check

A reader often opens one record file with nothing else open. The file is a rule, a use case, or an explanation beside it. This decision states the principle for every layer and every future change. A record states what its reader needs to read it alone. A check holds each stated copy to its source.

## Context

Two readings of Clean disagreed. One reading wrote each fact once and derived the rest, so an isolated file forced its reader to open every file it linked to. The other reading stated each fact where a reader needed it, so copies drifted from their sources with no finding to report it.

The review behind this release found both failures at once. Facts were stated in two to four places. The checks missed the defects that mattered. One consumer's model check reported no blocking finding while its records already disagreed with themselves.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| State what the reader needs, check each copy | Isolated files read whole, and copies cannot drift. | Every derived copy needs its agreement check. |
| Write each fact once, derive the rest | No copy exists to drift. | A reader opens every linked file to read one record. |
| State copies freely with no checks | Records read whole with no check work. | Copies drift with no finding. |

## Decision

A record states every fact and link a reader needs to understand it alone, even when the model could derive it from other records. Every stated fact the model can also derive carries a check that compares it with its source. An explicit copy never drifts.

Judgements stay computed and are never typed. They are completion, coverage, proof status, counts, and censuses. The link itself is written on the record it starts from. A record may also state the inverse when a reader needs it there, under the agreement check.

A sweep over every link and fact the model computes applies the rule link by link. Each fact a reader of the isolated file needs is stated with its check. Each one the sweep keeps computed is recorded with its reason.

## Consequences

The principle comes first, because the later decisions in this release apply it.

`governs`, `exposure`, per-field classification with data-class membership, and screen `serves` stay explicit, each with its agreement check. Flow citations are already written on the steps in the same file, so the flow file stays whole.

## Evidence

[research/comparable-rationale-practice](research/comparable-rationale-practice) found comparable projects keeping rationale in numbered records beside the normative text. The specification stays the only normative surface: rationale is stated beside it and held to the source. [research/check-coverage-evidence](research/check-coverage-evidence) found the checker dense below a requirement and almost absent above it, with the largest measured gaps on the unchecked links. The consumer disagreement was read from live records with high confidence.

## Revisit Conditions

This decision reopens when an agreement check costs more than the drift it catches. It also reopens when a stated copy misleads where a derived value would not.
