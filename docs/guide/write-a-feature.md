# Write a Feature

Write one feature as a record file, one requirement record per commitment, and an explanation file `<feature>.md`, so a developer can tell what to build and a tester can tell what passes.

A feature is one offering that answers one problem and advances goals. It sits in the `features/` folder of the problem it answers: one record file at `.litespec/intent/problems/<problem>/features/<name>/<name>.yaml` states the offering and its links, and each commitment the feature makes is a requirement record in its own folder under the feature's `requirements/` folder. For example, the Claimwick approval feature answers the expenses-wait-weeks problem, and this guide builds its records step by step.

The writing steps show a weak version and a strong version from the Claimwick expense product. Each step names the provisions it satisfies, and this guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer built through [Start a Consumer Repository](../tutorial/getting-started.md), with the product record already written.
- The problem the feature answers and the goals it advances, each in its own record file. [Write a Problem](write-a-problem.md) writes a problem.

The [Intent](../core/intent.md) and [Writing](../core/writing.md) pages state the obligations behind these steps. Read them first; this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how to spell `litespec` in a consumer.

## Procedure

1. Scaffold the feature file. Run `litespec new feature <problem>.<name>` before writing any prose, with the problem first and the name in kebab-case, such as `litespec new feature expenses-wait-weeks.expense-submission`, and the title in plain words. For example, that command scaffolds the Claimwick expense-submission feature under the expenses-wait-weeks problem. The command writes `<name>/<name>.yaml` under `.litespec/intent/problems/<problem>/features/`, with the problem as the parent and the identifier `feature/<name>` reserved. This satisfies `litespec/core/model#write-every-record-in-its-record-file`, `litespec/core/model#write-every-record-in-its-home`, and `litespec/core/intent#answer-one-problem-with-each-feature`.
2. State the offering and its links. Use `offering` for one paragraph that states what the software does and for whom. Link the goals it advances in `advances`, and name the glossary terms the feature uses in `terms`:

   ```yaml
   offering: Claimwick takes each expense with its receipt at submission time.
   advances:
     - goal/faster-reimbursement
   terms:
     - term/expense
   successCriteria:
     - An expense above the 25 EUR threshold cannot be submitted without a receipt.
     - No manager can approve their own expense.
   excludes:
     - Reminders for late decisions belong to feature/approval-reminders.
     - Whether a manager on leave hands decisions to a deputy is open for the owner.
   ```

   The explanation file restates `successCriteria` and `excludes` in its Success Criteria and Boundaries sections; it adds no fact the record lacks.

   Write each link on the feature, the record that points. For example, the approval feature names `goal/faster-reimbursement` in its own `advances`, not in the goal. This satisfies `litespec/core/intent#state-each-feature-with-its-offering` and `litespec/core/model#write-links-on-the-record-that-points`.
3. Write one requirement per commitment. Turn each commitment the feature makes into its own requirement record. Scaffold it with `litespec new requirement <feature>.<name>`, such as `litespec new requirement approval.above-threshold-receipt`. You get the requirement at `.litespec/intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml`, with its identifier `requirement/<feature>.<name>` and its title filled, and no explanation file. For example, the Claimwick receipt commitment becomes the requirement `requirement/approval.above-threshold-receipt` in its own folder.

   Then fill the record fields: the kind, `functional`, `quality`, or `policy`; one testable sentence in `statement`; one line of reason in `rationale`; and the records behind it in `cites`.

   Cite each applying constraint from the requirement it binds. This satisfies `litespec/core/intent#identify-every-requirement`, `litespec/core/intent#state-each-requirement-with-kind-and-statement`, and `litespec/core/intent#cite-the-reasons-of-every-requirement`.

   A commitment that belongs to no feature is a product requirement instead. It lives at `.litespec/intent/requirements/<name>/<name>.yaml` with its identifier `requirement/<name>`, its parent the product, and its `cites` naming at least one constraint, goal, or problem.

   The requirement file holds one mapping at its top level and no `records` list, because a requirement holds no entries:

   ```yaml
   type: requirement
   id: requirement/approval.above-threshold-receipt
   title: Expense Above Threshold Carries Receipt
   kind: functional
   statement: Claimwick accepts an expense above the policy threshold only with a receipt attached.
   rationale: Finance needs proof for each expense above the threshold to defend the tax file.
   cites:
     - goal/faster-reimbursement
   except:
     - condition: The amount is under the threshold.
       fallback: requirement/approval.auto-accepted-under-threshold
   setBy: product
   ```
4. Name each requirement by its behavior. Name the specific behavior or fact in each requirement title. In a list, a short title that states only a topic says nothing about what holds.

   | Instead of | Write |
   |:---|:---|
   | Receipts | Expense Above Threshold Carries Receipt |
   | Waiting Time | Approver Sees Days Waited |

   Name the title for the specific behavior or fact the requirement requires. This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense`.
5. Write a statement a tester can decide. Keep each statement to one behavior, with every condition, date, and amount its result depends on. Put the actor first. When you see two behaviors in one statement, split them into two requirements, so each one is fulfilled, proven, and changed on its own.

   | Instead of | Write |
   |:---|:---|
   | The approver can see how long expenses have waited. | Claimwick shows the approver each waiting expense with the number of business days since its submission. |
   | Claimwick reminds the approver and escalates late expenses to finance. | Two requirements: one for the reminder, one for the escalation, each with the day it happens. |

   The weak version states no starting point and no unit, so a tester cannot tell which number is right. This satisfies `litespec/core/intent#state-one-testable-behavior-in-each-requirement` and `litespec/core/writing#write-machine-read-fields-in-present-tense`.
