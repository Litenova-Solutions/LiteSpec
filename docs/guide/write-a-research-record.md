# Write a Research Record

Write one research record as a YAML file `<name>.yaml` and an explanation file `<name>.md`, answering one question with the sources that bear on it.

A research record states what those sources establish and what they leave open, so a reader can check each claim against its source, see the conditions the claim holds under, and tell a source's statement from the writer's inference.

Each step shows a weak version and a strong version from the Claimwick expense product, names the provisions it satisfies, and adds no rule of its own.

## Prerequisites

Start with:

- A consumer repository set up as [Start a Consumer Repository](../tutorial/getting-started.md) describes, with its product record written.
- A question that a choice depends on, such as how fast a sufferer's peers decide a claim.
- The sources you plan to read, each with its URL and the date you read it.

The [Intent](../core/intent.md) and [Writing](../core/writing.md) pages state the obligations behind these steps. Read them first; the guide shows one compliant way to meet them. The [command index](../tools/README.md) states how to spell `litespec` in a consumer.

## Procedure

### Scaffold the Research Record

Run `litespec new research <name>` from the consumer root. The command writes the record file at `.litespec/intent/research/<name>/<name>.yaml`, reserves the identifier `research/<name>`, and writes the explanation file `<name>.md` from the research template with the title filled in.

Research is an article type: the YAML file carries the question, the sources, the findings, and the conclusion, while the explanation file restates the findings in prose.

The template ships with placeholders. Replace every one of them, because `model check` reports any placeholder you leave unchanged. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#scaffold-pages-and-reserve-identifiers`.

### Ask One Question

Write `question` as the single question the record answers. Every finding in the record answers that question, and a finding that answers a different question belongs in a different record.

Split a record when any of these differ between two findings: the question, the sources, the conditions under which the claims hold, or the rate at which they change. A record on return boxes that also states deadlines and payment schemes answers three questions, so write it as three records.

| Instead of | Write |
|:---|:---|
| `question: How do companies handle expenses?` | `question: How fast do small companies decide expenses?` |

A broad question accepts any finding and supports no choice. A narrow question names one measure and one group, so a finding that does not answer it stands out at once. This satisfies `litespec/core/intent#split-records-whose-parts-change-separately` and `litespec/core/intent#state-each-research-finding-with-its-source`.

### List the Sources

List every source the record relies on in `sources`. Each entry needs a `name`, the `url`, and the `readOn` date you read it. Add `publishedOn` when the source states when it was published, so the two dates stay apart.

```yaml
sources:
  - name: example-expense-study
    url: https://example.com/expense-study
    readOn: 2026-02-10
```

| Instead of | Write |
|:---|:---|
| `readOn` taken as the date the rule took effect | `readOn` as the day you read the source, and `publishedOn` as the day the source says it was published |

`readOn` says when someone read the source, so a later reader knows how old the reading is. The date a rule takes effect and the period a figure covers are claims a finding states with its source, not fields of the source. This satisfies `litespec/core/intent#record-research-with-its-question-conclusion-and-sources` and `litespec/core/intent#cite-a-source-for-each-constraint-rule` for the shared source shape.

### Keep the Four Dates Apart

One record can hold four dates, and each means something different:

- `publishedOn`, the date the source says it was published.
- `readOn`, the date someone read the source.
- The date a rule takes effect, which a constraint records as `effectiveFrom` and a finding states with its source.
- The period a figure covers, which a finding states with its source.

| Instead of | Write |
|:---|:---|
| `readOn: 2026-02-10`, read as the date the rate applies | `readOn: 2026-02-10` for the read, `publishedOn` for the publication, and "the rate applies from 1 January 2026 [tax guidance][tax-guidance]" in the finding |

The read date shows only when someone read the source, never when a rate or rule applies. This satisfies `litespec/core/intent#record-research-with-its-question-conclusion-and-sources`.

### State the Findings in Fields

```yaml
type: research
id: research/approval-time-benchmarks
title: Approval Time Benchmarks
question: How fast do small companies decide expenses?
sources:
  - name: example-expense-study
    url: https://example.com/expense-study
    readOn: 2026-02-10
    note: The study surveys small companies in one country and addresses managers who approve expenses.
findings:
  - statement: The surveyed small companies decide expense claims within one week.
    sources:
      - example-expense-study
openQuestions:
  - Whether the one-week figure holds for companies with more than fifty employees.
conclusion: According to one small recent industry study, small companies decide expenses within one week.
```

The explanation file restates `findings`, `openQuestions`, and each source's `note` in its Findings, Open Questions, and Source Notes sections; it adds no fact the record lacks.

### Write One Claim per Finding

State each finding as one item of `findings`, and restate each item under Findings in its own paragraph. Each item states one claim with the conditions it holds under, names its sources by name, and says whether the source states the claim or you infer it.

