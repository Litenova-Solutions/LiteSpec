# Modules and Records

## Intent

The Spending module turns the Intent of [The Claimwick Product](the-product.md) into Behavior: the aggregates that hold each expense, policy and receipt, the rules that guard them, the use cases that change them, and the entry point, screen, reaction, job, notification and setting around them. This page lists those records as they are written in `examples/claimwick/.litespec/behavior/modules/spending/`, with the counts and IDs a reader can check in the folder.

## The Aggregates

An aggregate is one thing the module keeps true. Spending has four, each in its own folder under `behavior/modules/spending/`:

| Aggregate | File | Kind | States |
|:---|:---|:---|:---|
| Expense | `expenses/aggregate.yaml` | entity | Submitted, Approved, Refused, Reimbursed, Erased |
| Receipt | `receipts/aggregate.yaml` | entity | Attached |
| Policy | `policies/aggregate.yaml` | entity | Active |
| Suppression List | `suppression-lists/aggregate.yaml` | store | None; owned by `aggregate/spending.policy` |

The Expense aggregate file shows the pattern every aggregate follows: its states, its transitions with the use case that moves each one, its data with a classification, its data classes with their retention, and its domain events:

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
- name: Erased
  meaning: The personal data of the expense was erased after its retention.
transitions:
- from: "-"
  to: Submitted
  movedBy: use-case/spending.submit-expense
- from: Submitted
  to: Approved
  movedBy: use-case/spending.approve-expense
```

The `dataClasses` list of the same file gives each piece of data its retention. The `submitter-contact` class holds the employee who submitted the expense, is personal, and carries its period of seven years from `domain-event/expense.reimbursed`, its basis in `requirement/approval.erased-after-retention`, its purpose, and its enforcement by `job/spending.erase-expired-personal-data`, which pseudonymizes the employee on erasure. The `expense-ledger` class holds the amount, receipt and state, is kept for seven years from the same event under `constraint/tax-receipt-retention`, and is retained. The bare business facts stay for the tax file.

The five domain events the aggregate raises are `domain-event/expense.submitted`, `domain-event/expense.approved`, `domain-event/expense.refused`, `domain-event/expense.reimbursed` and `domain-event/expense.personal-data-erased`. The receipt aggregate raises `domain-event/receipt.attached`.

## The Rules

A rule states one condition the module enforces. The Spending module has twelve, in four rule-set files:

| Rule-set file | Rules |
|:---|:---|
| `expenses/rules/expense-rules.yaml` | 7 |
| `receipts/rules/receipt-rules.yaml` | 1 |
| `rules/spending-rules.yaml` | 3 |
| `policies/rules/policy-rules.yaml` | 1 |

The first rule in the expense file shows its shape. It names its kind, the use cases it governs, the refusal code it returns, and the requirement it fulfills:

```yaml
- type: rule
  id: rule/expense.receipt-required-above-threshold
  title: Receipt Required Above Threshold
  kind: precondition
  statement: An expense above the policy threshold needs a receipt.
  governs:
  - use-case/spending.submit-expense
  refusal: spending.receipt-missing
  basedOn: []
  fulfills:
  - requirement/approval.above-threshold-receipt
```

The other expense rules hold that an approver is not the submitter, that an expense is reimbursed once and only after approval, that an expense is decided only from Submitted, that an export carries only approved expenses, and that personal data is erased after its retention. A rule whose `governs` names more than one aggregate names every governed aggregate in `binds`: `rule/spending.receipt-filed-once` binds `aggregate/spending.expense` and `aggregate/spending.receipt`. Each rule names a refusal code from the module's refusal set in `behavior/modules/spending/refusals.yaml`, and each refusal result carries one of those codes.

## The Use Cases

A use case is one thing an actor does through the system. The module has nine, each in a file of its own. Every use case names its grant and the entry point that reaches it. Most also name the flow step they serve, and the erasure use case serves none, because a scheduled promise is a requirement that a job fulfills:

| Use case | Kind | Grant | Serves | Raises |
|:---|:---|:---|:---|:---|
| Submit expense | command | `permission/request-expense` | `flow/submit-and-reimburse#s1` | `domain-event/expense.submitted` |
| Attach receipt | command | `permission/request-expense` | `flow/submit-and-reimburse#s2` | `domain-event/receipt.attached` |
| Approve expense | command | `permission/approve-expense` | `flow/submit-and-reimburse#s3` | `domain-event/expense.approved` |
| Refuse expense | command | `permission/approve-expense` | `flow/submit-and-reimburse#s3` | `domain-event/expense.refused` |
| Reimburse expense | command | `permission/post-reimbursement` | `flow/submit-and-reimburse#s4` | `domain-event/expense.reimbursed` |
| List expenses | query | `permission/list-expenses` | `flow/submit-and-reimburse#s5` | none |
| Change receipt threshold | command | `permission/manage-policy` | `flow/set-receipt-threshold#s1` | none |
| Export expenses | query | `permission/export-expenses` | `flow/export-approved-expenses#s1` | none |
| Erase personal data | command | `permission/remove-personal-data` | none | `domain-event/expense.personal-data-erased` |

The Submit expense file lists its inputs, its results and its scenarios. It takes an amount, a currency and an optional receipt, and it has two results: `result/submit-expense.submitted`, and the refusal `result/submit-expense.receipt-missing` with the code `spending.receipt-missing`. Its two scenarios are the ones the acceptance tests prove, as [A Use Case in Code](a-use-case-in-code.md) shows.

