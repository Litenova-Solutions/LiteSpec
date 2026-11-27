# Modules and Records

Intent says what Claimwick promises; Behavior says how the product keeps those promises. The Spending module holds the Behavior side: the aggregates that hold each expense, policy, receipt, and reminder preference, the rules that guard the aggregates, the use cases that change them, and the entry point, screen, reaction, job, notification, and setting around them. This page lists those records as written in `examples/claimwick/.litespec/behavior/modules/spending/`, with the counts and IDs you can check in the folder. For the Intent they answer, see [The Claimwick Product](the-product.md).

## The Aggregates

An aggregate is one thing the module keeps true, and Spending has four of them, each in its own folder under `behavior/modules/spending/`:

| Aggregate | File | States |
|:---|:---|:---|
| Expense | `expenses/aggregate.yaml` | Submitted, Approved, Refused, Reimbursed, Erased |
| Receipt | `receipts/aggregate.yaml` | Attached |
| Policy | `policies/aggregate.yaml` | Active |
| Reminder preference | `reminder-preferences/aggregate.yaml` | Active |

The Expense aggregate file shows the pattern every aggregate follows. It holds its states, its transitions with the use case that moves each one, its data with a classification, and its data classes with their retention:

```yaml
type: aggregate
id: aggregate/spending.expense
title: Expense
summary: An expense is one work cost an employee submitted.
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

The `dataClasses` list of the same file gives each piece of data its retention. The `submitter-contact` class holds the employee who submitted the expense and is personal. It carries its period of seven years from `domain-event/expense.reimbursed`, its basis in `requirement/approval.erased-after-retention`, its purpose, and its enforcement by `job/spending.erase-expired-personal-data`, which pseudonymizes the employee on erasure.

The `expense-ledger` class holds the amount, receipt, and state, is kept for seven years from the same event under `constraint/tax-receipt-retention`, and is retained, so the bare business facts stay for the tax file.

The aggregate raises five domain events: `domain-event/expense.submitted`, `domain-event/expense.approved`, `domain-event/expense.refused`, `domain-event/expense.reimbursed`, and `domain-event/expense.personal-data-erased`. The receipt aggregate raises `domain-event/receipt.attached`. Each event is its own record file under `expenses/domain-events/<name>/<name>.yaml`, with its explanation file beside it.

The fourth aggregate is the reminder preference in `reminder-preferences/aggregate.yaml`: one per manager, with the single state `Active`. Its `preference-owner` class holds the manager and is personal, and its `preference-record` class holds the reason and the update time. Both classes erase when the manager asks for reminders again, enforced by the use case that changes the preference.

## The Rules

A rule states one condition the module enforces, and the Spending module has thirteen of them, each in its own folder under `rules/`. The expense aggregate's rules sit at `expenses/rules/<name>/<name>.yaml`, such as `expenses/rules/reimbursed-once/reimbursed-once.yaml`:

| Home | Rules |
|:---|:---|
| `expenses/rules/` | 7 |
| `receipts/rules/` | 1 |
| `rules/` (module level) | 3 |
| `policies/rules/` | 1 |
| `reminder-preferences/rules/` | 1 |

The rule in `expenses/rules/receipt-required-above-threshold/receipt-required-above-threshold.yaml` shows the shape. It names its kind, the use case and aggregate it governs, the refusal id it returns, and the requirement it fulfills:

```yaml
type: rule
id: rule/expense.receipt-required-above-threshold
title: Receipt Required Above Threshold
kind: precondition
statement: An expense above the policy threshold needs a receipt.
governs:
- use-case/spending.submit-expense
- aggregate/spending.expense
refusal: refusal/spending.receipt-missing
basedOn: []
fulfills:
- requirement/approval.above-threshold-receipt
```

The other expense rules hold that an approver is not the submitter, that an expense is reimbursed once and only after approval, that an expense is decided only from Submitted, that an export carries only approved expenses, and that personal data is erased after its retention.

A rule whose `governs` names more than one aggregate names every governed aggregate in `binds`, so `rule/spending.receipt-filed-once` binds `aggregate/spending.expense` and `aggregate/spending.receipt`. The reminder-preference rule holds that only the manager the preference belongs to, or finance, changes the preference, refusing anyone else with `spending.not-preference-owner`. Each rule names a refusal id from its aggregate's refusal set, and each refusal result carries one of those ids.

## The Use Cases

A use case is one thing an actor does through the system. The module has ten, each in a file of its own.

Every use case names its grant and the entry point that reaches it, and most name the flow step they serve. The erasure use case serves none, because a scheduled promise is a requirement that a job fulfills:

| Use case | Kind | Grant | Serves | Raises |
|:---|:---|:---|:---|:---|
| Submit expense | command | `permission/request-expense` | `step/submit-and-reimburse.submit-expense` | `domain-event/expense.submitted` |
| Attach receipt | command | `permission/request-expense` | `step/submit-and-reimburse.attach-receipt` | `domain-event/receipt.attached` |
| Approve expense | command | `permission/approve-expense` | `step/submit-and-reimburse.decide-expense` | `domain-event/expense.approved` |
| Refuse expense | command | `permission/approve-expense` | `step/submit-and-reimburse.decide-expense` | `domain-event/expense.refused` |
| Reimburse expense | command | `permission/post-reimbursement` | `step/submit-and-reimburse.reimburse-expense` | `domain-event/expense.reimbursed` |
| List expenses | query | `permission/list-expenses` | `step/submit-and-reimburse.watch-expenses` | none |
| Change receipt threshold | command | `permission/manage-policy` | `step/set-receipt-threshold.set-threshold` | none |
| Change reminder preference | command | `permission/manage-reminder-preference` | `step/submit-and-reimburse.set-reminder-preference` | none |
| Export expenses | query | `permission/export-expenses` | `step/export-approved-expenses.export-batch` | none |
| Erase personal data | command | `permission/remove-personal-data` | none | `domain-event/expense.personal-data-erased` |

The Submit expense file lists its inputs, its results, and its scenarios. It takes a submission identifier, an amount, a currency, and an optional receipt. It has two results: `result/submit-expense.submitted`, and the refusal `result/submit-expense.receipt-missing` with the refusal `refusal/spending.receipt-missing`. Its two scenarios are the ones the acceptance tests prove, as [A Use Case in Code](a-use-case-in-code.md) shows.

The flow steps run in the order the flow file gives them. Submit expense serves `step/submit-and-reimburse.submit-expense`, and Attach receipt serves `step/submit-and-reimburse.attach-receipt`. The flow file lists no use case; each use case states the step it serves.

## The Entry Point and the Screen

The product reaches the module through one entry point, `behavior/entry-points/claimwick-app/entry-point.yaml`, which the employee signs into with a session. The entry point holds three screens, and the first is the list, in `screens/claimwick-app.expense-list/claimwick-app.expense-list.yaml`:

```yaml
type: screen
id: screen/claimwick-app.expense-list
title: Expense List
summary: An employee sees their own expenses, newest first, with the amount and the latest state of each.
route: /expenses
offers:
- use-case/spending.list-expenses
serves:
- step/submit-and-reimburse.watch-expenses
pageFrame:
  pattern: list-page
  option: search-and-a-state-filter-only
