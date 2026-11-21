# The Model and Its Checks

The project model joins a project's written records to its code, screens, and tests, and its checks report every gap as a finding. This page shows those checks on Claimwick with real output: the full run of `litespec model check` on the example, the waivers that cover its 20 findings, and the record and query commands an agent uses to load one record. Each command below ran against `examples/claimwick`, and the output is copied from that run.

## The Full Check

`litespec model check` builds the model in memory and reports each finding with its file, level, code, and message. Against the example it exits zero, because every finding is covered by a waiver. The full result, 20 findings plus the summary line, copied from the run, is:

```text
.litespec/behavior/modules/reporting/use-cases/list-reimbursed-expenses/list-reimbursed-expenses.yaml:1 [covered G] [use-case-flow-missing] Use case 'use-case/reporting.list-reimbursed-expenses' sits in no flow.
.litespec/behavior/modules/reporting/use-cases/list-reimbursed-expenses/list-reimbursed-expenses.yaml:1 [covered S] [use-case-unreached] Use case 'use-case/reporting.list-reimbursed-expenses' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/reporting/use-cases/list-reimbursed-expenses/list-reimbursed-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/reporting.list-reimbursed-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/approve-expense/approve-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.approve-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/erase-personal-data/erase-personal-data.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.erase-personal-data' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered S] [use-case-endpoint-missing] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no endpoint carries its mark.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered S] [use-case-unimplemented] Use case 'use-case/spending.export-expenses' has no handler, reaction or job.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered S] [use-case-unreached] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/export-expenses/export-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.export-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/list-expenses/list-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.list-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/refuse-expense/refuse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.refuse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/reimburse-expense/reimburse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.reimburse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.submit-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/policies/use-cases/change-receipt-threshold/change-receipt-threshold.yaml:1 [covered S] [use-case-unreached] Use case 'use-case/spending.change-receipt-threshold' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/policies/use-cases/change-receipt-threshold/change-receipt-threshold.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.change-receipt-threshold' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/receipts/use-cases/attach-receipt/attach-receipt.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.attach-receipt' has scenarios with no passing proof.
.litespec/behavior/modules/spending/aggregates/reminder-preferences/use-cases/change-reminder-preference/change-reminder-preference.yaml:1 [covered S] [use-case-unreached] Use case 'use-case/spending.change-reminder-preference' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/aggregates/reminder-preferences/use-cases/change-reminder-preference/change-reminder-preference.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.change-reminder-preference' has scenarios with no passing proof.
.litespec/intent/flows/export-approved-expenses/export-approved-expenses.yaml:1 [covered G] [step-screen-missing] Flow step 'step/export-approved-expenses.export-batch' is served by a browser or device use case but by no screen.
.litespec/intent/flows/set-receipt-threshold/set-receipt-threshold.yaml:1 [covered G] [step-screen-missing] Flow step 'step/set-receipt-threshold.set-threshold' is served by a browser or device use case but by no screen.
0 findings, 0 blocking, 21 covered.
```

The 21 covered findings fall into four codes:

| Code | Count | Covered by |
|:---|:---|:---|
| `use-case-unverified` | 12 | `waiver/use-cases-unverified` |
| `use-case-unreached` | 4 | `waiver/expense-screens-unimplemented` |
| `step-screen-missing` | 3 | `waiver/expense-steps-unscreened` |
| `use-case-flow-missing` | 2 | `waiver/reimbursed-list-unserved` and `waiver/read-expense-unserved` |

Each code names one gap. The screen gap and the proof gap are real: the web app holds three screens, and no run records a passing proof for a scenario.

Every code in the table but `use-case-unverified` blocks when open. The link findings are level G on records and level S on code, while `use-case-unverified` blocks only with `--release`.

## A Waiver That Covers a Finding

The export use case has no handler yet, because the finance file format has not been decided. The project accepts that gap as a waiver, a record that covers it until a date. Its file is `.litespec/governance/waivers/export-expenses-unimplemented/export-expenses-unimplemented.yaml`:

```yaml
type: waiver
id: waiver/export-expenses-unimplemented
title: Export Expenses Unimplemented
summary: Export waits for the finance file format.
kind: gap
waives:
- use-case-unimplemented
appliesTo:
- use-case/spending.export-expenses
reason: Export waits for the finance file format.
exitWhen: Exporting expenses has no handler yet. The gap closes when the finance file format lands and the export use case gains its handler.
expiresOn: 2027-06-01
closedBy:
- use-case/spending.export-expenses
```

The `waives` list names the finding code and the `appliesTo` list names the record the finding is about, so the finding is reported as covered and the run passes; [Waivers](../core/waivers.md) states what each field carries. After 2027-06-01 the waiver stops covering the finding, and the check fails again, which keeps unfinished work visible instead of letting it sit unnoticed.

The example holds seven waivers in `.litespec/governance/waivers/`. Six of them cover the codes in the table above, and the `export-review-bypass` waiver covers a provision rather than a finding.

## Proof for the Use Cases

The `use-case-unverified` findings say that a use case has scenarios with no passing proof. Submit expense shows why: tests confirm its two scenarios by naming them in `<proves>` tags, but no run has passed for them. [A Use Case in Code](a-use-case-in-code.md) shows the links. The waiver `waiver/use-cases-unverified` covers all eleven use cases until the example ships a test run whose passing proofs cover every scenario.

## Querying One Record

`litespec model query` prints one record with its file, its line, and its links in both directions. Agents use it to load a record's context before they change it. Querying Submit expense returns its fields first, then its links and its computed state:

```text
dotnet run --project apps/cli/src/LiteSpec.Cli -- model query use-case/spending.submit-expense --root examples/claimwick
```

The `file` field names `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml`, and the `computed.completion.stage` field reads `coded`. The `implements` list names the three code files that carry the use case's mark, and the `proves` list names its two scenarios. The output is long, so [A Use Case in Code](a-use-case-in-code.md) shows only the computed part.

Two more forms find records without an identifier. `litespec model query --search "receipt threshold"` lists the records whose identifier, title, summary, or statement holds the text, with an exact identifier first. `litespec model query --list use-case --module spending` lists every use case of the spending module, one line per use case with identifier, title, and path. With `--search` or `--list`, no match prints nothing and exits 0.

## The Continuous Integration Jobs

The model check runs in the continuous integration jobs that [Continuous Integration](../quality/ci.md) describes. A pull request runs the jobs its changed areas select, so a change to a record or code file runs the model job.
