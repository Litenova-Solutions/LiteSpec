# Lower Layers Practice

The lower areas of LiteSpec are Behavior, Implementation, Proof and Governance. They sit below the Intent records. This research record holds the findings of an outside review of those 4 areas, made on 31 October 2026. The reviewers read each area as a newcomer would, compared it with outside practice and checked each finding in the files. It matters because several findings concern checks that pass when they should fail, or that claim more than they check.

## Findings

The review confirms that exceptions are uneven and too wide. It confirms that proof words overclaim, that tooling fails open, and that some design departs from outside practice without a stated reason. The claims in this record support decisions `D25` to `D46`.

The review used 4 reviewers, and each read one area against branch v3 at commit c9b2725. The Governance reviewer confirmed behavior on scratch builds, and 5 code claims were rechecked in the code on the same day. The review does not repeat the 124 findings that the findings table already verifies. The record gives no confidence for this method claim.

Exceptions are uneven and too wide. A known gap silences a tier G finding on presence alone. A deviation asks for a recorded reviewer, but a deviation holds every finding on its subject, not only the findings its provision governs. So a deviation that cites a precedence provision silenced unrelated findings. Deferrals and the frozen phase relax blocking checks with no reason and no expiry. That conflicts with the scope rule that each departure resolves to a deviation record. The record gives no confidence for this claim.

Expiry uses 4 field names, and a deviation expires silently. A retired subject silences tier G findings. The review proposes one exception record to replace the deviation, the known gap, the deferred provisions and the frozen check phases. The record holds the kind of departure or gap, the provision or finding codes it covers, the subject or paths, the reason, the exit and `expiresOn`. It covers only what it names, and `exception-expired` reports it after its date. The record gives no confidence for this proposal.

Agreement happens at merge. What is in git is decided, and a record present on the main line carries the team's agreement. Outside practice pulls toward separation of duties. NIST asks for distinct individuals, and the OWASP LLM Top 10 project asks for human approval of high-impact agent actions. LiteSpec serves one-person teams, so it keeps no second-reviewer rule. A team that needs separation of duties adds it above the merge. The record gives no confidence for these claims.

Verified does not mean what it says. The builder creates browser proof links with a null pass state, while every proof reader asks for a true pass state. So a scenario proved only by Playwright is never passing. A use case proved only by Playwright is never verified, and a step served only by browser flows is never exercised. Cucumber reports a status for each scenario instead. The record gives no confidence for this claim.

Staleness lives in a git-ignored cache that is empty in every fresh clone. So CI never sees a changed scenario, and locally a rerun of an unchanged test clears the cache. OpenFastTrace marks coverage outdated from the revision of the covered item. Nothing blocks on an unverified scenario, while SonarQube quality gates evaluate pass or fail mechanically. The record gives no confidence for these claims.

An assertion-free test still marks records tested. Background use cases verify through any `when` and `then` step, and the docs do not say so. `verified` names 2 predicates, while `tested` and `exercised` overlap. The book recomputes proof state with its own vocabulary, across thousands of lines. The record gives no confidence for these claims.

The proposed repair keeps proof state in a committed proofs file. The file holds each scenario's hash and its test body's hash at the last passing run. A scenario is stale when either hash changes, and browser outcomes are read. A finding blocks or reports by its tier. `use-case-unverified` reports by default. The release flag makes release-readiness items block, and the since flag blocks only on what changed. The record gives no confidence for this proposal.

The proposed repair also keeps 4 proof words, each with one meaning. `verified` applies to a use case, `met` to a requirement, `tested` to a passing test with an assertion, and `exercised` to a journey step. Finding codes follow the subject and problem pattern, with 15 problem words. An unknown code fails the build, and the book renders the model file without judging anything itself. The record gives no confidence for this proposal.

The review proposes 2 more repairs. A refusal code is retired and never renamed, and a scenario names the rules it shows in exercises. A use case may hold a shared background, as Gherkin allows. In Gherkin, `Rule` groups the scenarios of one business rule, and `Background` holds the `Given` steps shared before each scenario. The record gives no confidence for these proposals.

The tooling fails open in 4 places, and marks are matched by name. The manifest pins the analyzers to a release that lacks the traceability rules the stack pages cite. The analyzer project carries the newer version. A missing frontend reader turns S findings into reports, so the build stays green. An unknown layer value silently disables every rule, and a Worker project resolves to no layer at all. The record gives no confidence for these claims.

The proposed repairs match each failure. The analyzer pin matches the analyzer project, with every cited LS code shipped. An unknown layer name is reported, and Worker becomes a layer. Reader-missing blocks when CI is set. The record gives no confidence for these repairs.

Marks fail in 3 ways. Marks take the declared style on frontends, a third syntax on operations files and name matching on backends. A handler in one folder that is marked for another folder passes. The Blazor reader runs its own narrower regex, and a state change without an event escapes every check. The proposed repairs are that mark style belongs to the language and one parser reads it, that a backend mark matches its folder, that marks and handlers are recognized by symbol, and that a state-changing aggregate method needs a mark. The record gives no confidence for these claims.

