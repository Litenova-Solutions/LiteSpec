# Write a Research Record

## Purpose

A research record answers one question with the sources that bear on it, and it states what those sources establish and what they leave open. This guide writes one research record, `<name>.yaml`, and its explanation file `<name>.md`, so a reader can check each claim against its source, see the conditions the claim holds under, and tell a source's statement from the writer's inference. Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its product record written.
- A question that a choice depends on, such as how fast a sufferer's peers decide a claim.
- The sources you will read, each with its URL, and the date you read it.

Read the [Intent](../core/intent.md) page and the [Writing](../core/writing.md) page before this guide: they state the obligations, and this guide shows one compliant way to meet them. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Scaffold the Research Record

Run `litespec new research <name>` to write `.litespec/intent/research/<name>/<name>.yaml` and reserve the identifier `research/<name>`. The command also writes `<name>.md` from the research template, with the title filled in, because research is an article type. The template's values are examples, and `model check` reports a value left unchanged. This satisfies `litespec/core/intent#keep-intent-in-its-homes` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Bound the Question

Write `question` as one question that the record answers. Findings answer only that question. A finding that answers another question belongs in another research record.

Split a record when any of these differ between two findings: the question, the sources, the conditions under which the claims hold, or the rate at which they change. A record on return boxes that also states deadlines and payment schemes answers three questions, so it becomes three records.

| Instead of | Write |
|:---|:---|
| `question: How do companies handle expenses?` | `question: How fast do small companies decide expenses?` |

The first question has no answer a choice can rest on, and every finding fits under it. The second names one measure and one group, so a finding that does not answer it is visibly out of place. This satisfies `litespec/core/intent#split-records-whose-parts-change-separately` and `litespec/core/intent#state-each-research-finding-with-its-source`.

### List the Sources

Give each source in `sources` a `key`, its `url`, and the `readOn` date on which you read it. Add `publishedOn` when the source states its publication date, so the publication date stays apart from the read date.

```yaml
sources:
  - key: example-expense-study
    url: https://example.com/expense-study
    readOn: 2026-02-10
```

| Instead of | Write |
|:---|:---|
| `readOn` taken as the date the rule took effect | `readOn` as the day you read the source, and `publishedOn` as the day the source says it was published |

`readOn` says when someone read the source, so a later reader knows how old the reading is. The date a rule takes effect and the period a figure covers are claims a finding states with its source, not fields of the source. This satisfies `litespec/core/intent#record-research-with-read-sources` and `litespec/core/intent#cite-a-source-for-each-constraint-rule` for the shared source shape.

### Write Each Finding as One Claim

Give each finding its own paragraph under Findings. Each paragraph states one claim, with the conditions it holds under, and cites its source key as a reference link `[text][key]`. The paragraph also says whether the source states the claim or the writer infers it.

| Instead of | Write |
|:---|:---|
| Claimwick reimburses faster than comparable products. | The benchmark report states that the comparable products it surveyed reimburse within three days of approval [benchmark report][benchmark-report]. |

The first version is a claim about Claimwick that no source makes, and it names no comparison group. The second states what the source says, about the group the source surveyed. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source`.

A finding that is a writer's inference names it as one:

```markdown
This suggests that five business days is a defensible target for Claimwick, but the study does not state how many companies it covers [industry study][example-expense-study].
```

`model check` reports `finding-source-missing` for a Findings paragraph with no source link. It cannot tell a statement from an inference, so the wording carries that, and a reviewer checks it against the source.

### Keep the Qualification When You Shorten

A finding keeps each condition its source gives. "Where deductible" never becomes "is deducted", and "for quarterly filers" never disappears from a summary. A shortened sentence is shorter, not broader.

| Instead of | Write |
|:---|:---|
| The receipts are deducted from taxable income. | The receipts are deductible where the expense is a business cost, as the guidance states [tax guidance][tax-guidance]. |

The first version drops the condition the source states, so a reader takes a narrow rule for a general one. This satisfies `litespec/core/writing#keep-the-conditions-when-shortening`.

