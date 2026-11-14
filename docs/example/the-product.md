# The Claimwick Product

## Intent

Claimwick is the product this example follows: employees submit work expenses with receipts, managers approve or refuse them, and finance reimburses the approved ones. This page shows its Intent records as they are written in `examples/claimwick/.litespec/intent/`, from the product brief down to the journey that ties them together. Every record, field, ID and count below is copied from that folder, so a reader can open the same file and see the same text.

## The Product Brief

A product record states what is built and for whom. The Claimwick brief names the boundary, the actors it addresses and the outcome in one paragraph. Its file is `.litespec/intent/product.yaml`:

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

The `addresses` list names the three actors that hold the product's roles. The actors are in `.litespec/intent/actors/` and their roles in `.litespec/intent/roles/`.

## The Spending Module

The product has one module, Spending. A module record names one product area and the records that belong to it. It is declared in Behavior, beside the aggregates it groups, at `.litespec/behavior/modules/spending/module.yaml`:

```yaml
type: module
id: module/spending
title: Spending
summary: Spending submits, decides, reimburses and exports work expenses.
dependsOn: []
```

The one problem, `problem/expenses-wait-weeks`, sits at product level, and its two features sit in the problem's folder, `.litespec/intent/problems/expenses-wait-weeks/features/`. Each requirement sits in its own folder inside its feature's `requirements/` folder. The goal, research, decision and constraint records sit at product level under `.litespec/intent/`, where the folder rules allow them. The Behavior records of the module sit under `.litespec/behavior/modules/spending/`, as [Modules and Records](modules-and-records.md) shows.

## The Goal

A goal record states one measurable outcome that features advance. Claimwick keeps one, in `.litespec/intent/goals/decided-in-days/decided-in-days.yaml`:

```yaml
type: goal
id: goal/decided-in-days
title: Decided in Days
statement: Every submitted expense is decided within five business days.
measure: Median hours from submission to decision.
target: Under 120 hours.
```

The statement names the target, and the measure says how the team reads it. The goal carries no authority of its own: features and journeys point at it.

## The Problem

A problem record states one pain that a person outside the team has today. Claimwick keeps one, in `.litespec/intent/problems/expenses-wait-weeks/expenses-wait-weeks.yaml`. Submitted expenses wait weeks for a decision, so employees chase managers by email. The `affects` field names the employee as the person who suffers it, and the `evidence` list cites the research below with its grade, `reported`.

## The Features and Their Requirements

A feature record states what the product offers against a problem. Claimwick has two features.

The Approval feature, in `.litespec/intent/problems/expenses-wait-weeks/features/approval/approval.yaml`, answers the waiting problem and holds five functional requirements, each in its own folder inside the feature's folder:

| Requirement ID | Statement |
|:---|:---|
| `requirement/approval.decided-within-five-days` | Each submitted expense is decided within five business days. |
| `requirement/approval.receipt-over-threshold` | An expense above the receipt threshold carries a receipt. |
| `requirement/approval.no-self-approval` | Nobody approves their own expense. |
| `requirement/approval.single-reimbursement` | An approved expense is reimbursed at most once. |
| `requirement/approval.erase-after-retention` | The personal data of a decided expense is erased after its retention. |

The Approval Reminders feature, in `.litespec/intent/problems/expenses-wait-weeks/features/approval-reminders/approval-reminders.yaml`, advances the five-day goal and answers the same problem. Its offering is the reminder itself: managers get a reminder for expenses waiting past three business days. It holds one quality requirement, `requirement/approval-reminders.month-end-submissions`, in `.litespec/intent/modules/spending/features/approval-reminders/month-end-submissions/`, which states that each month-end submission is acknowledged within two seconds. That requirement names a check, `month-end-load`, that the model reads from `.litespec/proof/`.

Each requirement carries its identifier, title, kind, statement, rationale and the records it cites in `cites`. The `cites` list is how the tax file traces back to the constraint below.

## The Research

A research record states one outside fact that informs a choice. Claimwick keeps one, in `.litespec/intent/research/approval-time-benchmarks/approval-time-benchmarks.yaml`. Its sources carry a `key`, a `url` and the day they were read, and the explanation cites each key:

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

Research informs the five-day requirement without deciding it. Its explanation file, `.litespec/intent/research/approval-time-benchmarks/approval-time-benchmarks.md`, has the required Findings section, with Open Questions and Source Notes beside it. Each finding cites its source with a reference link and says whether the source states it or the writer infers it.

## The Decision

A decision record states one choice with the options it weighed. Claimwick chose a 25 EUR receipt threshold, recorded in `.litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.yaml`. The record lists three options, each with its pros and cons, and its `decides` field names the receipt requirement it settles:

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
- requirement/approval.receipt-over-threshold
basedOn: []
```

The explanation file, `.litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.md`, holds the Context, Options, Decision, Consequences and Revisit Conditions sections, with Evidence optional. The `reopenedWhen` field carries the condition for reopening the choice.

## The Constraint

A constraint states one outside rule, the source it rests on and whether it applies to the product. Claimwick answers the national tax law on receipts in `.litespec/intent/constraints/tax-receipt-retention/tax-receipt-retention.yaml`. The law keeps business receipts for seven years, the record names the law as its source with the day it was read, and the product applies because it stores receipts for reimbursed expenses. The receipt requirement cites this constraint, so the retention rule traces to the outside law that demands it.

## The Journey

A journey record holds the steps one actor takes toward one goal. Claimwick keeps one, in `.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.yaml`. An employee submits an expense, a manager decides it, finance reimburses it, and the employee sees it in their own list. The journey has eight steps with IDs `s1` to `s8`, and each step names the actor who takes it:

```yaml
type: journey
id: journey/submit-and-reimburse
title: Submit and Reimburse
actor: actor/employee
serves:
- goal/decided-in-days
finishedWhen: The employee holds the reimbursement, and the personal data of old expenses is erased past retention.
steps:
- id: s1
  statement: The employee submits the expense with its amount and currency.
  actor: actor/employee
- id: s2
  statement: The employee attaches the receipt to the expense.
  actor: actor/employee
- id: s3
  statement: The manager approves or rejects the expense within five business days.
  actor: actor/manager
  failure: The expense waits past three business days, and the approver is reminded.
# s4 to s8 follow the same shape; the file holds them all.
```

Use cases point at these steps: the submit use case serves `journey/submit-and-reimburse#s1`, and the approve use case serves `#s3`. Steps name no use case themselves, so a step can change its handler without the journey changing.

The explanation file, `.litespec/intent/journeys/submit-and-reimburse/submit-and-reimburse.md`, holds the Preconditions and Example sections, with one worked week of a train fare.

Next, [Modules and Records](modules-and-records.md) specifies this intent in the Spending module: its aggregates, rules, use cases, entry point and screen, and the reaction, job, notification and setting around them.
