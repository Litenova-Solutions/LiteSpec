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
litespec model approve [--root <consumer>] --batch <selector> [--batch <selector>...] [--batches <file>] --by <owner> [--second-reviewer <owner>|agent] [--review-report <file>] [--on <YYYY-MM-DD>] [--dry-run]
litespec model backfill [--root <consumer>] [<module>] [--dry-run]
litespec model diff <base> [--root <consumer>] [--format text|markdown|json] [--intent|--behavior] [--fail-on breaking] [--breaking-only]
```

## Description

The model builder reads the written records, the code marks, the routes, the tests, and the latest test run. It writes `model.json` with every record and its resolved links. The file names the code, screens, tests, and test runs it read. It adds the computed `implements`, `calls`, and `proves` links, the computed `implemented`, `verified`, `stale`, and `reachable` state of each record, and the effective status of each record. Computed states are never written on a record; they appear in the book and the model output only. The output also lists each unmarked Domain constituent type under the aggregate whose folder holds it, or under `kernel`, in `constituents`. `build` runs the checks once on the model it built and writes the result into `model.json` as `findings` and `known`, the same objects `check --format json` writes, so the book reads its findings from the model and needs no separate findings file.

Each screen in `screens` lists its `operations`, its `useCases`, and its `calls`, where each call pairs one operation with the use case it reaches. `links.calls` carries one link per such pair, so a use case reached through two operations gets two links, and a use case whose operation is unknown carries an empty `operation`.

A proof goes stale when its scenario changes after the proof passed. The first time the builder reads a passing run of a scenario's proof, it records the scenario semantic hash and the run's date in `.model/proofs.json`, beside `model.json`. On every later build against the same run, a scenario whose semantic hash differs from the recorded one has a stale proof, and a stale proof verifies nothing until a newer passing run is read. Run the model check after the test run, in the same job, so the hash is recorded before a record changes.

`approve` writes approval data onto the named records, or with `--all` onto every approved or reviewed record that carries no `approvedHash`. It sets `approvedBy` (from `--by`, else the record decider, else its owner) and `approvedOn` (from `--on`, else today) when they are missing, and always writes `approvedHash`, the builder hash over the record semantic fields. A person runs it when they approve or review a record; agents run it only with `--by "auto (veto window)"` for a tier-3 question past its veto window. With `--dry-run` it writes nothing and prints what it would do: each record that would be stamped for carrying no `approvedHash`, and each record that would be reopened for a semantic hash that differs from it. [The approval hash guide](../guide/approval-hash.md) lists which fields feed the hash per record type.

`review` prints the review queue the [release page](../core/release.md) describes under Batch Review. It lists every draft and every reopened record, grouped into batches: a use case with its results and scenarios, a decision with the records it decides, a tier 1 record no use case or decision holds on its own, and the remaining tier 3 drafts of one module and one record type, where an Intent record's module is its identifier home, or for a journey the goal it serves. Batches come in one order: batches holding a `fullReRead` record, then batches holding another reopened record, then by the count of drafts awaiting the batch, most first, then in authority order (Intent before Behavior), then by the earliest `decideBy`, then by selector. With `--format json` it writes one object with a `batches` array; each batch carries `selector`, `kind` (`use-case`, `decision`, `record`, or `module-type`), `tier`, `deciders`, `decideBy`, `awaiting`, and `records`, and each record carries `id`, `type`, `layer`, `tier`, `decider`, `decideBy`, `status`, `effectiveStatus`, `reopened`, `fullReRead`, `awaiting` (the drafts whose effective status is draft because of it, through `decides`, `fulfills`, or `serves`), `openQuestions`, `scenarios` (`total`, `confirmed`, `passing`, `stale`), and `findings` (the count of open findings whose subject is the record).

`approve --batch` approves one batch per `--batch`, and `--batch` may be given more than once to approve many batches in one run. `--batches <file>` names a text file with one selector per line; blank lines and lines starting with `#` are ignored, and a second whitespace-separated field on a line names that batch's review report, overriding `--review-report` for that batch. The model is built once, and the batches run in the order given; every other option applies to every batch. Each batch promotes each draft in the batch, Intent and governance records to `approved` and Behavior records to `reviewed`, writes `approvedBy` from `--by`, `approvedOn`, and `approvedHash`, re-stamps each reopened record, and leaves a record already approved with an unchanged hash as it is. A `decision/` batch promotes the decision first, then the records it decides in authority order. After a batch is applied, its records count as approved or reviewed for the batches after it in the same run. Any batch except a module and type batch is refused as a whole, writes nothing, and holds no later batch back when a guard fails: no `--by` naming an owner, a tier 1 record and no `--second-reviewer` other than the approver, a record with an open question, a decision that names no chosen option, a transcribed record that serves a journey still in draft, or a record whose effective status would stay draft. A module and type batch instead approves every record whose guards pass and holds the rest: each held record keeps its status, gets no stamp, is named with its guard, and is written with action `held` and its guards in `heldBecause`. A module and type batch is refused whole only when no `--by` names an owner, or when no record in it passes its guards. Stamping a page leaves each entry that inherits the page status and is not in the batch at the page's former status, written on the entry. Each refusal names the batch, the record, and the guard. On success each batch writes the manifest `docs/proof/approvals/<YYYY-MM-DD>-<selector>.json`, the selector with every `/`, `.`, and `:` replaced by `-`, and `-2`, `-3` and so on appended when a manifest of that name exists. The manifest holds `selector`, `approvedBy`, `secondReviewer`, `reviewReport`, `approvedOn`, `sampleShare`, `sample` (the sampled ids, tier 3 only), and `records`, each with `id`, `type`, `tier`, `action` (`promoted`, `re-stamped`, or `held`), `hash`, `scenarios`, `findings`, and `heldBecause` (the guards holding the record when a module and type batch held it, else null). A run over many batches prints one line per batch, approved with its manifest path or refused with its reasons, then `<n> batches approved, <m> refused.`, and exits 0 when no batch was refused and 1 otherwise. With `--dry-run` it prints the plans, the guard failures, and the sample, and writes nothing.

