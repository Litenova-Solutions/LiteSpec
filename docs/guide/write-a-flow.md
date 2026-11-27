# Write a Flow

Write one flow as a record file `<name>.yaml` and an explanation file `<name>.md`, so a reader can follow the path, see where it can leave its order, and find the requirement behind each block.

A flow states the path one actor takes toward one goal or one problem, from the state in which it starts to the state in which it ends, one step at a time. For example, the Claimwick submit-and-reimburse flow below follows an employee from an unpaid work cost to a paid reimbursement.

Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer built through [Start a Consumer Repository](../tutorial/getting-started.md), with the actor and goal records already written.
- The actor who takes the flow, as an actor record, and the goal it serves or the problem it answers, as a goal, problem, or constraint record.
- The requirements the steps cite, or the features that hold them, so each blocker names what holds it.

The [Intent](../core/intent.md) and [Writing](../core/writing.md) pages state the obligations behind these steps. Read them first; this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how to spell `litespec` in a consumer.

## Procedure

1. Scaffold the flow. Run `litespec new flow <name>`, which writes `.litespec/intent/flows/<name>/<name>.yaml` and reserves the identifier `flow/<name>`. For example, `litespec new flow submit-and-reimburse` writes the Claimwick reimbursement file and reserves `flow/submit-and-reimburse`. When the project lists `intent` in `explanations.layers`, you also get `<name>.md` from the flow template, with its Example and Branches sections. The template fields are placeholders, and `model check` reports a placeholder left unchanged. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.
2. Decide what one flow holds. Give each flow one actor and one reason. An actor who takes two paths toward two goals takes two flows. When one path reaches a goal through two actors, write one flow with a handoff, where the second actor takes over a step through its own `actor` field. A staff flow follows the same shape. Finance exporting the approved batch is a flow that finance takes, with one reason, `startedWhen`, `finishedWhen`, and steps with their branches. A scheduled promise, such as a nightly erasure, is a requirement that a job fulfills, not a flow. This satisfies `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for` and `litespec/core/intent#perform-each-flow-step-with-an-actor`.
3. Give the flow its one reason. Give the flow exactly one reason: `serves` or `answers`. Each is a list holding one identifier, the shape every link field has. The flow serves the goal it moves, or it answers the problem or constraint it resolves. The Claimwick reimbursement flow serves the decided-in-days goal:

   ```yaml
   serves:
   - goal/decided-in-days
   ```

   ```yaml
   answers:
   - problem/expenses-wait-weeks
   ```

   A flow that states both reasons, or neither, leaves a reader unable to tell what its steps lead toward. This satisfies `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for`.
4. Title the flow by the task. Name the task the actor completes, in the words the actor would use. A title that names a system component or a screen says where the path lives, not what the actor does.

   | Kind | Title |
   |:---|:---|
   | Screen | Expense List Page |
   | Component | Reimbursement Service |
   | Task | Submit and Reimburse |

   Name the title for the task the actor completes. This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense`.
5. Write the summary. Keep `summary` to four sentences at most. Say who takes the flow, what starts it, what good looks like, and the consequence at stake for a named party. Make the consequence a concrete effect, not a general reason. For example: an employee who paid a work cost submits the expense and waits for a decision; a good flow decides each expense within five business days, because the employee plans around the money.

   | Instead of | Write |
   |:---|:---|
   | This flow matters because the process is fast. | An employee who paid a work cost submits the expense and waits for a decision. A good flow decides each expense within five business days, because the employee plans around the money. |

   The weak version states no party and no effect. A reader who knows only the summary knows who the flow serves and what goes wrong when it fails. This satisfies `litespec/core/intent#summarize-every-flow-before-its-steps`.
6. Write startedWhen as an observable state. Write `startedWhen` as one sentence naming the state a reader can observe before the first step. It is a state of the world, not the first action the actor takes. For example: the employee has paid a work cost and has not yet submitted it.

   | Instead of | Write |
   |:---|:---|
   | The employee decides to submit an expense. | The employee has paid a work cost and has not yet submitted it. |

   The weak version names a decision, which no reader can observe. The strong version states a state that can be checked. Put any detail of one occasion into the Example, not into `startedWhen`. This satisfies `litespec/core/intent#state-when-each-flow-starts`.
