# Model Builder

## Name

`litespec model` builds the project model, checks it, diffs it against a base, renders generated regions, approves records, backfills drafts from code, answers record queries, and prints the names inventory.

## Synopsis

```bash
litespec model build [--root <consumer>] [--out <file>]
litespec model check [--root <consumer>] [--format json] [--staged]
litespec model render [--root <consumer>]
litespec model query [--root <consumer>] <id>
litespec model names [--root <consumer>] [--format text|json]
litespec model review [--root <consumer>] [--decider <owner>] [--tier 1|3] [--module <module>] [--type <type>] [--format json]
litespec model approve [--root <consumer>] (<id>... | --all) [--by <owner>] [--on <YYYY-MM-DD>] [--dry-run]
litespec model approve [--root <consumer>] --batch <selector> --by <owner> [--second-reviewer <owner>|agent] [--review-report <file>] [--on <YYYY-MM-DD>] [--dry-run]
litespec model backfill [--root <consumer>] [<module>] [--dry-run]
litespec model diff <base> [--root <consumer>] [--format text|markdown|json] [--intent|--behavior] [--fail-on breaking] [--breaking-only]
```

## Description

The model builder reads the written records, the code marks, the routes, the tests, and the latest test run. It writes `model.json` with every record and its resolved links. The file names the code, screens, tests, and test runs it read. It adds the computed `implements`, `calls`, and `proves` links, the computed `implemented`, `verified`, `stale`, and `reachable` state of each record, and the effective status of each record. Computed states are never written on a record; they appear in the book and the model output only.

A proof goes stale when its scenario changes after the proof passed. The first time the builder reads a passing run of a scenario's proof, it records the scenario semantic hash and the run's date in `.model/proofs.json`, beside `model.json`. On every later build against the same run, a scenario whose semantic hash differs from the recorded one has a stale proof, and a stale proof verifies nothing until a newer passing run is read. Run the model check after the test run, in the same job, so the hash is recorded before a record changes.

`approve` writes approval data onto the named records, or with `--all` onto every approved or reviewed record that carries no `approvedHash`. It sets `approvedBy` (from `--by`, else the record decider, else its owner) and `approvedOn` (from `--on`, else today) when they are missing, and always writes `approvedHash`, the builder hash over the record semantic fields. A person runs it when they approve or review a record; agents run it only with `--by "auto (veto window)"` for a tier-3 question past its veto window. With `--dry-run` it writes nothing and prints what it would do: each record that would be stamped for carrying no `approvedHash`, and each record that would be reopened for a semantic hash that differs from it. [The approval hash guide](../guide/approval-hash.md) lists which fields feed the hash per record type.

`review` prints the review queue the [release page](../core/release.md) describes under Batch Review. It lists every draft and every reopened record, grouped into batches: a use case with its results and scenarios, a decision with the records it decides, and the remaining tier 3 drafts of one module and one record type. Batches come in one order: batches holding a `fullReRead` record, then batches holding another reopened record, then by the count of drafts awaiting the batch, most first, then in authority order (Intent before Behavior), then by the earliest `decideBy`, then by selector. With `--format json` it writes one object with a `batches` array; each batch carries `selector`, `kind` (`use-case`, `decision`, or `module-type`), `tier`, `deciders`, `decideBy`, `awaiting`, and `records`, and each record carries `id`, `type`, `layer`, `tier`, `decider`, `decideBy`, `status`, `effectiveStatus`, `reopened`, `fullReRead`, `awaiting` (the drafts whose effective status is draft because of it, through `decides`, `fulfills`, or `serves`), `openQuestions`, `scenarios` (`total`, `confirmed`, `passing`, `stale`), and `findings` (the count of open findings whose subject is the record).

