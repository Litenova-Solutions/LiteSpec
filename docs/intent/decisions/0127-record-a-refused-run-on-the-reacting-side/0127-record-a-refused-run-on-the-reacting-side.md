# Record a Refused Run on the Reacting Side

A reaction that runs another module's use case can name, in `onRefusal`, the use case of its own module that records a refusal.

## Context

A reaction in one module often runs another module's public use case, and that use case can refuse. The reacting module then records the refusal on its own aggregate, so its reader sees the run failed instead of pending.

A reaction runs at most one use case, so the record had no field for the recording run. The code ran a second use case that the record did not state.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Name the recording use case in onRefusal on the runs effect | The record states both runs and their order. A refusal commits nothing, so the second run never follows a committed one, which is the reason a reaction runs one use case. | The effect entry gains a field, and every reader of runs reads it as a second target. |
| Raise a refused fact in the called module and answer it with a second reaction | Each reaction runs one use case. | A refusal commits nothing, so the called module would write a record only to announce that it refused. |
| Leave the refusal in the dead letter for an operator | No new field. | The reacting module's reader shows the run as pending forever, and the refusal waits on a person. |

## Decision

The triggered effect entry carries `onRefusal`, a use case of the reacting module, allowed only beside `runs`. In the library, the loan-returned reaction in Lending runs Billing's settle-fine use case, and when Billing refuses because the member account is closed, it runs `use-case/lending.record-fine-refused`.

## Consequences

The runs edge reads `onRefusal` after the use case it follows, so the access, reach, locality and proof checks cover it, and the one-use-case count ignores it. `litespec model check` reports `record-invalid` for an `onRefusal` on an effect with no `runs`.

## Revisit Conditions

Reopen this when a product needs to answer a refusal with something other than one recording use case of the reacting module, or when a refusal needs a retry policy of its own.
