# LiteSpec Style Card

This card states how LiteSpec itself is written: plainspoken technical prose from one developer to a capable colleague who is new to the topic.

It covers every page under `docs/`, the site under `apps/site/`, the READMEs, the templates, the Claimwick example, and the messages the tools print. Consumer record files follow the consumer writing protocol in `docs/core/writing.md`.

## Who This Card Serves

You write for a reader who arrives cold from a link or a search result and needs to act after reading.

You help that reader understand something or finish a task. You do not try to make LiteSpec sound impressive.

## The Eight Rules

1. **Lead with the answer.** Start with the task, the decision, or the fact, then give the reason and one concrete Claimwick example.
2. **Write for the cold reader.** Name who the page concerns and what is at stake in the first lines, before any pointer to another page.
3. **Name the actor and the act.** Put the doer first, in the active voice and the present tense, with ordinary technical verbs.
4. **Use literal domain words.** Use the glossary term for each concept, define a new term where it first appears, and avoid figures of speech.
5. **State what the reader can check.** Give the observable behavior, the exact path, command, or value, and the limit, instead of praise for the feature.
6. **Keep one idea in each paragraph.** Hold two to four sentences per paragraph, keep each paragraph on one line in Markdown, and use a list only for truly parallel items.
7. **Keep normative strength intact.** Never soften a requirement into advice, never harden advice into a requirement, and never drop a condition when you shorten a sentence.
8. **Show one Claimwick example per rule.** Place the example beside the rule it clarifies, using names and figures from the cast or from a cited record.

## Replace Stilted Phrasing

| Avoid | Use instead |
|---|---|
| `gains a home` | `lives in`, `is stored in` |
| `refuses other folders` | `only the listed folders are allowed` |
| `names no ` | `lists no`, `states no` |
| `the homes of records` | `where records live`, `record locations` |
| `holds the values ... speaks` | `states the values in plain words` |
| Code that `names` several modules | Code that references several modules |
| A closed set `refuses` other entries | Only the listed entries are allowed |
| `seamlessly`, `instantly`, `effortlessly` | The measured time, count, or visible result |
| `unlock the power of` | What the reader builds with it |

Pick the replacement that fits the sentence: `references`, `uses`, and `depends on` are not interchangeable.

## Unsafe Rewrites

| Original | Unsafe rewrite | Why it breaks meaning |
|---|---|---|
| A node **may contain** layer folders. | A node **contains** layer folders. | Permission becomes a requirement. |
| Endpoints **without a handler** live in the host. | Endpoints **outside a use case** live in the host. | The placement condition changes. |
| Types used by **two or more** modules. | Types used by **every** module. | The scope widens. |
| Domain code does not depend on outer layers. | Infrastructure changes never affect behavior. | A dependency rule becomes an outcome guarantee. |

## Golden Before and After Rewrite

Before (stilted, 12 lines):

> The approval chain gains a home in the spending module and refuses other folders.
> It seamlessly unlocks the power of accountable reimbursement and instantly routes each claim.
> The homes of records hold the values the policy speaks, and names no late claim.
> This robust and comprehensive solution ensures alignment between intent and implementation.
> It is not just fast; it is effortless. Not slow. Not manual. Just streamlined.
> In today's rapidly evolving landscape, approvers effortlessly spot every late expense.
> The module provides visibility into the facilitation of the creation of reminders.
> Without these rules, every team invents its own structure from scratch.
> Let's dive in: the chain performs validation of each receipt thresholdPolicy check.
> It matters because the figures must be correct and the process must be sound.
> This page explains the parts, layers, tools, and conventions that make up approvals.
> A team rarely adopts a whole standard on day one, and a big-bang switch stalls real work.

After (plainspoken, 10 lines):

> An approver clears each Claimwick expense within five business days of submission.
> The approval chain lives in the spending module, beside the use case that runs it.
> `litespec scaffold` writes one file per use case; `litespec sync` copies record data into it.
> A claim above EUR 25 carries a receipt; without one, the submit step is refused with `receipt-missing`.
> The approver sees each waiting claim with the number of business days it has waited.
> A reminder goes out after three business days of waiting, and states the claim and the next step.
> Only the spending module reads the policy threshold; other modules ask it through its read model.
> Start with Intent and Behavior, then add Implementation and Proof when the team is ready.
> You write the rule once in the record; the book and the site restate it from there.
> This page states where approval rules live, what each rule requires, and one Claimwick example.

The second version keeps every condition (five days, EUR 25, three days, one file per use case) and drops the praise, the slogans, and the figures of speech.

## Full Rewrite Workflow

The long rewriting workflow lives in the on-demand skill at `templates/skill/rewrite-docs/SKILL.md`.

Load that skill only when you edit documentation: it holds Copyedit versus Rewrite versus Technical revision, contradiction checks, and the semantic verification pass.

## What This Card Does Not Cover

This card states voice only; it adds no rule and changes no requirement.

Mechanical paragraph and phrasing checks run in `litespec-maintain validate` and `litespec model review`, and the independent editorial pass runs in `litespec docs review`, which other work owns.
