# Write a Journey

## Purpose

A journey states the path one actor takes toward one goal, from the state in which it starts to the state in which it ends, one step at a time. This guide writes one journey record, `<name>.yaml`, and its explanation file `<name>.md`, so a reader can follow the path, see where it can leave its order, and find the requirement behind each block. Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its actor and goal records written.
- The actor who takes the journey, as an actor record, and the goal it serves, as a goal record.
- The requirements the journey cites, or the features that hold them, so each blocker names what holds it.

Read the [Intent](../core/intent.md) page and the [Writing](../core/writing.md) page before this guide: they state the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Journey

Run `litespec new journey <name>` to write `.litespec/intent/journeys/<name>/<name>.yaml` and reserve the identifier `journey/<name>`. When the project lists `intent` in `explanations.layers`, the command also writes `<name>.md` from the journey template, with its Example and Branches sections. The template's values are examples, and `model check` reports a value left unchanged. This satisfies `litespec/core/intent#keep-intent-in-its-homes` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Decide What One Journey Holds

A journey has one actor and one goal. An actor who takes two paths toward two goals takes two journeys. A goal that one path reaches through two actors is one journey with a handoff, where the second actor takes over a step through its own `actor` field.

A staff journey follows the same shape. Finance exporting the approved batch is a journey that finance takes, with the same `serves`, `startedWhen`, `finishedWhen`, steps and `branches`. A scheduled promise, such as a nightly erasure, is a requirement that a job fulfills, not a journey.

This satisfies `litespec/core/intent#order-each-journey-toward-one-goal` and `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`.

### Title the Journey by the Task

Name the task the actor completes, in the form the actor would say it. A title that names a system component or a screen tells the reader where the path lives, not what the actor does.

| Kind | Title |
|:---|:---|
| Screen | Expense List Page |
| Component | Reimbursement Service |
| Task | Submit and Reimburse |