`backfill` drafts Behavior records from code during adoption: for each use case, reaction, and job a mark places, it reads the repositories, dispatches, and refusal codes the code model already holds and drafts the missing frontmatter. A use case gains `changes` or `reads`, `effects` entries, and refusal results; a reaction gains `reads` and its edges; a job gains `reads` and its edges. Every touched record is marked transcribed. Existing entries are never removed or rewritten; only missing entries are appended. For screens, backfill is scoped per frontend and entry point, not per module: for each browser or device frontend it drafts one `screen` record per route of that frontend's entry points that carries no screen mark, with its entry point, its address, the use cases its route calls as `offers`, and the steps those use cases serve as `serves`, as a transcribed draft, and the `<module>` argument does not narrow that screen pass. With a module name it covers that module only for use cases, reactions, and jobs; without one it covers every module. The per-module completeness table covers use cases, reactions, and jobs only; screens appear in the plan lines per frontend and entry point. With `--dry-run` it prints the plan and writes nothing. It prints a per-module completeness table first, then one line per planned record, then the count of files updated and records backfilled.

The .NET stack reader links each code element to its record through its mark, in the style `marks` declares: a handler, an event handler, a job, a port, an adapter, an endpoint, an aggregate root and its state-changing methods, a domain event, a refusal type, and a test. A folder or a type name is never the link. A marked refusal type declares the `code` of the refusal result its rule names.

Aggregate transitions and data, and scenario examples, may be written as Markdown tables the builder parses, so the parts people debate read naturally even in the raw file. A front-matter field wins over a table with the same name. A handwritten transitions, data, or retention table that differs from the frontmatter reports `table-drift`.

The eight subcommands share one reader:

