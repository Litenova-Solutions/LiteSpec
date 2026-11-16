# Model a Behavior Module

Specify one behavior module from Intent, working through one kind of record at a time. A behavior module gathers one business area's glossary terms, aggregates, rules, use cases, reactions, jobs, read models, notifications, settings, domain events, module events, and permissions into record files. The worked example is the spending module of the Claimwick product.

Intent records say what the product promises; Behavior records say how the product keeps those promises. The model builder, the tool that checks the project, reads these record files. Each step names the standard it satisfies, and the tutorial adds no rule of its own.

## Prerequisites

- Finish [Start a Consumer Repository](getting-started.md) through its configuration steps.
- Read the product brief and the requirements this module fulfills.
- Read the decisions that constrain identity, persistence, security, or external behavior.
- Copy the `module.yaml`, `aggregate.yaml`, `use-case-command.yaml`, `use-case-query.yaml`, and `module-event.yaml` templates from the [template index](../../templates/consumer/README.md), and open `.litespec/glossary.yaml` for the module terms.

## Lesson

1. Name the module terms. Write one term entry per word, with its meaning, its layer, its aliases, and the words it rejects. Then use the same term in documentation, domain types, use cases, API descriptions, and frontend features. This satisfies `litespec/backend/domain#use-one-ubiquitous-language`, `litespec/core/domain-model#define-every-behavior-term-in-the-glossary`, and `litespec/core/domain-model#mark-each-term-with-its-layer`.

    | Term | Meaning | Layer | Aliases | Rejected words |
    |:---|:---|:---|:---|:---|
    | `term/expense` | One work cost an employee submitted for approval | behavior | None | voucher |
    | `term/receipt` | Proof of payment attached to an expense | behavior | None | docket |
    | `term/policy` | The company spending policy and its receipt threshold | behavior | None | None |
    | `term/approver` | The manager who decides a submitted expense | intent | None | None |

    Rejected words stop a later contributor from writing `voucher` beside `expense`. Claimwick needs no aliases for these four terms; where synonyms exist, aliases let industry readers find your word under the name they already know. Rejected words and aliases together tell an agent which plausible names are wrong for this domain and which are accepted synonyms.

2. Set the scene before writing rules. Pick one occasion the module exists for, and name who acts, what surrounds them, and what they use today instead. Record it in the module's explanation file under Example, then reuse its people and figures in every record beneath it. Keep the shared cast on the example cast page. This satisfies `litespec/core/behavior#illustrate-pages-with-examples-from-the-cast`.

    Month end brings every team's expenses at once: employees paid work costs from their own pockets and wait weeks for a decision, chasing managers by email. Without an expense record, finance reconstructs who spent what from forwarded receipts and memory.

3. Write the module record file with its summary, and give the module a person-chosen identifier such as `module/spending`. List each module it depends on, or leave the dependency field empty when the module stands alone. Spending depends on no other module, so its list stays empty. This satisfies `litespec/core/behavior#shape-each-module-page`.
4. Draw aggregate boundaries from invariants. An aggregate is a consistency boundary, the state that must stay valid together in one command, and an invariant is a condition that holds before and after every successful change. State that exists only because of a technical mechanism is infrastructure, not a Behavior record, while state the business cares about is an aggregate with its invariants. This satisfies `litespec/backend/domain#treat-aggregates-as-consistency-boundaries`, `litespec/backend/domain#keep-child-entities-inside-the-aggregate-boundary`, and `litespec/core/domain-model#keep-technical-state-out-of-behavior`.

    | Record | Owns | References by typed ID |
    |:---|:---|:---|
    | `aggregate/spending.expense` | Expense lifecycle, total, reimbursement facts | `ReceiptId`, `EmployeeId` |
    | `aggregate/spending.receipt` | Receipt attachment and removal | `ExpenseId` |
    | `aggregate/spending.policy` | The company spending policy and its receipt threshold | None |
    | `aggregate/spending.reminder-preference` | Whether each manager gets approval reminders | `EmployeeId` |

    Each manager holds one reminder preference, `aggregate/spending.reminder-preference`, with the single state `Active`, because the preference carries no lifecycle of its own. An aggregate owns a child when the child has no independent boundary and changes only through the aggregate. Reference another aggregate by typed ID when it changes on its own. Do not place `Employee` inside `Expense` for convenient navigation. This satisfies `litespec/backend/domain#reference-other-aggregates-by-id`.

