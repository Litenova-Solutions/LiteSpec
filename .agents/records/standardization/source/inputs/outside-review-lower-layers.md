# Outside Review: LiteSpec Behavior, Implementation, Proof and Governance

Reviewed on 2026-10-31 against LiteSpec branch `v3`, commit `c9b2725`, read in the worktree `D:/Personal-Projects/LiteSpec/.claude/worktrees/standardization`. Four reviewers each read one area as a newcomer, compared it with outside practice, and checked each finding in the files. The Governance reviewer also confirmed behavior on scratch builds of `c9b2725` (`C:/Users/a.shafie/lsx-scratch/exp` to `exp5`); those findings say "run". Five critical claims were rechecked in code on the same day: `ModelBuilder.cs:615`, `Suppression.cs:38-41` and `:81-87`, `litespec.manifest.json:324`, and `BatchApprover.cs:352-368`.

The 124 findings already in `.agents/plans/Standardization/inputs/findings-verified.tsv` are not repeated. Paths are relative to the repository root. Ids: `BE` Behavior, `IM` Implementation, `PR` Proof, `GO` Governance.

## The Six Themes

1. **Exceptions are uneven and too wide.** A draft known gap nobody approved silences a tier G finding (GO5). A deviation silences every finding on its subject, not only its provision (GO7). The frozen phase and deferrals switch checks off with no owner or expiry (GO13). One exception record with approval, scope, tier and expiry would close all of them.
2. **Approval can be satisfied without a second person.** Any string passes as a second reviewer (GO1), an agent counts as the tier 1 second reading although the page forbids agents to gate (GO2), the single-record `approve` skips every guard (GO3), a question's own `tier: 3` lets a money figure auto-approve (GO4), re-approval keeps the old approver's name (GO6), and approval fields sit outside the hash (GO11).
3. **"Verified" does not mean what it says.** Browser proofs can never pass (PR1), staleness lives in a git-ignored cache that is empty in CI (PR2), nothing blocks on an unverified scenario (PR3), a draft requirement silences blocking findings on approved use cases (PR4), and an assertion-free test marks a record tested (PR7).
4. **The tooling fails open.** A missing frontend reader turns S findings into reports (IM2), an unknown layer name disables every analyzer rule (IM8), a Worker project is skipped (IM7), and the manifest pins an analyzer release without the traceability rules (IM1).
5. **Words carry several meanings.** Finding codes use synonyms (PR18), effect verbs collide with use-case verbs (BE8), and `operator`, `system`, `anonymous` and `door` each mean two or three things (BE9, BE10, GO20).
6. **Design departs from outside practice without saying why.** Atomic writes across aggregates (BE1), a saga called a process (BE2), untyped contracts (BE3), an incomplete GDPR Article 30 claim (BE4, BE5), a fixed five-project backend (IM12), mandatory test techniques for every size (IM11), and provision ids derived from headings (IM10).

## Findings

Severity follows the review skill: critical (wrong results without anyone knowing, or a broken safeguard), major (documented behavior does not happen, or work is blocked), minor (inconsistency or maintenance cost), note.

### Critical

**GO1. A tier 1 second reading is satisfied by any string.**
Confirmed (run). `BatchApprover.cs:352-368` checks only that `--second-reviewer` differs from `--by`, and for `agent` that a report file exists; `release.md:127-137` says tier 1 "MUST NOT ship on self review alone". A batch with `--second-reviewer Nobody-At-All` approved. Peer: NIST SP 800-53 Rev. 5 AC-5, separation of duties among different individuals. Fix: the second reviewer is a distinct owner-list person, bound to a code-owner handle; the review report names the batch it read.

**GO2. The agent may not gate a release, yet its reading satisfies the tier 1 gate.**
Confirmed (run). `release.md:397-405` says an agent review is advice; `release.md:135-137` and `BatchApprover.cs:364-367` accept `agent` as the second reading, and `agent.md:50` has the agent run `approve --batch`. Peer: OWASP LLM06 Excessive Agency (2025) asks for human approval of high-impact actions. Fix: remove `agent` as a second reviewer; record agent review as advice beside a human second reading.

