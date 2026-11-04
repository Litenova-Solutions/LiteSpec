# Changelog

## v4.0.0

LiteSpec 4.0.0 is a standard for specification-driven development of one bounded-context business application by people and coding agents. It defines a strict specification model of linked records beside graded engineering provisions, with git as the decision record. A consumer pins this release in `reviewedLiteSpecVersion`. The release takes effect as the `v4.0.0` tag.

### Part A: The Nature of LiteSpec

- The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve; a violation is a defect.
- Each provision states its strength, rule, default or advice, and where it applies; a check exists where one is cheap and sure.
- Git is the decision record: a record in the repository is decided, and the pull request that changes it is its review.
- The specification is closed: an agent builds what the specification states and takes anything missing to the owner or the backlog board.
- Every record is about the software: what the application does, holds, refuses at runtime or runs on.

### Part B: Git Is the Decision Record

- Records carry no history of their own; git carries who changed what and when.
- Completion is computed from code, tests and passing scenarios, and the book shows it first.
- An exception carries reason, exit and expiry; an expired exception is a blocking finding.
- A date about the outside world sits with its source as `readOn`.

### Part C: Eight Commands

- The CLI has eight commands: `init`, `new`, `rename`, `model check`, `model format`, `model query`, `book` and `rules`.
- `model check` is the one command CI runs: schema, folders, names, links, locality, reachability, marks and exceptions.
- `model render` has three regions: `alert-inventory`, `runbook-index` and `operations-log`.
- An additional command, finding code, analyzer rule or generated file needs an owner decision that records it.

### Part D: Provisions by Situation

- Each page declares its stack and layer; a provision adds `Applies to` only when narrower than its page.
- Every provision states `Strength`: rule, default or advice.
- The provision index carries scope and strength; the site groups Standard, Reference, Learn and LiteSpec's own records.

### Part E: Rules for the File at Hand

- `litespec rules <path or record id>` lists the applying provisions grouped by strength, with why each was retrieved; `--full` adds rationale and example.
- Project conventions and departures merge into the result; a departure shows in place of the provision it replaces.
- A retrieval evaluation with a precision target proves the command.

### Part F: The Consumer Uses the Same Structure

- Layer and artifact derive from the stack structure and the records a file marks.
- Project conventions live in `.litespec/governance/conventions/` in provision shape.
- A departure names the provision it replaces and a scope.
- Records use the same layer and artifact terms, so one query answers file, records and provisions together.

### Part G: Surfaces That State the Nature

- The README, AGENTS.md, the core pages, the command pages, the templates and this changelog state the nature above.
- The docs index groups pages with generated counts; the site navigation follows the same groups.

### Part H: Consumers Adopt the Whole Contract

- A consumer pins one release and adopts a later release by accepting its whole contract.

### Part I: Every Record Is About the Software

- Intent reads module, then feature, then requirement; Behavior reads module, then aggregate, then use case.
- Features sit under `intent/modules/<module>/features/` with their requirements as entries; a requirement identifier has the form `requirement/<feature>.<name>`.
- A folder under `intent/modules/` that names no declared module is a blocking finding.
- Reachability blocks: a goal reaches a journey or a quality requirement, a problem reaches a feature or requirement, a feature or requirement reaches Behavior, a constraint is answered, research is cited, and a decision decides a record that reaches Behavior.
- Identifiers are short and unique in the product; the qualified identifier is computed from identifier segments plus placement, never written.

### Part J: One Placement Model for Every Layer

- Every record has one parent; the container is the parent; product, module, aggregate and entry point are the only folder nodes.
- Behavior, Implementation and Governance place each type per the placement table in the model page; Proof holds no authored records.
- Code and tests match the qualified identifier of the record they mark in folder and namespace, including frontend module folders.
- Module `dependsOn` holds no cycle; an integration event has one publishing module.

### Part K: Fields That State the Product

- Every record carries type, id, title, terms and records plus its type fields; every record carries one `summary`, and a record that makes a testable claim carries one `statement`.
- The project file carries project, size, shape, stacks, stackConfig, paths, marks, proof, dependencies, screenshots, refusals, selectedExtensions and `reviewedLiteSpecVersion`.
- `risk` sits on permission only; `setBy` uses product, operator or tenant; a rule names where retained facts live in `retainedIn`; a use case states its field read and write lifecycle.

### Part L: Complete Work Before Merging

- A branch may be incomplete; what merges is complete. `model check` reports reachability locally and blocks in the pull-request check; an exception with `expiresOn` parks one record.
- The folder wins: an identifier whose home segment disagrees with its folder is a blocking finding; each name segment is unique in the product.
- Tiers: G reads records and always blocks, S reads code and blocks where qualified, A reports and never blocks.
- An exception is live when present and unexpired.

### Part M: The Module Layout as the Default Architecture

- Layers are roles: Domain, Application, Infrastructure and WebApi with the reference table the architecture page states.
- One application project holds every module; hosts hold composition and HTTP plumbing.
- The folder tree uses closed folder sets per level; use case folders are flat; cross-module access runs only through `Contracts/` along `dependsOn`.
- The `layer-projects` extension holds the five-project layout for consumers that select it.

### Part N: Gaps Closed in the Standard

- A deviation is an exception of kind departure whose `covers` names the provision it replaces.
- Product patterns live in `implementation/patterns/`; product copy rules live in `implementation/writing/`; document pages may sit at a layer root.
- Module rules live in `behavior/modules/<module>/rules/`; adapter tests sit under the adapter's `Infrastructure/` path.
- Guidance precedence lives in one provision on the agent page.

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