leadsTo:
- screen/claimwick-app.expense-record
- screen/claimwick-app.new-expense
```

The list offers only List expenses. The record at `/expenses/{expenseId}` offers Approve, Attach receipt, Erase personal data, Refuse, and Reimburse expense, and the new-expense form at `/expenses/new` offers Submit expense.

Export expenses, Change receipt threshold, Change reminder preference, and List reimbursed expenses are reached through the entry point but have no screen yet; the waivers in [The Model and Its Checks](model-and-checks.md) account for them.

The record names its page frame and the chosen option, and the frontend design contract maps each frame to the floorplan component that implements it.

## The Reaction, Job, Notification and Settings

Four records act outside a single request:

- The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` answers `domain-event/expense.submitted`: a submitted expense that waits past the reminder delay without a decision earns one reminder to its approver. The reaction declares its outcomes, its failure handling, and its idempotency, and it carries no `compensates`, because it undoes no committed step.
- The notification `notification/spending.approval-reminder` is the reminder itself. It names the manager as its audience, email as its channel, the external system that delivers it, and the rule that governs consent, once per expense.
- The job `job/spending.erase-expired-personal-data` runs every day in the Europe/Amsterdam timezone and erases the personal data of decided expenses past their retention. It declares its outcomes, its failure handling, and its idempotency, and it runs `use-case/spending.erase-personal-data` once for each selected expense, asynchronously.
- The setting `setting/spending.approval-reminder-delay` lives in `settings/approval-reminder-delay/approval-reminder-delay.yaml`: it defaults to 72 hours, ranges from 1 to 120 hours, and the operator sets it through the configuration path `Spending:ApprovalReminderDelay`. The receipt threshold is not a setting: the policy aggregate holds it, and a manager changes it with Change receipt threshold.

