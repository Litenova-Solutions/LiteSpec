# Behavior Containment Evidence

In LiteSpec, a Behavior record describes what the product does, such as an aggregate, a use case, a reaction, a job or a screen. Each one belongs under a parent. The parent is found either from the record's identifier or from the file the record sits in. This research record checks how the largest consumer, a project that uses LiteSpec, finds those parents, and where a parent rests on a file name or has no parent at all. It matters because a reader who opens one record needs to find its parent without a search.

## Findings

Containment by identifier is sound and complete in the largest consumer. Results, scenarios, events and rules find their parent only through file placement or a project-wide name lookup. Several Behavior types have no parent at all.

Containment by identifier holds everywhere it is defined. On 30 October 2026, the built model of the largest consumer held 21,548 records. Zero of 181 aggregates, zero of 1,272 use cases, and zero of 256 reactions, 73 jobs, 107 notifications and 76 settings name a missing module. Zero of 380 screens name a missing entry point. The record gives high confidence for this claim, because each count is a direct measurement over the built model.

Name-only parentage resolves everywhere too. All 5,413 written results, 6,517 scenarios, 806 events and 2,296 rules name a parent that exists. The project-wide name uniqueness checks guard that. So the consumer has no dangling container to fix. The record gives high confidence for this claim, for the same reason.

The file, not the identifier, is the real parent of most child records. The consumer places 5,382 of 5,413 written results (99.4 percent) and 6,395 of 6,517 scenarios (98.1 percent) on their use case's file. It places 805 of 806 events on their aggregate's file. A reader finds the parent by opening the file, but the identifier alone needs a project-wide name lookup. The record gives no confidence for this claim.

The rule for which page holds which record is not enforced for entries, and the consumer shows 2 leaks. Of the written results, 31 sit on reaction pages, although results belong only on use case pages. Of the rules, 54 sit on module `README.md` pages, a third home that the specification never names. No finding fires for either, because the builder accepts entries on any page that carries a `records` array. The record gives no confidence for this claim.

Few rules are justified upward. Zero of 2,296 rules govern nothing. Only 243 carry `fulfills` and 28 carry `basedOn`, so rules are governed downward and rarely justified upward. The record calls this legal today. The record gives no confidence for this claim.

One event is an orphan. One of 806 events, `event/contact-profile.attendance-recorded`, is raised by no use case, heard by no reaction and translated by nothing. Zero of 107 notifications lack a sender, and the sender check exists and passes. The record gives no confidence for this claim.

The specification defines no reader link for a setting, so the model cannot show which code reads a setting. The record treats the settings that no code reads as the finding, because they cannot be known. Of the settings, 15 carry a `readBy` field that the schema forbids. Rules carry `reads` (33 records) and notifications carry `serves` (43 records), and the schema and every check ignore both. The record says the projects need these edges and LiteSpec has not admitted them. The record gives no confidence for this claim.

Runbooks are named by almost nothing. Of 46 runbooks, 43 are named by no job, and of 73 jobs, 15 name no runbook. The record gives no confidence for this claim.

Several other records have no link that the model reads. One permission, `permission/synchronize-offline-check-in`, is allowed but never granted. One entry point, `entry-point/site`, hosts zero screens, while 12 use cases claim it in `reachedThrough`. Of the screens, 20 offer and serve nothing, which the navigation-screen convention allows. The record gives no confidence for this claim.

The builder derives no aggregate for 47 use cases. Of them, 33 are queries filed under `reads/`, which the home table allows. The other 14 are commands, which the table does not explain. Of 27 outbound integration events, 3 are published by nothing. Both inbound events are answered by no reaction. The record gives no confidence for this claim.

Fields that the specification requires are missing, and no finding reports them. Of 73 jobs, 61 lack `serviceLevel`. Of 895 commands, 741 declare no `consistency`. One aggregate declares an empty `exits` list, and 5 reactions lack `onFailure`. Each finding has a tier, and the tier decides whether it reports or blocks. Tier S reports in this consumer, which explains the silence at tier S only. It does not explain the silence at tier G or in the consumer. The record gives no confidence for this claim.

The specification uses 2 forms for result identifiers. A written result is named `result/<use-case-name>.<name>`, while a derived result is named `result/<module>.<use-case>.forbidden`. The consumer follows both forms exactly, with 5,413 written results and 2,062 derived results. The record gives no confidence for this claim.

The investigation proposes one parent for each type. Results and scenarios take the module-qualified derived form, so the parent reads from the identifier. Events take their module in the identifier. A requirement check makes `governs` non-empty at tier G once it is staged. The published set becomes the integration event's book parent, and `readBy` becomes the standard name for settings. The record states no confidence for these proposals.

The proposal also gives the 6 list types (role, permission, entry point, external system, runbook and term) their list or folder as the book parent. It states that a module's parent is the product's bounded context. Each new check starts advisory and then promotes. The record states no confidence for these proposals.

## Source Notes

The record names 4 sources, all read on 30 October 2026. They are the core model page (`docs/core/model.md`), the core Behavior page (`docs/core/behavior.md`), the core access page (`docs/core/access.md`) and the record schema (`schemas/record.schema.yaml`). The record does not say which claim each source supports.

The sources disagree in 2 places. The specification uses 2 result identifier forms, and the consumer follows both. The fields `readBy`, `reads` and `serves` sit outside the record schema, while the consumer uses them.

The record cannot confirm what the settings and reads of a consumer would show if its code were read. It also measured one consumer on one day, so the counts describe that consumer only.