7. Write the steps in order. Write each step as one act of its performer, in a product sentence with the actor first: what the person experiences, not which handler runs. A step can end with the product's observable response after the act, so a reader sees what the product does. Keep how the product computes that response in the cited requirement. A second act of a person is a second step. For example, the Claimwick flow below splits submission and receipt attachment into two steps, and states the payout as the finance step's response.

   | Instead of | Write |
   |:---|:---|
   | `statement: The employee submits the expense and attaches the receipt.` | Two steps: one where the employee submits the expense with its amount and currency, and one where the employee attaches the receipt to the expense. |
   | `statement: Finance pays the employee the amount with the correct tax.` | `statement: Finance reimburses the approved expense, and Claimwick pays the amount into the employee's account.` |

   The first weak version packs two acts into one step, so a reader cannot see where one act ends. The second states what the product does without the rule for how it computes the amount. This satisfies `litespec/core/intent#write-flow-steps-as-sentences-with-ids`.

   Give each step an `id` as `step/<flow>.<act>`, where `<flow>` is the flow's name and `<act>` is the step's act as a verb phrase in kebab case, such as `step/submit-and-reimburse.submit-expense`. Give each step a `title`, a short Title Case name of its act such as `Submit Expense`, and take the act from the title's words in kebab case.

   List order is the main path; no position lives in an id. The book, the trace, and `model query` show the title, while the `statement` keeps the full sentence.

   Use an `actor` field only at a handoff, when a second actor takes over a step, or for the system actor standing for the outside system when no person acts. A job or a reaction never appears in a step.
8. Give each blocker as the step's failure. When a condition stops the flow, write it as the `failure` of the step it blocks, and cite the requirement that holds it on that step. Never write a blocker as a sentence in a branch, a step of its own, or the explanation. For example, the Claimwick reimburse step carries the failure that the expense is not approved, so it waits and no payout is made, with `requirement/approval.decided-within-five-days` in the step's `carries`.

   | Instead of | Write |
   |:---|:---|
   | `failure` on the reimburse step: Finance must not pay out expenses that were not approved, as the policy requires. | `failure` on the reimburse step: The expense is not approved, so it waits and no payout is made, with `requirement/approval.decided-within-five-days` in the step's `carries` |

   The weak version states a rule in the flow, which belongs to the requirement, and it states no outcome a reader can observe. The strong version states the blocked outcome and cites the requirement that holds it. This satisfies `litespec/core/intent#state-each-flow-blocker-as-a-step-failure` and `litespec/core/writing#write-machine-read-fields-in-present-tense`.

   A flow states no rule of its own. Keep a condition with a number, such as a threshold or a limit, in the requirement or the setting that holds it, and cite that record from the step.
9. Write branches. Write each path that leaves the order of the steps as a branch on the step it leaves, in that step's `branches`. Each branch states one `when` condition, and exactly one of `then`, naming the step of the same flow where the path continues, or `ends`, stating the outcome when the flow ends there. A branch that does work of its own states that work in `instead`, with its own `carries` where the work needs it. Give each branch one condition. Two conditions joined by "or" are two branches. For example, the Claimwick decide step carries one branch for a missing receipt and one branch for a refusal, each with its own ending.

   | Instead of | Write |
   |:---|:---|
   | On the decide step, one branch with `when: the receipt is missing or the manager refuses the expense` | On the decide step, one branch per condition, each with its own `then` or `ends` |
   | On the decide step, `when: the manager rejects the expense` with both `then` and `ends` | On the decide step, `when: the manager rejects the expense` with `ends` stating that the expense is rejected and no payout is made |

   The weak versions pack two paths into one branch, so a reader cannot tell where each path goes. The strong versions give each path its own branch with its own ending. This satisfies `litespec/core/intent#branch-a-flow-only-between-its-steps`.

   When a branch retries a step, such as a reminder before a decision, name the step it returns to in `then`, and write the reminder as a step if it is an act of its own. When the branch is an alternative that does work of its own, such as uploading a paper receipt instead of forwarding an email, state that work in `instead` with the requirements it carries.
10. Write the finish and reach each clause. Write `finishedWhen` as one sentence naming the observable end state. Reach every clause of it with a step, and keep each step performable once `startedWhen` and the steps before it hold. For example: the employee holds the reimbursement.

    | Instead of | Write |
    |:---|:---|
    | `finishedWhen: The employee holds the reimbursement, and the expense is erased from the archive.` | `finishedWhen: The employee holds the reimbursement.` |

    The weak version states a clause no step reaches. The erasure happens in a job that fulfills its own requirement, so it stays out of this flow. This satisfies `litespec/core/intent#reach-each-finish-condition-through-a-step`.
