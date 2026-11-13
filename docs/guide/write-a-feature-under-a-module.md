# Write a Feature Under a Module

## Purpose

A feature is one answer to a problem under a module, advancing a goal and solving that problem. It lives under the module that holds the problem: one record file at `.litespec/intent/modules/<module>/features/<feature>/<feature>.yaml` states the offering and its links. Each commitment the feature makes is a requirement, written as its own record in its own folder inside the feature's folder. This guide writes those files and the feature's explanation file `<feature>.md` beside the feature file. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its product record written.
- The module the feature belongs to, declared at `.litespec/intent/modules/<module>/module.yaml`.
- The problem the feature answers and the goal it advances, each in its own record file.

Read the [Intent](../core/intent.md) page before this guide: it states the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Feature File

Run `litespec new feature <name>` to scaffold the feature and reserve the identifier `feature/<name>`. Give the name in kebab-case, such as `expense-submission`, and the title in plain words. The command writes the folder `<name>/` with `<name>.yaml` inside it under `.litespec/intent/features/`. Move that folder to `.litespec/intent/modules/<module>/features/<name>/`, so the record file ends at `.litespec/intent/modules/<module>/features/<name>/<name>.yaml`, where the module holds it. This satisfies `litespec/core/model#write-every-record-in-its-record-file` and `litespec/core/intent#keep-intent-in-its-homes`.

### State the Offering and Its Links

Write `offering` as one paragraph stating what the software does and for whom. Link the problem the feature answers in `solves` and the goal it advances in `advances`, and name the glossary terms the feature uses in `terms`:

```yaml
offering: Claimwick takes each expense with its receipt at submission time.
solves:
  - problem/expenses-wait-weeks
advances:
  - goal/faster-reimbursement
terms:
  - term/expense
```

Write each link on the feature, the record that points. This satisfies `litespec/core/intent#state-each-feature-with-its-offering` and `litespec/core/model#write-links-on-the-record-that-points`.

### Write One Requirement per Commitment

Scaffold each commitment the feature makes with `litespec new requirement <feature>.<name>`, such as `litespec new requirement expense-submission.receipt-over-threshold`. The command writes the requirement as its own record at `.litespec/intent/modules/<module>/features/<feature>/<name>/<name>.yaml`, with its identifier `requirement/<feature>.<name>` and its title filled. It writes no explanation file. Then fill the record's fields: its kind, `functional`, `quality`, or `policy`, one testable sentence in `statement`, one line of reason in `rationale`, and the records behind it in `cites`. Cite each applying constraint from the requirement it binds. This satisfies `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-requirement-kind-and-statement`, `litespec/core/intent#cite-the-reasons-of-every-requirement`, and `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`.

The requirement file holds one mapping at its top level, with no `records` list, because a requirement holds no entries:

```yaml
type: requirement
id: requirement/expense-submission.receipt-over-threshold
title: Receipt Over Threshold
kind: functional
statement: The product requires a receipt for every expense above the policy threshold.
rationale: Finance rebuilds spending from forwarded receipts today.
cites:
  - goal/faster-reimbursement
except:
  - condition: The amount is under the threshold.
    fallback: requirement/expense-submission.auto-accepted-under-threshold
setBy: product
```

### State Each Quality Requirement as a Scenario

A quality requirement carries the scenario that confirms it: the `stimulus` that triggers it, the `environment` it holds in, the `response` the product shows, the `measure` that names what is counted, the `target` that names the number aimed for, and the `check` that confirms it, with `conditions`, `runsOn`, and `evidence` where they apply. This satisfies `litespec/core/intent#state-each-quality-requirement-as-a-scenario`.

```yaml
type: requirement
id: requirement/expense-submission.reminder-delivery-time
title: Reminder Delivery Time
kind: quality
statement: The product delivers every reminder within one hour of schedule.
rationale: Late reminders push reimbursement past the deadline.
stimulus: The schedule fires.
environment: During working hours.
response: The product sends the reminder.
measure: Delivery delay per reminder.
target: Under one hour.
conditions: Up to ten thousand managers.
check: reminder-delivery-probe
runsOn: nightly
evidence: The nightly reminder delivery run.
cites:
  - goal/faster-reimbursement
setBy: product
```

### State Each Exception with Its Fallback

A requirement that bends in a named case carries that case in `except` with its `condition` and the `fallback` requirement that holds instead, as the receipt requirement above shows. Write the fallback requirement as its own named record, with its own folder, so every exception ends in a promise. Name who sets the requirement values in `setBy`: `product`, `operator`, or `tenant`. This satisfies `litespec/core/intent#state-each-exception-with-its-fallback`.

### Add the Explanation Files

When the feature needs prose beyond its fields, add `<feature>.md` beside the feature file in the same folder, with Capabilities, Success Criteria, Boundaries, and an optional Example. Keep checkable facts in the record file; the explanation file only helps a reader understand. This satisfies `litespec/core/model#split-record-fields-from-explanation` and `litespec/core/model#explain-each-record-beside-its-record-file`.

When a requirement's statement needs prose, add `<name>.md` beside `<name>.yaml` in the requirement's folder. Open it with one paragraph and add an optional Example. It carries no acceptance criteria, because the Behavior scenarios that fulfill the requirement state them.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the feature file or on any requirement file.
- `litespec model query feature/<name>` shows the feature with its requirements.
</content>