`approve --batch` approves one batch. It promotes each draft in the batch, Intent and governance records to `approved` and Behavior records to `reviewed`, writes `approvedBy` from `--by`, `approvedOn`, and `approvedHash`, re-stamps each reopened record, and leaves a record already approved with an unchanged hash as it is. A `decision/` batch promotes the decision first, then the records it decides in authority order. It refuses the whole batch, writes nothing, and exits 1 when a guard fails: no `--by` naming an owner, a tier 1 record and no `--second-reviewer` other than the approver, a record with an open question, or a record whose effective status would stay draft. Each refusal names the record and the guard. On success it writes the manifest `docs/proof/approvals/<YYYY-MM-DD>-<selector>.json`, the selector with every `/`, `.`, and `:` replaced by `-`, and `-2`, `-3` and so on appended when a manifest of that name exists. The manifest holds `selector`, `approvedBy`, `secondReviewer`, `reviewReport`, `approvedOn`, `sampleShare`, `sample` (the sampled ids, tier 3 only), and `records`, each with `id`, `type`, `tier`, `action` (`promoted`, `re-stamped`, or `held`), `hash`, `scenarios`, and `findings`, the same fields `review` writes. With `--dry-run` it prints the plan, the guard failures, and the sample, and writes nothing.

`backfill` drafts Behavior records from code during adoption: for each use case, reaction, and job a mark places, it reads the repositories, dispatches, and refusal codes the code model already holds and drafts the missing frontmatter. A use case gains `changes` or `reads`, `effects` entries, and refusal results; a reaction gains `reads` and its edges; a job gains `reads` and its edges. Every touched record is marked transcribed. Existing entries are never removed or rewritten; only missing entries are appended. With a module name it covers that module only; without one it covers every module. With `--dry-run` it prints the plan and writes nothing. It prints a per-module completeness table first, then one line per planned record, then the count of files updated and records backfilled.

The .NET stack reader links each code element to its record through its mark, in the style `marks` declares: a handler, an event handler, a job, a port, an adapter, an endpoint, an aggregate root and its state-changing methods, a domain event, a refusal type, and a test. A folder or a type name is never the link. A marked refusal type declares the `code` of the refusal result its rule names.

Aggregate transitions and data, and scenario examples, may be written as Markdown tables the builder parses, so the parts people debate read naturally even in the raw file. A front-matter field wins over a table with the same name.

The eight subcommands share one reader:

- `build` writes `model.json` under `.model/` in the consumer root, unless `--out` names another file. That folder holds generated output, and the consumer ignores it in Git.
- `check` builds the model in memory and reports every finding with its tier, code, message, path, and line. With `--staged` it reports only the findings in the staged scope: the records on pages changed in the git index, their direct links in both directions, and the records staged code implements. A finding is in scope when its path is staged or its subject is. Outside a git repository, or when git is unavailable, `--staged` exits 2.
- `render` rewrites the fenced generated regions and touches no text outside them.
- `query` prints one record with its links in both directions. Agents use it to load a record's context.
- `names` prints the per-module names inventory: the verbs in use with counts, the nouns after the verb, reaction facts, events, refusal codes, notification template keys, and setting names, with the lookalike groups. Agents read it without rendering the book.
- `backfill` drafts missing Behavior frontmatter from code during adoption, as above. On a module holding no use-case, reaction, or job records it exits 1.
- `diff` compares the working tree against a base. The base is a consumer folder holding `litespec.project.json`, or a git revision of the consumer. Records match by identifier. `--intent` shows Intent changes in product words, `--behavior` shows Behavior changes and the security queue, and without either flag it shows both. The changelog prints REMOVED, then MODIFIED, then ADDED; each section groups entries by family: goals and requirements, domain, access, operations. The header counts breaking, removed, modified, and added changes. `text` is the changelog reviewers read, `markdown` is the pull request comment, and `json` carries every change with its fingerprint and the counts for machines. `--breaking-only` shows breaking changes and hides the rest.

The security queue ends the behavior diff. It lists the changes the security reviewer named in `review.securityReviewer` approves: access tenant scope widenings to `cross`, new `calls` effects, and new personal or sensitive data classes.

A change breaks approved intent when it removes or narrows an approved requirement, adds a refusal or a precondition to an approved use case, removes a result, state, transition, or scenario, weakens a rule, or changes a quality measure or target. Each breaking entry names its reason code:

