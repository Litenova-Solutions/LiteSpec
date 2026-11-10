# The Claimwick Product

## Intent

Claimwick is the small product the example follows: employees submit work expenses with receipts, managers approve or reject them, and finance reimburses the approved ones. This page states what that product is and why through 9 Intent records, each stating one piece of product intent: the product brief, the spending module, the goal, the problem, the feature with its requirements, the research, the decision, the constraint, and the journey. Each record below shows the shape a project writes, with YAML taken from `examples/claimwick/.litespec/`, so a team sees what written intent looks like before the example turns it into Behavior, code, and checks.

## The Product Brief

A [product](../reference/glossary.md) record states what is built and for whom. The Claimwick brief below carries the fields every product record carries: a boundary that names what the product does, an `addresses` list naming its actors, and a `summary` that states the outcome in one paragraph. Its record file lives at `.litespec/intent/product.yaml`, the home the [Project Model](../core/model.md) names for the single product page, which applies `litespec/core/model#write-every-record-in-its-home`.

```yaml
type: product
id: product/claimwick
title: Claimwick
boundary: Claimwick submits, approves, and reimburses work expenses with receipts.
addresses:
  - actor/employee
  - actor/manager
  - actor/finance
summary: Employees submit and attach, managers approve and reject, and finance reimburses.
```

Its common fields, `type`, `id`, and `title`, appear on every record in this example, which applies `litespec/core/model#state-the-common-fields-on-every-record`. The `summary` field holds what the product exists to produce: employees submit and attach, managers approve and reject, and finance reimburses. The brief carries no explanation file.

## The Spending Module

A module record names one product area of the bounded context and holds the Intent records that belong to it. Claimwick keeps one module, spending, which tracks expenses, receipts, and the policy. Its record file lives at `.litespec/intent/modules/spending/module.yaml`, and its empty `dependsOn` list states that it depends on no other module.

```yaml
type: module
id: module/spending
title: Spending
summary: Spending submits, decides, reimburses and exports work expenses.
dependsOn: []
```

The module folder holds the goal, problem, feature, research, decision, and constraint below, each under its type folder, while the product, actors, roles, and journey stay at product level. Behavior follows the same name under `.litespec/behavior/modules/spending/`, as [Modules and Records](modules-and-records.md) shows, and code follows it into `Spending` folders, as [A Use Case in Code](a-use-case-in-code.md) shows. One name runs through every layer.

## The Goal

A goal record states one measurable outcome a feature advances and a journey serves, with the measure and the target. Claimwick keeps one: every submitted expense is decided within five business days, so employees plan around reimbursements. Its record file lives at `.litespec/intent/modules/spending/goals/decided-in-days.yaml`, which applies `litespec/core/intent#state-each-goal-with-its-measure` on [Intent Layer](../core/intent.md).

```yaml
type: goal
id: goal/decided-in-days
title: Decided in Days
statement: Every submitted expense is decided within five business days, so employees plan around reimbursements.
measure: Median hours from submission to decision.
target: Under 120 hours.
```

The statement states why the target matters: employees plan around reimbursements, and a late decision holds their money. The goal carries no authority of its own: it explains why the approval reminders feature exists, and the feature advances it through an `advances` link. The goal carries no explanation file.

## The Problem

A problem record states one pain a person outside the team has today, with who suffers and the graded evidence behind it, while its explanation file states what the pain costs and what sufferers do instead. Claimwick keeps one: submitted expenses wait weeks for a decision, so employees chase managers by email. Its record file lives at `.litespec/intent/modules/spending/problems/expenses-wait-weeks.yaml`, and its `affects` link names the employee actor as the sufferer, which applies `litespec/core/intent#admit-only-outside-pains-as-problems` and `litespec/core/intent#grade-problem-evidence-by-kind`.