6. Keep each rationale to its reason. Keep the rationale to one line that says why the requirement exists. Add no behavior there, claim no single cause where several exist, and make no comparison nobody measured.

   | Instead of | Because |
   |:---|:---|
   | A second pair of eyes keeps approvals honest. | A metaphor. Write: A second person checks each expense before it is paid. |
   | A late decision is the main reason employees chase managers. | An unmeasured claim. Write: Late decisions keep employees waiting for their money. |

   This satisfies `litespec/core/intent#keep-each-rationale-to-its-reason` and `litespec/core/writing#write-machine-read-fields-in-present-tense`.
7. State each quality requirement as a scenario. Give each quality requirement the scenario that confirms it. State the `stimulus` that triggers it, the `environment` it holds in, the `response` the product shows, the `measure` that names what is counted, the `target` that names the number aimed for, and the `check` that confirms it, and add `conditions`, `runsOn`, and `evidence` where they apply. For example, the Claimwick reminder requirement below confirms delivery within one hour under its nightly probe. This satisfies `litespec/core/intent#state-each-quality-requirement-as-a-scenario`.

   ```yaml
   type: requirement
   id: requirement/approval-reminders.reminder-delivered-within-hour
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
8. State each exception with its fallback. When a requirement bends in a named case, carry that case in `except` with its `condition` and the `fallback` requirement that holds instead, as the receipt requirement above shows. Give the fallback its own named record and its own folder, so every exception ends in a promise. For example, expenses under the threshold fall back to auto-acceptance under `requirement/approval.auto-accepted-under-threshold`. Use `setBy` to name who sets the requirement values: `product`, `operator`, or `tenant`. This satisfies `litespec/core/intent#state-each-exception-with-its-fallback`.
9. Open the explanation with the feature's purpose. Write `<feature>.md` beside the feature file. Open it with one paragraph on what the feature is for and what it covers, then add Capabilities, Success Criteria, Boundaries, and an optional Example, in that order. For example, the approval explanation opens with what approval covers for Claimwick, then walks its capabilities. Let each section do its own job, without repeating the opening or the summary. Keep checkable facts in the record files; the explanation helps a reader understand them. This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader` and `litespec/core/model#explain-each-record-beside-its-record-file`.
10. Group capabilities by task. Under Capabilities, describe what the user can do, with one short paragraph or subsection per task. Stay with behavior that a requirement states. A rule that appears only in the prose is a requirement nobody fulfills, so write it as a requirement or take it out.

    | Instead of | Because |
    |:---|:---|
    | Expenses waiting more than 10 days show in red. | No requirement states it. Add a requirement for it, or remove the sentence. |

    This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader`.
11. State success criteria the feature controls. Under Success Criteria, list only the outcomes the feature brings about on its own, each one checkable by a tester. State them in `successCriteria`; the section restates that field. Move an outcome that depends on people or on other features to the goal the feature advances.

    | Instead of | Write |
    |:---|:---|
    | Every expense is decided within 5 business days. | An expense above the 25 EUR threshold cannot be submitted without a receipt. No manager can approve their own expense. |

    How fast managers decide depends on the managers, so the weak version sets a test the feature cannot pass on its own. This satisfies `litespec/core/intent#state-success-criteria-the-feature-controls`.
12. State the boundaries. Under Boundaries, say what the feature leaves to other features, which features or decisions it depends on, and which questions the owner has not decided. State them in `excludes`; the section restates that field. Check the other Intent records first, and keep only the questions none of them settles. For example, the Claimwick approval boundaries read:

    ```markdown
    Reminders for late decisions are not part of this feature. They belong to `feature/approval-reminders`.

    The feature depends on `decision/0001-receipt-threshold` for the 25 EUR threshold.

    One question is open for the owner: whether a manager on leave hands decisions to a deputy.
    ```

    This satisfies `litespec/core/intent#state-the-boundaries-of-every-feature`.
13. Name the operation, not an impression. Give each sentence a verb that says what happens to what, and use the domain's precise term where one exists. Replace an intensifier or an experience claim with the behavior itself or the measured target.

    | Instead of | Write |
    |:---|:---|
    | The feature handles approvals smoothly. | The manager approves or refuses each expense, and the employee sees the decision in their own list. |
    | The approver quickly spots late expenses. | Claimwick shows the approver each waiting expense with the number of business days since its submission. |

    This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense`.
14. Explain a requirement only when it needs it. When a statement needs context it cannot carry, such as the date a comparison uses or a term a reader may misread, add `<name>.md` beside `<name>.yaml` in the requirement folder. Open it with one paragraph and add an optional Example. For example, the threshold requirement carries a note on which policy date its comparison uses. Leave acceptance criteria out of it, because the Behavior scenarios that fulfill the requirement state them, and describe no behavior the statement does not hold. When the statement and its rationale say everything, leave the file out. This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader`.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the feature file, its explanation, or any requirement file.
- `litespec model query feature/<name>` shows the feature with its problem and requirements.
- `litespec rules <path to the feature record>` lists the judgment provisions above, and you have answered each one.
- A developer can tell what to build from the requirements, and a tester can tell what passes and what fails. Every date, amount, and condition that matters is stated, and no explanation sentence adds behavior the requirements do not hold.