The flow steps run in the order the flow file gives them. Submit expense serves `flow/submit-and-reimburse#s1` and Attach receipt serves `flow/submit-and-reimburse#s2`; the flow file names no use case, so each use case points up to the step it serves.

## The Entry Point and the Screen

The product reaches the module through one entry point, `behavior/entry-points/claimwick-app/entry-point.yaml`, which the employee signs into with a session. The entry point holds three screens. The first, in `screens/claimwick-app.expense-list/claimwick-app.expense-list.yaml`, is the list:

```yaml
type: screen
id: screen/claimwick-app.expense-list
title: Expense List
summary: An employee sees their own expenses, newest first, with the amount and the latest state of each.
address: /expenses
offers:
- use-case/spending.list-expenses
serves:
- flow/submit-and-reimburse#s5
```

The list offers only List expenses. The record at `/expenses/{expenseId}` offers Approve, Attach receipt, Erase personal data, Refuse and Reimburse expense, and the new-expense form at `/expenses/new` offers Submit expense. Export expenses, Change receipt threshold and List reimbursed expenses are reached through the entry point but have no screen yet, which the waivers in [The Model and Its Checks](model-and-checks.md) account for. Which floorplan each route composes lives in the frontend design contract, not on the record.

## The Reaction, Job, Notification and Settings

Four records act outside a single request:

- The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` is triggered by `domain-event/expense.submitted`. A submitted expense that waits past three business days without a decision earns one reminder to its approver. The reaction declares its outcomes, its failure handling and its idempotency. It carries no `compensates`, because it undoes no committed step.
- The notification `notification/spending.approval-reminder` is the reminder itself. It names the manager as its audience, email as its channel, the external system that delivers it, and the rule that requires consent, once per expense.
- The job `job/spending.erase-expired-personal-data` runs every day in the Europe/Amsterdam timezone. It erases the personal data of decided expenses past their retention. It declares its outcomes, its failure handling and its idempotency, and it sends `use-case/spending.erase-personal-data` once for each selected expense, asynchronously.
- The settings file holds two values. `setting/spending.receipt-threshold` defaults to EUR 25, its range is EUR 0 to EUR 500, and the product sets it. `setting/spending.approval-reminder-delay` defaults to 72 hours, its range is 1 to 120 hours, and the operator sets it through the configuration key `Spending:ApprovalReminderDelay`.

## The Permissions and the Access Chain

The permission set in `behavior/modules/spending/permissions.yaml` names seven permissions. Two carry a risk: `permission/post-reimbursement` carries `money`, and `permission/remove-personal-data` carries `personal-data`. The roles allow these permissions: the employee role allows submitting and listing, the approver role allows approving, listing and managing the policy, and the finance role allows reimbursing, listing, exporting and erasing, including the reimbursed-expenses list, which the reporting module names in `permission/list-reimbursed-expenses`. The chain runs from the actor to the role, the role to the permission, and the permission to the use case it grants.

## The Reporting Module

The reporting module is the second module of the product. It lists the reimbursed expenses for finance, and it reads no table of the spending module. Its record declares the dependency on spending:

```yaml
type: module
id: module/reporting
title: Reporting
summary: Reporting keeps the reimbursed expenses and lists them for finance.
dependsOn:
- module/spending
```

The module keeps one row for each reimbursed expense in `read-model/reporting.reimbursed-expense`. The read model is keyed by `aggregate/spending.expense`. Each of its four fields takes its value from one payload field of `integration-event/expense-reimbursed`, and the employee field is personal. The employee field names `integration-event/expense-personal-data-erased` as the event that erases it, so the row keeps the amount and the reimbursement time when the personal data is erased. The read model declares freshness `{mode: snapshot, within: PT5S, decidedBy: use-case/spending.reimburse-expense}`: a reader sees a row at most five seconds old, and the reimburse command decides each figure it holds.

The query `use-case/reporting.list-reimbursed-expenses` reads the read model. Its grant is `permission/list-reimbursed-expenses`, which the permission set tags as personal data, because each row names the employee who was paid. Its output group carries the same freshness with the same `decidedBy`. The query fulfills `requirement/approval.reimbursed-expenses-listed`, and the finance role allows it.

## The External Systems

The module talks to three external systems in `behavior/modules/spending/external-systems/`: the event bus, which carries the module's integration events to the modules that read them; the mail relay, which delivers the approval emails to managers; and the payout bank in `payout-bank/payout-bank.yaml`, which moves reimbursed money to employees. Claimwick sends one bank file per payout batch and reads back a settlement report. When the bank rejects a file, finance fixes the entry and sends the batch again, which the runbook `implementation/modules/spending/runbooks/clear-stuck-reimbursements/clear-stuck-reimbursements.yaml` describes.

## Counting the Records

Every count on these pages comes from the folder, not from memory. The Spending module holds 4 aggregates, 12 rules, 9 use cases, 2 settings, 1 job, 1 reaction, 1 notification and 3 integration events. The Reporting module holds 1 read model and 1 use case. The entry point holds 3 screens, and the module talks to 3 external systems. The product holds 3 actors, 3 roles and 3 flows. Run `dotnet run --project apps/cli/src/LiteSpec.Cli -- model check --root examples/claimwick` to see the model's view of the same records, and [The Model and Its Checks](model-and-checks.md) shows that output.
