# Write a Constraint

Write one constraint record, `<name>.yaml`, and its explanation file `<name>.md`, so a reader can check the rule against its source, see whom it binds, and find each requirement that holds its effect. A constraint states one outside rule, by law or by contract, that the product respects, and it says which product choices the rule takes away.

For example, the Claimwick tax receipt rule below shows each step on one running case.

Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer built through [Start a Consumer Repository](../tutorial/getting-started.md), with the product record already written.
- The source of the rule: a public page, an article, or a contract clause that states it, which you have read on a known date.
- The requirements that hold the rule's effect, or the feature that will hold them. A constraint names them in its Effect on the Product section, so write the requirements first or alongside it.

The [Intent](../core/intent.md) and [Writing](../core/writing.md) pages state the obligations behind these steps. Read them first; this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how to spell `litespec` in a consumer.

## Procedure

1. Scaffold the constraint. Run `litespec new constraint <name>`, which writes `.litespec/intent/constraints/<name>/<name>.yaml` and reserves the identifier `constraint/<name>`. When the project lists `intent` in `explanations.layers`, you also get `<name>.md` from the constraint template, with the title filled in. For example, `litespec new constraint tax-receipt-retention` writes the Claimwick retention file and reserves its identifier. The template fields are placeholders, and `model check` reports a placeholder left unchanged. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.
2. Decide what one constraint holds. Keep in one constraint the rules that share one source, one scope, and one rate of change. Split rules that are verified separately or change separately into separate constraints: split when the source, the subjects or periods covered, or the rate of change differs, and keep rules together only when all three match. A library privacy law, for example, sets a retention period and a breach-notice deadline in different articles, and the two change on different dates, so they are two constraints. This satisfies `litespec/core/intent#split-records-whose-parts-change-separately`.
3. Title the constraint by its rule. Name the rule, not the source or your team's reaction to it. A title that names the source tells you where the rule comes from but not what it requires.

   | Kind | Title |
   |:---|:---|
   | Source | National Tax Law |
   | Reaction | Keep Receipts Longer |
   | Rule | Tax Receipt Retention |

   Name the title for the rule the constraint requires. This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense`.
4. State the rule, its condition, and whom it binds in the summary. Keep `summary` to one or two sentences that state the rule, the condition under which it holds, and whom it binds. For example, the Claimwick summary reads: national tax law requires a company to keep its business receipts for seven years.

   | Instead of | Write |
   |:---|:---|
   | `summary: Keeping receipts for seven years is important for audits.` | `summary: National tax law requires a company to keep its business receipts for seven years.` |
   | `summary: Finance reimburses the expense.` | `summary: Finance reimburses the approved expense.` |

   The first weak version states no authority, no subject, and no condition, so a reader cannot tell what is required or of whom. The second drops the condition the rule holds under. A shortened summary keeps every condition the rule carries. This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition` and `litespec/core/writing#write-human-docs-for-the-cold-reader`.
5. Name the authority. Keep `authority` to a few words naming the body that issues the rule, such as a tax administration, a regulator, or a provider. It states no rule; the rule stays in `summary` and in Rules. For example, the Claimwick authority is `National tax law`, not a restatement of the seven-year rule.

   | Instead of | Write |
   |:---|:---|
   | `authority: Tax law requiring seven-year storage for receipts.` | `authority: National tax law.` |

   The weak version states the rule a second time. This satisfies `litespec/core/intent#answer-every-constraint`.
6. List the sources. Give each entry in `sources` a `name`, its `url`, and the `readOn` date on which someone read it. Add `publishedOn` when the source states its publication date. A constraint can name several sources, with each one carrying its own dates. For example, the national tax law source below was read on 2026-02-10.

   ```yaml
   sources:
     - name: national-tax-law
       url: https://example.com/national-tax-law
       readOn: 2026-02-10
   ```

   No record-level `readOn` exists: each source has its own. `readOn` says when someone read the source, not when the rule took effect. Keep the publication date, the read date, and the start date apart, as the [layers page](../core/layers.md) describes. This satisfies `litespec/core/intent#cite-a-source-for-each-constraint-rule` and `litespec/core/intent#answer-every-constraint`.
