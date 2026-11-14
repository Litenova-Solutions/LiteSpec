# Write a Feature Under a Problem

## Purpose

A feature is one answer to a problem, advancing a goal. It lives in the features folder of the one problem it answers: one record file at `.litespec/intent/problems/<problem>/features/<feature>/<feature>.yaml` states the offering and its links. Each commitment the feature makes is a requirement, written as its own record in its own folder in the feature's `requirements/` folder. This guide writes those files and the feature's explanation file `<feature>.md`, so a developer can tell what to build and a tester can tell what passes. The writing steps show a weak version and a strong version from the Claimwick expense product. Each step names the provisions it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its product record written.
- The problem the feature answers and the goal it advances, each in its own record file. [Write a Problem](write-a-problem.md) writes the problem.

Read the [Intent](../core/intent.md) page and the [Writing](../core/writing.md) page before this guide: they state the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Feature File

Run `litespec new feature <problem>.<name>` to scaffold the feature and reserve the identifier `feature/<name>`. The problem names the folder, and the feature sits in it: the command writes `<name>/<name>.yaml` under `.litespec/intent/problems/<problem>/features/`. Give the name in kebab-case, such as `expense-submission`, and the title in plain words. This satisfies `litespec/core/model#write-every-record-in-its-record-file`, `litespec/core/intent#keep-intent-in-its-homes`, and `litespec/core/intent#answer-one-problem-with-each-feature`.

### State the Offering and Its Links

Write `offering` as one paragraph stating what the software does and for whom. Link the goal it advances in `advances`, and name the glossary terms the feature uses in `terms`. The problem the feature answers is its folder, so the feature names no problem:

```yaml
offering: Claimwick takes each expense with its receipt at submission time.
advances:
  - goal/faster-reimbursement
terms:
  - term/expense
```

Write each link on the feature, the record that points. This satisfies `litespec/core/intent#state-each-feature-with-its-offering` and `litespec/core/model#write-links-on-the-record-that-points`.

### Write One Requirement per Commitment

Scaffold each commitment the feature makes with `litespec new requirement <feature>.<name>`, such as `litespec new requirement expense-submission.receipt-over-threshold`. The command writes the requirement as its own record at `.litespec/intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml`, with its identifier `requirement/<feature>.<name>` and its title filled. It writes no explanation file. Then fill the record's fields: its kind, `functional`, `quality`, or `policy`, one testable sentence in `statement`, one line of reason in `rationale`, and the records behind it in `cites`. Cite each applying constraint from the requirement it binds. This satisfies `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-requirement-kind-and-statement`, `litespec/core/intent#cite-the-reasons-of-every-requirement`, and `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`.

The requirement file holds one mapping at its top level, with no `records` list, because a requirement holds no entries:

```yaml
type: requirement
id: requirement/expense-submission.receipt-over-threshold
title: Expense Above Threshold Carries Receipt
kind: functional
statement: Claimwick accepts an expense above the policy threshold only with a receipt attached.
rationale: Finance needs proof for each expense above the threshold to defend the tax file.
cites:
  - goal/faster-reimbursement
except:
  - condition: The amount is under the threshold.
    fallback: requirement/expense-submission.auto-accepted-under-threshold
setBy: product
```

### Name Each Requirement by Its Behavior

Give each requirement a title that names the specific behavior or fact it requires. A short title that names only a topic tells a reader in a list nothing about what holds.

| Instead of | Write |
|:---|:---|
| Receipts | Expense Above Threshold Carries Receipt |
| Waiting Time | Approver Sees Days Waited |