- `build` writes `model.json` under `.model/` in the consumer root, unless `--out` names another file. That folder holds generated output, and the consumer ignores it in Git. The file carries the build's check result as `findings` and `known`.
- `check` builds the model in memory and reports every finding with its level, code, message, path, and line. With `--staged` it reports only the findings in the staged scope: the records on pages changed in the git index, their direct links in both directions, and the records staged code implements. A finding is in scope when its path is staged or its subject is. Outside a git repository, or when git is unavailable, `--staged` exits 2.
- `render` rewrites the fenced generated regions and touches no text outside them.
- `query` prints one record with its links in both directions. Agents use it to load a record's context.
- `names` prints the per-module names inventory: the verbs in use with counts, the nouns after the verb, reaction facts, events, refusal codes, notification template keys, and setting names, with the lookalike groups. Agents read it without rendering the book.
- `backfill` drafts missing Behavior frontmatter from code during adoption, as above. On a module holding no use-case, reaction, or job records it exits 1.
- `diff` compares the working tree against a base. The base is a consumer folder holding `litespec.project.json`, or a git revision of the consumer. In a consumer, `<base>` is the consumer default branch, such as `main`, for a review diff, unless the review names a consumer folder or another git revision. Records match by identifier. `--intent` shows Intent changes in product words, `--behavior` shows Behavior changes and the security queue, and without either flag it shows both. The changelog prints REMOVED, then MODIFIED, then ADDED; each section groups entries by family: goals and requirements, domain, access, operations. The header counts breaking, removed, modified, and added changes. `text` is the changelog reviewers read, `markdown` is the pull request comment, and `json` carries every change with its fingerprint and the counts for machines. `--breaking-only` shows breaking changes and hides the rest.

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

Every check has a level. G reads records only and always blocks. S reads code through a stack reader: it blocks where the reader that produced the finding is qualified, and reports otherwise. Qualification is per reader: a reader qualifies when its handshake matches; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, including Blazor and Android screens, stays qualified. A finding no reader produced, a record finding, is qualified. A is advisory: it reports and never blocks. An S finding about a draft record reports without blocking and prints `draft`, and while `check.phase` is `frozen` a proof finding reports without blocking and prints `frozen`. A structural parent is any link or containment the Parents table and the Identifiers table on the [model page](../core/model.md) name as a record's parent (topic containment for a requirement, feature `solves`, journey `serves`, scenario `result`, screen `entryPoint`, and identifier containment); a missing structural parent is level G for every record, drafts included. Every other required link is coverage: level A for a tier 3 record and level G for a tier 1 record. The finding codes are:

| Code | Level | Meaning | Fix |
|:---|:---|:---|:---|
| `record-invalid` | G | A record fails the shape its type requires. | Correct the record to match the shape its type requires. |
| `identifier-invalid` | G | An identifier breaks the form its type requires, or a requirement topic segment differs from its page name. | Rename the identifier to the required form. |
| `identifier-duplicate` | G | One identifier names two records. | Give one of the records a new identifier. |
| `field-invalid` | G | A field holds a value outside its shape, or a record other than a decision carries `replaces`. | Write the field in its declared shape, or move `replaces` to a decision. |
| `reference-unknown` | G | A citation names no written record or shared provision of the installed release. | Point the citation at a written record or a provision of the installed release. |
| `link-unknown` | G | A Markdown link on a record page resolves to no file. | Point the link at a file that exists. |
| `owner-unknown` | G | A record names an owner or decider the owners list does not hold, or an owner no team owning its path claims. | Name a listed owner, or add the owner to the owners list. |
| `decider-missing` | G | A draft or a question entry names no decider or no decide-by date. | Name the decider and the decide-by date. |
| `approval-invalid` | G | An approved record rests on a draft decision. | Approve the decision first, or return the record to draft. |
| `product-count-invalid` | G | The project holds no single product record. | Write one product record. |
| `size-type-missing` | G | The size requires a type the project lacks. | Add a record of the missing type. |
| `requirement-fulfillment-missing` | G | A requirement is fulfilled by no rule, use case, reaction, job, notification or setting, unless it cites a planned, later or refused feature; a quality requirement is also fulfilled by a technical decision naming it in `basedOn`. | Fulfill the requirement from a Behavior record, or cite its planned, later or refused feature. |
| `requirement-measure-missing` | G (A for a statement naming a number with no `measure`) | A measured requirement names no check, or its check has no passing run; a statement naming a number with no `measure` reports at level A. | Name the check and record a passing run, or add the missing `measure`. |
| `requirement-cites-missing` | A (G for tier 1) | A requirement cites no goal, problem, feature, constraint, research or decision. | Cite the reason the requirement serves. |
| `constraint-unreached` | G | A constraint with `applies: true` is cited by no requirement. | Cite the constraint from a requirement, or set `applies` to false. |
| `decision-decides-missing` | G | A product decision names nothing it decides. | Name the records the decision decides. |
| `use-case-journey-missing` | G | A public use case sits in no journey and names no background caller. It runs at large size only. | Walk the use case in a journey, or name its background caller. |
| `state-unreached` | G | A state is reached by no transition. | Add a transition that reaches the state, or remove the state. |
| `transition-unknown` | G | A transition names a missing state or use case. | Point the transition at a written state and a written use case. |
| `use-case-success-missing` | G | A use case ends with no success result. | Add the success result to the use case. |
| `scenario-result-unknown` | G | A scenario ends in a result outside its own use case and outside the derived results. | End the scenario in a result of its own use case. |
| `calculation-table-missing` | G | A calculation rule has no scenario examples table. | Add the scenario examples table to the rule. |
| `table-drift` | A | A handwritten body table holds transitions, data fields or retention rows that differ from the frontmatter. | Keep the fact in frontmatter and render the table as a generated region. |
| `rule-name-invalid` | G | A rule statement negates what its name does not keep. | Keep the negation word in the rule name. |
| `known-gap-expired` | G | A known gap passed its end date with its check still red. | Fix the finding, or extend the gap with a new end date. |
| `gap-finding-unknown` | G | A known gap names a check that no check uses. | Name a finding code a check emits. |
| `aggregate-name-clash` | G | Two aggregates share one name across the project. | Give one aggregate a new name. |
| `use-case-name-clash` | G | Two use cases share one name across the project. | Give one use case a new name. |
| `use-case-verb-unknown` | G | A use-case name starts with a verb neither the baseline nor the project vocabulary declares; a retired synonym names its replacement. | Start the name with a declared verb, or declare the verb. |
| `use-case-verb-invalid` | G | A query opens with a changing verb, or a command opens with a reading verb. | Open the name with a verb of its kind. |
| `reaction-name-invalid` | G | A reaction name is not `<verb>-<what>-on-<fact>` with a declared verb. | Rename the reaction to `<verb>-<what>-on-<fact>` with a declared verb. |
| `job-name-invalid` | G | A job name does not start with the verb of a use case it issues. | Start the job name with the verb of a use case it issues. |
| `term-clash` | A | Two terms differ only in hyphenation, a British or American spelling, or a compound split, and both are in use. | Keep one term and update the other use. |
| `statement-word-invalid` | A | A rule statement, scenario step or result meaning uses a word that hides behavior, such as `handle`, `process` or `etc.`. | Replace the vague word with the concrete act. |
| `value-invalid` | A | A record field holds a value that breaks the value style. | Rewrite the value in the value style the finding names. |
| `use-case-noun-unknown` | A | The words after a use case's verb name no aggregate, declared aggregate part, or glossary term. | Name an aggregate, a declared aggregate part, or a glossary term after the verb. |
| `refusal-code-invalid` | A | A refusal code is not `<module>.<noun>-<state>` with the record's module or a declared generic prefix, or it uses a banned vague word. | Rewrite the code as `<module>.<noun>-<state>`, without a vague word. |
| `template-key-invalid` | A | A notification template key ends with no entry of `addresses`. | End the key with an entry of `addresses`. |
| `setting-unit-missing` | A | A setting whose value is a number or a duration declares no `unit`. | Declare the `unit` of the setting. |
| `integration-event-name-invalid` | G | An outbound integration event is named with an internal word such as `staged`, `dispatch` or `outbox`. | Rename the event without the internal word. |
| `reaction-trigger-unknown` | G | A reaction names a trigger no domain event or integration event carries. | Point the trigger at a written domain event or integration event. |
| `erasure-undeclared` | G | A personal or sensitive field stands outside a declared erasure position. | Declare the erasure position of the field. |
| `record-link-missing` | G | A use case, reaction, job, notification or setting names neither `serves` nor `fulfills`. | Link the record with `serves` or `fulfills`. |
| `record-parent-missing` | G | A requirement topic names no topic record, a feature solves no problem, a journey serves no goal, a scenario names no result, a screen names no entryPoint, or a rule, event, or module-homed record names an identifier home no record holds. | Name the missing parent. |
| `feature-goal-missing` | A (G for tier 1) | A feature that is not refused advances no goal. | Advance a goal from the feature, or record the refusal. |
| `result-scenario-missing` | A (G for success results) | A written result is shown by no scenario. | Write a scenario that shows the result. |
| `step-use-case-missing` | G | A journey step is served by no use case, reaction or job. Its pair `step-screen-missing` names a step a browser or device use case serves that no screen serves. | Serve the step from a use case, a reaction or a job. |
| `effect-loop-invalid` | G | The effect graph cycles outside a domain event with a stated end condition. | Break the cycle, or route it through a domain event with a stated end condition. |
| `consistency-cites-missing` | G | An atomic use case changing more than one aggregate names no rule in `cites`. | Cite the atomicity rule in `cites`. |
| `query-output-missing` | G | A query declares no `output`. | Declare the `output` of the query. |
| `paging-undeclared` | G | A collection query declares no `paging`. | Declare the `paging` of the query. |
| `widening-cites-missing` | G | A query group visible beyond its `reads` names no requirement in `cites`. | Cite the widening requirement in `cites`. |
| `retention-enforcement-missing` | G | A data class names no `enforcedBy`. | Name the enforcement in `enforcedBy`. |
| `risk-acceptance-missing` | G | A retained or exempt personal data class names no `acceptedBy` in Intent. | Record the acceptance in `acceptedBy`. |
| `exemption-expired` | G | An erasure exemption passed its `reviewBy` date. | Review the exemption and set a new `reviewBy` date. |
| `setting-requirement-missing` | G | A commercial, legal, or tenant-varying default fulfills no requirement. | Fulfill a requirement from the setting. |
| `reservation-release-missing` | G | A reservation rule names no `releasedBy`, or names never with no reason. | Name the releasing record in `releasedBy`, or never with a reason. |
| `notification-sender-missing` | G | A notification is sent by no reaction or use case. | Send the notification from a reaction or a use case. |
| `except-fallback-missing` | G | An `except` names no fallback requirement, or the fallback carries its own `except`. | Name a fallback requirement that carries no `except` of its own. |
| `approval-reopened` | G | A semantic hash differs from the recorded `approvedHash`. | Re-approve the record. |
| `parent-reopened` | A (G once approved) | A record cites, fulfills or serves a record whose semantic hash changed after the record was last approved. | Re-approve the record against the changed parent. |
| `transcription-status-invalid` | G | A transcribed record is reviewed while its parent is unapproved. | Approve the parent first, or return the record to draft. |
| `transcription-expired` | G | A transcribed record remains after the adoption decision is approved. | Adopt the record, or remove it. |
| `job-guard-missing` | G | An operator job states no `guard`. | State the `guard` of the job. |
| `schedule-invalid` | G | A job schedule is neither an `every` duration nor a `cron` expression. | Write the schedule as an `every` duration or a `cron` expression. |
| `behavior-unrecorded` | S | A refusal code in a declaring position, a screen call to no endpoint, an engine step that maps to no use case, or (the frontend case) a route with no screen mark or a mark naming no screen is named by no record. | Delete the code, or write its parent record from a requirement, drafting the requirement when none exists. |
| `mark-missing` | S | A code element the marks table names carries no mark. | Add the mark the marks table names. |
| `mark-unknown` | S | A mark names no record, or a record of a type its code element does not take, or sits on an element that takes no mark; or a `follows` mark names no provision of the carried release, no decision or deviation, a retired record, or a provision on a page the project's stacks do not apply. | Point the mark at a record its element takes, or remove the mark. |
| `decision-unimplemented` | S | A technical decision under `docs/implementation/decisions/` is followed by no code element. | Follow the decision from a code element, or retire the decision. |
| `decision-basis-missing` | A | A technical decision names no constraint or requirement in `basedOn`. | Name the constraint or requirement in `basedOn`. |
| `constituent-parent-missing` | A | An unmarked Domain type sits in a folder that holds no aggregate root, under no aggregate folder, and outside the shared kernel. | Move the type under its aggregate, or mark it. |
| `mark-count-invalid` | S | A code element carries more or fewer marks than its kind takes in the marks table, or names one record twice. | Carry the marks the marks table names, each once. |
| `mark-style-invalid` | S | A mark is written in the style the project did not declare. | Rewrite the mark in the declared style. |
| `runtime-value-drift` | S | A hand-written event `Classification` or exception `Code` differs from what its record states. | Match the value to what the record states. |
| `act-event-undeclared` | S | A root method marked with a use case raises an event the use case does not list in `raises`. | List the event in `raises`, or stop raising it. |
| `act-event-unimplemented` | S | A use case lists an event in `raises` that no root method marked with it raises. | Raise the event from a marked root method, or remove it from `raises`. |
| `use-case-act-missing` | S | A command handler changing an entity or process aggregate calls no root method marked with its use case. | Call a root method marked with the use case. |
| `aggregate-unimplemented` | S | No aggregate root carries the mark of an entity or process aggregate. | Mark the aggregate root. |
| `repository-unimplemented` | S | An aggregate a use case changes has no Infrastructure repository implementing its domain repository. | Write the Infrastructure repository. |
| `reaction-unimplemented` | S | No event handler carries the reaction mark. | Write the event handler, or remove the reaction. |
| `job-unimplemented` | S | No job host carries the job mark. | Write the job host, or remove the job. |
| `external-system-unimplemented` | S | No port carries the external system mark. | Write the port, or remove the external system. |
| `use-case-endpoint-missing` | S | A use case reached through a `browser` or `api` entry point has no endpoint carrying its mark. | Expose the endpoint and carry the use case mark. |
| `effect-unimplemented` | A | A declared effect edge is taken by no code. | Take the edge in code, or remove the declaration. |
| `use-case-unreached` | A | A use case reached through a browser or device entry point is offered by no screen of that entry point. It reports and does not block: a use case with no screen does not block a check or a release. | Offer the use case from a screen of the entry point. |
| `screen-unimplemented` | S | No route carries the screen's mark. | Carry the screen mark from the route. |
| `screen-offers-drift` | S | A screen's `offers` differs from the use cases its route reaches through the generated client. | Match `offers` to the use cases the route reaches. |
| `screen-serves-drift` | G | A screen serves a journey step no use case it offers serves. | Serve the step from an offered use case, or stop serving it. |
| `step-screen-missing` | G | A journey step a browser or device use case serves is served by no screen. Its pair `step-use-case-missing` names a step no use case serves. | Serve the step from a screen. |
| `refusal-sentence-missing` | A | A refusal a screen can reach has neither a dictionary sentence nor a browser test asserting its code. | Write the dictionary sentence and the browser test. |
| `reader-missing` | S | A frontend reader could not run: Node is absent, its handshake names another protocol, or it failed; the finding names the cause, and that frontend's S findings report instead of blocking. | Restore the reader: install Node, match the handshake, or fix the failure the finding names. |
| `use-case-unimplemented` | S | No code implements the use case and no known gap covers it. | Implement the use case, or record a known gap. |
| `refusal-unimplemented` | S | A refusal result names a code the code never throws, or a code matching a `refusals.composed` template whose key no code declares. | Throw the named code, or remove the refusal result. |
| `proves-duplicate` | A | A scenario carries `proves` marks on more than one test. | Keep the `proves` mark on one test. |
| `proves-unreached` | S | No proving step of a claimed scenario reaches its use case, no assertion matches its refusal by code, client alias, or HTTP status, and no integration fact drives its use case or rule. The frontend case: a browser test reaches no screen offering the claimed scenario's use case by a `page.goto` literal or template matching the screen's `address`, or no assertion in the same test holds the refusal code or the success result's declared text or code. A step reaches a use case when it belongs to the use case's steps class, calls its generated-client operation, requests its endpoint route through its own helpers, or looks its argument up in a probe table of routes in its steps class. A scenario proves the use case a when step reaches; the use case a given step reaches, when a later when step and then step observe it; or a use case a when step sets off through a declared producer, whose effects issue it or whose event a reaction issuing it reacts to, with an effect asserted. Codes and statuses asserted inside a then step's method count as the scenario's own. | Prove the scenario from a test that reaches its use case and asserts its outcome. |
| `use-case-scenario-missing` | S | A use case has no scenario. It runs at large size only. | Write a scenario for the use case. |
| `write-undeclared` | S | The code writes an aggregate outside `changes`. | Declare the aggregate in `changes`, or stop writing it. |
| `result-undeclared` | S | The code returns an outcome no result declares. | Declare the outcome in a result, or stop returning it. |
| `effect-undeclared` | S | The code takes an edge no record declares, naming a one-hop helper as `via 'Type.Method'`. | Declare the edge on the record, or stop taking it. |
| `prose-missing` | A | A use case needing Goal and What Happens prose carries none. | Write the Goal and What Happens prose. |
| `atomic-group-duplicate` | A | One command saves more than one atomic write group. | Save one atomic write group per command, or extract a saga. |
| `question-expired` | A | An unanswered question stands past its `decideBy` date and cannot auto-approve. | Answer the question, or record a new `decideBy` date. |
| `use-case-method-drift` | A | A command handler calls aggregate methods, none starting with the use-case verb. | Start an aggregate method with the use-case verb. |
| `event-tense-invalid` | A | An aggregate method raises an event not ending with the past tense of its verb. | End the event name with the past tense of the method verb. |
| `name-drift` | A | A message type, operation id or audit action differs from the use-case name. | Match the name to the use-case name. |
| `operation-id-duplicate` | S | Two endpoints declare one operation id, which the API document and every generated client refuse. | Give one endpoint a new operation id. |
| `operation-use-case-drift` | S | An operation in the committed contract `paths.contract` names carries no `x-litespec-use-case`, or names another use case than its endpoint's mark; an unreadable contract reports once. | Name the endpoint use case in `x-litespec-use-case`, or fix the unreadable contract. |
| `alert-link-missing` | S | An alert rule in `paths.alerts` has no `requirement` label naming a quality requirement or no `runbook` label naming a runbook. | Label the alert rule with the requirement and the runbook. |
| `alert-threshold-drift` | A | An alert rule's own text carries none of the decimal numbers its requirement's `measure` names. | Carry a decimal number from the `measure` in the alert text. |
| `quality-alert-missing` | G | A quality requirement no alert names, whose `check` matches no measured-check or check result, and no open known gap covers. | Name the requirement from an alert, or record a known gap. |
| `setting-key-missing` | G | A setting with `setBy: operator` names no `key`. | Name the `key` of the setting. |
| `setting-unimplemented` | S | A setting with a `key` is marked by no options class. | Mark an options class with the setting. |
| `setting-key-drift` | S | A marked options class's `SectionName` and property do not spell the setting's `key`. | Spell the setting `key` in the `SectionName` and property. |
| `template-unimplemented` | S | A notification declares a `template` key the template store lacks. | Add the key to the template store, or remove it from the notification. |
| `template-unrecorded` | S | The template store holds a key no notification declares. | Declare the key on a notification, or remove it from the store. |
| `template-param-drift` | S | A notification declares a parameter none of its template's texts uses as a placeholder. | Use the parameter as a placeholder in the template text, or remove it. |
| `dependency-undeclared` | S | A direct package in the bill of materials is pinned by no manifest entry and admitted by no decision. | Pin the package in the manifest and admit it in a decision. |
| `dependency-version-drift` | S | A direct package ships at a version other than its manifest pin. | Ship the package at its manifest pin. |
| `dependency-license-unknown` | S | A direct package's license is not in `dependencies.licenses` and no `admits` entry names it. | List the license in `dependencies.licenses`, or name the package in an `admits` entry. |
| `admission-invalid` | G | An `admits` entry misses its layer, license, location, account owner, failure behavior, or operating cost. | Complete the `admits` entry with its layer, license, location, account owner, failure behavior, and operating cost. |
| `route-invalid` | A | A command route ends outside its verb, or a route carries no aggregate plural. | End the command route at its verb and carry the aggregate plural. |
| `name-reason-missing` | S | A folder or type is named `Shared`, `Common`, `Helper`, `Manager` or the like outside the declared shared kernel. | Rename the folder or type without the shared word, or declare the shared kernel. |
| `collaborator-repository-invalid` | A | A class built with `new` takes a repository in its own constructor. | Stop passing a repository to a class built with `new`. |
| `repository-read-invalid` | A | A repository read sits inside a `for`, `foreach` or `while` body. | Move the repository read out of the loop body. |
| `query-filter-invalid` | A | A query handler filters a repository list with LINQ in memory. | Filter in the repository instead of in memory. |
| `dependency-count-invalid` | A | A handler, reaction or job takes more than ten constructor dependencies. | Split the handler, reaction, or job until it takes ten dependencies or fewer. |
| `effect-timing-invalid` | S | A command use case declares a `sends` or `calls` effect with `inline` timing. | Send or call after the commit, from an event handler or a job. |
| `partial-missing` | S | A marked domain event or rule exception is not declared `partial`, so the generator cannot add its runtime value. | Declare the domain event or rule exception `partial`. |