```yaml
type: problem
id: problem/expenses-wait-weeks
title: Expenses Wait Weeks
affects:
  - actor/employee
statement: Submitted expenses wait weeks for a decision, so employees chase managers by email.
summary: |
  Submitted expenses wait weeks for a decision, so employees chase managers by email.
  Decisions arrive late without notice.
evidence:
  - claim: Employees wait three weeks on average for reimbursement.
    source: research/approval-time-benchmarks
    readOn: 2026-11-01
    kind: reported
```

The statement stays to one sentence, and the `summary` field states the pain in one paragraph, adding one more sentence: decisions arrive late without notice. Its explanation file at `.litespec/intent/modules/spending/problems/expenses-wait-weeks.md` holds four sections in order:

```markdown
# Expenses Wait Weeks

## Who Has It

An employee who submitted an expense and waits for a decision, carrying the expense amount from their own account for up to three weeks.

## What They Do Today

The employee emails the approver for a faster decision, and the approver answers from memory with no record of the wait.

## Evidence

The benchmark in [approval time benchmarks](research/approval-time-benchmarks)
shows small companies decide within one week, while Claimwick averages three.

## What Would Retire It

Each submitted expense is decided within five business days.
```

The Who Has It section states what the wait costs, and What They Do Today states the workaround. The Evidence section cites the benchmark research below: small companies decide within one week, while Claimwick averages three. Later records answer this problem: the approval reminders feature solves it, and its requirements bound it.

## The Feature

A feature record states what the product offers against a problem, with the problem it solves, the goal it advances, the offering itself, and its requirements as entries. Claimwick keeps one: managers get a reminder for expenses waiting past three days. Its record file lives at `.litespec/intent/modules/spending/features/approval-reminders.yaml`, it solves the waiting problem and advances the five-day goal, and its `records` hold the 6 requirements below.

```yaml
type: feature
id: feature/approval-reminders
title: Approval Reminders
solves:
  - problem/expenses-wait-weeks
advances:
  - goal/decided-in-days
offering: |
  Managers get a reminder for expenses waiting past three business days.
  The reminder names the expense and its waiting time.
  Expenses decided in time never trigger one.
records:
  - type: requirement
    id: requirement/approval-reminders.decided-within-five-days
    title: Decided within five days
    kind: functional
    statement: Each submitted expense is decided within five business days.
    rationale: Late decisions stall employees and payouts.
    except:
      - condition: The expense is submitted during the year-end close.
        fallback: requirement/approval-reminders.extended-decision-in-close
    setBy: product
    cites:
      - feature/approval-reminders
      - research/approval-time-benchmarks
  - type: requirement
    id: requirement/approval-reminders.receipt-over-threshold
    title: Receipt over threshold
    kind: functional
    statement: An expense above the receipt threshold carries a receipt.
    rationale: Proof above the line keeps the tax file defensible.
    setBy: product
    cites:
      - feature/approval-reminders
      - constraint/tax-receipt-retention
  - type: requirement
    id: requirement/approval-reminders.erasure-on-request
    title: Erasure on request
    kind: functional
    statement: The personal data of a decided expense is erased when its employee asks.
    rationale: The company keeps no personal data past its need.
    setBy: product
    cites:
      - feature/approval-reminders
```

The `offering` field states what managers get: the reminder names the expense and its waiting time, and expenses decided in time never trigger one. Each requirement entry carries its own identifier, title, kind, statement, one-line rationale, and links, which applies `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#explain-each-requirement-in-one-line`, and `litespec/core/intent#cite-the-reasons-of-every-requirement`. The feature is the thread this example follows down the tree: rules and use cases point back at these identifiers with `fulfills` links, as [Modules and Records](modules-and-records.md) shows, and the reminder job and notification carry it out.

Its explanation file at `.litespec/intent/modules/spending/features/approval-reminders.md` tells the story in three sections:

```markdown
# Approval Reminders

## Who It Serves and Why

Managers who decide expenses, and employees who wait for the decision.

## What It Offers

Every submitted expense is decided within five business days.
Expenses above the receipt threshold carry a receipt.
Nobody approves their own expense, and each approved expense
is reimbursed at most once.

## What Good Looks Like

Employees learn the outcome within days instead of weeks,
and finance pays from a file it can defend.
Ava submits a 48.90 EUR train fare with its receipt on Monday.
Ben approves it on Tuesday, and Cara reimburses it on Friday.
```

## The Requirements

A requirement states one behavior the product shows or one guarantee it keeps. Each entry on the feature above carries its exceptions with their fallback and who sets its values, which applies `litespec/core/intent#state-each-exception-with-its-fallback`.

The five-day requirement states its exception beside the rule: during the year-end close, the fallback requirement `requirement/approval-reminders.extended-decision-in-close` holds instead, and the fallback carries no exception of its own. The remaining entries follow the same shape:

| Requirement | Statement |
|:---|:---|
| `requirement/approval-reminders.no-self-approval` | Nobody approves their own expense. |
| `requirement/approval-reminders.single-reimbursement` | An approved expense is reimbursed at most once. |
| `requirement/approval-reminders.extended-decision-in-close` | An expense submitted during the year-end close is decided within ten business days. |

All 6 requirements are functional requirements: each states something the product does. Their identifiers carry the feature name, so `requirement/approval-reminders.decided-within-five-days` belongs to approval reminders. The `setBy` value `product` states that the product fixes the value, neither the operator nor the tenant.

## The Research

A research record states one outside fact that informs a choice, with the question it answers, its conclusion, and the sources it read. Each source carries a `key` beside its `url` and `readOn`, and the explanation cites each key. Claimwick keeps one: small companies decide expenses within one week. Its record file lives at `.litespec/intent/modules/spending/research/approval-time-benchmarks.yaml`, which applies `litespec/core/intent#record-research-with-read-sources`.

```yaml
type: research
id: research/approval-time-benchmarks
title: Approval Time Benchmarks
sources:
  - key: expense-study
    url: https://example.com/expense-study
    readOn: 2026-02-10
question: How fast do small companies decide expenses?
conclusion: Small companies decide expenses within one week.
```

Research is an article type: its explanation file is required, with an Answer section. Its explanation file at `.litespec/intent/modules/spending/research/approval-time-benchmarks.md` reads:

```markdown
# Approval Time Benchmarks

## Answer

Small companies decide expenses within one week,
as [one industry study][expense-study] shows.
The study behind this answer is small but recent,
so the five-day target carries medium confidence.

## Notes on Sources

[One industry study][expense-study], read in February 2026.
```

The Answer section states the conclusion with its confidence, and Notes on Sources records what was read. Each `[text][key]` link names a source `key` from the record file. Research informs without deciding: the five-day requirement cites it in `cites`, and the decision below settles the threshold the requirement leaves open.

## The Decision

A decision record states one choice with the options it weighed and the consequences that follow. Claimwick chose a 25 EUR receipt threshold after a finance review in February 2026. Its record file lives at `.litespec/intent/modules/spending/decisions/0001-receipt-threshold.yaml`, and its `decides` link names the receipt requirement it settles, which applies `litespec/core/model#name-what-every-decision-decides`. Each option carries its pros and cons, which applies `litespec/core/intent#shape-each-decision-page`.

```yaml
type: decision
id: decision/0001-receipt-threshold
title: Receipt Threshold
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
  - requirement/approval-reminders.receipt-over-threshold
basedOn: []
```

A decision is an article type: its explanation file is required, with a Context section. Its explanation file at `.litespec/intent/modules/spending/decisions/0001-receipt-threshold.md` reads:

```markdown
# Receipt Threshold

## Context

Small expenses need no proof, large ones do.

## Decision

Expenses above 25 EUR need a receipt.

## Evidence with Confidence

Finance reviewed the filing cost against the fraud risk
in February 2026 and stands behind 25 EUR.

## What Would Reopen It

Finance sees abuse or friction.
```