**GO3. The single-record `approve` bypasses every batch guard, and is the only path for deviations and known gaps.**
Confirmed (run). `ModelCommand.cs:355` and `Approver.cs:115-125` check no owner, tier, question or manifest; `--by` falls back to `"unknown"` (`Approver.cs:117`). A draft deviation approved this way received approval fields and kept `status: draft`, with no finding. Fix: one approval command through the batch guards; deviations and known gaps enter the review queue; `approve` refuses a record whose status it does not promote.

**GO4. A question's own `tier: 3` clears the open-question guard on a tier 1 record.**
Confirmed (run). `ReviewQueue.cs:408-450` reads the question's `tier` before the record's `risk`. On a `risk: ["money"]` use case a tier 3 question with a recommendation auto-approved past its date. Plan decision D16 removes the field; this finding adds that price, fee, rate, term and retention questions are never auto-approvable. Fix: derive the tier from the record's risk and the figure's class.

**GO5. A draft known gap nobody approved silences a tier G finding.** (Also found by the Proof reviewer.)
Confirmed (run, and code). `Suppression.cs:81-87` requires only `status != "retired"`; deviations require `approved` (`Suppression.cs:38-41`). A draft gap turned `approval-reopened` into "known". Fix: known gaps require approval data like deviations; cap `endsBy`; report a draft gap as `known-gap-unapproved`.

**IM1. The manifest pins the analyzers to a release without the traceability rules the stack pages cite.**
Confirmed. `litespec.manifest.json:324` pins `LiteSpec.Analyzers` 2.0.0; the csproj is 3.0.0 (`apps/analyzers/src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj:15`); LS5001 to LS5012 are listed as unshipped. A consumer following the manifest runs none of them; one following the docs gets `dependency-version-drift`. Fix: pin 3.0.0; `litespec-maintain validate` compares the pin with the csproj and checks every cited LS code exists in the pinned release.

**PR1. Browser-proved scenarios can never be passing.**
Confirmed. `ModelBuilder.cs:615` creates browser `ProvesLink` entries with a null pass state, and every status reader requires `true` (`:630`, `:1305`, `:1336`, `:1363`). A use case proved only by Playwright is never verified, and a step served only by browser flows is never exercised. Peer: Cucumber reports a status per scenario. Fix: read browser outcomes by test title from the run; add a builder test.

**PR2. Staleness is recorded in a git-ignored cache, so CI never sees a changed scenario.**
Confirmed (code). `ProofLedger.cs:35-41`, `:95-106`; `.model/` is ignored (`.gitignore:17-18`). In a fresh clone the first passing run records the current hash; locally a rerun of an unchanged test clears staleness (`StaleProofTests.cs:63` pins this). Peer: OpenFastTrace marks coverage outdated from the revision of the covered item in the sources. Fix: store the proved hash in a tracked file or the scenario's approval data, and include the test body's hash.

### Major

**BE1. Atomic writes across aggregates contradict the page's own aggregate definition.** `behavior.md:34` (one step per aggregate) against `:118`, `:763-769` and `:315`, which allow `consistency: atomic` across several aggregates; `many-aggregates` is only tier A. Peer: Vernon, Effective Aggregate Design (2011), eventual consistency outside the boundary. Fix: `atomic` only for one aggregate; otherwise `compensated` or `per-record`, or a deviation.

**BE2. The `process` aggregate kind is a saga under a lifecycle name.** `behavior.md:57-66`, `:453-476`: steps with compensation, exits, a driver and a cursor. Likely (peer not fetched). Fix: rename to `saga` or split orchestration into its own record; keep lifecycles on `entity`.

**BE3. Event payloads and use-case inputs have no type or schema.** `behavior.md:103`, `:116`; `integrationEventPayload` and `useCaseInput` in `record.schema.json` carry no type, while `trust: payload` claims the fields are fit to act on. Peer: CloudEvents `dataschema` and `datacontenttype`; AsyncAPI 3.0.0 (2023-12-05) message payload schemas. Fix: a schema reference per payload and input; `dataschema` on integration events; generate AsyncAPI or OpenAPI fragments from records.

