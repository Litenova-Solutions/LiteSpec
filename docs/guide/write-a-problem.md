# Write a Problem

Write one problem as a record file `<name>.yaml` and an explanation file `<name>.md`, so a reader who has never met the author can understand the problem, judge its evidence, and propose a different solution.

A problem states a pain that people outside the team have today, before any feature answers it.

Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer built through [Start a Consumer Repository](../tutorial/getting-started.md), with the product record already written.
- An actor record for the person who has the pain, outside the team.
- At least one source that shows the pain: a research record reporting what sufferers said or did, or a URL.

The [Intent](../core/intent.md) and [Writing](../core/writing.md) pages state the obligations behind these steps. Read them first; the guide shows one compliant way to meet them. The [command index](../tools/README.md) states how to spell `litespec` in a consumer.

## Procedure

### Scaffold the Problem

Run `litespec new problem <name>` before writing any prose. The command writes `.litespec/intent/problems/<name>/<name>.yaml` and reserves the identifier `problem/<name>`. When the project lists `intent` in `explanations.layers`, you also get `<name>.md` from the problem template. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Name the Failure in the Title

Put the failure in the title: what goes wrong, or what it costs the people who have it. A theme or a feature names the topic, but it leaves out the problem itself.

| Kind | Title |
|:---|:---|
| Theme | Approval Process |
| Feature | Approval Reminders |
| Problem | Expenses Wait Weeks |