The dormant architecture-test library was last released in 2021. The review proposes that the active ArchUnitNET line replace it. The record gives no confidence for this proposal.

Design departs from outside practice in the behavior records without saying why. Atomic writes across aggregates contradict the page's own one-step-per-aggregate definition, and Vernon's eventual consistency applies outside the boundary. So an atomic write across aggregates stays allowed only through an atomicity rule that binds the group. Coordination across aggregates belongs to the workflow-engine extension. The record gives no confidence for these claims.

Other design departures also have repairs. Event payloads and use-case inputs carry no type or schema, although the trust rule says the fields are fit to act on. Following CloudEvents `dataschema` and AsyncAPI payload schemas, inputs and payload fields carry `type` and `required`. An integration event may name `dataschema`, and money values are `amount` with `currency`. The record gives no confidence for these repairs.

The retention schedule claims GDPR Article 30 order but omits purposes and other categories. Article 30(1) asks for identity, purposes, data subjects and categories, recipients, transfers, time limits and security measures. So each data class and retention row states its purpose, and each period is an ISO 8601 duration. The schedule claims only the time limits of a record of processing, and counsel confirms the wording. The record gives no confidence for this repair.

Pseudonymise sits as an erasure position. Recital 26 says pseudonymised data stays personal data, and only anonymous information falls outside the principles. So `pseudonymize` keeps the class personal with its basis, and `anonymize` is added. The record gives no confidence for this repair.

Entry points cover doors, triggers and content, and 2 pages assign auth to them. So an entry point is a door from outside only, and it states caller authentication. The use case states who may act. Reactions answer events, while jobs run on schedules or by hand. So the review returns reaction and job as 2 record types. A reaction names the events it answers in `triggeredBy`, with its skip condition in `skippedWhen`. A job names its kind as scheduled, one-off or manual, with its schedule, selection, lease, cursor and guard. The record gives no confidence for these repairs.

Several words carry 2 or 3 meanings. The verbs `issue` and `publish`, the word `operator`, and the words `system`, `anonymous` and `door` each do so. The review makes effect verbs into `runs`, `sends`, `emits` and `calls`. A manual job carries `kind: manual`. A coordinator step that a person runs is `person`, and the auth kind `anonymous` becomes `none`. The record gives no confidence for these repairs.

The set closes with 3 structural decisions. Provision ids come from headings, so a retitle breaks every citation in consumer code. The review proposes that sphinx-needs' explicit stable ids replace this. The ID line then becomes the provision's identity, and a heading may change without changing it. The record gives no confidence for this proposal.

Projects, property tests, model-based tests and mutation thresholds stand as must-level rules, with no tool and no size scaling. The review compares them with the test pyramid's proportions, which are guidance and not mandates. So they become Defaults by size. Every must-level rule names a real check or says `Reviewed for`, and one page answers where code goes. The record gives no confidence for this proposal.

The review also proposes one language standard for every layer, on a new core language page. A journey walks one actor toward one goal, and other actors appear only at handoffs. Governance is a group of records beside the 4 layers. The record gives no confidence for these proposals.

The review records what already works. Effects take exactly one verb each. Erasure is declared once per data class. Scenario example tables match Gherkin's Scenario Outline, with exactly one result each. All provision references in the analyzers and the agent file resolve. Computed proof comes from the builder only. Severity is decided in one place and tested with a matrix. The .NET and Node pins are current. The record gives no confidence for these findings.

The review ran no build in the repository. It did not examine the largest consumer beyond scratch copies. Its evidence is therefore findings confirmed in code, not consumer populations. The largest consumer migrates in the last wave. The record gives no confidence for these limits.

## Source Notes

The record read 8 web sources on 31 October 2026. They are the Gherkin reference, the CloudEvents 1.0.3-wip specification, GDPR Article 30, GDPR Recital 26, Martin Fowler's Test Pyramid page of 2012, the sphinx-needs 9.1.0 documentation home, the ArchUnitNET repository and the OWASP LLM Top 10 project home. The record names the sources that it read in the repository on 31 October 2026. They are the core Behavior page (`docs/core/behavior.md`), the core model page (`docs/core/model.md`), the core release page (`docs/core/release.md`), the core access page (`docs/core/access.md`), the record schema (`schemas/record.schema.yaml`) and the manifest (`litespec.manifest.yaml`). The record does not say which claim each source supports.

The review carried the rest of its bibliography without reading it again. The carried sources include Vernon 2011, AsyncAPI 3.0.0, Fowler on event-driven architecture, Spec Kit and Kiro, Microsoft Learn architectures, eShop, NetArchTest, the .NET and Node support pages, SonarQube Clean as You Code, OpenFastTrace, the Google flaky-test note, NIST AC-5, Nygard and MADR on decisions, and Snyk ignores. Evans, Richardson on sagas, RFC 9457, ITIL 4 and the Event Storming book were not fetched. So the coordination, dependency, read-model and emergency-path claims are marked likely.

The code locations and line numbers are the review's own. The review verified them against branch v3 at commit c9b2725. This record did not verify them again.
