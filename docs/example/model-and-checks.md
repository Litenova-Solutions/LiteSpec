# The Model and Its Checks

## Intent

The project model joins the written records of a project to its code, screens and tests, and its checks report every gap as a finding. This page shows those checks on Claimwick with real output: the full run of `litespec model check` on the example, the departures that cover its 25 findings, and the record and query commands an agent uses to load one record. Each command below was run against `examples/claimwick`, and the output is copied from that run.

## The Full Check

`litespec model check` builds the model in memory and reports each finding with its file, level, code and message. Against the example it exits zero, because every finding is covered by a departure. The full result, all 25 lines copied from the run, is:

```text
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/approve-expense/approve-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.approve-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/approve-expense/approve-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.approve-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/erase-personal-data/erase-personal-data.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.erase-personal-data' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/erase-personal-data/erase-personal-data.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.erase-personal-data' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered S] [use-case-endpoint-missing] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no endpoint carries its mark.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered S] [use-case-unimplemented] Use case 'use-case/spending.export-expenses' has no handler, reaction or job.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.export-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/list-expenses/list-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.list-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/refuse-expense/refuse-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.refuse-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/refuse-expense/refuse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.refuse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/reimburse-expense/reimburse-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.reimburse-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/reimburse-expense/reimburse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.reimburse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.submit-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.submit-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/policies/use-cases/change-receipt-threshold/change-receipt-threshold.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.change-receipt-threshold' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/policies/use-cases/change-receipt-threshold/change-receipt-threshold.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.change-receipt-threshold' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/receipts/use-cases/attach-receipt/attach-receipt.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.attach-receipt' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/receipts/use-cases/attach-receipt/attach-receipt.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.attach-receipt' has scenarios with no passing proof.
.litespec/intent/journeys/export-approved-expenses/export-approved-expenses.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/export-approved-expenses#s1' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/set-receipt-threshold/set-receipt-threshold.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/set-receipt-threshold#s1' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s1' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s2' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s3' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s4' is served by a browser or device use case but by no screen.
0 findings, 0 blocking, 25 covered.
```

The 25 covered findings fall into five codes:

| Code | Count | Covered by |
|:---|:---|:---|
| `use-case-unreached` | 8 | `departure/expense-screens-unimplemented` |
| `use-case-unverified` | 9 | `departure/use-cases-unverified` |
| `step-screen-missing` | 6 | `departure/expense-steps-unscreened` |
| `use-case-unimplemented` | 1 | `departure/export-expenses-unimplemented` |
| `use-case-endpoint-missing` | 1 | `departure/export-expenses-unexposed` |

Each code names one gap. The screen gap and the proof gap are real: the web app holds one screen, and no run records a passing proof for a scenario.

## A Departure That Covers a Finding

The export use case has no handler yet, because the finance file format has not been decided. The gap is a departure, a record that accepts the gap until a date. Its file is `.litespec/governance/departures/export-expenses-unimplemented/export-expenses-unimplemented.yaml`:

```yaml
type: departure
id: departure/export-expenses-unimplemented
title: Export Expenses Unimplemented
summary: Export waits for the finance file format.
covers:
- use-case-unimplemented
subject:
- use-case/spending.export-expenses
reason: Export waits for the finance file format.
exit: Exporting expenses has no handler yet. The gap closes when the finance file format lands and the export use case gains its handler.
expiresOn: 2027-06-01
```

The `covers` list names the finding code, and the `subject` list names the record the finding is about. The finding is reported as covered, so the run passes. The `exit` field states how the gap closes, and `expiresOn` makes the gap expire: after 2027-06-01 the departure no longer covers the finding, and the check fails again. The date keeps unfinished work visible instead of letting it sit unnoticed.

The example holds six departures in `.litespec/governance/departures/`. The `export-review-bypass` departure covers a provision rather than a finding, and the other five cover the codes in the table above.

## Proof for the Use Cases

The `use-case-unverified` findings say that a use case has scenarios with no passing proof. The Submit expense use case shows why: its two scenarios are confirmed by tests, which name them in `<proves>` tags, but no run has passed for them. [A Use Case in Code](a-use-case-in-code.md) shows the links. The departure `departure/use-cases-unverified` covers all nine use cases until the example ships a test run whose passing proofs cover every scenario.

## Querying One Record

`litespec model query` prints one record with its file, its line and its links in both directions. Agents use it to load a record's context before they change it. Querying the Submit expense use case returns its fields, then its links and its computed state:

```text
dotnet run --project apps/cli/src/LiteSpec.Cli -- model query use-case/spending.submit-expense --root examples/claimwick
```

The `file` field names `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml`, and the `computed.completion.stage` field reads `coded`. The `implements` list names the three code files that carry the use case's mark, and the `proves` list names its two scenarios. The output is long, so [A Use Case in Code](a-use-case-in-code.md) shows the computed part.

## The Continuous Integration Jobs

The model check runs in the continuous integration jobs that [Continuous Integration](../quality/ci.md) describes. A pull request runs the jobs that its changed areas select, so a change to a record or code file runs the model job.