7. Set the effective date when the rule starts on a date. When the rule starts to bind on a date, set `effectiveFrom` to that date. Leave it out when the rule has always applied or when the date is unknown. There is no end date: when a rule stops binding, set `applies: false` with the reason in `appliesBecause`. For example, a rule binding from 1 January 2026 sets `effectiveFrom: 2026-01-01`, with the article that states the date as a source.

   | Instead of | Write |
   |:---|:---|
   | `effectiveFrom: 2026` | `effectiveFrom: 2026-01-01`, with the article that states the date as a source |

   The [layers page](../core/layers.md) names `effectiveFrom` as the statutory date.
8. Link the research the rule rests on. When a rule rests on research, as observed provider behavior does, name that research in `basedOn`. A law that states its rule on a public page requires no research record, because its own source carries the rule. Research stays the home for a question that needs investigating, as the [layers page](../core/layers.md) describes. For example, the Claimwick retention rule cites the tax law page directly and lists no research record.
9. State why the rule binds the product. Use `appliesBecause` for the reason the rule binds the product, or the reason it does not. Name the product fact that brings the rule into play, not the rule itself. For example: Claimwick stores receipts for reimbursed expenses, so the seven-year rule binds it.

   | Instead of | Write |
   |:---|:---|
   | `appliesBecause: The law is important.` | `appliesBecause: Claimwick stores receipts for reimbursed expenses.` |

   This satisfies `litespec/core/intent#answer-every-constraint`.
10. Open the explanation with the rule and whom it binds. Treat the opening paragraph of the explanation file as the one paragraph a cold reader may read alone. Say the rule, its condition, and whom it binds, in plain words, without repeating the title and without stating an unverified fact as settled.

    | Instead of | Write |
    |:---|:---|
    | Receipts are very important for the company's tax position and must be handled carefully. | National tax law requires a company to keep its business receipts for seven years. Claimwick stores the receipts of reimbursed expenses, so the law binds the product. |

    The weak version states no rule and no subject. The strong version opens with the obligation, then names the product it binds. This satisfies `litespec/core/writing#write-human-docs-for-the-cold-reader`.
11. Write scope. Under Scope, say which subjects, transactions, and periods the rule covers, and what it leaves out. State it in `coverage`; Scope restates that field. Scope says whom the rule itself covers. Whether the rule binds the product stays in the `applies` field and `appliesBecause`, so Scope does not repeat them. For example, the Claimwick scope reads:

    ```yaml
    type: constraint
    id: constraint/tax-receipt-retention
    title: Tax Receipt Retention
    authority: National tax law
    summary: National tax law requires a company to keep its business receipts for seven years.
    kind: legal
    applies: true
    appliesBecause: Claimwick stores receipts for reimbursed expenses.
    coverage: The rule covers business receipts. Claimwick holds only the receipts of expenses it reimburses.
    obligations:
      - statement: A company keeps its business receipts for seven years.
        sources:
          - national-tax-law
    productEffect: An expense above the receipt threshold carries its receipt, which the product keeps for the retention period.
    openPoints:
      - Whether the seven-year period counts from the expense date or the payout date is open.
    ```

    The explanation file restates `coverage`, `obligations`, `productEffect`, and `openPoints` in its Scope, Rules, Effect on the Product, and Open Points sections; it adds no fact the record lacks.

    | Instead of | Write |
    |:---|:---|
    | The rule applies to everything Claimwick does. | The rule covers business receipts. Claimwick holds only the receipts of expenses it reimburses, so those are the receipts the rule reaches in the product. |

    This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition`.
12. Write rules, one paragraph per rule. Under Rules, restate each `obligations` item as its own paragraph with its condition. Each item states one outside rule as a fact and names its sources by name. Cite the source that states the rule as a reference link `[text][name]` to a name in `sources`. Keep to the outside obligation; what the product does about it belongs in Effect on the Product.

    | Instead of | Write |
    |:---|:---|
    | Claimwick deletes personal data after seven years. | The national tax law requires a company to keep its business receipts for seven years [National tax law][national-tax-law]. |
    | A company keeps its receipts for seven years, and Claimwick keeps them for the same period. | The national tax law requires a company to keep its business receipts for seven years [National tax law][national-tax-law]. |

    The first weak version names the product, not the law, and states a rule the source does not show. The second mixes the obligation with the product response. Keep a condition in the paragraph of a rule that holds only under it, such as one for quarterly filers, so the rule does not read wider than its source. This satisfies `litespec/core/intent#cite-a-source-for-each-constraint-rule` and `litespec/core/intent#state-each-constraint-rule-with-its-condition`.
