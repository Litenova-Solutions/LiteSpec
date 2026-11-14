# Write a Constraint

## Purpose

A constraint states one outside rule, by law or by contract, that the product must respect, and it says which product choices the rule takes away. This guide writes one constraint record, `<name>.yaml`, and its explanation file `<name>.md`, so a reader can check the rule against its source, see whom it binds, and find each requirement that holds its effect. Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its product record written.
- The source of the rule: a public page, an article or a contract clause that states it, which you have read on a known date.
- The requirements that hold the rule's effect, or the feature that will hold them. A constraint names them in its Effect on the Product section, so the requirements come first or are written alongside it.

Read the [Intent](../core/intent.md) page and the [Writing](../core/writing.md) page before this guide: they state the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Constraint

Run `litespec new constraint <name>` to write `.litespec/intent/constraints/<name>/<name>.yaml` and reserve the identifier `constraint/<name>`. When the project lists `intent` in `explanations.layers`, the command also writes `<name>.md` from the constraint template, with the title filled in. The template's values are examples, and `model check` reports a value left unchanged. This satisfies `litespec/core/intent#keep-intent-in-its-homes` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Decide What One Constraint Holds

A constraint holds rules that share one source, one scope and one rate of change. Rules that are verified separately or change separately are separate constraints. A library privacy law, for example, sets a retention period and a breach-notice deadline in different articles, and the two change on different dates, so they are two constraints.

Split the record when any of these differ between two rules: the source they come from, the subjects or periods they cover, or how often they change. Keep them together when all three match. This satisfies `litespec/core/intent#split-records-whose-parts-change-separately`.

### Title the Constraint by Its Rule

Name the rule, not the source or the team's reaction to it. A title that names the source tells the reader where the rule comes from but not what it requires.

| Kind | Title |
|:---|:---|
| Source | National Tax Law |
| Reaction | Keep Receipts Longer |
| Rule | Tax Receipt Retention |

