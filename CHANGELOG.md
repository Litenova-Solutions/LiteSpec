# Changelog

## v4.0.0

LiteSpec 4.0.0 is a standard for specification-driven development of one bounded-context business application by people and coding agents. It changes what LiteSpec is: a strict specification model that a tool checks, beside graded engineering provisions that agents retrieve by situation, with git as the decision record and no approval or review workflow. It replaces 3.0.0 with no compatibility layer, and a consumer records `4.0.0` in `reviewedLiteSpecVersion`. The release takes effect as the `v4.0.0` tag.

### One Folder per Consumer

- Every LiteSpec file in a consumer lives under `.litespec/` at the repository root: `.litespec/project.json` (was `litespec.project.json`), `.litespec/glossary.yaml`, the record layers `.litespec/intent/`, `.litespec/behavior/`, `.litespec/implementation/`, `.litespec/proof/` and `.litespec/governance/`, and the generated `.litespec/model/` and `.litespec/book/`, which `.litespec/.gitignore` ignores.
- Every command finds the consumer by walking up to the first folder holding `.litespec/project.json`. Paths in the project file stay relative to the repository root.

### Writing

- The new [Writing](docs/core/writing.md) standard sets voice, tone, grammar, sentence and paragraph rules, characters, one title form per record type, the fixed explanation sections per type, and the four documentation kinds, for LiteSpec and every consumer.
- Checks report a title over eight words, a sentence over 25 words in a statement, summary, situation or description, and a modal word in a statement.

### Scope

- An Intent or Behavior record states only behavior the application performs, data it holds, or a limit it enforces. Prices as an offer, markets, sales, hiring, launch dates and founder risks stay out; they enter only as a constraint or as dated research.
- `intent-area-invalid` rejects Intent areas named `business`, `company`, `go-to-market`, `markets` or `roadmap`.
- The new opt-in `delivery` extension holds the release, CI, operations, dependency and structure provisions a small project can skip; the core keeps what every project needs to keep the specification honest.
- `runbook-unused` reports a runbook that no alert, job, reaction or failure policy names.

### Intent Records

- A problem is a pain of a person or organization outside the team. It requires `affects`, whose first entry is the primary sufferer, and `affectsSegment`, `costToday` and `workaroundToday`, and lists `evidence` entries with `claim`, `source`, `readOn` and `kind` (`observed`, `reported` or `assumed`). The model computes `evidenceStrength`; an approved problem needs observed or reported evidence. Its explanation file has the sections Who Has It, What They Do Today, Evidence, What Would Retire It.
- An actor's `kind` is `person` or `organization`, and `belongsToTeam` says whether it works for the team building the product. An external system names no actor; internal machinery is never an actor.
- A journey requires `description`. A step names at most one performer: an actor at a handoff, or the external system, job or reaction in `system` when no person acts.
- New checks: `problem-sufferer-invalid`, `problem-cost-missing`, `problem-workaround-missing`, `problem-evidence-missing`, `problem-statement-invalid`, `problem-duplicate`, `actor-unused`, `goal-mover-missing` and `term-missing`.

### Model

- The risk tier is gone. A record's `risk` classes show by name, and the project file's `approvals` table names who approves each class and the default rule for records with no risk class. The review queue, batches and ballots group by risk class; `litespec model review --risk <class>|none` filters them.
- The model computes `completion` for every requirement, use case, journey, feature and goal: a stage of `unfulfilled`, `specified`, `implemented` or `proven`, whether proof is known, the children holding it back, and labelled counts. A parent is only as far as its weakest in-scope child. `feature-shipped-drift` flags a shipped feature below proven.
- The project file's `screenshots` setting names gallery folders and a file-name pattern; the model records each screen's `captures` and `captureState`.

### Book

- Every record page follows one anatomy: breadcrumb, title, a visible type line with the type's question, the identifier with Copy Title and Copy ID, the lead sentence, the completion block, one aligned field table, prose, and links grouped by relation. Facts are table rows, never badges or nested cards.
- Lists group by fields the records already have, with counts and completion per group, and show `Show all N` instead of cutting silently.
- A screen page leads with its captured screenshot, with desktop, phone, light and dark variants.
- Glossary terms in prose link to their definitions.

## v3.0.0

LiteSpec 3.0.0 is a top-down specification standard for one bounded-context business application, written by agents and judged by machines, so people review only what is theirs to decide. A consumer installs this release as the `LiteSpec.Cli` local .NET tool at the release version and records it in `reviewedLiteSpecVersion`. The release takes effect as the `v3.0.0` tag.

