---
{
  "type": "research",
  "id": "research/lower-layers-practice",
  "title": "Lower Layers Practice",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "What do outside practice and the 2026-10-31 review show about exceptions, approval, proof, tooling, and design wording below Intent?",
  "conclusion": "The review confirms exceptions that are uneven and too wide, approval satisfiable without a second person, proof words that overclaim, tooling that fails open, and design departures without stated reasons, so one exception record, one approval path sized for one-person teams, tracked proof with four proof words, fail-closed checks, and renamed records follow.",
  "sources": [
    {
      "url": "https://cucumber.io/docs/gherkin/reference",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://github.com/cloudevents/spec",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://gdpr-info.eu/art-30-gdpr/",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://gdpr-info.eu/recitals/no-26/",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://martinfowler.com/bliki/TestPyramid.html",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://sphinx-needs.readthedocs.io/en/latest/",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://github.com/TNG/ArchUnitNET",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://owasp.org/projects/top-10-for-large-language-model-applications/",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/behavior.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/model.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/release.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/access.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-31"
    },
    {
      "path": "litespec.manifest.json",
      "readOn": "2026-10-31"
    }
  ]
}
---

# Lower Layers Practice

## Answer

This record distils the outside review of 2026-10-31, which read the Behavior, Implementation, Proof, and Governance areas as a newcomer against branch v3 at commit c9b2725, into the claims behind decisions D25 to D46. Four reviewers each read one area, compared it with outside practice, and checked each finding in the files; the Governance reviewer confirmed behavior on scratch builds, and five code claims were rechecked in code on the same day. The review does not repeat the 124 findings already verified in the findings table. Web sources re-read for this record carry 2026-10-31; the review's remaining bibliography is named in prose with Notes on Sources stating what was carried.

Exceptions are uneven and too wide. A draft known gap nobody approved silences a tier G finding, because suppression asks only that the status differ from retired, while deviations ask for approved status. An approved deviation holds every finding on its subject, not only the findings its provision governs, so a deviation citing a precedence provision silenced approval-reopened. Deferrals and the frozen phase relax blocking checks with no owner, approval, or expiry, against the scope rule that each departure resolves to a deviation record. Expiry uses four field names and a deviation expires silently, while a retired subject silences tier G findings. One exception record therefore replaces deviation, known gap, deferred provisions, and frozen check phases, with kind of departure or gap, covers naming a provision or finding codes, subject or paths, reason, owner, status, and expiresOn. It covers only what it names, it applies once approved where one person may approve, and exception-expired reports it after its date.

Approval can be satisfied without a second person, and the standing direction keeps it loose on purpose. Any string passes as a second reviewer, since the batch approver checks only that it differs from the author; an agent reading satisfies the tier 1 gate although the release page calls agent review advice; the single-record approve bypasses every batch guard, falls back to unknown as author, and never promotes the record's status, yet it is the only path for deviations and known gaps; a question's own tier 3 clears the open-question guard on a tier 1 record; re-approval keeps the previous approver's name and date on the single-record path while the batch path overwrites; and approval fields sit outside the hash with manifests never read. Outside practice pulls the other way: NIST separation of duties asks for distinct individuals, and the OWASP LLM Top 10 project asks for human approval of high-impact agent actions. The owner direction holds that approval stays loose because LiteSpec serves one-person teams: an agent's reading counts as the tier 1 second reading, a project whose owners list holds one name needs no second reviewer, the security reviewer stays optional with the security queue as a list to read rather than a gate, single-record approval promotes the record's status, and re-approval writes the new approver and date.

Verified does not mean what it says. Browser-proved scenarios can never be passing, because the builder creates browser proof links with a null pass state while every status reader asks for true, so a use case proved only by Playwright is never verified and a step served only by browser flows is never exercised; Cucumber reports a status per scenario instead. Staleness lives in a git-ignored cache that is empty in every fresh clone, so CI never sees a changed scenario, and locally a rerun of an unchanged test clears it; OpenFastTrace instead marks coverage outdated from the revision of the covered item in the sources. Nothing blocks on an unverified scenario, while SonarQube quality gates evaluate pass or fail mechanically. A draft requirement or decision silences blocking findings on approved use cases. An assertion-free test marks records tested, background use cases verify through any when and then step without the docs saying so, verified names two predicates while tested and exercised overlap, and the book recomputes proof state with its own doubled vocabulary across thousands of lines. Proof state therefore lives in the committed proofs file holding each scenario's hash and its test body's hash at the last passing run, stale when either changes, with browser outcomes read. A finding blocks or reports by its record's own status with an inherited draft shown as a label. Use-case-unverified reports by default, the release flag makes release-readiness items block, and the since flag blocks only on what changed. Four proof words each keep one meaning: verified for a use case, met for a requirement, tested for a passing test with an assertion, exercised for a journey step. Finding codes follow subject-problem with twelve problem words, an unknown code fails the build, and the book renders the model file while judging nothing itself. A refusal code is retired, never renamed; a scenario names the rules it shows in exercises; and a use case may hold a shared background, matching Gherkin, where Rule groups the scenarios of one business rule and Background holds the Given steps shared before each scenario.

