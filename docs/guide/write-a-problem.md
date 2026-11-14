# Write a Problem

## Purpose

A problem states a pain that people outside the team have today, before any feature answers it. This guide writes one problem record, `<name>.yaml`, and its explanation file `<name>.md`, so a reader who has never met the author can understand the problem, judge its evidence, and propose a different solution. Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its product record written.
- The actor who has the pain, as an actor record outside the team.
- At least one source that shows the pain: a research record that reports what sufferers said or did, or a URL.

Read the [Intent](../core/intent.md) page and the [Writing](../core/writing.md) page before this guide: they state the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Problem

Run `litespec new problem <name>` to write `.litespec/intent/problems/<name>/<name>.yaml` and reserve the identifier `problem/<name>`. When the project lists `intent` in `explanations.layers`, the command also writes `<name>.md` from the problem template. This satisfies `litespec/core/intent#keep-intent-in-its-homes` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Name the Failure in the Title

Name what goes wrong, or what it costs the people who have it. A theme or a feature in the title tells the reader the topic but not the problem.

| Kind | Title |
|:---|:---|
| Theme | Approval Process |
| Feature | Approval Reminders |
| Problem | Expenses Wait Weeks |

This follows the problem row of the title forms on the [Writing](../core/writing.md#title-forms) page and satisfies `litespec/core/writing#write-every-record-title-in-title-case`.

### State the Pain in Its Fields

Name the sufferer first in `affects`, then state the pain in `summary` and in a one-sentence `statement`. The statement names the sufferer, the condition, and the consequence, and it names no solution.

| Instead of | Write |
|:---|:---|
| `statement: Employees need a reminder dashboard for approvals.` | `statement: Submitted expenses wait weeks for a decision, so employees chase managers by email.` |

The first sentence files a missing feature as a pain, so every other answer reads as off-topic. This satisfies `litespec/core/intent#admit-only-outside-pains-as-problems`, `litespec/core/intent#name-no-solution-in-a-problem-statement`, and `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`.

### Open with the Gap, Its Cause, and Its Consequence

Write the opening paragraph of the explanation file as the one paragraph a cold reader may read. Say what the sufferer cannot reliably do, the gap that causes it, and what they lose or risk. Use "the sufferer cannot reliably do the task because of the gap, so they lose the consequence" as a check on the paragraph, not as a sentence to copy.

| Instead of | Write |
|:---|:---|
| Employees email the approver to ask about their expenses. | Employees cannot get a work expense paid back quickly, because nothing shows the approver how long an expense has waited. As a result, employees carry the amount from their own account for weeks and chase the approver by email. |

The first version describes the workaround: it tells the reader that something happens by hand, never why it has to. The workaround gets its own section later. This satisfies `litespec/core/intent#open-each-problem-with-its-gap-and-consequence` and `litespec/core/writing#open-each-explanation-file-with-a-paragraph`.

### Describe Who Has It

Under Affected Users, describe the people and the circumstances in which they meet the problem. Describe the people the evidence covers, not a whole market.

| Instead of | Write |
|:---|:---|
| Employees. | Employees who submit work expenses and wait for a manager's decision, carrying the amount from their own account while they wait. |

This satisfies `litespec/core/intent#claim-no-more-than-the-evidence-shows`.

### Give Each Failure Its Own Entry

Under Failure Cases, list each failure that can happen on its own as its own entry. For each one, say what is missing or wrong, how the sufferer finds it, and what follows when they do not.

```markdown
- **An expense waits without a decision.** Nothing shows the approver how long an expense has waited, so it waits until the employee chases it. Until then, the employee carries the amount from their own account.
- **The employee cannot see the decision.** Nothing tells the employee when the approver decides, so the employee learns the outcome only by asking.
```

Two failures joined in one sentence read as one, and a solution that ends one of them looks finished. The direction matters: when two records should agree, an entry missing from the first and an entry missing from the second are found by different checks. This satisfies `litespec/core/intent#state-each-failure-case-on-its-own`.

### Add an Example When the Failure Is Hard to Follow

Add the optional Example section when a reader needs a concrete case to see the failure. Use a table when several cases compare, and keep only the details the failure needs. Leave the section out when the failure cases are clear without it. This follows the problem row of the explanation sections on the [Writing](../core/writing.md#explanation-sections) page.

### Describe the Workaround and Its Burden

Under Current Workaround, say what the sufferer does today and what that costs them in effort: the time it takes, what they repeat or have to remember, and what it still misses. Work done by hand is no burden on its own.

| Instead of | Write |
|:---|:---|
| Employees follow up manually. | Employees email the approver for each expense that waits, and keep track of which expenses are still open. The approver answers from memory, and no record shows how long an expense has waited. |

This satisfies `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`.

### Separate Measured Facts, Estimates, and Unknowns

Under Cost, state time, money, and risk apart, each in the sufferer's own units. Say whether each figure was measured, estimated, or reported, and write "unknown" where nobody has a figure. A possible loss stays possible until someone observes it.

| Instead of | Write |
|:---|:---|
| Employees wait 3 weeks, which costs them money. | Employees report an average wait of 3 weeks; nobody has measured it. No total of carried money is recorded, so the money cost is unknown. |

This satisfies `litespec/core/intent#state-the-cost-and-workaround-of-every-problem` and `litespec/core/intent#claim-no-more-than-the-evidence-shows`.

### Back Each Claim with Evidence of the Pain

List each evidence entry with its `claim`, its `source` as a record identifier or a URL, its `readOn` date, and its `kind`: observed, reported, or assumed. Under Evidence in the explanation file, say what each source shows and what it does not.

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

This satisfies `litespec/core/intent#grade-problem-evidence-by-kind`, `litespec/core/intent#keep-feasibility-out-of-problem-evidence`, and `litespec/core/intent#state-each-problem-with-its-evidence`.

### Write One Resolution Criterion per Failure Case

Under Resolution Criteria, give each failure case one observable outcome for the sufferer that shows the failure has ended. Name no feature, format, or screen: the feature that delivers the outcome names the problem in `solves`.

| Instead of | Write |
|:---|:---|
| Add reminders and a status page to the approval screen. | Each submitted expense is decided within 5 business days. The employee sees the decision without asking the approver. |

The first version picks the answer and covers neither failure as an outcome a reader can check. This satisfies `litespec/core/intent#resolve-every-failure-case-with-an-outcome` and `litespec/core/intent#explain-every-problem-in-its-fixed-sections`.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the problem record or its explanation file.
- `litespec rules <path to the problem record>` lists the judgment provisions above, and each one is answered.
- A reader who has never met the author can say what fails, judge how strong the evidence is, and propose a solution the author did not.
