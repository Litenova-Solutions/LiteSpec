# The Claimwick Product

Claimwick is the product this example follows: employees submit work expenses with receipts, managers approve or refuse them, and finance reimburses the approved ones. Claimwick keeps its promises as Intent records: a product brief, a goal, a problem, two features with their requirements, one piece of research, one decision, one constraint, and three flows. This page shows them as written in `examples/claimwick/.litespec/`, from the product brief down to the flow that ties them together, so you can see what each record type holds and how the records point at each other. Every record, field, ID, and count below is copied from that folder, so you can open the same file and see the same text.

## The Product Brief

A product record states what is built and for whom. The Claimwick brief names its boundary, the actors it addresses, and the outcome, in one paragraph. Its file is `.litespec/product.yaml`:

```yaml
type: product
id: product/claimwick
title: Claimwick
summary: |-
  The company submits work expenses with receipts.

  Employees submit and attach. Managers approve and reject. Finance reimburses.
boundary: Claimwick submits, approves, and reimburses work expenses with receipts.
addresses:
- actor/employee
- actor/manager
- actor/finance
```

The `addresses` list names the three actors that hold the product's roles. The actors live in `.litespec/intent/actors/`, and their roles live in `.litespec/intent/roles/`. A `brand` block after `addresses` states the voice the screens write in: calm, plain, and exact, with the rules each screen follows.

## The Spending Module

Claimwick has two modules, Spending and Reporting. A module record names one product area and the records that belong to it. The Spending module is declared in Behavior, beside the aggregates it groups, at `.litespec/behavior/modules/spending/module.yaml`:

```yaml
type: module
id: module/spending
title: Spending
summary: Spending submits, decides, reimburses and exports work expenses.
dependsOn: []
```

The empty `dependsOn` says Spending stands alone; Reporting names Spending there, as [Modules and Records](modules-and-records.md) shows. The one problem, `problem/expenses-wait-weeks`, sits at product level, and the two features stand alone in `.litespec/intent/features/`, each naming the problem in `solves`. Each requirement sits in its own folder inside its feature's `requirements/` folder. The goal, research, decision, and constraint records sit at product level under `.litespec/intent/`, where the folder rules allow them. The module's Behavior records sit under `.litespec/behavior/modules/spending/`.

## The Goal

A goal record states one measurable outcome that features advance. Claimwick keeps one, in `.litespec/intent/goals/decided-in-days/decided-in-days.yaml`:

```yaml
type: goal
id: goal/decided-in-days
title: Decided in Days
summary: Employees plan around reimbursements, and a late decision holds their money. A fast decision keeps trust in the whole flow.
statement: Every submitted expense is decided within five business days.
measure: Median hours from submission to decision.
target: Under 120 hours.
```

The `statement` names the outcome, and `measure` says how the team reads it. The goal carries no authority of its own: features and flows point at it.

## The Problem

A problem record states one pain that someone outside the team feels today. Claimwick keeps one, in `.litespec/intent/problems/expenses-wait-weeks/expenses-wait-weeks.yaml`: submitted expenses wait weeks for a decision, so employees chase managers by email. The `affects` field names the employee as the person who suffers it, and the `evidence` list cites the research below with its kind, `reported`.

## The Features and Their Requirements

A feature record states what the product offers against a problem. Claimwick has two.

The Approval feature, in `.litespec/intent/features/approval/approval.yaml`, solves the waiting problem with six functional requirements, each in its own folder inside the feature's `requirements/` folder:

| Requirement ID | Statement |
|:---|:---|
| `requirement/approval.decided-within-five-days` | Each submitted expense is decided within five business days. |
| `requirement/approval.above-threshold-receipt` | An expense above the receipt threshold carries a receipt. |
| `requirement/approval.nobody-approves-own-expense` | Nobody approves their own expense. |
| `requirement/approval.expense-reimbursed-at-most-once` | An approved expense is reimbursed at most once. |
| `requirement/approval.erased-after-retention` | The personal data of a decided expense is erased after its retention. |
| `requirement/approval.reimbursed-expenses-listed` | Finance lists each reimbursed expense with the employee it was paid to. |

The Approval Reminders feature, in `.litespec/intent/features/approval-reminders/approval-reminders.yaml`, advances the five-day goal and solves the same problem. Its offering is the reminder itself: a manager gets a reminder for each expense that waits past three business days without a decision. It holds two requirements: the functional requirement `requirement/approval-reminders.remind-approver-after-three-days`, which states that the product sends one reminder for each expense that waits past three business days without a decision, and the quality requirement `requirement/approval-reminders.month-end-submission`, in `.litespec/intent/features/approval-reminders/requirements/month-end-submission/`, which states that each month-end submission is acknowledged within two seconds. The quality requirement names a check, `month-end-load`, that the model reads from `.litespec/proof/`.

A functional requirement carries its identifier, title, kind, statement, rationale, and the records it cites in `cites`. A quality requirement carries stimulus, environment, response, and measure beside its rationale. The `cites` list is how the tax file traces back to the constraint below.

## The Research

A research record states one outside fact that informs a choice. Claimwick keeps one, in `.litespec/intent/research/approval-time-benchmarks/approval-time-benchmarks.yaml`. Its sources carry a `key`, a `url`, and the day they were read, and the explanation cites each key:

```yaml
type: research
id: research/approval-time-benchmarks
title: Approval Time Benchmarks
summary: Small companies decide expenses within one week. The study behind this answer is small but recent.
sources:
- url: https://example.com/expense-study
  readOn: 2026-02-10
  key: example-expense-study
question: How fast do small companies decide expenses?
conclusion: According to one small recent industry study, small companies decide expenses within one week.
```

The five-day requirement leans on this research without being decided by it. Its explanation file, `.litespec/intent/research/approval-time-benchmarks/approval-time-benchmarks.md`, carries the required Findings section, with Open Questions and Source Notes beside it. Each finding cites its source with a reference link, and says whether the source states it or the writer infers it.

## The Decision

A decision record states one choice with the options it weighed. Claimwick set the receipt threshold at 25 EUR, recorded in `.litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.yaml`. The record weighs three options with pros and cons each, and its `decides` field names the receipt requirement the choice settles:

```yaml
type: decision
id: decision/0001-receipt-threshold
title: Receipt Threshold
summary: Small expenses need no proof, large ones do.
options:
- name: 0 EUR
  pros: Every expense carries proof.
  cons: Tiny expenses cost more to file than they pay.
- name: 25 EUR
  pros: Small expenses stay light to submit.
  cons: Some fraud slips under the line.
- name: 75 EUR
  pros: Almost nothing needs a receipt.
  cons: Finance cannot defend the tax file.
choice: 25 EUR
consequences: Small expenses stay light to submit.
reopenedWhen: Finance sees abuse or friction.
decides:
- requirement/approval.above-threshold-receipt
basedOn: []
```

The explanation file, `.litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.md`, holds the Context, Options, Decision, Consequences, and Revisit Conditions sections, with Evidence optional. The `reopenedWhen` field states what would make the team reopen the choice.

## The Constraint

A constraint states one outside rule, the source it rests on, and whether it applies to the product. Claimwick answers the national tax law on receipts in `.litespec/intent/constraints/tax-receipt-retention/tax-receipt-retention.yaml`: the law keeps business receipts for seven years. The record's `sources` list names the law with its key, its URL, and the day it was read, and the rule applies because the product stores receipts for reimbursed expenses.

The explanation file holds Scope, Rules, and Effect on the Product. Its Rules paragraph cites the law by its key as a reference link, and `model render` writes that link's definition into the file. Its Effect on the Product names the two requirements that carry the rule's effect, `requirement/approval.above-threshold-receipt` and `requirement/approval.erased-after-retention`. Each of them cites this constraint back, so the retention rule traces to the outside law that demands it.

## The Flow

A flow record holds the steps one actor takes toward one goal or against one problem. Claimwick keeps three flows, one per path. This one, in `.litespec/intent/flows/submit-and-reimburse/submit-and-reimburse.yaml`, is the employee's path; the two staff flows, `flow/export-approved-expenses` and `flow/set-receipt-threshold`, sit beside it. An employee submits an expense, a manager decides it, finance reimburses it, and the employee sees it in their own list. The flow starts when the employee has paid a work cost and ends when the employee holds the reimbursement. Its steps carry IDs `s1` to `s5`, each names the actor who takes it, and a branch ends the flow when the manager rejects the expense:

```yaml
type: flow
id: flow/submit-and-reimburse
title: Submit and Reimburse
actor: actor/employee
summary: An employee who paid a work cost submits the expense and waits for a decision. The flow starts at the paid cost and ends when the reimbursement lands in the employee account. A good flow decides each expense within five business days, because the employee plans around the money.
serves:
- goal/decided-in-days
startedWhen: The employee has paid a work cost and has not yet submitted it.
finishedWhen: The employee holds the reimbursement.
steps:
- id: s1
  statement: The employee submits the expense with its amount and currency.
  actor: actor/employee
  cites:
  - requirement/approval.above-threshold-receipt
  - requirement/approval-reminders.month-end-submission
- id: s2
  statement: The employee attaches the receipt to the expense.
  actor: actor/employee
  cites:
  - requirement/approval.above-threshold-receipt
- id: s3
  statement: The manager approves or rejects the expense.
  actor: actor/manager
  failure: The expense waits unseen, and the approver gets a reminder.
  cites:
  - requirement/approval.decided-within-five-days
  - requirement/approval.nobody-approves-own-expense
  - requirement/approval-reminders.remind-approver-after-three-days
# s4 and s5 follow the same shape; the file holds them all.
branches:
- step: s3
  condition: the manager rejects the expense
  nextStep: end
```

The flow names its one reason in `serves`, the goal it moves. A flow that resolves a pain names its problem or constraint in `answers` instead. Each step cites the requirements it depends on, while the flow itself carries no `cites`. Use cases point at these steps: the submit use case serves `flow/submit-and-reimburse#s1`, and the approve use case serves `#s3`. A branch names the step where the path differs and the step it continues at, so the order of steps changes only through `branches`. Steps name no use case themselves, so a step can change its handler without the flow changing.

The explanation file, `.litespec/intent/flows/submit-and-reimburse/submit-and-reimburse.md`, holds the Example and Branches sections, with one worked week of a train fare. The Example begins from the `startedWhen` state instead of repeating it as a precondition.

Next, [Modules and Records](modules-and-records.md) specifies this intent in the Spending module: its aggregates, rules, use cases, entry point and screen, and the reaction, job, notification and setting around them. It also covers the Reporting module, which lists the reimbursed expenses from a read model.