13. Write effect on the product, naming each requirement. Under Effect on the Product, give each effect the rule has on the product its own paragraph, and name each requirement that holds it by identifier. State it in `productEffect`; the section restates that field. Name here every requirement that cites this constraint, and make every requirement named here cite it back. For example: an expense above the receipt threshold carries its receipt, which the product keeps for the retention period, under `requirement/approval.above-threshold-receipt`.

    | Instead of | Write |
    |:---|:---|
    | The product handles receipts correctly under the law. | An expense above the receipt threshold carries its receipt, which the product keeps for the retention period: `requirement/approval.above-threshold-receipt`. |

    The weak version states no requirement and no product choice, so a reader cannot check it. `model check` reports `constraint-effect-unlisted` for a citing requirement this section leaves out, and `constraint-effect-uncited` for a requirement named here that does not cite the constraint. This satisfies `litespec/core/intent#answer-every-constraint`.

    When a constraint carries `applies: false`, say in one sentence that the rule binds nothing in the product, and why.
14. Write open points as questions about application. Under Open Points, optional, keep to questions about how a verified rule applies to the product. State each question as one item of `openPoints`; Open Points restates that field. Name who settles each question and which behavior waits on it. A question about the facts of the rule belongs in the research record's `openQuestions`; the constraint names that question in one line. For example: whether the seven-year period counts from the expense date or the payout date is open; Finance settles it, and the retention requirement waits on its answer.

    | Instead of | Write |
    |:---|:---|
    | We are not sure what the law means for old expenses. | Whether the seven-year period counts from the expense date or the payout date is open. Finance settles it, and the retention requirement waits on its answer. |

    This satisfies `litespec/core/intent#state-each-constraint-rule-with-its-condition` and `litespec/core/intent#state-each-research-finding-with-its-source`.
15. Keep your team's interpretation apart. Use "requires" and "must" in the explanation only for an obligation a source states. Keep three things apart: the outside obligation under Rules, the product response in the requirements, and the team interpretation in a decision. When the source does not settle an interpretation, write it as an Open Point, not a Rule. For example, the team's choice to keep its own records for the same seven years is a decision, and it lives in a decision record of its own.

    | Instead of | Write |
    |:---|:---|
    | Claimwick keeps receipts for seven years because the team decided it is safer. | The national tax law requires a company to keep its business receipts for seven years. The team's choice of the same period for its own records is a decision, and it lives in a decision record of its own. |

    This satisfies `litespec/core/writing#write-machine-read-fields-in-present-tense` and `litespec/core/intent#state-each-constraint-rule-with-its-condition`.
16. Run model render. Run `litespec model render` from the consumer root. It appends a generated `source-links` region to the explanation file: one Markdown link definition per source, with its read date, such as `[national-tax-law]: https://example.com/national-tax-law "Read on 2026-02-10"`. Each `[text][name]` citation then works in a plain Markdown preview, without the book. The region is generated, so a hand edit inside it counts as drift. For example, the Claimwick constraint page renders its tax law link from its `sources` entry. This satisfies `litespec/core/model#explain-each-record-beside-its-record-file`, and the [Model](../tools/model.md) command page states what `render` writes.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the constraint record. Keep the generated region equal to what `render` writes.
- `litespec model check` reports `constraint-effect-unlisted` and `constraint-effect-uncited` for no requirement that cites the constraint.
- `litespec rules <path to the constraint record>` lists the judgment provisions above, and you have answered each one.
- A reader who has never met you can open the explanation, follow each source link, and say which requirements hold the rule's effect.