5. Define every state before writing transitions. A state is a business-valid condition with its required data and permitted behavior. This satisfies `litespec/backend/domain#model-every-aggregate-lifecycle-with-state-types` and `litespec/core/domain-model#model-each-aggregate-lifecycle-with-states`.

    | State | Meaning | Required facts |
    |:---|:---|:---|
    | `Submitted` | The expense awaits a decision | None |
    | `Approved` | The manager approved the expense | Approver, decision date |
    | `Refused` | The manager refused the expense | Approver, decision date, reason |
    | `Reimbursed` | The reimbursed state of an expense whose payout reached the submitter | Reimbursement date |
    | `Erased` | The personal data of the expense was erased after its retention | None |

    Create an abstract state record and one sealed record per state, in the C# shape the [.NET Domain Layer](../backend/dotnet/domain.md) page states, and put state-specific data on the state record. This satisfies `litespec/backend/dotnet/domain#model-every-aggregate-lifecycle-with-state-records`.

    For each state, answer what makes it true, which data it alone requires, and which actions it allows and rejects. Define every state fully.
6. Connect the states with transitions. List every allowed state change with the use case or job that moves it in the aggregate's `transitions` field. This satisfies `litespec/core/domain-model#connect-every-state-through-transitions`.

    | From | To | Moved By |
    |:---|:---|:---|
    | `-` | Submitted | `use-case/spending.submit-expense` |
    | Submitted | Approved | `use-case/spending.approve-expense` |
    | Submitted | Refused | `use-case/spending.refuse-expense` |
    | Approved | Reimbursed | `use-case/spending.reimburse-expense` |
    | Reimbursed | Erased | `job/spending.erase-expired-personal-data`, after `P7Y` |

    Creation is a transition from `-`, so the first state stays reachable. Approval and refusal are separate use cases because each ends Submitted in its own state. The last transition is timed: seven years after reimbursement, the erasure job moves the expense to Erased.
7. Declare the data classes with their erasure positions. A data class groups fields that share one classification and one fate on erasure. Then list every field the aggregate keeps in the aggregate's `data` field, with its meaning, data class, and classification. This satisfies `litespec/core/privacy#declare-every-data-class-with-its-erasure-position` and `litespec/core/privacy#classify-every-aggregate-data-field`.

    | Data class | Classification | Fields | On erasure | Enforced by |
    |:---|:---|:---|:---|:---|
    | `submitter-contact` | personal | employee | pseudonymize after 7 years | `job/spending.erase-expired-personal-data` |
    | `expense-ledger` | internal | amount, receipt, state | retain for 7 years by statute | `job/spending.erase-expired-personal-data` |

    | Name | Meaning | Data Class | Classification |
    |:---|:---|:---|:---|
    | `employee` | The employee who submitted the expense | `submitter-contact` | `personal` |
    | `amount` | The exact amount with its currency | `expense-ledger` | `internal` |
    | `receipt` | The proof of payment, when one is attached | `expense-ledger` | `internal` |
    | `state` | The lifecycle state of the expense | `expense-ledger` | `internal` |

    Every field names its data class, and each class carries its own retention: the period, what starts it, the basis, the purpose, what enforces it, and the exceptions. A personal or sensitive class that is retained or exempt needs an Intent decision or requirement that accepts it.
8. Write the rules with kinds. Give each rule a person-chosen identifier of one to six lowercase words, and keep `not`, `no`, `never`, or `cannot` in the name when the statement uses it. This satisfies `litespec/core/domain-model#state-each-rule-with-its-kind` and `litespec/core/domain-model#keep-the-negation-word-in-the-rule-name`.

    | Rule | Kind | Statement | Refusal code |
    |:---|:---|:---|:---|
    | `rule/expense.receipt-required-above-threshold` | `precondition` | An expense above the policy threshold needs a receipt. | `spending.receipt-missing` |
    | `rule/expense.approver-is-not-submitter` | `precondition` | An approver is not the submitter. | `spending.own-expense` |
    | `rule/expense.reimbursed-once` | `invariant` | An expense is reimbursed at most once. | `spending.already-reimbursed` |
    | `rule/expense.approved-before-reimbursement` | `precondition` | An expense is reimbursed only after approval. | `spending.not-approved` |
    | `rule/receipt.attaches-before-decision` | `precondition` | A receipt attaches before the expense is decided. | `spending.expense-decided` |

    An `invariant` holds after every change to its aggregate, while a `precondition` guards the behavior before it runs and refuses caller-correctable input. A `calculation` derives a value and is proven by a scenario with `examples`. The remaining kinds cover atomicity, held reservations, and frozen values.

    Each rule that refuses names its refusal code from the module's refusal set. This satisfies `litespec/core/domain-model#name-the-refusal-a-broken-rule-produces`. A rule whose `governs` names more than one aggregate names every governed aggregate in `binds`, or the check reports `rule-binds-missing`: `rule/spending.receipt-filed-once`, a receipt file supports one expense across spending, binds `aggregate/spending.expense` and `aggregate/spending.receipt`. This satisfies `litespec/core/domain-model#name-every-spanned-aggregate-in-binds`.

    A value the business changes while the system runs, such as the receipt threshold a manager sets, is aggregate data: its range is a rule with a refusal, and its change is a use case with access and audit. A setting is operator configuration the running system reads, such as a provider timeout or a delay the operator tunes. Input shape stays in the validator, and current aggregate state stays in the aggregate. This satisfies `litespec/backend/application#separate-input-validation-from-invariants`.