The tooling fails open in four places, and marks are matched by name. The manifest pins the analyzers to a release without the traceability rules the stack pages cite, while the analyzer project carries the newer version. A missing frontend reader turns S findings into reports and the build stays green. An unknown layer value silently disables every rule, and a Worker project resolves to no layer at all. The analyzer pin therefore matches the analyzer project with every cited LS code shipped, an unknown layer name is reported, Worker is a layer, and reader-missing blocks when CI is set. Marks take the declared style on frontends, a third syntax on operations files, and name matching on backends, where a handler in one folder marked for another passes, the Blazor reader runs its own narrower regex, and state changes without an event escape every check. Mark style therefore belongs to the language, read by one parser; a backend mark matches its folder; marks and handlers are recognized by symbol; and a state-changing aggregate method needs a mark. The dormant architecture-test library, last released in 2021, gives way to the active ArchUnitNET line.

Design departs from outside practice in the behavior records without saying why. Atomic writes across aggregates contradict the page's own one-step-per-aggregate definition, against Vernon's eventual consistency outside the boundary, so an atomic write across aggregates stays allowed only through an atomicity rule that binds the group, the use case's changes equal that group, and a group above four aggregates reports at tier A as a saga candidate, with best-effort removed. The process aggregate kind carries steps with compensation, exits, a driver, and a cursor, which is a saga under a lifecycle name, so it becomes saga while a record with only a lifecycle is an entity. Event payloads and use-case inputs carry no type or schema while trust claims the fields are fit to act on, against CloudEvents dataschema and AsyncAPI payload schemas, so inputs and payload fields carry type and required, an integration event may name dataschema, and money values are amount with currency. The retention schedule claims GDPR Article 30 order but omits purposes and other categories; Article 30(1) was verified to ask for identity, purposes, data subjects and categories, recipients, transfers, time limits, and security measures, so each data class and retention row states purpose, period is an ISO 8601 duration, and the schedule claims only the time limits of a record of processing, with counsel confirming the wording. Pseudonymise sits as an erasure position, but Recital 26 was verified: pseudonymised data stays personal data while only anonymous information falls outside the principles, so pseudonymize keeps the class personal with its basis and counts in risk-acceptance-missing, and anonymize is added. Entry points cover doors, triggers, and content with auth assigned on two pages, so an entry point is a door from outside only, stating caller authentication, while the use case states who may act. Jobs and reactions share one shape under two names, so one automation record with triggeredBy of event, schedule, or manual replaces both. The verbs issue and publish, the word operator, and the words system, anonymous, and door each carry two or three meanings, so effect verbs become runs, sends, emits, and calls, a manual job is triggeredBy manual, a process driver run by a person is person, the auth kind anonymous becomes none, and the risk value door becomes physical-access.

Three structural decisions close the set. Provision ids come from headings, so a retitle breaks every citation in consumer code; sphinx-needs instead carries explicit stable ids, so the ID line becomes a provision's identity and a heading may change without changing it. Five projects, property tests, model-based tests, and mutation thresholds stand as must-level rules with no tool and no size scaling, against the test pyramid's proportions rather than mandates, so they become Defaults by size, every must-level rule names a real check or says Reviewed for, and one page answers where code goes. One language standard for every layer lives on a new core language page; a journey walks one actor toward one goal with other actors named only at handoffs; each risk value is defined in the glossary; governance is a group of records beside the four layers; and an agent skips the model query only when the change touches no record with risk or an approval.

The review also records what already works: effects take exactly one verb each; erasure is declared once per data class; scenario example tables match Gherkin's Scenario Outline with exactly one result each; all provision references in the analyzers and the agent file resolve; computed statuses come from the builder only; severity is decided in one place with a matrix test; decisions are replaced, never edited; and the .NET and Node pins are current. The review ran no build in the repository and did not examine the largest consumer beyond scratch copies, so its evidence is code-confirmed findings rather than consumer populations; the largest consumer migrates in the last wave.

## Notes on Sources

Eight web sources were read for this record on 2026-10-31: the Gherkin reference including keywords, Rule, and Background; the CloudEvents 1.0.3-wip specification text including required and optional context attributes; GDPR Article 30 including the seven record categories; GDPR Recital 26 including the pseudonymised and anonymous distinction; Fowler's Test Pyramid page of 2012 on proportions across test levels; the sphinx-needs 9.1.0 documentation home confirming explicit need ids; the ArchUnitNET repository confirming an active C# architecture-test library; and the OWASP LLM Top 10 project home for the human-approval context behind the approval discussion. The review's remaining bibliography was carried, not re-read: Vernon 2011, AsyncAPI 3.0.0, Fowler on event-driven architecture, Spec Kit and Kiro, Microsoft Learn architectures, eShop, NetArchTest, the .NET and Node support pages, SonarQube Clean as You Code, OpenFastTrace, the Google flaky-test note, NIST AC-5, Nygard and MADR on decisions, and Snyk ignores. Evans, Richardson on sagas, RFC 9457, ITIL 4, and the Event Storming book were not fetched by the review either, so the saga, dependency, read-model, and emergency-path claims stay marked likely. Code locations and line numbers are the review's, verified against branch v3 at commit c9b2725, and were not re-verified here.
