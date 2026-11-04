# The Model and Its Checks

## Intent

The project model is the computed join between a project's written records and its code, screens, and tests: it proves they agree, and its checks report every gap as a finding with a level. This page shows that model at work on the Claimwick product, the example product these pages follow, with real output from `litespec model check` and `litespec model query`: how an exception (a missing piece the team accepts until an expiry date) covers the still-missing handler for Export expenses, what the check reports when the Approve expense handler is removed, how the book shows journey detail, what `model check --since` reports for a breaking contract change, and which continuous integration jobs run these checks.

## Checking the Model

`litespec model check` builds the model in memory and reports every finding with its level, code, message, path, and line. Against the unchanged example it reports this, and exits zero:

```text
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [S excepted use-case-unimplemented] Use case 'use-case/spending.export-expenses' has no handler.
0 open findings, 1 excepted.
```

The Export expenses use case has no handler, so the `use-case-unimplemented` check fires at level S, through the stack reader. A finding that an open exception covers is reported as excepted and exits zero, so the line carries the `excepted` marker and the summary counts `0 open findings, 1 excepted`.

## The Exception for Export Expenses

The team accepts that exporting expenses has no handler yet, so the gap lives as an exception of kind gap with an expiry date. The record file sits at `.litespec/governance/modules/spending/exceptions/export-expenses-unimplemented.yaml`, since every record it sets aside is in the spending module, and the use case it covers waits for its handler at `.litespec/behavior/modules/spending/expenses/export-expenses.yaml`. The exception record file reads:

```yaml
type: exception
id: exception/export-expenses-unimplemented
title: Export Expenses Unimplemented
kind: gap
covers:
  - use-case-unimplemented
subject:
  - use-case/spending.export-expenses
reason: Export waits for the finance file format.
exit: The gap closes when the finance file format lands and the export use case gains its handler.
expiresOn: 2027-06-01
```

The exception covers the finding because its `covers` lists the finding code and its `subject` lists the finding record. The `exit` field states how the gap closes: when the finance file format lands and the export use case gains its handler. The check reports without failing until 2027-06-01. An expired exception fails the check, so the date keeps the missing work visible instead of letting it sit unnoticed.

## The Run with the Approve Expense Handler Removed

Removing `apps/api/src/Claimwick.Application/Spending/Expenses/ApproveExpense/ApproveExpenseCommandHandler.cs` and running the check again gives one open finding:

```text
.litespec/behavior/modules/spending/expenses/approve-expense.yaml:1 [S use-case-unimplemented] Use case 'use-case/spending.approve-expense' has no handler.
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [S excepted use-case-unimplemented] Use case 'use-case/spending.export-expenses' has no handler.
1 open findings, 1 excepted.
exit 1
```

The finding says no code implements the Approve expense use case. The fix is to restore the marked handler at its path under `Claimwick.Application/Spending/Expenses/ApproveExpense/`, which the computed `implemented` state then picks up on the next run. The Export expenses line stays excepted, because its exception still covers it. The run exits 1, because one open finding remains.

## Querying One Record

`litespec model query` prints one record with its links in both directions, and agents use it to load a record's context. The book and the CLI show both the identifier and the qualified identifier, and `model query` accepts either. Querying the Expense aggregate by either `aggregate/spending.expense` or `spending/expenses` returns its states, transitions, data, rules, and events:

```json
{
  "type": "aggregate",
  "id": "aggregate/spending.expense",
  "qualifiedId": "spending/expenses",
  "title": "Expense",
  "file": ".litespec/behavior/modules/spending/expenses/aggregate.yaml",
  "line": 1,
  "fields": {
    "type": "aggregate",
    "id": "aggregate/spending.expense",
    ...
    "states": [
      {
        "name": "Submitted",
        "meaning": "The expense waits for a decision."
      },
      ...
    ],
    ...
  },
  ...
}
```

Querying the Submit expense use case by either `use-case/spending.submit-expense` or `spending/expenses/use-case/submit-expense` adds the computed links the builder derived:

```json
{
  "type": "use-case",
  "id": "use-case/spending.submit-expense",
  "qualifiedId": "spending/expenses/use-case/submit-expense",
  "title": "Submit Expense",
  ...
  "computed": {
    "implemented": true,
    "verified": false,
    "implements": [
      "apps/api/src/Claimwick.Application/Spending/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs"
    ],
    "calls": [],
    "proves": [
      {
        "scenario": "Submitting_with_a_receipt_succeedsAsync",
        "claim": "scenario/submit-expense.submitting-with-a-receipt-succeeds",
        "confirmed": true,
        "passing": null
      },
      ...
    ]
  }
}
```

The record reads `implemented` because its handler exists, and `proves` names the scenario test that confirms its scenario. Both excerpts above shorten the full output with `...`; the run files carry the complete text.

## The Consumer Check

`litespec model check` runs the consumer pass over the project configuration, the record files, and the language of a consumer repository, per [Model Builder](../tools/model.md), and reports its findings in the same shape. Against the Claimwick example it reports this, and exits zero:

```text
0 findings, 0 blocking, 0 known.
```

The pass covered 44 files under `docs` and passed: no mannered term, no rejected synonym.

## The Journey Detail

`litespec book` shows each journey as record detail for product readers: the description first, then the observable end, the steps as sentences with server badges for the serving use cases and screens, and the requirements with their exceptions and fallbacks inline. Field names never render literally. The submit-and-reimburse detail reads:

```text
Submit and Reimburse

An employee who paid a work cost submits the expense and waits for a decision.

Finished when: the employee holds the reimbursement, and the expense
record shows every step.

1. The employee submits the expense with its receipt.
2. The manager approves the expense within five days.
3. Finance reimburses the approved expense.
4. The employee sees the reimbursement land.
If it goes wrong: past three days the manager gets a reminder;
past five the wait is reported.

Promises
- Each submitted expense is decided within five business days.
    Except during the year-end close. Then each submitted expense
    is decided within ten business days.
- An expense above the receipt threshold carries a receipt.
- Nobody approves their own expense.
- An approved expense is reimbursed at most once.
```

The finance lead reads the promises without opening a Behavior page.

## The Breaking Contract Change

`litespec model check --since main` blocks only on findings about records and code changed since `main`, and reports every client-breaking contract change since `main` as a `contract-break` finding of level S: a removed result, a removed refusal code, or a changed integration event. Say the team removes the refusal code from the receipt-missing result of Submit expense. The run reports:

```text
.litespec/behavior/modules/spending/expenses/submit-expense.yaml:1 [S contract-break] Refusal code 'spending.receipt-missing' of result 'result/submit-expense.receipt-missing' no longer exists.
```

The developer sees which published contract the change breaks before it merges.

## The Continuous Integration Jobs That Run the Checks

[Continuous Integration](../quality/ci.md) names the jobs each pull request runs, and every pull request runs the jobs its changed areas select, per `litespec/quality/ci#run-applicable-gates-on-every-pull-request`. The `model` job runs `litespec model check`, is selected by a record, code, or test change, and runs for small and large projects alike. The `docs` job runs `litespec model check` on a documentation change to check pages, configuration, language, and prose, with the output shown above. Every pull request also publishes a book preview, per `litespec/ext/delivery#publish-a-book-preview-for-every-change`, so reviewers read the change in product words. The `release` job reruns every job on the release commit and promotes the artifact that passed.