| Code | Meaning |
|:---|:---|
| `requirement-removed` | An approved requirement no longer exists. |
| `requirement-narrowed` | An approved requirement statement changed. |
| `quality-measure-changed` | A quality requirement measure or target changed. |
| `refusal-added` | A refusal was added to an approved use case. |
| `precondition-added` | A precondition was added to an approved use case. |
| `refusal-path-added` | A scenario proves a new refusal path. |
| `result-removed` | A result was removed from approved intent. |
| `scenario-removed` | A scenario was removed from approved intent. |
| `state-removed` | A state was removed from an approved aggregate. |
| `transition-removed` | A transition was removed from an approved aggregate. |
| `aggregate-removed` | An approved aggregate was removed. |
| `rule-weakened` | A rule was weakened. |
| `refusal-meaning-changed` | A refusal meaning changed. |
| `result-outcome-changed` | A result outcome changed. |

Every reason except `refusal-path-added` fires only on approved or reviewed records.

A finding that an open known gap covers is reported as known and exits zero. The gap covers the finding when its `check` equals the finding code, its `subject` lists the finding's subject, the gap is not retired, and its end date has not passed. A gap on a use case also covers the findings about the scenarios and results it holds. An expired known gap fails the check.

Every check has a tier. G reads records only and always blocks. S reads code through a stack reader: it blocks where the stack reader is qualified (.NET today), and reports otherwise. A is advisory: it reports and never blocks. An S finding about a draft record reports without blocking and prints `draft`, and while `check.phase` is `frozen` a proof finding reports without blocking and prints `frozen`. The finding codes are:

| Code | Tier | Meaning |
|:---|:---|:---|
| `record-shape` | G | A record fails the shape its type requires. |
| `identifier-form` | G | An identifier breaks the form its type requires. |
| `identifier-duplicate` | G | One identifier names two records. |
| `invalid-field` | G | A field holds a value outside its shape, or a record other than a decision carries `replaces`. |
| `reference-unresolved` | G | A citation names no written record or shared provision of the installed release. |
| `link-unresolved` | G | A Markdown link on a record page resolves to no file. |
| `owner-unknown` | G | A record names an owner or decider the owners list does not hold, or an owner no team owning its path claims. |
| `draft-without-decider` | G | A draft names no decider or no decide-by date. |
| `draft-gate` | G | An approved record rests on a draft decision. |
| `product-not-singleton` | G | The project holds no single product record. |
| `size-type-missing` | G | The size requires a type the project lacks. |
| `requirement-unfulfilled` | G | A requirement is fulfilled by no rule, use case or journey, and a quality requirement also by no technical decision naming it in `basedOn`. |
| `requirement-unmeasured` | G | A measured requirement names no check, or its check has no passing run. |
| `decision-decides-nothing` | G | A product decision names nothing it decides. |
| `journey-reach` | G | A public use case sits in no journey and names no background caller. It runs at large size only. |
| `state-unreachable` | G | A state is reached by no transition. |
| `transition-unresolved` | G | A transition names a missing state or use case. |
| `use-case-without-success` | G | A use case ends with no success result. |
| `scenario-result-unresolved` | G | A scenario ends in a result outside its own use case and outside the derived results. |
| `calculation-without-table` | G | A calculation rule has no scenario examples table. |
| `rule-name-negation` | G | A rule statement negates what its name does not keep. |
| `known-gap-expired` | G | A known gap passed its end date with its check still red. |
| `gap-unknown-check` | G | A known gap names a check that no check uses. |
| `aggregate-name-clash` | G | Two aggregates share one name across the project. |
| `use-case-name-clash` | G | Two use cases share one name across the project. |
| `use-case-verb-unknown` | G | A use-case name starts with a verb neither the baseline nor the project vocabulary declares; a retired synonym names its replacement. |
| `use-case-verb-kind` | G | A query opens with a changing verb, or a command opens with a reading verb. |
| `reaction-name-form` | G | A reaction name is not `<verb>-<what>-on-<fact>` with a declared verb. |
| `job-name-form` | G | A job name does not start with the verb of a use case it issues. |
| `term-lookalike` | A | Two terms differ only in hyphenation, a British or American spelling, or a compound split, and both are in use. |
| `statement-vague-word` | A | A rule statement, scenario step or result meaning uses a word that hides behaviour, such as `handle`, `process` or `etc.`. |
| `use-case-noun-unknown` | A | The words after a use case's verb name no aggregate, declared aggregate part, or glossary term. |
| `refusal-code-form` | A | A refusal code is not `<module>.<noun>-<state>` with the record's module or a declared generic prefix, or it uses a banned vague word. |
| `template-key-form` | A | A notification template key does not end with an audience the record declares. |
| `setting-unit-missing` | A | A setting whose value is a number or a duration declares no `unit`. |
| `integration-event-wording` | G | An outbound integration event is named with an internal word such as `staged`, `dispatch` or `outbox`. |
| `reaction-trigger-unresolved` | G | A reaction names a trigger no domain event or integration event carries. |
| `erasure-undeclared` | G | A personal or sensitive field stands outside a declared erasure position. |
| `record-orphaned` | G | A Behavior record names neither `serves` nor `fulfills`. |
| `step-unserved` | G | A journey step is served by no use case or reaction. |
| `effect-loop` | G | The effect graph cycles outside a domain event with a stated end condition. |
| `consistency-unexplained` | G | An atomic use case changing more than one aggregate names no `because` rule. |
| `query-output-missing` | G | A query declares no `output`. |
| `paging-undeclared` | G | A collection query declares no `paging`. |
| `widening-unjustified` | G | A query group visible beyond its sources names no `wideningBecause` requirement. |
| `retention-without-enforcement` | G | A data class names no `enforcedBy`. |
| `risk-without-acceptance` | G | A retained or exempt personal data class names no `acceptedBy` in Intent. |
| `exemption-expired` | G | An erasure exemption passed its `reviewBy` date. |
| `setting-default-without-requirement` | G | A commercial, legal, or tenant-varying default fulfills no requirement. |
| `reservation-without-release` | G | A reservation rule names no `releasedBy`. |
| `notification-without-sender` | G | A notification is sent by no reaction or use case. |
| `except-without-fallback` | G | An `except` names no fallback requirement, or the fallback carries its own `except`. |
| `approval-reopened` | G | A semantic hash differs from the recorded `approvedHash`. |
| `transcribed-reviewed` | G | A transcribed record is reviewed while its parent is unapproved. |
| `transcribed-after-adoption` | G | A transcribed record remains after the adoption decision is approved. |
| `operator-job-without-guard` | G | An operator job states no `guard`. |
| `schedule-unstructured` | G | A job schedule is neither an `every` duration nor a `cron` expression. |
| `behavior-unrecorded` | S | A refusal code in a declaring position, a screen call to no endpoint, or an engine step that maps to no use case is named by no record. The remedy reads: delete the code, or write its parent record from a requirement, drafting the requirement if none exists. |
| `mark-missing` | S | A code element the marks table names carries no mark. |
| `mark-unresolved` | S | A mark names no record, or a record of a type its code element does not take, or sits on an element that takes no mark; or a `follows` mark names no provision of the carried release, no decision or deviation, a retired record, or a provision on a page the project's stacks do not apply. |
| `decision-unfollowed` | S | A technical decision under `docs/implementation/decisions/` is followed by no code element. |
| `decision-without-basis` | A | A technical decision names no constraint or requirement in `basedOn`. |
| `mark-count` | S | A code element carries more or fewer marks than its kind takes in the marks table, or names one record twice. |
| `mark-style-mixed` | S | A mark is written in the style the project did not declare. |
| `runtime-value-drift` | S | A hand-written event `Classification` or exception `Code` differs from what its record states. |
| `act-event-undeclared` | S | A root method marked with a use case raises an event the use case does not list in `raises`. |
| `act-event-unraised` | S | A use case lists an event in `raises` that no root method marked with it raises. |
| `use-case-act-uncalled` | S | A command handler calls no root method marked with its use case. |
| `aggregate-unimplemented` | S | No aggregate root carries the aggregate mark. |
| `reaction-unimplemented` | S | No event handler carries the reaction mark. |
| `job-unimplemented` | S | No job host carries the job mark. |
| `external-system-unimplemented` | S | No port carries the external system mark. |
| `use-case-unexposed` | S | A use case reached through a `browser` or `api` entry point has no endpoint carrying its mark. |
| `effect-unimplemented` | A | A declared effect edge is taken by no code. |
| `use-case-unreached` | A | A use case reached through a browser entry point has no screen calling it. |
| `use-case-unimplemented` | S | No code implements the use case and no known gap covers it. |
| `refusal-not-thrown` | S | A refusal result names a code the code never throws, or a code matching a `refusals.composed` template whose key no code declares. |
| `proves-unconfirmed` | S | No proving step of a claimed scenario reaches its use case, no assertion matches its refusal by code, client alias, or HTTP status, and no integration fact drives its use case or rule. A step reaches a use case when it belongs to the use case's steps class, calls its generated-client operation, requests its endpoint route through its own helpers, or looks its argument up in a probe table of routes in its steps class. A scenario proves the use case a when step reaches; the use case a given step reaches, when a later when step and then step observe it; or a use case a when step sets off through a declared producer, whose effects issue it or whose event a reaction issuing it reacts to, with an effect asserted. Codes and statuses asserted inside a then step's method count as the scenario's own. |
| `use-case-without-scenario` | S | A use case has no scenario. It runs at large size only. |
| `write-undeclared` | S | The code writes an aggregate outside `changes`. |
| `result-undeclared` | S | The code returns an outcome no result declares. |
| `effect-undeclared` | S | The code takes an edge no record declares, naming a one-hop helper as `via 'Type.Method'`. |
| `prose-missing` | A | A use case needing Goal and What Happens prose carries none. |
| `many-aggregates` | A | One command saves more than one atomic write group. |
| `question-aged` | A | A question stands past its `decideBy` date. |
| `use-case-verb-method` | A | A command handler calls aggregate methods, none starting with the use-case verb. |
| `event-tense-mismatch` | A | An aggregate method raises an event not ending with the past tense of its verb. |
| `name-drift-across-layers` | A | A message type, operation id or audit action differs from the use-case name. |
| `operation-id-duplicate` | S | Two endpoints declare one operation id, which the API document and every generated client refuse. |
| `route-shape` | A | A command route ends outside its verb, or a route carries no aggregate plural. |
| `reasonless-name` | S | A folder or type is named `Shared`, `Common`, `Helper`, `Manager` or the like outside the declared shared kernel. |
| `collaborator-holds-repository` | A | A class built with `new` takes a repository in its own constructor. |
| `repository-read-in-loop` | A | A repository read sits inside a `for`, `foreach` or `while` body. |
| `query-filters-in-memory` | A | A query handler filters a repository list with LINQ in memory. |
| `dependency-count` | A | A handler, reaction or job takes more than ten constructor dependencies. |
| `effect-before-commit` | S | A command use case declares a `sends` or `calls` effect with `inline` timing. |