This follows the journey row of the title forms on the [Writing](../core/writing.md#title-forms) page and satisfies `litespec/core/writing#write-every-record-title-in-title-case`.

### Write the Summary

Write `summary` in up to four sentences. Say who takes the journey, what starts it, what good looks like, and the consequence at stake for a named party. The consequence names a concrete effect, not a general reason.

| Instead of | Write |
|:---|:---|
| This journey matters because the process must be fast. | An employee who paid a work cost submits the expense and waits for a decision. A good journey decides each expense within five business days, because the employee plans around the money. |

The first version names no party and no effect. A reader who knows only the summary should know who the journey serves and what goes wrong when it fails. This satisfies `litespec/core/intent#describe-every-journey`.

### Write startedWhen as an Observable State

Write `startedWhen` as the state a reader can observe before the first step, in one sentence. It is a state of the world, not an action the actor takes first.

| Instead of | Write |
|:---|:---|
| The employee decides to submit an expense. | The employee has paid a work cost and has not yet submitted it. |

The first version names a decision, which no reader can observe. The second names a state that can be checked. Any detail of one occasion goes into the Example, not into `startedWhen`. This satisfies `litespec/core/intent#state-when-each-journey-starts`.

### Write the Steps in Order

Write each step as one act of its performer, in a product sentence, actor first. A step may end with the product's observable response after the act, so a reader sees what the product does. How the product computes that response stays in the cited requirement. A second act of a person is a second step.

| Instead of | Write |
|:---|:---|
| `statement: The employee submits the expense and attaches the receipt.` | Two steps: `s1` The employee submits the expense with its amount and currency. `s2` The employee attaches the receipt to the expense. |
| `statement: Finance pays the employee the amount with the correct tax.` | `statement: Finance reimburses the approved expense, and Claimwick pays the amount into the employee's account.` |

The first version packs two acts into one step, so a reader cannot see where one act ends. The second states what the product does without the rule of how it computes the amount. This satisfies `litespec/core/intent#write-journey-steps-as-sentences-with-ids`.

Give each step an `id` in order: `s1`, `s2`, and so on. Use an `actor` field only at a handoff, when a second person takes over a step.

### Give Each Blocker as the Step's Failure

When a condition stops the journey, state it as the `failure` of the step it blocks, and cite the requirement that holds it. A blocker never appears as a sentence in a branch, a step of its own or the explanation.

| Instead of | Write |
|:---|:---|
| `s4 failure: Finance must not pay out expenses that were not approved, as the policy requires.` | `s4 failure: The expense is not approved, so it waits and no payout is made.`, with `requirement/approval.decided-within-five-days` in `cites` |

The first version states a rule in the journey, which belongs to the requirement, and it names no outcome a reader can observe. The strong version states the blocked outcome and cites the requirement that holds it. This satisfies `litespec/core/intent#state-each-journey-blocker-as-a-step-failure` and `litespec/core/writing#use-literal-words`.

A journey states no rule of its own. A condition with a number, such as a threshold or a limit, lives in the requirement or the setting that holds it, and the journey cites that record.

### Write Branches

List in `branches` each path that leaves the order of the steps. Each entry names the `step` it leaves, the `condition` under which it leaves, and the `nextStep` it continues at, or `end`. A branch states no action of its own: an action that happens on a branch is a step, placed in the order.

| Instead of | Write |
|:---|:---|
| `branches: - step: s3, condition: the manager rejects the expense, nextStep: end`, with the rejection email described in the explanation only | `branches: - step: s3, condition: the manager rejects the expense, nextStep: end`, and the rejection notice as a step if the journey must show it |

The first version hides an action in prose where no check reads it. The second names the path in the structured list, so the notice has a place in the order if the journey needs one. This satisfies `litespec/core/intent#branch-a-journey-only-between-its-steps`.

A branch that retries a step, such as a reminder before a decision, names the step it returns to as its `nextStep` and states the reminder as a step if it is an act of its own.

### Write the Finish and Reach Each Clause

Write `finishedWhen` as one sentence that names the observable end state. Every clause of it must be reached by a step, and a step must be performable once `startedWhen` and the steps before it hold.

| Instead of | Write |
|:---|:---|
| `finishedWhen: The employee holds the reimbursement, and the expense is erased from the archive.` | `finishedWhen: The employee holds the reimbursement.` |

The first version names a clause no step reaches. The erasure happens in a job that fulfills its own requirement, so it stays out of this journey. This satisfies `litespec/core/intent#reach-each-finish-condition-through-a-step`.

### Cite the Requirements

List in `cites` each requirement the journey depends on, so the requirement that holds a blocker or a response is in the record and not only in prose. Use the requirement identifiers, such as `requirement/approval.decided-within-five-days`.

This satisfies `litespec/core/intent#state-each-journey-blocker-as-a-step-failure`.

### Open the Explanation with the Journey

Write the opening paragraph of the explanation file as the one paragraph a cold reader may read. Say who takes the journey and what they reach by its end, in plain words.

| Instead of | Write |
|:---|:---|
| This is the main journey of the product and it has several steps. | An employee who paid for a work cost follows this path from submission to reimbursement. The employee submits the expense, a manager decides it, and finance pays it out. |

The first version names no actor and no end. The strong version states who takes the journey and what they reach. This satisfies `litespec/core/writing#open-each-explanation-file-with-a-paragraph` and `litespec/core/writing#write-for-the-reader-who-arrives-cold`.

### Write the Example from the Start to the End

Under Example, describe one concrete run from `startedWhen` to `finishedWhen`. Take every step in order and do not skip one. Use only the names, dates and numbers the case needs, each taken from the example cast or from a record the journey cites.

| Instead of | Write |
|:---|:---|
| Cara reimburses the fare on Friday, and Ava sees it in her list. | Ava submits the fare with its amount, 48.90 EUR. Ava attaches the receipt to the fare. Ben approves the fare within the 5 business days. Cara reimburses the approved fare on Friday. Ava sees the expense as paid in her list. |

The first version skips the submission, the receipt and the approval, so a reader cannot check the path against the steps. The second takes each step in order. The numbers come from the cast and the requirement the journey cites, not from a new figure. This satisfies `litespec/core/writing#take-each-example-detail-from-the-cast-or-a-cited-record` and `litespec/core/intent#reach-each-finish-condition-through-a-step`.

### Write the Branches Section with an Occasion

Under Branches, optional, explain when each branch happens and why, with one occasion for each. Do not restate the field. The section takes the field's name so one concept keeps one term.

| Instead of | Write |
|:---|:---|
| If the manager says no, the journey ends. | At step 3, when Ben rejects the fare instead of approving it, the journey ends with the rejection and no payout. Ben may reject any expense, so the employee learns the decision from the list before any money moves. |

This satisfies `litespec/core/writing#keep-the-conditions-when-shortening`.

### Check That the Parts Agree

Read the summary, `startedWhen`, the steps, each `failure`, `branches`, `finishedWhen` and the explanation together. The start in the summary and in `startedWhen` must match, the Example must begin from `startedWhen`, and every blocker in the explanation must appear as a step's `failure`. A step in the Example that the steps do not state, or a path in Branches that `branches` does not list, is a disagreement to fix.

This satisfies `litespec/core/intent#reach-each-finish-condition-through-a-step` and `litespec/core/intent#branch-a-journey-only-between-its-steps`.

### Run Model Check

Run `litespec model check` from the consumer root. It reports `record-invalid` for a journey without `startedWhen`, a journey still holding `betweenSteps`, or a branch missing `step`, `condition` or `nextStep`, and `field-invalid` for a branch whose `step` or `nextStep` names no step of the journey.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the journey record or its explanation file.
- `litespec rules <path to the journey record>` lists the judgment provisions above, and each one is answered.
- A reader who has never met the author can say who takes the journey, where it starts, where it can leave its order, and what ends it, without opening another record.
