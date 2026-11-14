# Modules and Records

## Intent

The Spending module turns the Intent of [The Claimwick Product](the-product.md) into Behavior: the aggregates that hold each expense, policy and receipt, the rules that guard them, the use cases that change them, and the entry point, screen, reaction, job, notification and setting around them. This page lists those records as they are written in `examples/claimwick/.litespec/behavior/modules/spending/`, with the counts and IDs a reader can check in the folder.

## The Aggregates

An aggregate is one thing the module keeps true. Spending has three, each in its own folder under `behavior/modules/spending/`:

| Aggregate | File | Kind | States |
|:---|:---|:---|:---|
| Expense | `expenses/aggregate.yaml` | entity | Submitted, Approved, Refused, Reimbursed |
| Receipt | `receipts/aggregate.yaml` | entity | Attached |
| Policy | `policies/aggregate.yaml` | entity | Active |

The Expense aggregate file shows the pattern every aggregate follows: its states, its transitions with the use case that moves each one, its data with a classification, its data classes with their retention, and its events:

```yaml
type: aggregate
id: aggregate/spending.expense
title: Expense
summary: An expense is one work cost an employee submitted.
kind: entity
states:
- name: Submitted
  meaning: The expense waits for a decision.
- name: Approved
  meaning: A manager approved the expense.
- name: Refused
  meaning: A manager refused the expense.
- name: Reimbursed
  meaning: The reimbursed state of an expense whose payout reached the submitter.
transitions:
- from: "-"
  to: Submitted
  by: use-case/spending.submit-expense
- from: Submitted
  to: Approved
  by: use-case/spending.approve-expense
```

The `dataClasses` list of the same file gives each piece of data its retention. The `submitter-contact` class holds the employee who submitted the expense, is personal, and is erased by `job/spending.erase-expired-personal-data` after seven years. The `expense-ledger` class holds the amount, receipt and state, is kept for seven years under a statute, and is never erased. The bare business facts stay for the tax file.

The four events the aggregate raises are `event/expense.submitted`, `event/expense.approved`, `event/expense.refused` and `event/expense.reimbursed`. The receipt aggregate raises `event/receipt.attached`.

## The Rules

A rule states one condition the module enforces. The Spending module has nine, in four rule-set files:

| Rule-set file | Rules |
|:---|:---|
| `expenses/rules/expense-rules.yaml` | 6 |
| `receipts/rules/receipt-rules.yaml` | 1 |
| `rules/spending-rules.yaml` | 1 |
| `policies/rules/policy-rules.yaml` | 1 |

The first rule in the expense file shows its shape. It names its kind, the use cases it governs, the refusal it returns, and the requirement it fulfills:

```yaml
- type: rule
  id: rule/expense.receipt-required-above-threshold
  title: Receipt Required Above Threshold
  kind: precondition
  statement: An expense above the policy threshold needs a receipt.
  governs:
  - use-case/spending.submit-expense
  refusal: result/submit-expense.receipt-missing
  basedOn: []
  fulfills:
  - requirement/approval.above-threshold-receipt
```

The other expense rules hold that an approver is not the submitter, that an expense is reimbursed once and only after approval, that an expense is decided only from Submitted, and that an export carries only approved expenses. Each rule has a refusal result with a stable code.

## The Use Cases

A use case is one thing an actor does through the system. The module has nine, each in a file of its own. Every use case names its grant and the entry point that reaches it. Most also name the journey step they serve, and the erasure use case serves none, because a scheduled promise is a requirement that a job fulfills:

| Use case | Kind | Grant | Serves | Raises |
|:---|:---|:---|:---|:---|
| Submit expense | command | `permission/request-expense` | `journey/submit-and-reimburse#s1` | `event/expense.submitted` |
| Attach receipt | command | `permission/request-expense` | `journey/submit-and-reimburse#s2` | `event/receipt.attached` |
| Approve expense | command | `permission/approve-expense` | `journey/submit-and-reimburse#s3` | `event/expense.approved` |
| Refuse expense | command | `permission/approve-expense` | `journey/submit-and-reimburse#s3` | `event/expense.refused` |
| Reimburse expense | command | `permission/post-reimbursement` | `journey/submit-and-reimburse#s4` | `event/expense.reimbursed` |
| List expenses | query | `permission/list-expenses` | `journey/submit-and-reimburse#s5` | none |
| Change receipt threshold | command | `permission/manage-policy` | `journey/set-receipt-threshold#s1` | none |
| Export expenses | query | `permission/export-expenses` | `journey/export-approved-expenses#s1` | none |
| Erase personal data | command | `permission/remove-personal-data` | none | none |