**BE4. The retention schedule claims GDPR Article 30 order but omits purposes and other categories.** `behavior.md:288-296`. Article 30(1) lists purposes, data subjects and categories, recipients, transfers, time limits and security measures. Fix: add the columns, or narrow the claim to the Article 30(1)(f) time limits and link the rest. Legal reading for counsel.

**BE5. `pseudonymise` is listed as an erasure position, but pseudonymised data stays personal data.** `behavior.md:49`, `:421-431`. Peer: GDPR Recital 26. Fix: a pseudonymised class stays `personal` with a basis and counts in `risk-without-acceptance`; add `anonymize` as a separate position with a stated test (American spelling per plan decision D14).

**BE6. "Entry point" covers doors, triggers and content, and two pages assign auth differently.** `behavior.md:258-266`, `:1233-1239` require kind and auth; `access.md:45-51`, `:291-299` require kind only and list `reaction` and `job`, which have no caller. Peer: AsyncAPI separates channels from operations. Fix: entry points are doors only; triggers move to the automation record; auth lives on the use case, or the door is a ceiling.

**IM2. A missing frontend reader fails open, so the CI model gate passes.** `FrontendReadBuilder.cs:103-108`, `Severity.cs:69`, `ModelCommand.cs:232`; `model.md:382`. Without Node every frontend S finding reports and the build stays green. Fix: block on `reader-unavailable` for a selected frontend in CI, or require a dated exception.

**IM3. Frontend marks take the declared style, so the one-mark-style rule cannot fire for frontends.** `FrontendReadBuilder.cs:133`, `:154`; `Checker.Marks.cs:53-57`; `web/structure.md:63`. Fix: a `typed` mark style in the marks table; record the form actually seen.

**IM4. Operations marks use a third syntax that ignores the declared style.** `OperationsMarkReader.cs:80` accepts only `<follows>...</follows>` in `#` comments; `model.md:306` says one style. Fix: state the operations form as a named exception or accept the declared style; cover it with `mark-style-mixed`.

**IM5. Backend marks are never compared with their namespace; frontend marks are.** `Checker.Marks.cs:113`, `FrontendModuleMirror.cs:44-47`; the mirror is a MUST at `backend/architecture.md:101`. A handler in the `SubmitExpense` folder marked `cancel-expense` passes. Fix: apply the mirror check to handlers, endpoints and aggregates.

**IM6. State changes without an event are invisible to the Implement check.** `model.md:302`; `docs/analyzers/rules/ls5001.md:15`. A mutation raising no event escapes `write-undeclared`, `behavior-unrecorded` and LS5001. Fix: recognize public aggregate-root methods that assign state, or record the gap.

**IM7. Background work has two homes, and the analyzers skip the Worker project.** `backend/dotnet/architecture.md:51` (jobs in Infrastructure) against `backend/architecture.md:204-208` (a Worker project); `LayerResolver.cs:40` returns `None` for `*.Worker`. Fix: a Worker layer in the resolver and the layer table; one home for job classes.

**IM8. An unknown layer value silently disables every rule.** `LayerResolver.cs:22`, `:31-40`: `LiteSpecLayer=Domian` turns off LS1xxx without a diagnostic. Fix: report an unrecognized value.

**IM9. Marks and handler kinds are matched by name, not by symbol.** `MarkReader.cs:18-21`, `:221-226`, `:276`; `ls5001.md:15` (handlers by interface name, jobs by `*Job` or `*Worker`). A domain attribute named `Follows` becomes a mark; a handler on another mediator is never checked. Fix: resolve through `SemanticModel` or require the generated attribute's full name.

**IM10. Provision ids come from headings, so a retitle breaks every citation in consumer code.** `authoring.md:154-160`, `:556`, `:614`; `maintaining.md:23` forbids aliases. Peer: sphinx-needs and Doorstop allow explicit stable ids. Fix: an explicit id that a heading edit cannot change, validated for uniqueness and immutability.