9. Name the people and what each may do. Write one actor record file per party, and one role record file per grantable set of permissions. Roles grant permissions, and each use case later declares its access against one grant. This satisfies `litespec/core/access#grant-permissions-through-roles` and `litespec/backend/application#enforce-target-authorization-in-the-pipeline`.

    | Actor | Role | Permission |
    |:---|:---|:---|
    | `actor/employee` | `role/employee`, whose scope is self | `permission/request-expense` |
    | `actor/manager` | `role/approver`, whose scope is team | `permission/approve-expense` |
    | `actor/finance` | `role/finance`, whose scope is platform | `permission/post-reimbursement` |

10. Write the use cases. Give each use case a name that is unique across the project, so its results and scenarios resolve. Name the kind, `command` or `query`, the goal, what happens, the flow steps it serves, the requirements it fulfills, the access it declares, the entry points, the aggregates it writes and reads, its effects, and the domain events it raises. This satisfies `litespec/backend/application#use-this-use-case-layout`, `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`, `litespec/core/model#connect-every-behavior-record-to-intent`, and `litespec/core/access#declare-access-on-every-use-case`.

    | Use case | Kind | Serves | Access grant | Writes |
    |:---|:---|:---|:---|:---|
    | `use-case/spending.submit-expense` | `command` | `flow/submit-and-reimburse#s1` | `permission/request-expense` | `aggregate/spending.expense` |
    | `use-case/spending.approve-expense` | `command` | `flow/submit-and-reimburse#s3` | `permission/approve-expense` | `aggregate/spending.expense` |
    | `use-case/spending.reimburse-expense` | `command` | `flow/submit-and-reimburse#s4` | `permission/post-reimbursement` | `aggregate/spending.expense` |
    | `use-case/spending.list-expenses` | `query` | `flow/submit-and-reimburse#s5` | `permission/list-expenses` | None |

    Every use case carries at least one of `serves` and `fulfills`. Access declares one grant and the tenant scope, and a tenant of `own` limits the permission to the caller's own records. `writes` names every aggregate the use case writes, and `reads` names every aggregate, setting, and read model it reads. None of these commands writes several aggregates, so none declares `consistency`. This satisfies `litespec/core/access#declare-access-on-every-use-case`, `litespec/core/use-cases#name-every-aggregate-a-use-case-writes-or-reads`, `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`, and `litespec/backend/application#read-another-modules-state-through-a-read-model`.

    Every edge the use case causes goes in `effects` with one verb and its timing: `sends` or `calls`. This satisfies `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`. Every input field names its origin and classification, and caller-supplied values on money or access paths carry bounding rules. This satisfies `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`. Every command names its idempotency, and when the project selects the audit extension, every command states its audit position. This satisfies `litespec/core/use-cases#name-the-idempotency-of-every-command` and `litespec/core/use-cases#state-the-audit-position-of-every-command`.

    A query changes nothing, and declares its `output` in `include` groups with freshness, plus its paging. This satisfies `litespec/core/use-cases#declare-every-query-output` and `litespec/core/use-cases#declare-paging-on-every-collection-query`. An output group whose figure a command re-decides names the deciding command or its aggregate in `freshness.decidedBy`, allowed only with `snapshot` or `eventual` freshness. The reimbursed-expenses list names `use-case/spending.reimburse-expense`, the command that decides each payout. This satisfies `litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure`.
