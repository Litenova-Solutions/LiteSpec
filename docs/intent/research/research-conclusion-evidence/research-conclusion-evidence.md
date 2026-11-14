# Research Conclusion Evidence

A LiteSpec research record gathers evidence on one question, and a decision can cite that record when it makes a choice. This research record checks whether the consumer's research records state a conclusion that a decision can cite, and where that conclusion lives today. The consumer is a project that uses LiteSpec. It matters because a decision that cites a research page needs a conclusion it can point to.

## Findings

Every consumer research record states its finding in Answer. But 16 of the 29 records restate what follows under 7 invented headings. The register index repeats the conclusions again, outside the records. So the conclusion needs a field of its own.

The consumer's research record requires a frontmatter `question`, a `sources` array and one body section, Answer. No field holds the conclusion where a tool can read it. Answer is body prose, and only `sources` and `question` are data. So a record's finding can change in prose, and no field marks that change. The record gives high confidence for this gap, because it read the schema, the template, the validator, the checker and the book field list.

The book renders only `question` and `sources` for research. So a decision that cites research through `basedOn` points at a page whose takeaway no field states. The record gives high confidence for this gap, for the same reason.

The schema does not enforce the read date that the provision already requires. The `read` field is optional on a source entry. The record gives high confidence for this gap, for the same reason.

All 29 consumer records carry Answer. Of them, 16 add a second section that states the conclusion, under 7 drifting names. Of them, 6 records use `What This Means for the Choice`, and 4 use `What This Means for Each Choice`. One record each uses `What This Means for the Model`, a product-named variant, `Conclusion`, `The Position` and `What This Record Changes`. Short records state their whole conclusion inside Answer. Long records separate what the sources say from what it means for each choice. The record gives no confidence for this claim.

The register index repeats the conclusions a third time, in a beliefs table. The table gives a confidence and a required action for each record. The index sits outside any record, and no check reads it. The record gives no confidence for this claim.

All 125 source entries already carry a `read` date. Of 29 records, 15 are cited by no other record's `basedOn`. The record calls that tolerated, not defective. The record gives no confidence for these counts.

The record proposes one required frontmatter field, `conclusion`, on the research page. It makes `read` required on every source. Each source takes exactly one of `url` for a web source, or `path` for a repository-relative evidence path. Repository evidence cannot name a URL honestly. The provision then requires a research record to state its `question`, to state its `conclusion` in one sentence, to list each source with its read date and to carry Answer. The record gives no confidence for this proposal.

The checks that follow the proposal are 3. The consumer fails a page through the schema. The model checker reports a missing conclusion with the other `record-invalid` findings. The book renders a Conclusion row, and it shows `path` sources as code text. The record gives no confidence for these checks.

The record rejects 2 alternatives. It rejects a `Conclusion` body section, because the 7 invented names are the drift that a section would make official, and prose cannot be cited as data. It also rejects a record-level `confidence` field. Multi-claim records carry a label for each claim, so one value would make some claims false. Confidence stays in Answer, claim by claim. The record gives no confidence for these rejections.

The cost to the consumer is 29 one-sentence fields, lifted from the existing openings. The `read` tightening costs nothing, because all 125 of 125 entries already carry a `read` date. The record gives no confidence for this cost.

## Source Notes

The record names 3 sources: the consumer research template (`templates/consumer/research.md`), the record schema (`schemas/record.schema.yaml`) and the core Intent page (`docs/core/intent.md`). All 3 were read on 30 October 2026. The record does not say which claim each source supports.

The Answer also cites the consumer's research folder, `.litespec/intent/research/`, with its 29 records and its index. It read that folder directly on 30 October 2026, but the folder is not in the sources list. The record also names the validator, the checker and the book field list as sources for its gap claims, and they are not in the sources list either.

The record names no disagreement between its sources. It names no claim that it could not confirm.