The Submit expense file lists its inputs, its results and its scenarios. It takes an amount, a currency and an optional receipt, and it has two results: `result/submit-expense.submitted`, and the refusal `result/submit-expense.receipt-missing` with the code `spending.receipt-missing`. Its two scenarios are the ones the acceptance tests prove, as [A Use Case in Code](a-use-case-in-code.md) shows.

The journey steps run in the order the journey file gives them. Submit expense serves `journey/submit-and-reimburse#s1` and Attach receipt serves `journey/submit-and-reimburse#s2`; the journey file names no use case, so each use case points up to the step it serves.

## The Entry Point and the Screen

The product reaches the module through one entry point, `behavior/entry-points/claimwick-app/entry-point.yaml`, which the employee signs into with a session. The entry point holds one screen in `screens/claimwick-app.expense-list/claimwick-app.expense-list.yaml`:

```yaml
type: screen
id: screen/claimwick-app.expense-list
title: Expense List
summary: An employee sees their own expenses, newest first, with the amount and the latest state of each.
address: /expenses
offers:
- use-case/spending.list-expenses
serves:
- journey/submit-and-reimburse#s5
floorplan: list-page
```

The screen offers only List expenses. The other eight use cases are reached through the entry point but have no screen yet, which the departures in [The Model and Its Checks](model-and-checks.md) account for.

## The Reaction, Job, Notification and Settings

Four records act outside a single request:

- The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` is triggered by `event/expense.submitted`. A submitted expense that waits past three business days without a decision earns one reminder to its approver.
- The notification `notification/spending.approval-reminder` is the reminder itself. It goes by email to a manager, once per expense, using the template `approval-reminder-manager`.
- The job `job/spending.erase-expired-personal-data` runs every day in the Europe/Amsterdam timezone. It erases the personal data of decided expenses past their retention, and it runs `use-case/spending.erase-personal-data` asynchronously.
- The settings file holds two values. `setting/spending.receipt-threshold` defaults to EUR 25, its range is EUR 0 to EUR 500, and the product sets it. `setting/spending.approval-reminder-delay` defaults to 48 hours, its range is 1 to 120 hours, and the operator sets it through the configuration key `Spending:ApprovalReminderDelay`.

## The Permissions and the Access Chain

The permission list in `behavior/modules/spending/permissions.yaml` names seven permissions. Two carry a risk: `permission/post-reimbursement` carries `money`, and `permission/remove-personal-data` carries `personal-data`. The roles allow these permissions: the employee role allows submitting and listing, the approver role allows approving and managing the policy, and the finance role allows reimbursing, exporting and erasing. The chain runs from the actor to the role, the role to the permission, and the permission to the use case it grants.

## The External System

The payout bank is the one external system the module talks to. It is in `behavior/external-systems/payout-bank/payout-bank.yaml`. Claimwick sends one bank file per payout batch and reads back a settlement report. When the bank rejects a file, finance fixes the entry and sends the batch again, which the runbook `implementation/modules/spending/runbooks/clear-stuck-reimbursements/clear-stuck-reimbursements.yaml` describes.

## Counting the Records

Every count on these pages comes from the folder, not from memory. The Spending module holds 3 aggregates, 9 rules, 9 use cases, 2 settings, 1 job, 1 reaction and 1 notification. The entry point holds 1 screen, and the payout bank is the one external system. The product holds 3 actors, 3 roles and 3 journeys. Run `dotnet run --project apps/cli/src/LiteSpec.Cli -- model check --root examples/claimwick` to see the model's view of the same records, and [The Model and Its Checks](model-and-checks.md) shows that output.