11. Name one result per ending. A result is one way a use case ends: `success`, or a named `refusal` with its stable `code`. Order the refusals by precedence, since the first match wins. This satisfies `litespec/core/use-cases#end-every-use-case-in-named-results`, `litespec/core/use-cases#order-every-refusal-list-by-precedence`, and `litespec/backend/application#model-expected-use-case-refusals-explicitly`.

    | Result | Outcome | Code | Reader Text |
    |:---|:---|:---|:---|
    | `result/submit-expense.submitted` | `success` | None | The expense is submitted. |
    | `result/submit-expense.receipt-missing` | `refusal` | `spending.receipt-missing` | Attach the receipt for this expense, then submit again. |

    A success carries its ending in `meaning`. A refusal names its `code` from the module's refusal set and keeps its `sentence`, while the meaning lives on the refusal set.

    Leave the generic refusals out: `forbidden`, `invalid-input`, `not-found`, `conflict`, `unauthenticated` and `sign-in-too-old` derive from access and input, so the use case writes only its own. This satisfies `litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them`. A refusal that stores the attempt carries `recorded: true`, and a success with recorded outcomes inside it carries `codes`.

    Each refusal explains what the caller sees, and the code stays stable across releases. The backend stack derives the HTTP status from the outcome. This satisfies `litespec/backend/api#use-consistent-status-codes`.
12. Write the scenarios. A scenario is one working example in `given`, `when`, `then`: the situation, the action, and the expected ending, with the result it ends in. A scenario with no proving test yet needs a gap waiver naming the use case whose proof closes it, as the [waiver page](../core/waivers.md) states. This satisfies `litespec/core/use-cases#write-every-scenario-in-given-when-then` and `litespec/core/use-cases#name-the-result-each-scenario-ends-in`.

    | Scenario | Given | When | Then | Result |
    |:---|:---|:---|:---|:---|
    | `scenario/submit-expense.submitting-with-a-receipt-succeeds` | An employee paid for a train ticket and kept the receipt. | The employee submits the expense with the receipt. | The expense is submitted. | `result/submit-expense.submitted` |
    | `scenario/approve-expense.approving-a-submitted-expense-succeeds` | An employee submitted an expense with its receipt. | The manager approves the expense. | The expense is approved. | `result/approve-expense.approved` |

    When the scenario proves a calculation, add `examples` with the debated rows. This satisfies `litespec/core/domain-model#tabulate-every-calculation-with-examples`. Name each test by the observable behavior it asserts. This satisfies `litespec/backend/testing#name-tests-by-observable-behavior`. An acceptance test carries one proves mark naming its scenario, and the model builder confirms the claim when the test calls the use case through the public boundary and asserts the result the scenario names.
13. Record the domain events. A domain event is an immutable record of a completed fact, so name what completed and which fields it carries. This satisfies `litespec/core/domain-model#state-each-event-with-meaning-and-carries` and `litespec/backend/dotnet/domain#raise-immutable-domain-facts`.

    | Domain event | Meaning | Carries |
    |:---|:---|:---|
    | `domain-event/expense.submitted` | An expense was submitted. | `expenseId`, `amount`, `currency`, `employeeId` |
    | `domain-event/expense.reimbursed` | An expense was reimbursed. | `expenseId`, `employeeId`, `amount`, `currency`, `reimbursedAt` |

    A fact that leaves the product is an integration event with a direction, a CloudEvents type and source, and a payload. A fact one module publishes for the other modules of the same product is a module event instead: it names the domain event it translates in `translates` and selects the fields it publishes from that event in `fields`, with no direction and no external transport. Reimbursing publishes `module-event/spending.expense-reimbursed`, which translates `domain-event/expense.reimbursed` for the reporting module. The use case declares nothing for it: it raises `domain-event/expense.reimbursed`, and the module event names that domain event in `translates`. This satisfies `litespec/core/edges#describe-every-integration-event-for-its-direction` and `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`.