This follows the constraint row of the title forms on the [Writing](../core/writing.md#title-forms) page and satisfies `litespec/core/writing#write-every-record-title-in-title-case`.

### State the Rule, Its Condition, and Whom It Binds in the Summary

Write `summary` as one or two sentences that state the rule, the condition under which it holds, and whom it binds. The form "<authority or law> requires <subject> to <act> when <condition>" keeps the actor first and needs no modal word in the summary.

| Instead of | Write |
|:---|:---|
| `summary: Keeping receipts for seven years is important for audits.` | `summary: National tax law requires a company to keep its business receipts for seven years.` |
| `summary: Finance reimburses the expense.` | `summary: Finance reimburses the approved expense.` |

The first version names no authority, no subject and no condition, so a reader cannot tell what is required or of whom. The second drops the condition the rule holds under. A shortened summary keeps every condition the rule carries, as `litespec/core/writing#keep-the-conditions-when-shortening` requires. This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition` and `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`.

### Name the Authority

Write `authority` as a few words naming the body that issues the rule, such as a tax administration, a regulator or a provider. It states no rule, so the rule stays in `summary` and in Rules.

| Instead of | Write |
|:---|:---|
| `authority: Tax law requiring seven-year storage for receipts.` | `authority: National tax law.` |

The first version states the rule a second time. This satisfies `litespec/core/intent#answer-every-constraint`.

### List the Sources

Give each source in `sources` a `key`, its `url` and the `readOn` date on which someone read it. Add `publishedOn` when the source states its publication date. A constraint can name several sources, and each one carries its own dates.

```yaml
sources:
  - key: national-tax-law
    url: https://example.com/national-tax-law
    readOn: 2026-02-10
```

The record-level `readOn` is gone: each source has its own. `readOn` says when someone read the source, not when the rule took effect. Keep the publication date, the read date and the start date apart, as `litespec/core/intent#record-research-with-read-sources` requires. This satisfies `litespec/core/intent#cite-a-source-for-each-constraint-rule` and `litespec/core/intent#answer-every-constraint`.

### Set the Effective Date When the Rule Starts on a Date

When the rule starts to bind on a date, set `effectiveFrom` to that date. Leave it out when the rule has always applied or the date is unknown. There is no end date: a rule that stops binding is set `applies: false` with its reason in `appliesBecause`.

| Instead of | Write |
|:---|:---|
| `effectiveFrom: 2026` | `effectiveFrom: 2026-01-01`, with the article that states the date as a source |

This satisfies `litespec/core/layers#build-ahead-of-a-regulation-only-under-its-constraint`, which names `effectiveFrom` as the statutory date.

### Link the Research the Rule Rests On

When a rule rests on research, as observed provider behavior does, name that research in `basedOn`. A law that states its rule on a public page needs no research record, because its own source carries the rule. Research stays the home for a question that needs investigating, as `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints` describes.

This satisfies `litespec/core/layers#record-changed-provider-behavior-as-observed-research`.

### State Why the Rule Binds the Product

Write `appliesBecause` as the reason the rule binds the product, or the reason it does not. Name the product fact that brings the rule into play, not the rule itself.

| Instead of | Write |
|:---|:---|
| `appliesBecause: The law is important.` | `appliesBecause: Claimwick stores receipts for reimbursed expenses.` |

This satisfies `litespec/core/intent#answer-every-constraint`.

### Open the Explanation with the Rule and Whom It Binds

Write the opening paragraph of the explanation file as the one paragraph a cold reader may read. Say the rule, its condition and whom it binds, in plain words, with no title repeated and no unverified fact stated as settled.

| Instead of | Write |
|:---|:---|
| Receipts are very important for the company's tax position and must be handled carefully. | National tax law requires a company to keep its business receipts for seven years. Claimwick stores the receipts of reimbursed expenses, so the law binds the product. |

The first version states no rule and no subject. The strong version opens with the obligation, then names the product it binds. This satisfies `litespec/core/writing#open-each-explanation-file-with-a-paragraph` and `litespec/core/writing#write-for-the-reader-who-arrives-cold`.

### Write Scope

Under Scope, say which subjects, transactions and periods the rule covers, and what it leaves out. Scope says whom the rule itself covers. The `applies` field and `appliesBecause` say whether the rule binds the product, so Scope does not repeat them.

| Instead of | Write |
|:---|:---|
| The rule applies to everything Claimwick does. | The rule covers business receipts. Claimwick holds only the receipts of expenses it reimburses, so those are the receipts the rule reaches in the product. |

This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition`.

### Write Rules, One Paragraph per Rule

Under Rules, give each rule its own paragraph with its condition. Cite the source that states the rule as a reference link `[text][key]` to a key in `sources`. State the outside obligation only. What the product does about it belongs in Effect on the Product.

| Instead of | Write |
|:---|:---|
| Claimwick must delete personal data after seven years. | The national tax law requires a company to keep its business receipts for seven years [National tax law][national-tax-law]. |
| A company keeps its receipts for seven years, and Claimwick keeps them for the same period. | The national tax law requires a company to keep its business receipts for seven years [National tax law][national-tax-law]. |

The first version names the product, not the law, and states a rule the source does not show. The second mixes the obligation with the product's response. A rule that holds only under a condition, such as one that holds for quarterly filers, keeps that condition in its paragraph, so the rule does not read wider than its source. This satisfies `litespec/core/intent#cite-a-source-for-each-constraint-rule` and `litespec/core/intent#state-each-constraint-rule-with-its-condition`.

### Write Effect on the Product, Naming Each Requirement

Under Effect on the Product, give each effect the rule has on the product its own paragraph, and name by identifier each requirement that holds it. Every requirement that cites this constraint must be named here, and every requirement named here must cite it.

| Instead of | Write |
|:---|:---|
| The product handles receipts correctly under the law. | An expense above the receipt threshold carries its receipt, which the product keeps for the retention period: `requirement/approval.receipt-over-threshold`. |

The first version names no requirement and no product choice, so a reader cannot check it. `model check` reports `constraint-effect-unlisted` for a citing requirement this section leaves out, and `constraint-effect-uncited` for a requirement named here that does not cite the constraint. This satisfies `litespec/core/intent#answer-every-constraint`.

A constraint with `applies: false` says in one sentence that the rule binds nothing in the product, and why.

### Write Open Points as Questions About Application

Under Open Points, optional, write only questions about how a verified rule applies to the product. Name who settles each question and which behavior waits on it. A question about the facts of the rule belongs in the research record's Open Questions, and the constraint names that question in one line.

| Instead of | Write |
|:---|:---|
| We are not sure what the law means for old expenses. | Whether the seven-year period counts from the expense date or the payout date is open. Finance settles it, and the retention requirement waits on its answer. |

This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition` and `litespec/core/intent#state-each-research-finding-with-its-source`.

### Keep the Team's Interpretation Apart

Use "requires" and "must" in the explanation only for an obligation a source states. Keep three things apart in the file: the outside obligation under Rules, the product's response in the requirements, and the team's interpretation in a decision. An interpretation that the source does not settle is an Open Point, not a Rule.

| Instead of | Write |
|:---|:---|
| Claimwick must keep receipts for seven years because the team decided it is safer. | The national tax law requires a company to keep its business receipts for seven years. The team's choice of the same period for its own records is a decision, and it lives in a decision record of its own. |

This satisfies `litespec/core/writing#use-literal-words` and `litespec/core/intent#state-each-constraint-rule-with-its-condition`.

### Run Model Render

Run `litespec model render` from the consumer root. It writes a generated `source-links` region at the end of the explanation file: one Markdown link definition per source, with its read date, such as `[national-tax-law]: https://example.com/national-tax-law "Read on 2026-02-10"`. Each `[text][key]` citation then works in a plain Markdown preview, without the book. The region is generated, so a hand edit inside it is drift.

This satisfies `litespec/core/model#use-one-metadata-carrier` as reworded for generated regions, and the [Model](../tools/model.md) command page states what `render` writes.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the constraint record or its explanation file. It reports `source-links-drift` when the generated region differs from what `render` writes, and `source-links-missing` when a file citing a source has no region.
- `litespec model check` reports `constraint-effect-unlisted` and `constraint-effect-uncited` for no requirement that cites the constraint.
- `litespec rules <path to the constraint record>` lists the judgment provisions above, and each one is answered.
- A reader who has never met the author can open the explanation, follow each source link, and say which requirements hold the rule's effect.