| Instead of | Write |
|:---|:---|
| Claimwick reimburses faster than comparable products. | The benchmark report states that the comparable products it surveyed reimburse within three days of approval [benchmark report][benchmark-report]. |

The weak version claims something about Claimwick that no source states, and it names no comparison group. The strong version stays with what the source says about the group the source surveyed. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source`.

State an inference of your own as one:

```markdown
This suggests that five business days is a defensible target for Claimwick, but the study does not state how many companies it covers [industry study][example-expense-study].
```

`model check` reports `finding-source-missing` for a Findings paragraph with no source link. The tool cannot tell a stated claim from an inference, so the wording has to carry that difference, and a reviewer checks it against the source.

### Keep the Conditions When You Shorten

A finding keeps each condition its source gives. "Where deductible" never becomes "is deducted", and "for quarterly filers" never drops out of a summary. A shorter sentence says less, not more.

| Instead of | Write |
|:---|:---|
| The receipts are deducted from taxable income. | The receipts are deductible where the expense is a business cost, as the guidance states [tax guidance][tax-guidance]. |

The weak version drops the condition the source states, so a reader takes a narrow rule for a general one. This satisfies `litespec/core/writing#keep-the-conditions-when-shortening`.

### Separate Stated Claims from Inferences

Write "the study states ..." for a claim the source makes, and "this suggests ..., but the study does not address ..." for an inference. Take the source's own position when it states a figure, and name the gap when it does not reach your conclusion.

| Instead of | Write |
|:---|:---|
| Small companies decide claims within one week, so Claimwick can promise five days. | The industry study states that small companies decide expense claims within one week [industry study][example-expense-study]. This suggests that five business days is a defensible target for Claimwick, but the study does not address companies outside its sample [industry study][example-expense-study]. |

The weak version turns a source's figure into a promise for the product. The strong version keeps the source's statement apart from your inference. The promise itself belongs to the requirement that cites the research. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source`.

### Write Open Questions as Specific Questions

Under Open Questions, optional, write what the sources do not establish. State each question as one item of `openQuestions`; Open Questions restates that field. State each gap as a question that names what to investigate next.

| Instead of | Write |
|:---|:---|
| More research is needed. | The reviewed sources do not establish whether the one-week figure holds for companies with more than fifty employees. A survey of companies in that size band would answer it. |

The weak version names no gap and no next step. The strong version names the population the sources miss and the study that would fill the gap. This satisfies `litespec/core/intent#state-each-research-finding-with-its-source` and `litespec/core/writing#use-literal-words`.

### Write Source Notes

Where the source needs a note, state it in the entry's `note`. Under Source Notes, optional, describe each source: what it covers, the audience it addresses, and where sources disagree. A note on a source's audience tells the reader how far its figures travel.

| Instead of | Write |
|:---|:---|
| The study is good. | The study surveys small companies in one country and addresses managers who approve expenses. It does not state how many companies it covers, and it disagrees with the vendor survey on the share of claims decided within a day. |

Source Notes holds what a later reader needs to know about each source. A gap in the facts belongs in Open Questions, not here.

### Write the Conclusion No Broader Than the Findings

Write `conclusion` as one sentence that states what the sources establish, with the conditions the findings carry. Leave out what follows for the product: a consequence belongs to the constraint, decision, or requirement that cites this research.

| Instead of | Write |
|:---|:---|
| Small companies decide expenses within one week, so Claimwick can promise five days. | According to one small recent industry study, small companies decide expenses within one week. |

The weak version adds a product promise the research cannot make. The strong version states the source's claim with its qualifier, `according to one small recent industry study`, and stops there. This satisfies `litespec/core/writing#keep-the-conditions-when-shortening` and `litespec/core/intent#state-each-research-finding-with-its-source`.

### Run Model Render

Run `litespec model render` from the consumer root. It appends a generated `source-links` region to the end of the explanation file, with one Markdown link definition per source, such as `[example-expense-study]: https://example.com/expense-study "Read on 2026-02-10"`.

With those definitions in place, each `[text][name]` citation resolves in a plain Markdown preview. The region is generated, so never edit inside it by hand: a hand edit is drift.

The [Model](../tools/model.md) command page states what `render` writes. `model check` reports `source-links-missing` when the region is absent and `source-links-drift` when it differs from the sources.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on the research record or its explanation file. It reports `reference-unknown` for a name the record does not declare, `source-link-bare` for a bare URL, and `source-unused` for a source no paragraph cites.
- `litespec rules <path to the research record>` lists the judgment provisions above, and you have answered each one.
- A reader who has never met you can say which claims the sources state, which are your inferences, what the sources leave open, and what each source covers.