### Layers

- Four layers: Intent (why and what we promise; product people review it), Behavior (how the system behaves; developers review it), Implementation (code) and Proof (tests, checks, computed statuses and the book).
- Each layer is written from the one above. Nothing above Implementation is computed from code; code and tests point up. Facts from the world enter Intent as research or constraints, and what a lower layer needs travels up as a question with a decider.
- The translation rules Specify, Implement and Verify state what each boundary receives, must add, may not decide, and checks.
- Consumer docs sit in one folder per layer: `docs/intent`, `docs/behavior`, `docs/implementation`, `docs/proof` and `docs/governance`.

### Records

- Each record page is a YAML record file holding one mapping with its entries in `records`; an optional explanation file beside it holds Markdown under H2 sections.
- A module page is `module.yaml` in its module folder and an aggregate page is `aggregate.yaml` in its aggregate folder; the glossary is `docs/glossary.yaml`.
- Anything a check reads or a reviewer approves is a record field; the explanation file only helps a reader understand. Product, constraint, rule set, actor, role and external system hold `summary`; goal holds `whyItMatters`; problem holds `situation`; feature holds `offering`; decision holds `consequences`; topic holds `summary`, `position` and `refusals`; journey holds `betweenSteps`.
- Use cases hold `purpose`, `whatHappens` and `exposure`; reactions, jobs, notifications and setting sets hold `purpose` and `whatHappens`; screens hold `summary`; integration events hold `summary` and `delivery`; runbooks hold `impact`, `prerequisites`, `procedure`, `verification`, `recovery` and `escalation`; exceptions hold `exit` and `consequences`; research sources carry a `key`.
- Prose cites a record by its identifier, such as `[the refund rule](rule/refund.within-window)`; research cites a source as `[text][key]`.
- A Markdown page that holds no record is a document with an optional frontmatter block holding only its page style and common fields.
- Journeys are product sentences with step ids, `finishedWhen` and a per-step `failure`; use cases point up with `serves`.
- Requirements state exceptions with `except` and a `fallback`, and carry `risk` and `setBy`.
- Use cases are commands or queries with `access` (`grant` and `tenant`; `auth` sits on the entry point), `changes`, `reads`, `consistency`, `effects` (`runs`, `sends`, `emits`, `calls`), `idempotency`, `audit`, typed query output in `include` groups, freshness and paging. Generic refusals are derived.
- Record types include reaction, job, notification, setting and integration event. Aggregates have `entity`, `saga` and `store` kinds, and data classes with erasure positions. A retention schedule page holds retention rows.
- Reactions and jobs declare edges in `effects` with the same verb grammar as use cases; a reaction may omit `timing`, which reads as `async`, and a top-level verb beside `effects` is refused.
- An idempotency `key` templates `{field}` placeholders over the record's `input` fields, and `scope` is `tenant`, `account`, `login` or `global`; reaction and job placeholders name triggering event or run window fields and are not checked.
- Reaction and job `onFailure` is a structured policy: `mode` is `retry`, `skip-and-log` or `discard`; a `retry` policy holds `maxAttempts`, `initialDelay`, `maxDelay` and `backoff`; `then` is `dead-letter`, with `deadLetter`, `surfacesIn` and `runbook`, with ISO 8601 durations.
- Approval is data: `approvedBy`, `approvedOn` and a semantic `approvedHash`, written by `litespec model approve`.

### Review and checks