This follows the problem row of the title forms on the [Writing](../core/writing.md#title-forms) page and satisfies `litespec/core/writing#write-every-record-title-in-title-case`.

### State the Pain in Its Fields

Start the fields with the sufferer in `affects`. Then state the pain in `summary` and in a one-sentence `statement`. That sentence names the sufferer, the condition they face, and the consequence, without naming any solution.

```yaml
type: problem
id: problem/expenses-wait-weeks
title: Expenses Wait Weeks
affects:
  - actor/employee
summary: Employees who submit work expenses wait weeks for a decision and chase managers by email.
statement: Submitted expenses wait weeks for a decision, so employees chase managers by email.
failureCases:
  - An expense waits without a decision because nothing shows the approver how long it has waited.
  - The employee cannot see the decision because nothing tells the employee when the approver decides.
workaround: Employees email the approver for each expense that waits and keep track of which expenses are still open.
cost: Employees report an average wait of 3 weeks; nobody has measured it. No total of carried money is recorded, so the money cost is unknown.
resolutionCriteria:
  - Each submitted expense is decided within 5 business days.
  - The employee sees the decision without asking the approver.
evidence:
  - claim: Employees wait three weeks on average for reimbursement.
    source: research/approval-time-benchmarks
    readOn: 2026-11-01
    kind: reported
```

The explanation file restates `failureCases`, `workaround`, `cost`, and `resolutionCriteria` in its Failure Cases, Current Workaround, Cost, and Resolution Criteria sections; it adds no fact the record lacks.

| Instead of | Write |
|:---|:---|
| `statement: Employees need a reminder dashboard for approvals.` | `statement: Submitted expenses wait weeks for a decision, so employees chase managers by email.` |

The weak sentence files a missing feature as a pain, so every other answer reads as off-topic. This satisfies `litespec/core/intent#admit-only-outside-pains-as-problems`, `litespec/core/intent#name-no-solution-in-a-problem-statement`, and `litespec/core/writing#write-for-the-reader-who-arrives-cold`.

### Open with the Gap, Its Cause, and Its Consequence

Treat the opening paragraph of the explanation file as the one paragraph a cold reader may read alone. Say what the sufferer cannot reliably do, which gap causes it, and what they lose or risk. Check the paragraph against "the sufferer cannot reliably do the task because of the gap, so they lose the consequence", without copying that sentence.

| Instead of | Write |
|:---|:---|
| Employees email the approver to ask about their expenses. | Employees cannot get a work expense paid back quickly, because nothing shows the approver how long an expense has waited. As a result, employees carry the amount from their own account for weeks and chase the approver by email. |

The weak version only describes the workaround. It tells you something happens by hand, never why it has to. Keep the workaround for its own section later. This satisfies `litespec/core/intent#open-each-problem-with-its-gap-and-consequence` and `litespec/core/writing#open-each-explanation-file-with-a-paragraph`.

### Describe Who Has It

Under Affected Users, describe who meets the problem and in which circumstances. Stay with the people the evidence covers; do not stretch to a whole market.

| Instead of | Write |
|:---|:---|
| Employees. | Employees who submit work expenses and wait for a manager's decision, carrying the amount from their own account while they wait. |

This satisfies `litespec/core/intent#claim-no-more-than-the-evidence-shows`.

### Give Each Failure Its Own Entry

Under Failure Cases, give each failure that can happen on its own as its own item of `failureCases`. For each item, say what is missing or wrong, how the sufferer notices it, and what follows when they do not.

```markdown
- **An expense waits without a decision.** Nothing shows the approver how long an expense has waited, so it waits until the employee chases it. Until then, the employee carries the amount from their own account.
- **The employee cannot see the decision.** Nothing tells the employee when the approver decides, so the employee learns the outcome only by asking.
```

Two failures joined in one sentence read as one failure, so a solution that ends one of them looks finished. Direction matters when two records should agree: an entry missing from the first and an entry missing from the second are found by different checks, so a solution checked one way only leaves the other failure in place. This satisfies `litespec/core/intent#state-each-failure-case-on-its-own`.

### Add an Example When the Failure Is Hard to Follow

Add the optional Example section when a concrete case helps the reader see the failure. Use a table when several cases compare, and keep only the details the failure needs. When the failure cases read clearly on their own, leave the section out. This follows the problem row of the explanation sections on the [Writing](../core/writing.md#explanation-sections) page.

### Describe the Workaround and Its Burden

Under Current Workaround, say what the sufferer does today and what that costs in effort: the time it takes, what they repeat or have to remember, and what it still misses. State it in `workaround`; the section restates that field. The burden is what the manual work costs, not the fact that it is manual.

| Instead of | Write |
|:---|:---|
| Employees follow up manually. | Employees email the approver for each expense that waits, and keep track of which expenses are still open. The approver answers from memory, and no record shows how long an expense has waited. |

This satisfies `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`.

### Separate Measured Facts, Estimates, and Unknowns

Under Cost, keep time, money, and risk apart, each in the sufferer's own units. State it in `cost`; the section restates that field. Say whether each figure was measured, estimated, or reported, and write "unknown" where nobody has a figure. Keep a possible loss possible until someone observes it.

| Instead of | Write |
|:---|:---|
| Employees wait 3 weeks, which costs them money. | Employees report an average wait of 3 weeks; nobody has measured it. No total of carried money is recorded, so the money cost is unknown. |

This satisfies `litespec/core/intent#state-the-cost-and-workaround-of-every-problem` and `litespec/core/intent#claim-no-more-than-the-evidence-shows`.

### Back Each Claim with Evidence of the Pain

Give each evidence entry its `claim`, its `source` as a record identifier or a URL, its `readOn` date, and its `kind`: observed, reported, or assumed. Under Evidence in the explanation file, say what each source shows and where it stops.

```yaml
evidence:
  - claim: Employees wait three weeks on average for reimbursement.
    source: research/approval-time-benchmarks
    readOn: 2026-11-01
    kind: reported
```

| Instead of | Because |
|:---|:---|
| A payment provider offers same-day transfers. | It shows that a solution can be built, not that anyone has the pain. It belongs in research that a feature cites. |
| An employee asked for a reminder dashboard. | A request is evidence of the requested solution. It supports the pain only for what the employee reports about their own wait. |

This satisfies `litespec/core/intent#grade-problem-evidence-by-kind`, `litespec/core/intent#keep-feasibility-out-of-problem-evidence`, and `litespec/core/intent#admit-only-outside-pains-as-problems`.

### Write One Resolution Criterion per Failure Case

Under Resolution Criteria, give each failure case one observable outcome for the sufferer that shows the failure has ended, as one item of `resolutionCriteria`. Name no feature, format, or screen there. The feature that delivers the outcome sits in the problem's `features/` folder.

| Instead of | Write |
|:---|:---|
| Add reminders and a status page to the approval screen. | Each submitted expense is decided within 5 business days. The employee sees the decision without asking the approver. |

The weak version picks the answer, and it covers neither failure as an outcome a reader can check. This satisfies `litespec/core/intent#name-no-solution-in-a-problem-statement` and `litespec/core/intent#explain-every-problem-in-its-fixed-sections`.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the problem record or its explanation file.
- `litespec rules <path to the problem record>` lists the judgment provisions above, and you have answered each one.
- A reader who has never met you can say what fails, judge how strong the evidence is, and propose a solution you did not.