The `render` subcommand writes these generated regions, each fenced by `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`: `history`, `use-case-coverage`, `sub-processors`, `runbook-index`, `rule-map`, `operations-log`, `open-questions`, `journeys`, `glossary`, `deviations`, `census`, `aggregate-map`, `audit-register`, `alert-inventory`, and `counts`.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<id>` | for `query` | Prints the record with this identifier and its links both ways. |
| `<base>` | for `diff` | The consumer folder or git revision to compare against; in a consumer, the default branch, such as `main`, for a review diff. |
| `<module>` | for `backfill` | Backfills this module only for use cases, reactions, and jobs; without it, every module. Screen backfill runs per frontend and entry point either way. |

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
| `--batch <selector>` | none | Only `approve` reads it. May be given more than once; each value approves one batch, in the order given: `use-case/<module>.<name>`, `decision/<id>`, a tier 1 record's own id, or `<module>/<type>`. |
| `--batches <file>` | none | Only `approve` reads it. Names a text file with one batch selector per line; blank lines and lines starting with `#` are ignored. A second field on a line names that batch's review report and overrides `--review-report` for that batch. |
| `--second-reviewer <owner>\|agent` | none | Only `approve --batch` reads it. Names the second reading of a tier 1 batch: another owner, or `agent` with `--review-report`. It applies to every batch in the run. |
| `--review-report <file>` | none | Only `approve --batch` reads it, with `--second-reviewer agent`. The agent review report the manifest cites; the file must exist. A report named in a `--batches` file overrides it for that batch. |
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

A web frontend is read by a TypeScript reader that ships with the tool as one bundled script: the tool runs it with Node once per build or check, sends one versioned JSON request on standard input, and reads one versioned JSON facts document back, so a consumer installs nothing beyond Node itself. The reader opens with a handshake naming the protocol version, the reader, and the TypeScript and Svelte versions; a version mismatch, an absent Node, or a reader failure produces the single `reader-missing` finding for that frontend instead of passing silently. A finding records the reader that produced its facts, and its level takes that reader's qualification: a reader qualifies when its handshake matches; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, which reads Blazor and Android frontends, stays qualified. A finding no reader produced, a record finding, is qualified.

The builder reads `paths`, `shape`, and `stacks` from the consumer's `litespec.project.json`. It reads shared provisions from the release the tool carries, the record owners from the CODEOWNERS file at `.github/CODEOWNERS`, `CODEOWNERS`, or `docs/CODEOWNERS`, and the test run from TRX or JUnit XML files under `paths.testResults`. A git diff base is read through an archive into a temporary folder, which is removed after the diff. Nothing beneath it is separately runnable.