- Product people read journey chapters and ballots; developers review Behavior diffs; a named security reviewer sees tier 1 access and data changes. Tier 3 questions auto-approve after a veto window.
- Every check has a level: G (record graph, blocking), S (stack reader, blocking where qualified) and A (advisory).
- Every edge between use cases, reactions, jobs, notifications, integration events and external systems is declared and checked. A calls edge counts only through a port anchored to an external-system record.
- A refusal proof confirms from the asserted code, a client code `refusals.aliases` translates, or the HTTP status `refusals.statuses` maps to the code.
- An integration fact that drives a use case or rule confirms its scenario or rule claim from inside the host.
- An `effect-undeclared` finding names a one-hop helper as `via 'Type.Method'`.
- A term's `labels` sit outside the approval hash, so adding a language reopens no approval.
- A project declares its use-case verb vocabulary in `vocabulary`: `verbs`, `phrasal` verbs counted as one, and `retired` synonyms with their replacement, checked by `use-case-verb-unknown`.
- `effect-timing-invalid` (level S) reports a command whose `sends` or `calls` effect runs inline; consequences run from an event handler or a job after the commit.
- `event-tense-invalid` accepts a past tense followed by a prepositional complement and a mark method naming its state, and `term-clash` skips a hyphenated pair opening with a function word.
- `state-unreached` (level G) reports every state no transition enters. An aggregate writes its creation as a transition from `-` in its `transitions` field, so its first state is reached too.
- The approval hash covers every written record field except the identity, workflow and approval fields the guide lists, and compares numbers by value, so `50.00` and `50` hash alike. For research, decision and topic pages it also covers the explanation file.
- `prose-missing` reports a significant use case with no `purpose` field and a significant command with no `whatHappens` field.
- `explanation-orphaned`, `explanation-missing`, `format-drift`, `source-link-bare`, `source-key-unknown` and `source-unused` (level A) report an orphaned explanation, a missing article explanation, a record outside canonical form, and a research source linked directly, cited by an unknown key, or never cited.
- `record-invalid` reports a record file that does not parse, with the parser message and line.
- `reader-missing` (level S) also reports a .NET file that cannot be read or parsed, a declared stack root that resolves to no folder, a `server` project with no backend reader, and a configured bill of materials that cannot be read, is not SPDX or CycloneDX, or yields no packages.
- A test proves a scenario across machines: the proof ledger hashes each test file by its path relative to the project root and its text with normalized line endings.

### Application Layer

- A command handler loads or creates one aggregate, calls one domain method, stages it and returns; validation, authorization, idempotency, audit, the timestamp, holds, the commit and fault classification are pipeline stages, and a change to another aggregate is a consequence. A record declaring `consistency: atomic` is the one exception.
- Each command implements one use case with one success path; a find-or-create flow is two use cases the owning boundary chooses between, and a lost race ends with the use case's conflict refusal at commit.
- Uniqueness is enforced by the store: a unique index, an identity derived from the owner, aggregate state for a once-only rule, or a key the aggregate declares and persistence claims at commit. Each constraint maps by name to its conflict refusal, and no handler reserves or checks a key.
- A refusal code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in the project file `refusals.composed`, and `refusal-unimplemented` reads the code as raised where its key is a literal in the code.
- Every aggregate carries a version the store compares at commit.
- A message whose target must be the caller carries no target or declares a self grant the guard enforces.
- Audit targets and reasons the message carries are declared beside it; the handler adds only values it learns while running.
- Messages, provider calls and changes to other aggregates run in event handlers after the commit; the outbox delivers each event to each handler separately.
- Query handlers take no repository and never write. Work over many records is a job issuing one command per record, each in its own unit of work.
- Result parts are named `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`, event handlers are `{Action}On{Fact}Handler` in an `On{Fact}` folder of the subscriber's aggregate, and repository variables are named after their interface.

### Mobile

- An `android` stack builds a native Android app with .NET for Android and plain Android views: no MAUI, no WebView screens. It sits at `docs/frontend/mobile/android/` under shared mobile pages for structure, screens and navigation, data and state, device capabilities, and testing, so a later mobile stack reuses them.
- The mobile solution lives at `apps/mobile/` with platform-free core projects beside one app project per platform; a `client-only` app holds Domain, Application and Infrastructure, a `server` app holds the generated API client.
- The `android-client` preset starts a client-only Android consumer, and the manifest pins the Android API level in `stack.android`.

### Tools