**IM11. Test techniques are MUSTs with no tool and no size scaling.** `backend/testing.md:31`, `:149-153` (zero surviving mutants), `:161` (property tests for every calculation), `:173` (model-based tests for every aggregate); none checked. Peer: Fowler, TestPyramid (2012, updated 2017), proportions, not mandates. Fix: Default with Replacement; equivalent-mutant record; placement guidance as Reviewed for.

**IM12. "Five projects" is stated as the whole backend and conflicts with hosts and Worker.** `backend/dotnet/architecture.md:5`, `:21-23`. Peer: Microsoft Learn common web application architectures (updated 2026-07-08) treats Clean Architecture as a dependency rule; dotnet/eShop (2026-10-15) adds an AppHost. Fix: the layer set as Default with Replacement; one table of allowed extra projects.

**PR3. Nothing blocks on an unverified scenario.** `release.md:389` (MUST show verified scenarios), `:395` (Reviewed for); no finding code covers verification; `ModelCommand.cs:232`. Peer: SonarQube quality gates evaluate pass or fail mechanically. Fix: tier G `use-case-unverified` on approved use cases with a dated exemption, or a release command that exits non-zero.

**PR4. A draft requirement or decision silences blocking findings on approved use cases.** `Authority.cs:46-49`, `Severity.cs:59-61` against `model.md:386`. Fix: block on the record's own status; show inherited draft as a label.

**PR6. An assertion-free test marks a requirement, decision, external system or integration event as tested.** `ModelBuilder.cs:780-783`, `:1363`. Fix: require an assertion or a measure result, or rename the status to a linkage word.

**PR7. Background use cases are verified by any when step and any then step, and the docs do not say so.** `ModelBuilder.cs:810`, `:1221-1227`, `:733-742`; no mention in `model.md` or `tools/model.md`. Fix: document the indirect proof; require the then step to resolve and assert.

**PR8. "Verified" names two predicates, and "tested" and "exercised" overlap.** `model.md:331-336`; `ModelBuilder.cs:1385-1410`. Fix: `verified` for use cases, `met` for requirements; one table of the four proof words and their inputs.

**PR9. No baseline or new-code gate, so adoption pushes toward global silencing.** `Suppression.cs`, `ModelCommand.cs:180-186`. Peer: SonarQube applies gate conditions to new code by default. Fix: findings on records and code unchanged since a base ref report without blocking, or a baseline file with a ceiling per code.

**PR10. The book recomputes proof state with its own vocabulary.** `BookModel.cs:700-721` and `BookEnrich.cs:626-646` name each state twice (`not-run` and `norun`, `proved-passing` and `passing`) and turn `proved-indirect` into `passing`. Against `model.md:924`. Fix: render each scenario's proof state from `model.json`.

**GO6. Re-approval keeps the previous approver's name and date.** Confirmed (run). `Approver.cs:115-125` writes `approvedBy` and `approvedOn` only when empty; the batch path overwrites (`:226-228`). Fix: overwrite on every stamp and keep history in the manifest.

**GO7. An approved deviation holds every finding on its subject, not only its provision.** Confirmed (run). `Suppression.cs:45-61`; `model.md:611`. A deviation citing a precedence provision silenced `approval-reopened`. Also stated by the Proof reviewer as "tier G is not always blocking". Fix: scope a deviation to the codes its provision governs; state the remaining exceptions in the tier table.

**GO8. Deviations carry no risk tier, so waiving an access or money provision needs one approver and no security review.** `templates/consumer/deviation.md`; `ReviewQueue.cs:240-258`; the model example waives target authorization (`model.md:600-612`). Fix: inherit the tier from the waived provision; send access and money deviations to the security queue.

**GO9. The security reviewer is never required, validated or enforced.** `ConsumerConfig.cs:373-397`, `BookCommand.cs:526`, `:536`; `release.md:257`, `scope.md:164` and `project-file.md:117` describe checks that do not exist; `project-file.md:111` marks it optional. Fix: required, checked against owners.

