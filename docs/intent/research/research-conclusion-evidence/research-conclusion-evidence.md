# Research Conclusion Evidence

A LiteSpec research record gathers evidence on one question, and a decision can cite that record when it makes a choice. This research record checks whether the consumer's research records state a conclusion that a decision can cite, and where that conclusion is held. The consumer is a project that uses LiteSpec. It matters because a decision that cites a research page needs a conclusion it can point to.

## Findings

Every consumer research record states its finding in `Answer` [research template][research]. Sixteen of the 29 records restate what follows under 7 invented headings. The register index repeats the conclusions again, outside the records. The record infers that the conclusion needs a field of its own.

The consumer research record requires a frontmatter `question`, a `sources` array, and one body section, `Answer` [research template][research]. No field holds the conclusion where a tool can read it [record schema][record-schema]. `Answer` is body prose, and only `sources` and `question` are data. The record infers that a finding can change in prose with no field marking that change.

The book renders only `question` and `sources` for research [research template][research]. The record infers that a decision citing research through `basedOn` points at a page whose takeaway no field states.

The schema does not enforce the read date that the provision already requires [record schema][record-schema]. The [Intent page][intent] states that requirement, and the `read` field is optional on a source entry.

All 29 consumer records carry `Answer` [research template][research]. Sixteen add a second section that states the conclusion, under 7 drifting names. Six records use `What This Means for the Choice`, and 4 use `What This Means for Each Choice`. One record each uses `What This Means for the Model`, a product-named variant, `Conclusion`, `The Position`, and `What This Record Changes`. Short records state their whole conclusion inside `Answer`. Long records separate what the sources say from what it means for each choice.

The register index repeats the conclusions a third time, in a beliefs table [research template][research]. The table gives a confidence and a required action for each record. The index sits outside any record, and no check reads it.

All 125 source entries already carry a `read` date [research template][research]. No other record `basedOn` cites 15 of 29 records. The record calls that tolerated, not defective.

The record proposes one required frontmatter field, `conclusion`, on the research page [record schema][record-schema]. It makes `read` required on every source. Each source takes exactly one of `url` for a web source, or `path` for a repository-relative evidence path. Repository evidence cannot name a URL honestly. The record proposes that the provision require `question`, one-sentence conclusion, dated sources, and `Answer` [Intent page][intent].

Three checks follow the proposal [record schema][record-schema]. Schema validation fails a page that breaks it. The model checker reports a missing conclusion with the other `record-invalid` findings. The book renders a `Conclusion` row, and it shows `path` sources as code text.

The record rejects two alternatives [record schema][record-schema]. It rejects a `Conclusion` body section, because the 7 invented names are the drift that a section would make official. Prose cannot be cited as data. It also rejects a record-level `confidence` field. Multi-claim records carry a label for each claim, so one value would make some claims false. Confidence stays in `Answer`, claim by claim.

The cost to the consumer is 29 one-sentence fields, lifted from the existing openings [research template][research]. The `read` tightening costs nothing, because all 125 of 125 entries already carry a `read` date.

## Source Notes

The record read the consumer [research template][research] on 30 October 2026.

The record read the [record schema][record-schema] on 30 October 2026.

The record read the core [Intent page][intent] on 30 October 2026.

[research]: ../../../../templates/consumer/research.md "Read on 2026-10-30"
[record-schema]: ../../../../schemas/record.schema.yaml "Read on 2026-10-30"
[intent]: ../../../../docs/core/intent.md "Read on 2026-10-30"