14. Write the reactions, jobs, read models, notifications, and settings as their own records. Work that answers a domain event, a module event, or an integration event is a reaction: it names the events it answers in `triggeredBy`, runs at most one use case through `effects`, and never writes an aggregate directly. A reaction that undoes a committed step of a multi-step process names the step's use case in `compensates`, runs another use case, and answers the event that reports the later failure; Claimwick's reminder reaction carries no `compensates`, because it undoes nothing. This satisfies `litespec/core/automation#name-the-step-a-compensating-reaction-undoes`.

    Work that runs on a schedule is a job: it names its `kind` with its `schedule`, `selection`, and `guard`, and it never writes an aggregate directly either.

    Work that needs another module's state reads a read model: reporting keeps `read-model/reporting.reimbursed-expense` with one row per reimbursed expense, each field naming its published field in `from`, and its use cases name the read model in `reads`. A read model whose figure a command re-decides names the deciding command in `freshness.decidedBy`: the reimbursed-expense read model names `use-case/spending.reimburse-expense`. This satisfies `litespec/core/automation#trigger-every-reaction-from-its-declared-events`, `litespec/core/automation#run-every-job-from-its-declared-kind`, `litespec/core/automation#declare-every-reaction-and-job-edge-in-effects`, `litespec/core/automation#write-no-aggregate-directly-from-a-reaction-or-job`, `litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job`, and `litespec/core/automation#declare-each-read-model-field-from-a-published-field`.

    File each reaction and job with the module whose records it changes, and run each reaction in a dedicated handler. This satisfies `litespec/core/automation#file-every-reaction-and-job-with-the-module-it-changes` and `litespec/backend/dotnet/application#run-reactions-in-dedicated-handlers`.

    | Record | Shape |
    |:---|:---|
    | `reaction/spending.send-approver-reminders-on-expense-submitted` | On `domain-event/expense.submitted`, sends `notification/spending.approval-reminder`. |
    | `job/spending.erase-expired-personal-data` | Scheduled daily; runs `use-case/spending.erase-personal-data` per expired expense. |
    | `read-model/reporting.reimbursed-expense` | One row per reimbursed expense, each field from `module-event/spending.expense-reimbursed`. |
    | `notification/spending.approval-reminder` | Email to the manager with the expense and its waiting time. |
    | `setting/spending.approval-reminder-delay` | Duration, default 72 hours, set by the operator, refuses at the bound. |

    Describe every notification with its audience, channel, delivery, and consent rule, and claim each promise it makes from a requirement. This satisfies `litespec/core/edges#describe-every-notification-with-audience-channel-and-consent` and `litespec/core/edges#cite-the-requirement-behind-every-notification-claim`. Hold every commercial, legal, or varying setting default to a requirement, and keep model invariants in rules. This satisfies `litespec/core/use-cases#tie-every-setting-default-to-a-requirement` and `litespec/core/domain-model#keep-model-invariants-in-rules`.
15. Name the entry points and the external systems. Claimwick has one entry point: the expenses app, a browser the employee signs into with a session. Give each entry point its kind. This satisfies `litespec/core/edges#give-each-entry-point-a-kind`.

    Declare every operation on an outside system with its timeout, retries, and response, and route erasure through an operation. The payout bank declares the data it holds, the expense ledger of each expense, in `processes`. This satisfies `litespec/core/edges#describe-every-external-system-operation` and `litespec/core/access#declare-erasure-for-every-external-system`.

    Declare retention on every data class with its period, start, basis, purpose, enforcement, and exceptions.
16. Place the code for one use case. Follow [Write Code for a Use Case](../guide/write-code-for-a-use-case.md): stage the aggregates the use case owns in one handler, and let the pipeline commit once, with the consistency the record declares. This satisfies `litespec/backend/application#dispatch-only-declared-effect-edges` and `litespec/backend/persistence#commit-each-command-pipeline-once`.
17. Mirror the records in folders. Follow [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) to mirror the module, aggregate, `UseCases/`, and use case path in every layer, and create subfolders only when real types require them. This satisfies `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`.
18. Run the model check with `dotnet litespec model check` from the consumer root. It resolves every citation, follows the marks from code and tests to their records and the screens calling each use case, prints a level beside each finding, and reports each unproved scenario.

    Every finding about a missing, unknown, or wrong link blocks: level G on records, level S on code. A record that must stay unreached carries a gap waiver. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## What You Built

- One module with its terms, aggregates, data classes, rules, use cases, results, scenarios, domain events, module events, reactions, jobs, read models, notifications, settings, and permissions.
- Person-chosen identifiers throughout, with every citation resolved and every edge between records declared.
- Every scenario proved by a passing test, or covered by a gap waiver with `kind`, `expiresOn`, `exit` and `closedBy`.
- Next, follow [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) to place each specified element in code. When you entered this tutorial from [Start a Consumer Repository](getting-started.md) step 16, return there instead and continue with step 17.