**GO10. The security queue has no recorded decision, and the release gate omits it.** `release.md:33`, `:37-39`, `:385-395`; `Differ.cs:121`, `:1589` recompute it from a diff. Fix: store each queue item with decision, decider, date and commit; add it to the gate.

**GO11. Approval fields sit outside the hash, and approval manifests are never read.** `Checker.Graph.cs:15-22`; `BatchApprover.cs:523-528`. A hand edit to `approvedBy` or `status` reopens nothing. Fix: hash the approval fields; verify each approved record against its manifest.

**GO12. "A named person" accepts team names.** `model.md:491`; `BatchApprover.cs:334-349`; owner `claimwick-team` at `model.md:600`. The Proof reviewer adds that `--by` is a typed name and the book's `review.json` carries no identity. Fix: separate owner (may be a team) from approver (one person with a code-owner handle).

**GO13. Deferrals and the frozen phase relax blocking checks with no owner, approval or expiry.** `litespec-project.schema.json:129-135`, `:530`; `ext/stage.md:43-53`; `model.md:840-875`; against `scope.md:146-148` ("each departure resolves to a deviation record"). Also found by the Proof reviewer. Peer: SonarQube bounds the new-code period. Fix: each deferral and frozen phase is an exception record with an end date.

**GO14. There is no emergency path, so the bypasses become one.** `release.md:127-137` applies to every tier 1 change. Likely (ITIL not fetched). Fix: an emergency change with a named approver, a retrospective review within a fixed number of days, and an expiry.

### Minor

**BE7. Jobs and reactions share one shape under two names** (`behavior.md:195-225`). Fix: one automation record with `triggeredBy: event | schedule | operator`.

**BE8. The verbs `issue` and `publish` each mean two things.** Effect `issues` runs a use case; use-case verb `issue` creates a credential; effect `publishes` emits an event; verb `publish` makes visible (`behavior.md:119`, `:154`, `:160`). Fix: effect verbs `runs` and `emits`.

**BE9. `operator` names three things**: a setting setter, a job kind, a process driver (`behavior.md:249`, `:217`, `:66`). Fix: job kind `manual`, driver `person`.

**BE10. `system` and `anonymous` each appear as actor kind, auth kind and grant** (`access.md:15`, `:25-27`). Fix: give each word one job.

**BE11. Rule kinds mix three axes, and `atomicity` duplicates consistency** (`behavior.md:73-83`, `:118`). Fix: drop `atomicity`; a model default is a rule, a tunable default is a setting.

**BE12. Transaction semantics have three overlapping knobs, and `best-effort` is undefined** (`behavior.md:118-119`, `:787-791`). Fix: one table of modes and permitted timings.

**BE13. `parts` names child entities that the page says never appear in records** (`behavior.md:42`, `:1343-1347`). Fix: allow a names-only list explicitly, or move the names to glossary terms.

**BE14. Refusal codes are a public contract with no retirement rule** (`behavior.md:887-897`). Fix: a code is retired, never renamed, with its replacement named; a retired code is never reused.

**BE15. Rules have no scenario link, and scenarios have no shared context** (`ruleEntry`; `behavior.md:965-987`). Peer: Gherkin `Rule` and `Background` (page updated 2026-10-27). Fix: `proves` on rules; a shared `given` on a use case.

**BE16. `dependsOn` is untyped.** Peer: DDD context-mapping patterns. Likely. Fix: direction and relationship per entry.

**BE17. A lagging query is never called a read model** (`behavior.md:125-134`). Likely. Fix: name it once.

**IM13. The Blazor mark reader is a separate regex that accepts fewer verbs** (`DotnetScreenReader.cs:714-718`). Fix: route through `MarkReader`.

**IM14. A `follows` mark is accepted for any provision that resolves** (`MarkRules.cs:141-160`). Fix: the target names the shape it requires, or review judges relevance.

**IM15. The technical-class kind is narrower in code than in the docs** (`KindReader.cs:604-607` against `model.md:298`). Fix: state the exclusions or remove them.

**IM16. Analyzer README counts are stale** (`apps/analyzers/README.md:61`, `:64`). Fix: generate or remove.