11. Cite the requirements from each step. Give each step `carries` with the requirement identifiers it depends on, so the requirement behind a blocker or a response sits in the record, not only in prose. Use the requirement identifiers, such as `requirement/approval.decided-within-five-days`. Leave `carries` off the flow itself: its reason sits in `serves` or `answers`, and each step cites what it alone needs. This satisfies `litespec/core/intent#state-each-flow-blocker-as-a-step-failure`.
12. Open the explanation with the flow. Treat the opening paragraph of the explanation file as the one paragraph a cold reader may read alone. In plain words, say who takes the flow and what they reach by its end. For example: an employee who paid for a work cost follows this path from submission to reimbursement; the employee submits the expense, a manager decides it, and finance pays it out.

    | Instead of | Write |
    |:---|:---|
    | This is the main flow of the product and it has several steps. | An employee who paid for a work cost follows this path from submission to reimbursement. The employee submits the expense, a manager decides it, and finance pays it out. |

    The weak version states no actor and no end. The strong version states who takes the flow and what they reach. This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader`.
13. Write the example from the start to the end. Under Example, describe one concrete run from `startedWhen` to `finishedWhen`. Take every step in order, skipping none. Use only the names, dates, and numbers the case needs, each taken from the cast or from a record a step cites. For example: Ava submits the fare with its amount, 48.90 EUR; Ava attaches the receipt to the fare; Ben approves the fare within the 5 business days; Cara reimburses the approved fare on Friday; Ava sees the expense as paid in her list.

    | Instead of | Write |
    |:---|:---|
    | Cara reimburses the fare on Friday, and Ava sees it in her list. | Ava submits the fare with its amount, 48.90 EUR. Ava attaches the receipt to the fare. Ben approves the fare within the 5 business days. Cara reimburses the approved fare on Friday. Ava sees the expense as paid in her list. |

    The weak version skips the submission, the receipt, and the approval, so a reader cannot check the path against the steps. The strong version takes each step in order. The numbers come from the cast and the requirement a step cites, not from a new figure. This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader` and `litespec/core/intent#reach-each-finish-condition-through-a-step`.
14. Write the branches section with an occasion. Under Branches, optional, explain when each branch happens and why, giving one occasion for each. Do not restate the field. For example: at the decide step, when Ben rejects the fare instead of approving it, the flow ends with the rejection and no payout; Ben may reject any expense, so the employee learns the decision from the list before any money moves. The section keeps the field name, so one concept keeps one term.

    | Instead of | Write |
    |:---|:---|
    | If the manager says no, the flow ends. | At the decide step, when Ben rejects the fare instead of approving it, the flow ends with the rejection and no payout. Ben may reject any expense, so the employee learns the decision from the list before any money moves. |

    This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense`.
15. Check that the parts agree. Read the summary, `startedWhen`, the steps, each `failure`, each step's `branches`, `finishedWhen`, and the explanation together. The start in the summary and in `startedWhen` match, the Example begins from `startedWhen`, and every blocker in the explanation appears as a step `failure`. Fix any disagreement: a step in the Example that the steps do not state, or a path in Branches that no step's `branches` lists. For example, check that the Example opens with Ava holding an unsubmitted fare, exactly what the Claimwick `startedWhen` states. This satisfies `litespec/core/intent#reach-each-finish-condition-through-a-step` and `litespec/core/intent#branch-a-flow-only-between-its-steps`.
16. Run model check. Run `litespec model check` from the consumer root. It reports `record-invalid` for a flow without `startedWhen`, a flow holding both `serves` and `answers` or neither, a step without `title`, or a branch missing `when`, carrying both or neither of `then` and `ends`, joining two conditions with "or" in `when`, or continuing at a step of another flow. It reports `identifier-invalid` for a step id outside the `step/<flow>.<act>` form, `identifier-title-invalid` for a step whose act is no word subsequence of its title, `reference-unknown` for a `then` naming no step, and `step-unreached` for a step no path reaches.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the flow record or its explanation file.
- `litespec rules <path to the flow record>` lists the judgment provisions above, and you have answered each one.
- `litespec model query step/<flow>.<act>` shows each step with its trace: the screens and use cases serving it, the requirements it carries, and the code and tests behind them.
- A reader who has never met you can say who takes the flow, where it starts, where it can leave its order, and what ends it, without opening another record.
