# Behavior Containment Evidence

In LiteSpec, a Behavior record describes what the product does, such as an aggregate, a use case, a reaction, a job, or a screen. Each record belongs under a parent. A reader finds the parent either in the record's identifier or in the file that holds the record. This research record checks how the largest consumer project finds those parents. It also checks where a reader reads the parent from the file name only or finds no parent at all. The consumer is a project that uses LiteSpec. A reader who opens one record needs to find its parent without a search.

## Findings

Containment by identifier is sound and complete in the largest consumer. A reader finds the parent of results, scenarios, domain events, and rules only through file placement or a project-wide name lookup. Several Behavior types give a reader no parent at all.

Containment by identifier has no gaps where it is defined. On 30 October 2026, the built model of the largest consumer contained 21,548 records. Zero of 181 aggregates, zero of 1,272 use cases, and zero of 256 reactions, 73 jobs, 107 notifications, and 76 settings name a missing module. Zero of 380 screens name a missing entry point. The record gives high confidence for this claim, because each count is a direct measurement over the built model.

Name-only parentage also has no gaps. All 5,413 written results, 6,517 scenarios, 806 domain events, and 2,296 rules name a parent that exists. The project-wide name uniqueness checks keep those parents valid. So the consumer has no parent that names a missing record. The record gives high confidence for this claim, because each count is a direct measurement over the built model.

A reader reads the parent of most child records from the file, not from the identifier. The consumer places 5,382 of 5,413 written results (99.4 percent) and 6,395 of 6,517 scenarios (98.1 percent) on their use case's file. It places 805 of 806 domain events on their aggregate's file. A reader finds the parent by opening the file, but reads nothing from the identifier without a project-wide name lookup. The record gives no confidence for this claim.

No check enforces which page contains which record for entries, and the consumer shows 2 leaks. The consumer places 31 written results on reaction pages, although results belong only on use case pages. The consumer places 54 rules on module `README.md` pages, a third home that the specification never names. The checker reports no finding for either leak, because the builder accepts entries on any page that carries a `records` array. The record gives no confidence for this claim.

Few rules cite a record above them. Zero of 2,296 rules govern nothing, so every rule governs something. Only 243 rules carry `fulfills` and 28 carry `basedOn`, so rules govern downward and rarely cite upward. The record states that the specification allows this. The record gives no confidence for this claim.

One domain event is an orphan. One of 806 domain events, `domain-event/contact-profile.attendance-recorded`, stands unused: no use case raises it, no reaction hears it, and nothing translates it. Zero of 107 notifications lack a sender, and the sender check exists and passes. The record gives no confidence for this claim.

The specification defines no reader link for a setting, so the model shows no code that reads a setting. The record names unread settings as the finding, because the model cannot identify them. Still, 15 settings carry a `readBy` field that the schema forbids. Rules carry `reads` in 33 records, and notifications carry `serves` in 43 records. The schema and every check ignore both fields. The record states that projects need these links and that LiteSpec defines none of them. The record gives no confidence for this claim.

Few records name a runbook. No job names 43 of 46 runbooks, and 15 of 73 jobs name no runbook. The record gives no confidence for this claim.

Several other records carry no link that the model reads. One permission, `permission/synchronize-offline-check-in`, is allowed but never granted. One entry point, `entry-point/site`, has zero screens, while 12 use cases name it in `reachedThrough`. 20 screens offer and serve nothing, which the navigation-screen convention allows. The record gives no confidence for this claim.

The builder derives no aggregate for 47 use cases. Of them, 33 are queries that the consumer files under `reads/`, which the home table allows. The other 14 are commands, which the table does not cover. Of 27 outbound integration events, nothing publishes 3. No reaction answers both inbound events. The record gives no confidence for this claim.

Fields that the specification requires are missing, and no finding reports them. Of 73 jobs, 61 lack `serviceLevel`. Of 895 commands, 741 declare no `consistency`. One aggregate declares an empty `exits` list, and 5 reactions lack `onFailure`. Each finding carries a tier, and the tier sets whether the checker reports or blocks it. The checker reports tier S in this consumer, which explains the absence at tier S only. That explains neither the absence at tier G nor the absence in the consumer. The record gives no confidence for this claim.

The specification uses 2 forms for result identifiers. The specification names a written result `result/<use-case-name>.<name>` and a derived result `result/<module>.<use-case>.forbidden`. The consumer follows both forms exactly in 5,413 written results and 2,062 derived results. The record gives no confidence for this claim.

The investigation proposes one parent for each type. Results and scenarios take the module-qualified derived form, so a reader reads the parent from the identifier. Domain events name their module in the identifier. A requirement check requires a non-empty `governs` at tier G once it is staged. The published set becomes the integration event's book parent, and `readBy` becomes the standard name for settings. The record states no confidence for these proposals.

The proposal also gives the 6 list types (role, permission, entry point, external system, runbook, and term) their list or folder as the book parent. It states that a module's parent is the product's bounded context. Each new check starts advisory and then promotes. The record states no confidence for these proposals.

## Source Notes

The record names 4 sources, all read on 30 October 2026. They are the core model page (`docs/core/model.md`), the core Behavior page (`docs/core/behavior.md`), the core access page (`docs/core/access.md`), and the record schema (`schemas/record.schema.yaml`). The record does not say which claim each source supports.

The sources disagree in 2 places. The specification uses 2 result identifier forms, and the consumer follows both. The fields `readBy`, `reads`, and `serves` are missing from the record schema, while the consumer uses them.

The record cannot confirm which settings a consumer's code reads, because it did not read that code. It also measured one consumer on one day, so the counts describe that consumer only.