**IM17. Orphaned and misidentified pins, and a dormant architecture-test library.** `preact` and `@preact/preset-vite` are referenced by nothing; `cn` is a Tailwind class merger, not the shadcn helper; `NetArchTest.Rules` last released 2021-05-23 (`litespec.manifest.json:346`, `:375`, `:406`, `:418`). Peer: ArchUnitNET 0.13.5 (2026-10-26). Fix: remove orphans, fix `cn`, replace or drop NetArchTest.

**IM18. 548 Statements in backend, frontend and workspace pages have 105 Checked by lines**, and `dotnet/config.md:43` lacks one although the check exists (`Checker.Dependencies.cs:164-180`). Fix: cite existing checks; mark the rest Reviewed for.

**IM19. Some Checked by lines name text greps, missing scripts or missing files** (`web/rendering.md:158`, `react/components.md:38`, `web/components.md:73`, `:97`). Fix: name real checks or ship the files.

**IM20. "Qualified" has four parts on the page and one real input** (`model.md:382`; `NodeFrontendReader.cs:14-15`). Fix: one sentence: the reader ran and its handshake matched.

**IM21. The decision type has two homes, and the Implementation folder has no landing page** (`model.md:82`, `:92`; `docs/README.md`). Fix: state the second home; add an Implementation page or say the folder exists only in consumers.

**IM22. The Android stack bans .NET MAUI by MUST, and AGENTS.md omits the Android stack.** Likely. Fix: a Default with a reason; list the stack.

**PR11. A retired subject silences tier G findings, and `Finding.Blocks` duplicates the blocking rule with no caller** (`Severity.cs:47-49`; `Finding.cs:167`). Fix: delete `Blocks`; state the exception.

**PR12. An unknown finding code defaults to tier G** (`Finding.cs:164`). Fix: a test that every emitted code is in the table; an unknown code fails the build.

**PR13. `known-gap-expired` fires on the date, not on the check still being red** (`Checker.cs:1024-1031` against `tools/model.md:102`). Fix: decide the intent and make the docs match.

**PR14. One code prints as G or A depending on the record** (`Checker.Graph.cs:116-121`; `Checker.Results.cs:45`). Fix: split the codes by grade, or state the grading rule in the table row.

**PR15. No flaky-test concept** (`JUnitRunReader.cs:60-72`). Peer: Google Testing Blog (2016-05-27). Fix: derive a flaky state from recent runs, tier A.

**PR16. Finding codes use synonyms for one problem.** `unknown` and `unresolved`, `drift` and `mismatch`, `without` and `missing`; 38 codes end in ten different `un-` words. Fix: `<subject>-<problem>` with a closed list of problems: `missing`, `unknown`, `undeclared`, `unrecorded`, `unimplemented`, `unreached`, `drift`, `invalid`, `duplicate`, `clash`, `expired`, `reopened`. About 40 renames.

**PR17. The diff's change reasons are a second code vocabulary with no tier** (`Differ.cs:151`, `:189`, `:1728-1732`). Fix: one namespace, or document both.

**PR18. Browser success proofs accept any string literal inside `expect`** (`fe-reader/src/tests.ts:165-172`, `:192`; `ModelBuilder.cs:665`). Fix: match the result's sentence or the Then text.

**PR19. The book's judgement logic is 5,888 lines** (`apps/cli/src/LiteSpec.Cli/Book/`). Fix: shrink the book to a renderer of `model.json`.

**GO15. The tier 3 sample is computed and printed, and nothing gates on it** (`BatchApprover.cs:107-110`, `:503-510`; `release.md:139-149`). Fix: record the sampled reader in the manifest and refuse the batch without it.

**GO16. Governance records have no settled layer** (`model.md:9`, `:20`, `:239`; glossary Deviation and Known Gap). Fix: one statement on all pages.

**GO17. Four expiry fields, and a deviation expires silently** (`reviewBy`, `endsBy`, `until`, `decideBy`; `Checker.cs:1024-1031`). Peer: Snyk ignores with an expiry that returns the issue. Fix: one `expiresOn` and an expiry finding for every exception.