- `litespec model diff` has `--intent` and `--behavior` views with a security queue, and `litespec book` renders journey chapters and a ballots page.
- `litespec new` scaffolds a record file with its explanation file for an article type, `litespec rename` rewrites an id in record files, explanation files and documents, and `litespec model approve` records approval in the record file.
- `litespec new result` and `litespec new scenario` append one entry to a use-case record file in house order: successes, refusals, then scenarios.
- `litespec model format` rewrites every record file and the glossary file into canonical form; `--check` writes nothing, lists each file that would change, and exits 1 when any would. It exits 2 when a file does not parse.
- `litespec append` adds an entry to a record file; `litespec init` writes `docs/glossary.yaml`.
- Consumer templates ship as `<type>.yaml` record files, with `<type>.md` explanation files for article types and `<type>.md` documents for document styles.
- `litespec model check --staged` reports only staged records, their direct links, and the records staged code implements.
- `litespec model approve --dry-run` prints which records would be stamped and which would be reopened, without writing.
- The `litespec-maintain` tool checks the standard's own pages, schemas, manifest, and provision index from `apps/maintain/` with `validate` and `provisions`; the `litespec` tool serves consumers only.
- Extensions added: privacy, devices, automation, workflow-engine, pipeline, queues and ai. LS2002 is retired.
- `litespec model check` prints each finding once, the blocking ones first, then a count per finding code and level, and ends with the number of findings, blocking findings and known findings. `--level` limits the findings and counts to the named levels, such as `--level G,S`.
- `litespec model check --since` and `--staged` leave out files under `.litespec/model/`, which the tool writes itself.
- `litespec model approve --all --dry-run` lists exactly the records `litespec model approve --all` stamps.
- Every command reports an unexpected failure as one line and exit code 1; with `--format json` the line is a JSON object with an `error` field. A repeated option, an empty value, and an unknown option given with `--help` are usage errors.
- The book links only `http`, `https`, `mailto`, in-page fragments, relative paths, record identifiers and the configured editor scheme, and renders any other link target as text. `litespec book` exits with code 2 when the carried release is missing.
- A project that opts out of the analyzers with `LiteSpecLayer=None` gets no diagnostics and no generated code from the analyzer package.
- The navigation and page-frame patterns draw their wireframes on the Claimwick cast. Their option ids follow the option names: `cost-centre-sections-as-tabs-on-the-cost-centre-page`, `no-switcher-back-to-the-cost-centre-list`, `the-count-the-cost-centres-the-exceptions`, `per-cost-centre-cards-with-a-rate`, `payouts-as-a-timeline`, `split-payouts-left-incidents-right`, `month-grid-with-trips-marked`, `week-lanes-per-cost-centre` and `receipt-scanner-is-offline`.
- Provisions cover file uploads and the `429` status on the API, and offline commands, saved navigation state, app links and touch and text accessibility on Android. The shared frontend data rules live on the frontend data page for every stack, and the `jobs` extension runs scheduled jobs in a Worker. FsCheck is pinned for property-based Domain tests.
- Record files use a strict YAML subset in canonical key order with two-space indentation; `litespec model format` owns the layout.
- The YAML block at the top of a document is called frontmatter throughout the standard.

### Record Hierarchy

- Every record has exactly one parent: by identifier containment, by one required written up-link, or, for a type with no record above it, its layer list under the product. A record whose required parent is missing is an error at level G, reported even when the record is a draft. Every inverse link is computed, never written.
- A feature names at least one problem in `solves`; its parent is the first entry. Every non-refused feature names at least one goal in `advances`. A refused feature names `refusedBecause` and the problem it declines in `solves`, and carries no `advances`.
- Every journey names its goal record in a required single link `serves`, which is its parent. A use case, reaction, job or screen serves journey steps; a journey serves one goal, and a use case never serves a goal directly.
- Every requirement names its parent: a carrying feature, a constraint or research in `basedOn`, or a decision naming it in `decides`, in that precedence.
- Written result and scenario identifiers use the `result/<use-case>.<name>` and `scenario/<use-case>.<name>` form, and derived generic results take the same form. A written result no scenario shows is reported: level G for a success result, level A for a refusal, on large projects.
- Structural parents are level G for all records; other required links are level A for tier 3 records and level G for tier 1 records.
- Field names follow one grammar: a classifier is `kind`; a link is a present-tense verb, plural when it holds a list; a deadline is `<verb>By`, a recorded date `<verb>On`; a reason is `reason` or `<verb>Because`. Feature `lifecycle`, goal and requirement `dueBy`, feature `plannedFor`, decision `approvedOn`, and constraint `authority` and `kind` follow it, checked by `litespec-maintain validate`. Glossary terms carry `layer` and `aliases`.
- Use cases list the settings they read in `reads`; a setting's readers are computed, never written. The Links table carries a "reads as" phrase in both directions for every link, and the book takes its connection labels from it. A follows edge marks export beside implements in the built model.
- Research states its `conclusion`, and LiteSpec keeps its own research and decision records under `docs/intent/`.
- `litespec book` is one offline shell with sharded data, generated from the model, with the outline from the parent table and the labels from the Links table and the glossary.
- Approval batches a journey with the goal it serves, keeps each entry a page stamp did not hold at the page's former status, refuses a decision naming no chosen option, and keeps a transcribed record serving a draft journey in draft. Screens are Behavior, and reaction and job scenarios carry their own stamp.