The Context section states the situation, the Decision section states the choice in one line, and Evidence with Confidence cites the finance review of February 2026. The `consequences` field notes that small expenses stay light to submit, and What Would Reopen It states the `reopenedWhen` condition. A later change edits this record in place, so the reasoning stays readable in git.

## The Constraint

A constraint states one outside rule the product obeys, with a summary of that rule. Claimwick answers the national tax law on receipts: receipts stay kept for 7 years because Claimwick stores receipts for reimbursed expenses. Its record file lives at `.litespec/intent/modules/spending/constraints/tax-receipt-retention.yaml`, which applies `litespec/core/intent#answer-every-constraint`.

```yaml
type: constraint
id: constraint/tax-receipt-retention
title: Tax Receipt Retention
authority: National tax law keeps business receipts for 7 years.
readOn: 2026-02-01
kind: legal
applies: true
appliesBecause: Claimwick stores receipts for reimbursed expenses.
summary: The national tax law requires a company to keep its business receipts for seven years.
```

The `applies` field answers whether the outside rule covers this product, and `appliesBecause` gives the reason in one sentence. The `readOn` field states when the outside rule was read. A project answers this question for every constraint it lists, even when the answer is that none applies. The receipt requirement cites this constraint in `cites`, so the tax file traces to the outside rule that demands it, which applies `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`. The book renders What It Requires from the records that cite the constraint; the constraint carries no explanation file.

## The Journey

A journey record holds product sentences with ids toward one goal for one actor, with its summary, its observable end, and what passes between the steps. Claimwick keeps one: an employee submits an expense and receives reimbursement. Its record file lives at `.litespec/intent/journeys/submit-and-reimburse.yaml` at product level, since journeys cross modules, it states its summary and its observable end, it serves the five-day goal, and it cites the five-day requirement, which applies `litespec/core/intent#order-each-journey-toward-one-goal`, `litespec/core/intent#describe-every-journey`, `litespec/core/intent#write-journey-steps-as-sentences-with-ids`, and `litespec/core/intent#state-when-each-journey-finishes`.

```yaml
type: journey
id: journey/submit-and-reimburse
title: Submit and Reimburse
actor: actor/employee
serves:
  - goal/decided-in-days
summary: An employee who paid a work cost submits the expense and waits for a decision. The walk starts at submission and ends when the reimbursement lands in the employee account. A good walk decides each expense within five business days, because the employee plans around the money.
finishedWhen: The employee holds the reimbursement, and the expense record shows every step.
steps:
  - id: s1
    statement: The employee submits the expense with its receipt.
    actor: actor/employee
  - id: s2
    statement: The manager approves the expense within five days.
    actor: actor/manager
    failure: Past three days the manager gets a reminder; past five the wait is reported.
  - id: s3
    statement: Finance reimburses the approved expense.
    actor: actor/finance
  - id: s4
    statement: The employee sees the reimbursement land.
    actor: actor/employee
cites:
  - requirement/approval-reminders.decided-within-five-days
betweenSteps: |
  The expense waits between submission and decision.
  Past three days without a decision the manager gets a reminder.
```

The 4 steps run from submission to reimbursement in product words: the employee submits, the manager decides, finance reimburses, and the employee sees the money land. Steps name no use case: each use case points up to the steps it serves, so Submit expense serves `journey/submit-and-reimburse#s1` and Approve expense serves `#s2`. The `betweenSteps` field states what passes between the steps. Its explanation file at `.litespec/intent/journeys/submit-and-reimburse.md` holds the worked week and the starting state:

```markdown
# Submit and Reimburse

## Example

Ava submits a train fare on Monday with the receipt attached.
Ben approves it on Tuesday, and Cara reimburses it on Friday.
Ava watches each step in her own expense list.

## Before It Starts

Ava signed in as an employee.
The policy carries its receipt threshold.
```

Next, [Modules and Records](modules-and-records.md) specifies this intent into the spending module: its aggregates, rules, access chain, entry point with its screen, use cases, and the reaction, jobs, notifications, setting, integration event, and retention row around them.