**GO18. The agent may skip the model query on structure, not risk** (`agent.md:71`). Fix: the query is mandatory for any change touching a record with risk or approval state.

**GO19. "Task authorizer" is undefined, and side-effect approvals leave no record** (`agent.md:37`, `:131-139`). Fix: define it; record each side-effect approval.

**GO20. `door` is an undefined risk value and also means an entry point** (`model.md:179`, `:346`, `:493`; `behavior.md:631`). Fix: define the risk classes; rename `door`.

### Notes

**BE18.** Spec-driven peers (GitHub Spec Kit, Kiro, 2026) keep contracts in code; LiteSpec's Behavior records restate contracts the stack generates, so the records should drive generation (BE3) rather than repeat it.

**IM23.** The mark rules sit in five provisions on three pages (`system.md:81`, `:93`; `model.md:298-306`, `:856-874`; `backend/dotnet/stack.md`).

**IM24.** "Where does this code go" crosses at least six pages; a single "Write Code for a Use Case" page with element, folder, mark and check would answer it.

**GO21.** Two examples break the rules they illustrate: the known-gap example at `model.md:575-586` has `status: approved` without approval data, and `release.md:541-549` names a different expiry finding than the code.

## Simplification Proposal

| Change | Removes | Findings |
|:---|:---|:---|
| One exception record (provision or check, subject, codes, reason, owner, approver, tier, `expiresOn`) replacing known gap, deviation, deferral and frozen phase | Uneven approval, wide scope, silent expiry, unbounded switches | GO5, GO7, GO8, GO13, GO17, PR11 |
| One approval path through the batch guards, with a human second reviewer, approval fields in the hash, and manifests verified by `model check` | Bypasses and unattributed approvals | GO1, GO2, GO3, GO6, GO11, GO12, GO15 |
| Question tier derived from risk and figure class | Self-declared tiers | GO4 |
| Proof state in tracked data, browser outcomes read, blocking on the record's own status, a tier G unverified finding | "Verified" that is not | PR1, PR2, PR3, PR4, PR6, PR7, PR8 |
| Fail closed: unavailable reader, unknown layer, unknown code | Silent green gates | IM2, IM7, IM8, PR12 |
| One finding-code grammar `<subject>-<problem>` | Synonym codes | PR16, PR17 |
| Automation record with `triggeredBy`; entry points as doors only; effect verbs `runs` and `emits` | Duplicate shapes and colliding verbs | BE6, BE7, BE8 |
| `atomic` for one aggregate; `saga` for orchestration; typed payloads and inputs | Design contradictions and untyped contracts | BE1, BE2, BE3 |
| Five projects, test techniques and Android framework as Defaults with Replacements | MUSTs that do not fit every size | IM11, IM12, IM22 |
| Stable explicit provision ids | Broken citations on retitle | IM10 |

## Scope

Examined: `docs/core/behavior.md`, `access.md`, `model.md`, `layers.md`, `system.md`, `release.md`, `agent.md`, `scope.md`, `principles.md`, `maintaining.md`, `authoring.md`; `docs/backend/`, `docs/frontend/` (top level and the React stack), `docs/workspace/`, `docs/quality/`, `docs/analyzers/`, `docs/tools/`; the schemas, templates and manifest; `apps/cli`, `apps/model`, `apps/analyzers`, `apps/cli/fe-reader`. Not examined: the Svelte, Blazor and Android stack pages in depth, `apps/site`, and NightHall. No build or test ran in the repository; the Governance scratch runs used `git archive` copies.

## What Already Works

- Reactions are named with the industry alias `policy` in the glossary; effects take exactly one verb each, enforced by the schema.
- Erasure is declared once per data class, with per-field exceptions; refusals stay transport-neutral, and HTTP mapping lives in the backend.
- Scenario `examples` tables match Gherkin's Scenario Outline; each scenario ends in exactly one result.
- All 35 provision references in the analyzers and all 62 in AGENTS.md resolve; the mark-kind table lives in one place, `MarkRules.cs`, and matches `model.md`.
- Computed statuses are produced by the builder only; failing outcomes win over passing ones for duplicate test names; severity is decided in one place with a matrix test; all 123 finding codes are emitted and documented with matching tiers.
- Decisions are replaced, never edited; batch approval holds blocked records by name and writes a manifest; transcription mode has a named end decision.
- .NET 10 (LTS to November 2028) and Node 24 (LTS) pins are current.