This follows the requirement row of the title forms on the [Writing](../core/writing.md#title-forms) page.

### Write a Statement a Tester Can Decide

State one behavior in each statement, with every condition, date, and amount its result depends on. Put the actor first and use no modal word. When a statement holds two behaviors, write two requirements, so each one is fulfilled, proven, and changed on its own.

| Instead of | Write |
|:---|:---|
| The approver can see how long expenses have waited. | Claimwick shows the approver each waiting expense with the number of business days since its submission. |
| Claimwick reminds the approver and escalates late expenses to finance. | Two requirements: one for the reminder, one for the escalation, each with the day it happens. |

The first version names no starting point and no unit, so a tester cannot tell which number is right. This satisfies `litespec/core/intent#state-one-testable-behavior-in-each-requirement`, `litespec/core/writing#state-each-statement-without-modal-words`, and `litespec/core/writing#put-the-actor-first`.

### Keep Each Rationale to Its Reason

Write the rationale as one line that says why the requirement exists. Add no behavior, and claim no single cause where several exist and no comparison nobody measured.

| Instead of | Because |
|:---|:---|
| A second pair of eyes keeps approvals honest. | A metaphor. Write: A second person checks each expense before it is paid. |
| A late decision is the main reason employees chase managers. | An unmeasured claim. Write: Late decisions keep employees waiting for their money. |

This satisfies `litespec/core/intent#explain-each-requirement-in-one-line`, `litespec/core/intent#keep-each-rationale-to-its-reason`, and `litespec/core/writing#use-literal-words`.

### State Each Quality Requirement as a Scenario

A quality requirement carries the scenario that confirms it: the `stimulus` that triggers it, the `environment` it holds in, the `response` the product shows, the `measure` that names what is counted, the `target` that names the number aimed for, and the `check` that confirms it, with `conditions`, `runsOn`, and `evidence` where they apply. This satisfies `litespec/core/intent#state-each-quality-requirement-as-a-scenario`.

```yaml
type: requirement
id: requirement/expense-submission.reminder-delivery-time
title: Reminder Delivered Within 1 Hour
kind: quality
statement: Claimwick delivers every reminder within 1 hour of its scheduled time.
rationale: Late reminders push reimbursement past the deadline.
stimulus: The schedule fires.
environment: During working hours.
response: Claimwick sends the reminder.
measure: Delivery delay per reminder.
target: Under 1 hour.
conditions: Up to 10,000 managers.
check: reminder-delivery-probe
runsOn: nightly
evidence: The nightly reminder delivery run.
cites:
  - goal/faster-reimbursement
setBy: product
```

### State Each Exception with Its Fallback

A requirement that bends in a named case carries that case in `except` with its `condition` and the `fallback` requirement that holds instead, as the receipt requirement above shows. Write the fallback requirement as its own named record, with its own folder, so every exception ends in a promise. Name who sets the requirement values in `setBy`: `product`, `operator`, or `tenant`. This satisfies `litespec/core/intent#state-each-exception-with-its-fallback`.

### Open the Explanation with What the Feature Is For

Write `<feature>.md` beside the feature file. Open it with one paragraph that says what the feature is for and what it covers, then add Capabilities, Success Criteria, Boundaries, and an optional Example, in that order. Each section does its own job, so none repeats the opening or the summary. Keep checkable facts in the record files; the explanation helps a reader understand them. This satisfies `litespec/core/writing#give-each-explanation-file-its-fixed-sections`, `litespec/core/model#split-record-fields-from-explanation`, and `litespec/core/model#explain-each-record-beside-its-record-file`.

### Group Capabilities by Task

Under Capabilities, describe what the user can do, one short paragraph or subsection per task. Describe only behavior that a requirement states. A rule that appears only in the prose is a requirement nobody fulfills: write it as a requirement, or take it out.

| Instead of | Because |
|:---|:---|
| Expenses waiting more than 10 days show in red. | No requirement states it. Add a requirement for it, or remove the sentence. |

This satisfies `litespec/core/intent#add-no-behavior-in-an-explanation-file`.

### State Success Criteria the Feature Controls

Under Success Criteria, list the outcomes the feature brings about on its own, each one a tester can check. An outcome that depends on people or on other features belongs to the goal the feature advances.

| Instead of | Write |
|:---|:---|
| Every expense is decided within 5 business days. | An expense above the 25 EUR threshold cannot be submitted without a receipt. No manager can approve their own expense. |

How fast managers decide depends on the managers, so the first version sets a test the feature cannot pass on its own. This satisfies `litespec/core/intent#state-success-criteria-the-feature-controls`.

### State the Boundaries

Under Boundaries, say what the feature leaves to other features, which features or decisions it depends on, and each question the owner has not decided. Check the other Intent records first, and list only the questions none of them settles.

```markdown
Reminders for late decisions are not part of this feature. They belong to `feature/approval-reminders`.

The feature depends on `decision/0001-receipt-threshold` for the 25 EUR threshold.

One question is open for the owner: whether a manager on leave hands decisions to a deputy.
```

This satisfies `litespec/core/intent#state-the-boundaries-of-every-feature`.

### Name the Operation, Not an Impression

Write each sentence with a verb that says what happens to what, and with the domain's precise term where one exists. Replace an intensifier or an experience claim with the behavior or the measured target.

| Instead of | Write |
|:---|:---|
| The feature handles approvals seamlessly. | The manager approves or refuses each expense, and the employee sees the decision in their own list. |
| The approver instantly spots late expenses. | Claimwick shows the approver each waiting expense with the number of business days since its submission. |

This satisfies `litespec/core/writing#use-literal-words` and `litespec/core/writing#name-observable-behavior-instead-of-intensifiers`.

### Explain a Requirement Only When It Needs It

When a statement needs context it cannot carry, such as the date a comparison uses or a term a reader may misread, add `<name>.md` beside `<name>.yaml` in the requirement's folder. Open it with one paragraph and add an optional Example. It carries no acceptance criteria, because the Behavior scenarios that fulfill the requirement state them, and it describes no behavior the statement does not hold. Leave it out when the statement and its rationale say everything. This satisfies `litespec/core/intent#add-no-behavior-in-an-explanation-file`.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the feature file, its explanation, or any requirement file.
- `litespec model query feature/<name>` shows the feature with its requirements.
- `litespec rules <path to the feature record>` lists the judgment provisions above, and each one is answered.
- A developer can tell what to build from the requirements, and a tester can tell what passes and what fails. Every date, amount, and condition that matters is stated, and no sentence in the explanation adds behavior the requirements do not hold.
