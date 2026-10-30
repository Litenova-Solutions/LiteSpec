---
{
  "type": "research",
  "id": "research/behavior-containment-evidence",
  "title": "Behavior Containment Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "How do Behavior records find their parent today, and where does parentage rest on a file name or exist not at all?",
  "conclusion": "Containment by identifier is sound and complete in the largest consumer, while results, scenarios, events, and rules find their parent only through file placement or a project-wide name lookup, and several Behavior types have no parent at all.",
  "sources": [
    {
      "path": "docs/core/model.md",
      "read": "2026-10-30"
    },
    {
      "path": "docs/core/behavior.md",
      "read": "2026-10-30"
    },
    {
      "path": "docs/core/access.md",
      "read": "2026-10-30"
    },
    {
      "path": "schemas/record.schema.json",
      "read": "2026-10-30"
    }
  ]
}
---

# Behavior Containment Evidence

## Answer

This record distils the Behavior layer containment investigation of 2026-10-30 into the claims this release relies on. Counts below were measured over the largest consumer's built model at `D:/Personal-Projects/Nighthall/.model/model.json` on 2026-10-30, holding 21,548 records.

Containment by identifier holds everywhere it is defined. Zero of 181 aggregates, zero of 1,272 use cases, and zero of 256 reactions, 73 jobs, 107 notifications, and 76 settings name a missing module. Zero of 380 screens name a missing entry point, and the screen identifier agrees with its `entryPoint` field 380 of 380 times. Name-only parentage also resolves everywhere: all 5,413 written results, 6,517 scenarios, 806 events, and 2,296 rules resolve their named parent, guarded by the project-wide name uniqueness checks. There is no dangling-container problem to fix. Confidence is high: every count is a direct measurement over the built model.

The file, not the identifier, is the real parent for child records. The consumer places 5,382 of 5,413 written results (99.4 percent) and 6,395 of 6,517 scenarios (98.1 percent) on their use case's file, and 805 of 806 events on their aggregate's file, so a reader finds the parent by opening the file while the identifier alone needs a project-wide name lookup. Two leaks show the home rule is unenforced for entries: 31 written results sit on reaction pages, although results are homed only on use case pages, and 54 rules sit on module `README.md` pages, a third home the specification never names. No finding fires on either, because the builder accepts entries on any page carrying a `records` array.

The measured orphan list is short but real. Zero of 2,296 rules govern nothing, yet only 243 carry `fulfills` and 28 carry `basedOn`, so rules are governed downward and barely justified upward, which is legal today. One of 806 events is raised by no use case, heard by no reaction, and translated by nothing: `event/contact-profile.attendance-recorded`. Zero of 107 notifications lack a sender; that check exists and passes. Settings no code reads are unknowable, and that is the finding: the specification defines no reader link for a setting, while 15 settings carry a nonstandard `readBy` field the schema forbids. Rules carry nonstandard `reads` (33) and notifications carry nonstandard `serves` (43), likewise outside the schema and every check; the projects need these edges and LiteSpec has not admitted them. Runbooks are named by almost nothing: 43 of 46 are named by no job, and 15 of 73 jobs name no runbook at all. One permission is allowed but never granted, `permission/synchronize-offline-check-in`. One entry point, `entry-point/site`, hosts zero screens while 12 use cases claim it in `reachedThrough`. Twenty screens offer and serve nothing, which the navigation-screen convention allows. Forty-seven use cases carry no builder-derived aggregate: 33 queries filed under `reads/`, which the home table allows, and 14 commands, which it does not explain. Three of 27 outbound integration events are published by nothing, and both inbound events are answered by no reaction.

Required-on-paper fields sit missing and silent. Sixty-one of 73 jobs lack `serviceLevel`, 741 of 895 commands declare no `consistency`, one process aggregate declares an empty `exits` list, and 5 reactions lack `onFailure`; none produces a finding in the built model. Every Behavior record in the consumer is still draft, so tier S reports instead of blocking, but that explains only the tier S silence, not the tier G or consumer silence. One inconsistency sits in the specification itself: a written result is `result/<use-case-name>.<name>` while a derived result is `result/<module>.<use-case>.forbidden`, and the consumer follows both forms exactly (5,413 written, 2,062 derived).

The investigation proposes one parent per type: adopt the module-qualified derived form for results and scenarios so the parent reads off the identifier; qualify events with their module; require `governs` non-empty at tier G once staged; name the published set as the integration event's book parent; standardize `readBy` for settings; give the six list types (role, permission, entry point, external system, runbook, term) their list or folder as the book parent; and state once that a module's parent is the product's bounded context. Each new check starts advisory, then promotes.
