# Delete the Unwired Language Checker

LiteSpec's code includes a glossary language checker, a program meant to check glossary wording against rules. No command runs it. This decision deletes the checker, so the tools keep only checks that a command runs.

## Context

The glossary language checker exists in code, but no command runs it. Its rules and the finding codes it would report exist only in the docs. A reader can mistake them for checks that the tools run.

## Options

LiteSpec compared 3 options for the unwired checker. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Delete the glossary language checker | Unwired code leaves the release, and each kept check runs under a command. | Glossary wording rules rest on review, with no checker wired to them. |
| Wire the checker into `validate` now | Glossary wording is checked on every run. | The team never reviewed its findings against the pages, and wiring it adds a feature outside this release. |
| Keep the checker unwired | No change to the code. | Unwired code drifts from the rules it was written for, and a reader mistakes it for a check that the tools run. |

## Decision

LiteSpec deletes the checker. A glossary wording rule stays a review question. A new checker needs its own decision.

## Consequences

The checker leaves the code. The `refusals.prefixes` setting leaves the project file, because no check reads it once the checker is gone.

The finding table and the docs lose the 14 documented finding codes that no check emitted. Most were the checker's output. The 4 pages that cited them state the rule as a review question.

## Revisit Conditions

This decision reopens when the owner funds a glossary checker with its own tests, or when a glossary rule appears that review cannot hold.

A glossary rule that review cannot hold would leave wording unchecked on every run.