The `render` subcommand writes these generated regions, each fenced by `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`: `history`, `use-case-coverage`, `sub-processors`, `runbook-index`, `rule-map`, `operations-log`, `open-questions`, `journeys`, `glossary`, `deviations`, `census`, `aggregate-map`, `audit-register`, `alert-inventory`, and `counts`.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<id>` | for `query` | Prints the record with this identifier and its links both ways. |
| `<base>` | for `diff` | The consumer folder or git revision to compare against. |
| `<module>` | for `backfill` | Backfills this module only; without it, every module. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | Selects the consumer repository to read. |
| `--out <file>` | `.model/model.json` under the root | Writes the model to this file instead. Only `build` reads it. |
| `--format <name>` | human lines | Only `check`, `diff`, and `names` read it. `check` accepts `json` and writes the findings as one JSON object. `diff` accepts `text`, `markdown`, or `json`. `names` accepts `text` or `json`. |
| `--intent` | off | Only `diff` reads it. Shows the Intent changes in product words. |
| `--behavior` | off | Only `diff` reads it. Shows the Behavior changes and the security queue. |
| `--fail-on breaking` | off | Only `diff` reads it. Exits 1 when the change breaks approved intent. |
| `--breaking-only` | off | Only `diff` reads it. Shows the breaking changes and hides the rest. |
| `--by <owner>` | the record decider, else its owner | Only `approve` reads it, and never with `--dry-run`. Sets `approvedBy` where it is missing. |
| `--on <YYYY-MM-DD>` | today | Only `approve` reads it, and never with `--dry-run`. Sets `approvedOn` where it is missing. |
| `--dry-run` | off | Only `approve` and `backfill` read it. Prints the plan and writes nothing. |
| `--decider <owner>`, `--tier 1\|3`, `--module <module>`, `--type <type>` | none | Only `review` reads them. Keeps the batches holding a record that matches every given filter. |
| `--batch <selector>` | none | Only `approve` reads it. Approves one batch: `use-case/<module>.<name>`, `decision/<id>`, or `<module>/<type>`. |
| `--second-reviewer <owner>\|agent` | none | Only `approve --batch` reads it. Names the second reading of a tier 1 batch: another owner, or `agent` with `--review-report`. |
| `--review-report <file>` | none | Only `approve --batch` reads it, with `--second-reviewer agent`. The agent review report the manifest cites; the file must exist. |
| `--staged` | off | Only `check` reads it. Reports the staged scope only: staged records, their direct links, and the records staged code implements. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The command finished with no open blocking finding. |
| `1` | `check` found an open blocking finding or an expired known gap, `query` found no record with the `id`, `approve` named an unknown record id, `approve --batch` refused its batch, `backfill` named a module with no use-case, reaction, or job records, `diff` with `--fail-on breaking` found a breaking change, or the run failed. |
| `2` | The arguments are wrong, the root holds no project record, the diff base is neither a consumer folder nor a git revision, or `check --staged` runs outside git. |

## Examples

Build the model for a consumer:

```bash
litespec model build --root ../consumer
```

Check the model and read the findings from another program:

```bash
litespec model check --root ../consumer --format json
```

Render the generated regions after a record change:

```bash
litespec model render --root ../consumer
```

Print one record with its links:

```bash
litespec model query --root ../consumer use-case/orders.cancel-order
```

Print the names inventory with the lookalike groups:

```bash
litespec model names --root ../consumer --format json
```

Record the finance lead approving two requirements:

```bash
litespec model approve --root ../consumer requirement/approval.receipt-over-threshold requirement/approval.no-self-approval --by "Claimwick finance lead"
```

Show what approving every record would stamp or reopen, without writing:

```bash
litespec model approve --root ../consumer --all --dry-run
```

Check only the staged records and their links before committing:

```bash
litespec model check --root ../consumer --staged
```

Draft the missing spending frontmatter from code during adoption:

```bash
litespec model backfill --root ../consumer spending
```

Show what backfilling every module would draft, without writing:

```bash
litespec model backfill --root ../consumer --dry-run
```

Diff the working tree against the main branch:

```bash
litespec model diff main --root ../consumer
```

Show the Intent changes in product words for the finance lead review:

```bash
litespec model diff main --root ../consumer --intent
```

Show the Behavior changes with the security queue for the developer review:

```bash
litespec model diff main --root ../consumer --behavior
```

Show only the breaking changes as a pull request comment, and fail on them:

```bash
litespec model diff main --root ../consumer --format markdown --breaking-only --fail-on breaking
```

## Underneath

The builder reads `paths`, `shape`, and `stacks` from the consumer's `litespec.project.json`. It reads shared provisions from the release the tool carries, the record owners from the CODEOWNERS file at `.github/CODEOWNERS`, `CODEOWNERS`, or `docs/CODEOWNERS`, and the test run from TRX or JUnit XML files under `paths.testResults`. A git diff base is read through an archive into a temporary folder, which is removed after the diff. Nothing beneath it is separately runnable.
