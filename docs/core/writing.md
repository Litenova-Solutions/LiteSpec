# Writing

**Stack:** any
**Concern:** workspace

This standard states how a project writes its records, its product copy, and its human documentation. It is for consumers who write record fields, screens, and guides. A reader who lands on one record or page with nothing read before it still learns who acts, what is at stake, and what to do next.

## Concepts

### The Three Scopes

| Scope | Covers | Fields |
|:---|:---|:---|
| Machine-read record text | What the model builds from | `statement`, `summary`, `meaning`, `title` |
| End-user product copy | What the product shows a person | `message`, `translations`, screen labels, notifications |
| Human documentation | What people read to learn and run the product | `README.md`, guides, decisions, runbooks, generated book |

Scope 1 serves agents and builders: the words state facts the tools check and the code implements. A builder implements the sentence and a check compares it with code, so the actor, the condition, and the exact domain term decide whether the work passes.

Scope 2 serves the person using the product. The words state what happened and the next step the person takes, in the voice the product record declares. Product copy lists no raw refusal code or trace and places no blame on the reader. The record and the log carry the code; the screen carries the fix.

Scope 3 serves the colleague who arrives cold. The prose is plainspoken and technical: direct, active voice, short paragraphs, concrete examples, honest limits. Guides and runbooks address the reader directly, while record values keep the third person.

### Brand Voice and the Universal Baseline

Each product record declares its voice in a `brand:` block in `product.yaml`, with the tone it keeps and the tone it avoids.

The universal baseline applies under every brand. Product copy states what happened and what the person does next, lists no raw refusal code or trace, and places no blame on the reader. The declared brand shapes the wording; the baseline holds for each product.

## Standards

### Write Machine-Read Fields in Present Tense

**ID:** `litespec/core/writing#write-machine-read-fields-in-present-tense`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every machine-read field in a record MUST name its actor first, in the present tense, with each condition kept and each domain term taken from the glossary.

**Rationale:** Passive forms hide the actor, dropped conditions widen the claim, and loose verbs leave the act open. The actor, the condition, and the exact domain term decide whether the work passes, so each sentence keeps all three. Narrowed to meaning by [decision/0057-narrow-the-writing-rules-to-meaning](../intent/decisions/0057-narrow-the-writing-rules-to-meaning/0057-narrow-the-writing-rules-to-meaning.md): the rule covers action sentences with their conditions, never a word cap. Stated once for every record type by [decision/0054-write-each-shared-record-rule-once](../intent/decisions/0054-write-each-shared-record-rule-once/0054-write-each-shared-record-rule-once.md), which keeps the conditions of each statement in one provision instead of repeating them per type. Settled by decision/0087-keep-prose-checks-advisory-and-names-per-type.

**Example:** Claimwick writes "The approver approves the expense within five business days." The actor opens, the tense states what holds, and the days carry the condition.

**Reviewed for:** Does every machine-read field name its actor first in the present tense with conditions and glossary terms intact? Check the statements, summaries, meanings, and titles the change adds.

### Write Human Docs for the Cold Reader

**ID:** `litespec/core/writing#write-human-docs-for-the-cold-reader`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every human documentation page MUST open with its reader and purpose, hold one idea per paragraph, and show one concrete Claimwick example.

**Rationale:** Most readers land from a search result with no page read before it, so the opening decides in two sentences whether the page answers their question. Short paragraphs put each fact where a scanning reader finds it, and a concrete example shows the rule in use. Shared wording rules live once in this standard by [decision/0054-write-each-shared-record-rule-once](../intent/decisions/0054-write-each-shared-record-rule-once/0054-write-each-shared-record-rule-once.md), and each explanation describes only what its record states. Examples take each detail from the cast or a cited record, with the cast held as data by [decision/0112-cast-as-a-record](../intent/decisions/0112-cast-as-a-record/0112-cast-as-a-record.md).

**Example:** A Claimwick how-to opens with "Remind an approver who has held an expense past three business days." The reader and the one problem solved stand before the first step.

**Reviewed for:** Does every human documentation page open with reader and purpose, hold one idea per paragraph, and show one Claimwick example? Check the opening lines and paragraphs of each page the change adds.

### Keep Raw Refusal Codes Out of Product Copy

**ID:** `litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every product copy string MUST carry no raw refusal code, no error trace, and no blame toward the reader.

**Rationale:** A code such as a missing-receipt code routes the builder to the refused state; it tells the person nothing about what to fix. The copy states the missing receipt and the fix, while the code stays in the record and the log.

**Example:** Claimwick shows "The receipt is missing. Attach a photo of the receipt, then submit again." The record behind it carries the refusal, and the screen lists no code.

**Reviewed for:** Does any product copy string list a raw refusal code or trace, or blame the reader? Check each user-facing string against its record.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/writing#write-machine-read-fields-in-present-tense`, `litespec/core/writing#write-human-docs-for-the-cold-reader`, and `litespec/core/writing#keep-raw-refusal-codes-out-of-product-copy`.

A Claimwick requirement states "An expense above the threshold carries a receipt." Its product copy reads "The receipt is missing. Attach a photo of the receipt, then submit again." Its guide opens with the approver who waits past three business days and walks through one reminder with the cast.
