# Delete the Unwired Language Checker

LiteSpec's code includes a glossary language checker, a program meant to check glossary wording against rules. No command runs it. This decision deletes the checker, so each check that the tools keep is one that a command runs.

## Context

The glossary language checker is written, but no command runs it. Its rules and the finding codes it would report exist only in the docs. A reader can take them for checks that the tools perform.

## Options

LiteSpec compared 3 options for the unwired checker. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Delete the glossary language checker | No code runs that no command reaches, and each kept check is one that a command runs. | Glossary wording rules rest on review until a later release wires a checker. |
| Wire the checker into `validate` now | Glossary wording is checked on every run. | Its findings were never reviewed against the pages, and wiring it adds a feature that this release does not plan. |
| Keep the checker unwired | No change to the code. | Unwired code drifts from the rules it was written for, and a reader takes it for a check that the tools run. |

## Decision

The checker is deleted. A glossary wording rule stays a review question until a later release wires a checker under its own decision.

## Consequences

The checker leaves the code. The `refusals.prefixes` setting leaves the project file as well, because no check reads it once the checker is gone.

The finding table and the docs drop the documented finding codes that no check emitted. There are 14 of them, and most were the checker's output. The 4 pages that cited them now state the rule as a review question.

## Revisit Conditions

This decision reopens when the owner funds a glossary checker with its own tests, or when a glossary rule appears that review cannot hold.

A glossary rule that review cannot hold would leave wording unchecked on every run. That gap is the case that would change the choice.
