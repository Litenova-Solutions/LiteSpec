# Research States Its Conclusion

A research record in LiteSpec answers one question and cites the sources it read. This decision requires each research record to state its conclusion in one sentence, and to date every source it reads. It matters because the book shows the conclusion beside the question, and a decision can cite that one sentence.

## Context

Every research record states its finding in an Answer section. Beyond that section, conclusions drift into invented section names and into index tables that no tool reads.

## Options

LiteSpec compares 4 ways to record a research conclusion.

| Option | Strengths | Drawbacks |
|---|---|---|
| A conclusion field plus dated sources | One sentence as data renders in the book and gives citing decisions a stable point to cite | Every research record gains a field to write |
| A Conclusion body section | No schema or checker change is needed | 7 invented heading names already show the drift that a section would codify, and prose cannot be cited as data |
| A record-level confidence field | One value summarizes how far to trust each record | One value is false for a record with several claims, so confidence stays in Answer, claim by claim |
| Leave research as question plus Answer | No author writes anything new | A finding stays editable in prose, and the book renders pages whose takeaway no field states |

The chosen option adds one field. A field is data that the book and a decision can both read.

## Decision

Each research record gains a required `conclusion` field, which holds one sentence. Every source carries its read date. A source names exactly one of a web `url` or a repository-relative evidence `path`.

## Consequences

The provision `litespec/core/intent#record-research-with-read-sources` states the question, the conclusion, the dated sources and the Answer. The book shows the conclusion beside the question.

## Evidence

`research/research-conclusion-evidence` is read from the research folder of the largest consumer on 2026-10-30. Its finding on section drift and its counts of read dates have high confidence.

## Revisit Conditions

This decision reopens when a research kind appears whose takeaway one sentence cannot state.