### Separate a Stated Claim from an Inference

Write "the study states ..." for a claim the source makes, and "this suggests ..., but the study does not address ..." for an inference. Use the source's own position when the source states a figure, and name the gap when the source does not reach the conclusion.

| Instead of | Write |
|:---|:---|
| Small companies decide claims within one week, so Claimwick can promise five days. | The industry study states that small companies decide expense claims within one week [industry study][example-expense-study]. This suggests that five business days is a defensible target for Claimwick, but the study does not address companies outside its sample [industry study][example-expense-study]. |

The first version turns a source's figure into a promise for the product. The strong version keeps the source's statement apart from the writer's inference, and the promise belongs to the requirement that cites the research. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source`.

### Write Open Questions as Specific Questions

Under Open Questions, optional, write what the sources do not establish, each as a question that names what to investigate. A limit stated precisely tells a reader what to read next.

| Instead of | Write |
|:---|:---|
| More research is needed. | The reviewed sources do not establish whether the one-week figure holds for companies with more than fifty employees. A survey of companies in that size band would answer it. |

The first version names no gap and no investigation. The second names the population the sources miss and the study that would fill the gap. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source` and `litespec/core/writing#use-literal-words`.

### Write Source Notes

Under Source Notes, optional, state what each source covers, the audience it addresses, and where sources disagree. A note on a source's audience tells the reader how far its figures travel.

| Instead of | Write |
|:---|:---|
| The study is good. | The study surveys small companies in one country and addresses managers who approve expenses. It does not state how many companies it covers, and it disagrees with the vendor survey on the share of claims decided within a day. |

Source Notes holds what a later reader needs about each source. An open question about the facts belongs in Open Questions, not here.

### Write the Conclusion No Broader Than the Findings

Write `conclusion` in one sentence that states what the sources establish, with the conditions the findings carry. Leave out what follows for the product: a consequence belongs to the constraint, decision or requirement that cites this research.

| Instead of | Write |
|:---|:---|
| Small companies decide expenses within one week, so Claimwick can promise five days. | According to one small recent industry study, small companies decide expenses within one week. |

The first version adds a product promise the research cannot make. The second states the source's claim with its qualifier, `according to one small recent industry study`, and stops there. This satisfies `litespec/core/writing#keep-the-conditions-when-shortening` and `litespec/core/intent#state-each-research-finding-with-its-source`.

### Keep the Dates Apart

Four dates can sit in one record, and each means something else:

- `publishedOn`, the date the source says it was published.
- `readOn`, the date someone read the source.
- The date a rule takes effect, which a constraint records as `effectiveFrom` and a finding states with its source.
- The period a figure covers, which a finding states with its source.

| Instead of | Write |
|:---|:---|
| `readOn: 2026-02-10`, read as the date the rate applies | `readOn: 2026-02-10` for the read, `publishedOn` for the publication, and "the rate applies from 1 January 2026 [tax guidance][tax-guidance]" in the finding |

The read date shows only when someone read the source. This satisfies `litespec/core/intent#record-research-with-read-sources`.

### Run Model Render

Run `litespec model render` from the consumer root. It writes a generated `source-links` region at the end of the explanation file, with one Markdown link definition for each source, such as `[example-expense-study]: https://example.com/expense-study "Read on 2026-02-10"`. Each `[text][key]` citation then works in a plain Markdown preview. The region is generated, so a hand edit inside it is drift.

The [Model](../tools/model.md) command page states what `render` writes, and `model check` reports `source-links-missing` and `source-links-drift` when the region is absent or differs.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the research record or its explanation file. It reports `source-key-unknown` for a key the record does not declare, `source-link-bare` for a bare URL, and `source-unused` for a source no paragraph cites.
- `litespec rules <path to the research record>` lists the judgment provisions above, and each one is answered.
- A reader who has never met the author can say which claims the sources state, which are inferences, what the sources leave open, and what each source covers.