## Sources

| Source | Date | Why used |
|:---|:---|:---|
| Vernon, Effective Aggregate Design, dddcommunity.org | 2011-10-01 | Aggregate transaction boundaries (BE1) |
| GDPR Art. 30(1), gdpr-info.eu; Recital 26, gdpr-text.com | Regulation 2016 | Records of processing; pseudonymisation (BE4, BE5) |
| CloudEvents specification, github.com/cloudevents/spec | 1.0.3-wip | `dataschema` (BE3) |
| AsyncAPI 3.0.0 release | 2023-12-05 | Channels, operations, payload schemas (BE3, BE6) |
| Cucumber Gherkin reference | Updated 2026-10-27 | Rule, Background, Scenario Outline (BE15) |
| Fowler, The Many Meanings of Event-Driven Architecture | 2017-02-07 | Notification versus state transfer (BE3) |
| GitHub Spec Kit; Kiro specs documentation | 2026; 2026-10-15 | Spec-driven peers (BE18) |
| Microsoft Learn, Common web application architectures | Updated 2026-07-08 | Clean Architecture as a dependency rule (IM12) |
| dotnet/eShop | 2026-10-15 | Current .NET reference application (IM12) |
| Fowler, TestPyramid | 2012, updated 2017-11-15 | Test proportions (IM11) |
| sphinx-needs 9.1.0; Doorstop | Undated | Explicit stable ids (IM10) |
| NetArchTest; ArchUnitNET 0.13.5 | 2021-05-23; 2026-10-26 | Dormant versus active architecture-test libraries (IM17) |
| Microsoft .NET support policy; Node.js release index | 2026-05-15; 2026-10-14 | Version currency |
| SonarQube Clean as You Code | Undated, fetched 2026-10-31 | Quality gates, new-code gating (PR3, PR9, GO13) |
| OpenFastTrace design document | Undated, snippet | Revision-based outdated coverage (PR2) |
| Google Testing Blog, flaky tests | 2016-05-27 | Flaky-test handling (PR15) |
| NIST SP 800-53 Rev. 5 AC-5 (csf.tools mirror) | Rev. 5.2.0 | Separation of duties (GO1) |
| OWASP Top 10 for LLM Applications, LLM06 | 2025 | Human approval of high-impact agent actions (GO2) |
| Nygard, Documenting Architecture Decisions; MADR 4.0.0 | 2011-11-15; 2024-09-17 | Decision lifecycle |
| Snyk, ignore issues | Undated | Exceptions with expiry (GO17) |

Not fetched: Evans, Richardson on sagas, RFC 9457, ITIL 4, the Event Storming book. BE2, BE16, BE17 and GO14 rest on them and are marked likely.

## Local Rules Met

- `litespec/core/maintaining#replace-every-citation-on-a-rename` (no aliases): conflicts with IM10's proposal to keep ids stable across retitles.
- `litespec/core/model#name-each-field-for-one-concept`: BE8, BE9, BE10, PR16 apply it to verbs and codes.
- `litespec/core/scope` "each departure resolves to a deviation record": GO13 shows the deferral and frozen switches break it.
- AGENTS.md "never name a consumer project": this report names none.

## Open Questions

1. Is a multi-aggregate atomic write an intended exception (BE1)?
2. Should `reader-unavailable` block in CI (IM2)?
3. Does a team count as "a named person" for tier 1 (GO12)?
4. Should a deviation inherit the tier of the provision it waives (GO8)?
5. Is `verified` meant to block a release or only to be read (PR3)?
6. Should a rerun of an unchanged test clear staleness (PR2)?
7. Can provision ids become explicit without a compatibility period, given the no-alias rule (IM10)?
