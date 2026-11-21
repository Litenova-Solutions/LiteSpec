# Behavior Containment Evidence

In LiteSpec, a Behavior record describes what the product does, such as an aggregate, a use case, a reaction, a job, or a screen. Each record belongs under a parent. A reader finds the parent either in the record's identifier or in the file that holds the record. This research record checks how the largest consumer project finds those parents. It also checks where a reader reads the parent from the file name only or finds no parent at all. The consumer is a project that uses LiteSpec. A reader who opens one record needs to find its parent without a search.

## Findings

Containment by identifier is sound and complete in the largest consumer (`model`, `record-schema`). A reader finds the parent of results, scenarios, domain events, and rules only through file placement or a project-wide name lookup (`model`, `record-schema`). Several Behavior types give a reader no parent at all (`model`, `behavior`).

Containment by identifier has no gaps where it is defined (`model`, `record-schema`). On 30 October 2026, the built model of the largest consumer contained 21,548 records (`model`). Zero of 181 aggregates, zero of 1,272 use cases, and zero of 256 reactions, 73 jobs, 107 notifications, and 76 settings name a missing module (`model`, `behavior`, `record-schema`). Zero of 380 screens name a missing entry point (`model`, `access`, `record-schema`).

Name-only parentage also has no gaps (`model`, `record-schema`). All 5,413 written results, 6,517 scenarios, 806 domain events, and 2,296 rules name a parent that exists (`model`, `record-schema`). The project-wide name uniqueness checks keep those parents valid (`model`). So the consumer has no parent that names a missing record (`model`, `record-schema`).

A reader reads the parent of most child records from the file, not from the identifier (`model`, `record-schema`). The consumer places 5,382 of 5,413 written results (99.4 percent) and 6,395 of 6,517 scenarios (98.1 percent) on their use case's file (`model`, `behavior`). It places 805 of 806 domain events on their aggregate's file (`model`, `behavior`). A reader finds the parent by opening the file, but reads nothing from the identifier without a project-wide name lookup (`model`, `record-schema`).

No check enforces which page contains which record for entries, and the consumer shows 2 leaks (`model`, `record-schema`). The consumer places 31 written results on reaction pages, although results belong only on use case pages (`model`, `behavior`). The consumer places 54 rules on module `README.md` pages, a third home that the specification never names (`model`, `record-schema`). The checker reports no finding for either leak, because the builder accepts entries on any page that carries a `records` array (`model`, `record-schema`).

Few rules cite a record above them (`model`, `record-schema`). Zero of 2,296 rules govern nothing, so every rule governs something (`model`, `record-schema`). Only 243 rules carry `fulfills` and 28 carry `basedOn`, so rules govern downward and rarely cite upward (`model`, `record-schema`). The record states that the specification allows this (`model`, `record-schema`).

One domain event is an orphan (`model`, `behavior`). One of 806 domain events, `domain-event/contact-profile.attendance-recorded`, stands unused: no use case raises it, no reaction hears it, and nothing translates it (`model`, `behavior`). Zero of 107 notifications lack a sender, and the sender check exists and passes (`behavior`).

The specification defines no reader link for a setting, so the model shows no code that reads a setting (`behavior`, `record-schema`). The record names unread settings as the finding, because the model cannot identify them (`behavior`, `record-schema`). Still, 15 settings carry a `readBy` field that the schema forbids (`record-schema`). Rules carry `reads` in 33 records, and notifications carry `serves` in 43 records (`behavior`, `record-schema`). The schema and every check ignore both fields (`record-schema`). The record states that projects need these links and that LiteSpec defines none of them (`behavior`, `record-schema`).

Few records name a runbook (`behavior`). No job names 43 of 46 runbooks, and 15 of 73 jobs name no runbook (`behavior`).

Several other records carry no link that the model reads (`model`, `behavior`, `access`). One permission, `permission/synchronize-offline-check-in`, is allowed but never granted (`behavior`, `access`). One entry point, `entry-point/site`, has zero screens, while 12 use cases name it in `reachedThrough` (`behavior`, `access`). 20 screens offer and serve nothing, which the navigation-screen convention allows (`access`).

The builder derives no aggregate for 47 use cases (`model`, `behavior`). Of them, 33 are queries that the consumer files under `reads/`, which the home table allows (`model`, `behavior`). The other 14 are commands, which the table does not cover (`model`, `behavior`). Of 27 outbound integration events, nothing publishes 3 (`behavior`). No reaction answers both inbound events (`behavior`).

Fields that the specification requires are missing, and no finding reports them (`model`, `record-schema`). Of 73 jobs, 61 lack `serviceLevel` (`behavior`, `record-schema`). Of 895 commands, 741 declare no `consistency` (`behavior`, `record-schema`). One aggregate declares an empty `exits` list, and 5 reactions lack `onFailure` (`behavior`, `record-schema`). Each finding carries a tier, and the tier sets whether the checker reports or blocks it (`model`). The checker reports tier S in this consumer, which explains the absence at tier S only (`model`). That explains neither the absence at tier G nor the absence in the consumer (`model`).

The specification uses 2 forms for result identifiers (`record-schema`). The specification names a written result `result/<use-case-name>.<name>` and a derived result `result/<module>.<use-case>.forbidden` (`record-schema`). The consumer follows both forms exactly in 5,413 written results and 2,062 derived results (`record-schema`).

The investigation proposes one parent for each type (`model`, `record-schema`). Results and scenarios take the module-qualified derived form, so a reader reads the parent from the identifier (`model`, `record-schema`). Domain events name their module in the identifier (`model`, `record-schema`). A requirement check requires a non-empty `governs` at tier G once it is staged (`model`, `record-schema`). The published set becomes the integration event's book parent, and `readBy` becomes the standard name for settings (`model`, `record-schema`).

The proposal also gives the 6 list types (role, permission, entry point, external system, runbook, and term) their list or folder as the book parent (`model`). It states that a module's parent is the product's bounded context (`model`). Each new check starts advisory and then promotes (`model`).

## Source Notes

The record names 4 sources, all read on 30 October 2026. They are the core model page (`docs/core/model.md`), the core Behavior page (`docs/core/behavior.md`), the core access page (`docs/core/access.md`), and the record schema (`schemas/record.schema.yaml`). Each finding cites the sources that support it.

The specification uses 2 result identifier forms, and the consumer follows both. The fields `readBy`, `reads`, and `serves` are missing from the record schema, while the consumer uses them.

The record cannot confirm which settings a consumer's code reads, because it did not read that code. It also measured one consumer on one day, so the counts describe that consumer only. Each count is a direct measurement over that consumer's built model.