## The Permissions and the Access Chain

The permission set in `behavior/modules/spending/permissions.yaml` names eight permissions, and two carry a risk: `permission/post-reimbursement` carries `money`, and `permission/remove-personal-data` carries `personal-data`.

Each permission names the roles that hold it in `grantedTo`. Submitting and listing name the employee role, approving, listing, managing the policy, and managing reminder preferences name the approver role, and reimbursing, listing, exporting, erasing, and managing reminder preferences name the finance role, including the reimbursed-expenses list, which the reporting module names in `permission/list-reimbursed-expenses`. The chain runs from the actor to the role, the role to the permission, and the permission to the use case it grants.

## The Reporting Module

The reporting module is the second module of the product. It lists the reimbursed expenses for finance without reading any table of the spending module, and its record declares the dependency on spending:

```yaml
type: module
id: module/reporting
title: Reporting
summary: Reporting keeps the reimbursed expenses and lists them for finance.
dependsOn:
- module/spending
```

The module keeps one row for each reimbursed expense in `read-model/reporting.reimbursed-expense`, keyed by `aggregate/spending.expense`. Each of its four fields takes its value from one published field of `module-event/spending.expense-reimbursed`, and the employee field is personal. The employee field names `module-event/spending.expense-personal-data-erased` as the event that erases it, so the row keeps the amount and the reimbursement time when the personal data is erased. Each module event travels in process through the transactional outbox, with no external transport.

The read model declares freshness `{mode: snapshot, within: PT5S, decidedBy: use-case/spending.reimburse-expense}`, so a reader sees a row at most five seconds old, and the reimburse command decides each figure it holds.

The query `use-case/reporting.list-reimbursed-expenses` reads the read model under the grant `permission/list-reimbursed-expenses`, which the permission set tags as personal data, because each row names the employee who was paid. Its output group carries the same freshness with the same `decidedBy`. The query fulfills `requirement/approval.reimbursed-expenses-listed`, and the permission names the finance role in `grantedTo`.

## The External Systems

The module talks to three external systems in `behavior/modules/spending/external-systems/`: the event bus, which carries the module's integration event out of the product; the mail relay, which delivers the approval emails to managers; and the payout bank in `payout-bank/payout-bank.yaml`, which moves reimbursed money to employees.

Claimwick sends one bank file per payout batch and reads back a settlement report. When a reimbursement stays queued past one business day, the operator lists the queued reimbursements and retries each one, as the runbook `implementation/modules/spending/runbooks/clear-stuck-reimbursements/clear-stuck-reimbursements.yaml` describes.

## Counting the Records

Every count on these pages comes from the folder, not from memory. The Spending module holds 4 aggregates, 13 rules, 10 use cases, 1 setting, 1 job, 1 reaction, 1 notification, 2 module events, and 1 integration event. The Reporting module holds 1 read model and 1 use case. The entry point holds 3 screens, and the module talks to 3 external systems. The product holds 3 actors, 3 roles, and 3 flows.

Run `dotnet run --project apps/cli/src/LiteSpec.Cli -- model check --root examples/claimwick` to see the model's view of the same records; [The Model and Its Checks](model-and-checks.md) shows that output.
