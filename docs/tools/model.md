# Model Builder

## Name

`litespec model` checks the project model, formats record files, renders generated outputs, and answers record queries.

## Synopsis

```bash
litespec model check [--root <consumer>] [--format json] [--release] [--since <ref>] [--all]
litespec model format [--root <consumer>] [--check]
litespec model render [--root <consumer>]
litespec model query [--root <consumer>] <id>
```

## Description

The model builder reads the written records, the code marks, the routes, the tests, and the latest test run into one model. The model holds every record, each with its identifier and its qualified identifier, and its resolved links. It names the code, screens, tests, and test runs it read. It adds the computed `implements`, `calls`, and `proves` links and the computed `implemented`, `verified`, `stale`, and `reachable` state of each record. Computed states are never written on a record; the book shows them. The model also lists each unmarked Domain constituent type under the aggregate whose folder holds it, or under `kernel`, in `constituents`.

Each screen lists its `operations`, its `useCases`, and its `calls`, where each call pairs one operation with the use case it reaches. The `calls` links carry one link per such pair, so a use case reached through two operations gets two links, and a use case whose operation is unknown carries an empty `operation`.

A proof goes stale when its scenario changes after the proof passed. `.litespec/proof/proofs.json` is committed: for each scenario it holds the scenario hash and its test body's hash at the last passing run. On every later build, a scenario whose hash or test body differs from the recorded one has a stale proof, and a stale proof verifies nothing until a newer passing run is read. A test that both passed and failed within the runs read reports `proof-invalid`. Run the model check after the test run, in the same job, so the hash is recorded before a record changes.

The .NET stack reader links each code element to its record through its mark, in the style `marks` declares: a handler, an event handler, a job host, a port, an adapter, an endpoint, an aggregate root and its state-changing methods, a domain event, a refusal type, and a test. A folder or a type name is never the link. A marked refusal type declares the `code` of the refusal result its rule names.

The four subcommands share one reader:

- `check` builds the model in memory and reports every finding with its level, code, message, path, and line. It runs only the findings of the conformance level the project claims in `conformance`, in both the model checks and the consumer pass, as the finding table below states per code. It runs the consumer pass over the project file, the record pages, the glossary, and the controlled UI, and reports those findings in the same shape, so one command and one CI step hold the whole consumer. In text it lists every open blocking finding in full, then one line per finding code with its level and count ordered by level (G, then S, then A) and then by count, such as `record-invalid G 3`, and the totals line. By default the level A findings are summarized in those counts and not listed one by one; `--all` lists them. With `--format json` it writes one JSON object: the `findings` array holds every finding the run shows, and the `known` array holds every finding an exception covers. With `--release` the release-readiness findings block: `use-case-unverified`. With `--since <ref>` only the findings about records and code changed since `<ref>` block, and the rest report; it also reports every client-breaking contract change since `<ref>`: a removed result, a removed refusal code, and a changed integration event: each as a `contract-break` finding of level S, which always blocks at the level that runs it.
- `format` rewrites every record file under the record roots, and the glossary file, into canonical form. With `--check` it writes nothing and lists each file that would change.
- `render` rewrites the fenced generated regions in explanation files and documents, touching no text outside the fences, and writes `litespec-records.ts` into each React and Svelte frontend frame.
- `query` prints one record with its links in both directions, showing both its identifier and its qualified identifier. Agents use it to load a record's context.

Each finding in that object carries these fields. A continuous integration job uses `outcome` to decide what blocked the run, because the level alone does not decide it.

| Field | Holds |
|:---|:---|
| `code` | The finding code, such as `record-invalid`. |
| `level` | The finding's level: G, S or A. |
| `severity` | The severity name for annotation tools: `error` for G, `warning` for S and `note` for A. |
| `outcome` | What the run does with the finding: `blocking` when it fails the run, `reported` when it prints and the run passes, and `known` when an exception covers it. |
| `reason` | `unchanged` when `--since` demoted a blocking finding outside the changed scope, and `null` otherwise. |
| `message` | What is wrong, in one sentence. |
| `path` | The path of the finding, relative to the consumer root. |
| `line` | The line number of the finding, counting from one. |
| `subject` | The identifier of the record the finding is about, and `null` when the finding concerns no record. |

A finding that an exception covers is reported as known and exits zero, as the [model page](../core/model.md) states under The Model Builder. An exception on a use case also covers the findings about the scenarios and results it holds. An expired exception fails the check.

Every check has a level. G reads records only and always blocks. S reads code through a stack reader: it blocks where the reader that produced the finding is qualified, and reports otherwise. A stack reader is the reader a stack selects to read its code, screens or tests. Qualification is per reader: a reader is qualified when it ran and its handshake matched; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, including Blazor and Android screens, stays qualified. A finding no reader produced, a record finding, is qualified. A is advisory: it reports and blocks only when `litespec model check --release` makes a release-readiness finding block. A structural parent is any link or containment the Parents table and the Identifiers table on the [model page](../core/model.md) name as a record's parent; a missing structural parent is level G. Every other required link is level G.

Every finding code also has a conformance level: the lowest claimed level that runs it, from what its check reads. A check that reads records alone runs at `records`. A check that reads code, screens, tests, test runs or measure results through a stack reader runs at `traced`. A check that reads LiteSpec's code layout, operations files, the bill of materials or the controlled UI runs at `full`. At `records` no stack reader runs. At `traced` the readers run and the layout group does not: the layout group is `code-placement-drift` and `mark-folder-drift`, the findings about LiteSpec's own code layout, each running only at `full`. A code with no row runs at `full`. The finding codes are:

| Code | Level | Conformance | Meaning | Fix |
|:---|:---|:---|:---|:---|
| `record-invalid` | G | records | A record file fails to parse, or a record fails the shape its type requires. | Correct the file so it parses and matches the shape its type requires. |
| `identifier-invalid` | G | records | An identifier breaks the form its type requires. | Rename the identifier to the required form. |
| `identifier-duplicate` | G | records | One identifier names two records. | Give one of the records a new identifier. |
| `identifier-folder-drift` | G | records | An identifier's module segment disagrees with its folder. | Rename the identifier to its folder's module and update every citation. |
| `name-duplicate` | G | records | One name segment names two records in the product. | Give one of the records a new name. |
| `field-invalid` | G | records | A field holds a value outside its shape. | Write the field in its declared shape. |
| `reference-unknown` | G | records | A citation names no written record or shared provision of the installed release. | Point the citation at a written record or a provision of the installed release. |
| `link-unknown` | G | records | A Markdown link in an explanation file or a document resolves to no file and cites no record. | Point the link at a file or a record that exists. |
| `product-count-invalid` | G | records | The project holds no single product record. | Write one product record. |
| `size-type-missing` | G | records | The size requires a type the project lacks. | Add a record of the missing type. |
| `requirement-fulfillment-missing` | G | records | A requirement is fulfilled by no rule, use case, reaction, job, notification or setting; a quality requirement is also fulfilled by a technical decision naming it in `basedOn`. | Fulfill the requirement from a Behavior record, or name a quality requirement from a technical decision in `basedOn`. |
| `requirement-measure-missing` | G | traced | A measured requirement names no check, or its check has no passing run; a statement naming a number with no `measure` reports at level A. | Name the check and record a passing run, or add the missing `measure`. |
| `requirement-cites-missing` | A | records | A requirement cites no goal, problem, feature, constraint, research or decision. | Cite the reason the requirement serves. |
| `constraint-unreached` | G | records | A constraint with `applies: true` is cited by no requirement. | Cite the constraint from a requirement, or set `applies` to false. |
| `intent-unreached` | G | records | An Intent record reaches no Behavior. | Reach Behavior from the record, or move the record to the backlog. |
| `decision-decides-missing` | G | records | A product decision names nothing it decides. | Name the records the decision decides. |
| `decision-number-duplicate` | G | records | Two decisions share one number. | Give one decision the next free number. |
| `decision-decides-duplicate` | G | records | Two decisions decide one record. | Keep one decision on the record and move the other. |
| `use-case-journey-missing` | G | records | A public use case sits in no journey, and no reaction, job or other use case runs it. It runs at large size only. | Walk the use case in a journey, or name what runs it. |
| `state-unreached` | G | records | A state is reached by no transition. | Add a transition that reaches the state, or remove the state. |
| `transition-unknown` | G | records | A transition names a missing state or use case. | Point the transition at a written state and a written use case. |
| `use-case-success-missing` | G | records | A use case ends with no success result. | Add the success result to the use case. |
| `scenario-result-unknown` | G | records | A scenario ends in a result outside its own use case and outside the derived results. | End the scenario in a result of its own use case. |
| `calculation-table-missing` | G | records | A calculation rule has no scenario examples. | Add the scenario examples to the rule. |
| `rule-name-invalid` | G | records | A rule statement negates what its name does not keep. | Keep the negation word in the rule name. |
| `exception-expired` | G | records | An exception stands past its expiresOn date. | Fix the finding, or extend the exception with a new expiresOn date. |
| `exception-finding-unknown` | G | records | An exception names a finding code no check emits. | Name a finding code a check emits. |
| `job-name-invalid` | G | records | A job name does not start with the verb of a use case it runs. | Start the job name with the verb of a use case it runs. |
| `template-key-invalid` | A | records | A notification template key ends with no entry of `addresses`. | End the key with an entry of `addresses`. |
| `setting-unit-missing` | A | records | A setting whose value is a number or a duration declares no `unit`. | Declare the `unit` of the setting. |
| `integration-event-name-invalid` | G | records | An outbound integration event is named with an internal word such as `staged`, `dispatch` or `outbox`. | Rename the event without the internal word. |
| `reaction-trigger-unknown` | G | records | A reaction names a trigger no domain event or integration event carries. | Point the trigger at a written domain event or integration event. |
| `erasure-undeclared` | G | records | A personal or sensitive field stands outside a declared erasure position. | Declare the erasure position of the field. |
| `record-link-missing` | G | records | A use case, reaction, job, notification or setting names neither `serves` nor `fulfills`. | Link the record with `serves` or `fulfills`. |
| `record-parent-missing` | G | records | A feature `solves` names no problem, a journey `serves` names no goal, a scenario names no result, a screen `entryPoint` names no entry point, or a result, rule, event, or module-homed record names an identifier home no record holds. | Name the missing parent. |
| `folder-invalid` | G | records | A folder sits outside the closed set of its layer and level. | Move the folder into the closed set its layer and level allow, or remove it. |
| `module-undeclared` | G | records | A `modules/<m>/` folder names no declared module. | Declare the module with a module record, or remove the folder. |
| `record-placement-invalid` | G | records | A record sits outside the folder and level its type allows, or a `module.yaml` holds rules. | Move the record to its home, or move the rules to a rule-set file. |
| `role-orphaned` | G | records | A role is held by no actor. | Hold the role from an actor, or remove the role. |
| `permission-orphaned` | G | records | A permission is allowed by no role. | Allow the permission from a role, or remove the permission. |
| `problem-sufferer-invalid` | G | records | A problem names a sufferer outside the actors. | Name a sufferer the actors hold. |
| `problem-evidence-missing` | G | records | A problem cites no evidence. | Cite the evidence of the problem. |
| `actor-unused` | A | records | An actor is named by no journey or problem. | Walk the actor in a journey, name it in a problem, or remove the actor. |
| `goal-mover-missing` | G | records | No use case moves the goal measure. | Move the measure from a use case. |
| `term-missing` | A | records | A glossary term appears in no record summary or statement. | Use the term in a summary or statement, or remove it. |
| `result-scenario-missing` | A | records | A written result is shown by no scenario; a success result reports at level G. | Write a scenario that shows the result. |
| `step-use-case-missing` | G | records | A journey step is served by no use case, reaction or job. Its pair `step-screen-missing` names a step a browser or device use case serves that no screen serves. | Serve the step from a use case, a reaction or a job. |
| `effect-loop-invalid` | G | records | The effect graph cycles outside a domain event with a stated end condition. | Break the cycle, or route it through a domain event with a stated end condition. |
| `module-dependency-invalid` | G | records | Module `dependsOn` links form a cycle. | Break the cycle so one module stops depending on the other. |
| `link-locality-invalid` | G | records | Within one layer, a module record is linked from another module outside a declared `dependsOn` or an integration event. | Move the record to product level, or declare the module tie. |
| `event-publisher-invalid` | G | records | An integration event has more than one publishing module. | Keep one publishing module and read the event from the other modules. |
| `consistency-cites-missing` | G | records | An atomic use case changing more than one aggregate names no rule in `cites`. | Cite the atomicity rule in `cites`. |
| `query-output-missing` | G | records | A query declares no `output`. | Declare the `output` of the query. |
| `paging-undeclared` | G | records | A collection query declares no `paging`. | Declare the `paging` of the query. |
| `widening-cites-missing` | G | records | A query group visible beyond its `reads` names no requirement in `cites`. | Cite the widening requirement in `cites`. |
| `retention-enforcement-missing` | G | records | A data class names no `enforcedBy`. | Name the enforcement in `enforcedBy`. |
| `setting-requirement-missing` | G | records | A commercial, legal, or tenant-varying default fulfills no requirement. | Fulfill a requirement from the setting. |
| `reservation-release-missing` | G | records | A reservation rule names no `releasedBy`, or names never with no reason. | Name the releasing record in `releasedBy`, or never with a reason. |
| `notification-sender-missing` | G | records | A notification is sent by no reaction, job or use case. | Send the notification from a reaction, a job or a use case. |
| `except-fallback-missing` | G | records | An `except` names no fallback requirement, or the fallback carries its own `except`. | Name a fallback requirement that carries no `except` of its own. |
| `job-guard-missing` | G | records | A manual job states no `guard`. | State the `guard` of the job. |
| `schedule-invalid` | G | records | A job schedule is neither an `every` duration nor a `cron` expression. | Write the schedule as an `every` duration or a `cron` expression. |
| `atomic-group-drift` | G | records | An atomic use case's `changes` differ from its atomicity rule's `binds`. | Match `changes` to the rule's `binds`, or update the rule. |
| `atomic-group-invalid` | A | records | An atomic group binds more than 4 aggregates. | Extract a saga for the group. |
| `behavior-unrecorded` | S | traced | A refusal code in a declaring position, a screen call to no endpoint, or (the frontend case) a route with no screen mark or a mark naming no screen is named by no record. | Delete the code, or write its parent record from a requirement, drafting the requirement when none exists. |
| `mark-missing` | S | traced | A code element the marks table names carries no mark. | Add the mark the marks table names. |
| `mark-unknown` | S | traced | A mark names no record, or a record of a type its code element does not take, or sits on an element that takes no mark; or a `follows` mark names no provision of the carried release, no decision or exception, or a provision on a page the project's stacks do not apply. | Point the mark at a record its element takes, or remove the mark. |
| `decision-unimplemented` | S | traced | A technical decision under `.litespec/implementation/decisions/` is followed by no code element. | Follow the decision from a code element, or retire the decision. |
| `decision-basis-missing` | A | records | A technical decision names no constraint or requirement in `basedOn`. | Name the constraint or requirement in `basedOn`. |
| `constituent-parent-missing` | A | traced | An unmarked Domain type sits in a folder that holds no aggregate root, under no aggregate folder, and outside the shared kernel. | Move the type under its aggregate, or mark it. |
| `mark-count-invalid` | S | traced | A code element carries more or fewer marks than its kind takes in the marks table, or names one record twice. | Carry the marks the marks table names, each once. |
| `mark-style-invalid` | S | traced | A C# mark is written in the style the project did not declare. | Rewrite the mark in the declared style. |
| `mark-folder-drift` | S | full | A handler, endpoint, event handler, job host or aggregate root marks a use case, reaction, job or aggregate outside the module folder it sits in, or a handler or endpoint marks a use case outside the use-case folder it sits in. | Move the element to the folder its mark names, or point the mark at the record its folder holds. |
| `code-placement-drift` | S | full | A code element, test, or folder sits outside its record's qualified path, or a folder names no declared record. | Move the element to the folder its mark names, or point the mark at the record its folder holds. |
| `runtime-value-drift` | S | traced | A hand-written event `Classification` or exception `Code` differs from what its record states. | Match the value to what the record states. |
| `act-event-undeclared` | S | traced | A root method marked with a use case raises an event the use case does not list in `raises`. | List the event in `raises`, or stop raising it. |
| `act-event-unimplemented` | S | traced | A use case lists an event in `raises` that no root method marked with it raises. | Raise the event from a marked root method, or remove it from `raises`. |
| `use-case-act-missing` | S | traced | A command handler changing an entity or saga aggregate calls no root method marked with its use case. | Call a root method marked with the use case. |
| `aggregate-unimplemented` | S | traced | No aggregate root carries the mark of an entity or saga aggregate. | Mark the aggregate root. |
| `repository-unimplemented` | S | full | An aggregate a use case changes has no Infrastructure repository implementing its domain repository. | Write the Infrastructure repository. |
| `reaction-unimplemented` | S | traced | No event handler carries the reaction mark. | Write the event handler, or remove the reaction. |
| `job-unimplemented` | S | traced | No job host carries the job mark. | Write the job host, or remove the job. |
| `external-system-unimplemented` | S | traced | No port carries the external system mark. | Write the port, or remove the external system. |
| `use-case-endpoint-missing` | S | traced | A use case reached through a `browser` or `api` entry point has no endpoint carrying its mark. | Expose the endpoint and carry the use case mark. |
| `effect-unimplemented` | A | traced | A declared effect edge is taken by no code. | Take the edge in code, or remove the declaration. |
| `use-case-unreached` | A | traced | A use case reached through a browser or device entry point is offered by no screen of that entry point. It reports and does not block: a use case with no screen does not block a check or a release. | Offer the use case from a screen of the entry point. |
| `screen-unimplemented` | S | traced | No route carries the screen's mark. | Carry the screen mark from the route. |
| `screen-offers-drift` | S | traced | A screen's `offers` differs from the use cases its route reaches through the generated client. | Match `offers` to the use cases the route reaches. |
| `screen-serves-drift` | G | traced | A screen serves a journey step no use case it offers serves. | Serve the step from an offered use case, or stop serving it. |
| `step-screen-missing` | G | traced | A journey step a browser or device use case serves is served by no screen. Its pair `step-use-case-missing` names a step no use case serves. | Serve the step from a screen. |
| `refusal-sentence-missing` | A | traced | A refusal a screen can reach has neither a dictionary sentence nor a browser test asserting its code. | Write the dictionary sentence and the browser test. |
| `reader-missing` | S | traced | A reader could not read what it was given: a frontend reader could not run (Node is absent, its handshake names another protocol, or it failed), a .NET reader could not read or parse a backend, test or screen file, a declared stack root resolved to no folder, a `server` project selected no backend reader, or a configured bill of materials was missing, not JSON, not SPDX or CycloneDX, or yielded no packages. The finding names the cause and the file or path; a frontend's S findings report instead of blocking. | Restore the reader: install Node, match the handshake, or fix the failure the finding names. |
| `use-case-unimplemented` | S | traced | No code implements the use case and no exception covers it. | Implement the use case, or record an exception. |
| `refusal-unimplemented` | S | traced | A refusal result names a code the code never throws, or a code matching a `refusals.composed` template whose key no code declares. | Throw the named code, or remove the refusal result. |
| `proves-duplicate` | A | traced | A scenario carries `proves` marks on more than one test. | Keep the `proves` mark on one test. |
| `proves-unreached` | S | traced | No proving step of a claimed scenario reaches its use case, no assertion matches its refusal by code, client alias, or HTTP status, and no integration fact drives its use case or rule. The frontend case: a browser test reaches no screen offering the claimed scenario's use case by a `page.goto` literal or template matching the screen's `address`, or no assertion in the same test holds the refusal code or the success result's declared text or code. A step reaches a use case when it belongs to the use case's steps class, calls its generated-client operation, requests its endpoint route through its own helpers, or looks its argument up in a probe table of routes in its steps class. A scenario proves the use case a when step reaches; the use case a given step reaches, when a later when step and then step observe it; or a use case a when step sets off through a declared producer, whose effects run it or whose event a reaction running it reacts to, with an effect asserted. Codes and statuses asserted inside a then step's method count as the scenario's own. | Prove the scenario from a test that reaches its use case and asserts its outcome. |
| `use-case-scenario-missing` | S | records | A use case has no scenario. It runs at large size only. | Write a scenario for the use case. |
| `use-case-unverified` | A | traced | A use case holds scenarios with no passing proof. | Prove every scenario through the public boundary. |
| `proof-invalid` | A | traced | A test passed and failed within the runs read. | Stabilize the test so it passes on every run. |
| `write-undeclared` | S | traced | The code writes an aggregate outside `changes`. | Declare the aggregate in `changes`, or stop writing it. |
| `result-undeclared` | S | traced | The code returns an outcome no result declares. | Declare the outcome in a result, or stop returning it. |
| `effect-undeclared` | S | traced | The code takes an edge no record declares, naming a one-hop helper as `via 'Type.Method'`. | Declare the edge on the record, or stop taking it. |
| `prose-missing` | A | records | A record holds no `summary` field, or a goal, problem, requirement or rule holds no `statement` field. | Write the missing `summary` or `statement` field. |
| `explanation-orphaned` | A | records | A `.md` file under a record root has no frontmatter and no record file beside it, and is not `README.md`. | Pair the file with a record file of the same name, or give it a document frontmatter block. |
| `explanation-missing` | A | records | A research or decision record has no explanation file, or its explanation lacks a required section. | Write the explanation file with its required sections. |
| `format-drift` | A | records | A record file differs from canonical form. | Run `litespec model format`. |
| `source-link-bare` | A | records | A research explanation links a URL directly instead of citing a source key. | Cite the source with a `[text][key]` reference link. |
| `source-key-unknown` | A | records | A research explanation cites a source key the record's `sources` do not hold. | Point the link at a declared source key, or add the source. |
| `source-unused` | A | records | A research source's `key` is cited nowhere in its explanation. | Cite the source from the explanation, or remove the source. |
| `atomic-group-duplicate` | A | records | One command saves more than one atomic write group. | Save one atomic write group per command, or extract a saga. |
| `operation-id-duplicate` | S | full | Two endpoints declare one operation id, which the API document and every generated client refuse. | Give one endpoint a new operation id. |
| `operation-use-case-drift` | S | full | An operation in the committed contract `paths.contract` names carries no `x-litespec-use-case`, or names another use case than its endpoint's mark; an unreadable contract reports once. | Name the endpoint use case in `x-litespec-use-case`, or fix the unreadable contract. |
| `contract-break` | S | full | A result or refusal code the `--since` base held no longer exists, or an integration event changed since the base. It runs on `check --since` only. | Keep the result, refusal code and integration event the base published. |
| `analyzer-lowering-unrecorded` | S | traced | A LiteSpec analyzer rule is lowered or switched off in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable` or `[SuppressMessage]` while no live exception covers the provision the rule enforces. | Remove the lowering, or record an exception for the provision. |
| `alert-link-missing` | S | full | An alert rule in `paths.alerts` has no `requirement` label naming a quality requirement or no `runbook` label naming a runbook. | Label the alert rule with the requirement and the runbook. |
| `alert-threshold-drift` | A | full | An alert rule's own text carries none of the decimal numbers its requirement's `measure` names. | Carry a decimal number from the `measure` in the alert text. |
| `quality-alert-missing` | G | full | A quality requirement no alert names, whose `check` matches no measured-check or check result, and no open exception covers. | Name the requirement from an alert, or record an exception. |
| `runbook-unused` | A | full | A runbook no alert, job or use case names. | Name the runbook from an alert, a job or a use case, or remove the runbook. |
| `setting-key-missing` | G | records | A setting with `setBy: operator` names no `key`. | Name the `key` of the setting. |
| `setting-unimplemented` | S | traced | A setting with a `key` is marked by no options class. | Mark an options class with the setting. |
| `setting-key-drift` | S | traced | A marked options class's `SectionName` and property do not spell the setting's `key`. | Spell the setting `key` in the `SectionName` and property. |
| `template-unimplemented` | S | traced | A notification declares a `template` key the template store lacks. | Add the key to the template store, or remove it from the notification. |
| `template-unrecorded` | S | traced | The template store holds a key no notification declares. | Declare the key on a notification, or remove it from the store. |
| `template-param-drift` | S | traced | A notification declares a parameter none of its template's texts uses as a placeholder. | Use the parameter as a placeholder in the template text, or remove it. |
| `dependency-undeclared` | S | full | A direct package in the bill of materials is pinned by no manifest entry and admitted by no decision. | Pin the package in the manifest and admit it in a decision. |
| `dependency-version-drift` | S | full | A direct package ships at a version other than its manifest pin. | Ship the package at its manifest pin. |
| `dependency-license-unknown` | S | full | A direct package's license is not in `dependencies.licenses` and no `admits` entry names it. | List the license in `dependencies.licenses`, or name the package in an `admits` entry. |
| `admission-invalid` | G | records | An `admits` entry misses its layer, license, location, account owner, failure behavior, or operating cost. | Complete the `admits` entry with its layer, license, location, account owner, failure behavior, and operating cost. |
| `name-reason-missing` | S | traced | A folder or type is named `Shared`, `Common`, `Helper`, `Manager` or the like outside the declared shared kernel. | Rename the folder or type without the shared word, or declare the shared kernel. |
| `collaborator-repository-invalid` | A | full | A class built with `new` takes a repository in its own constructor. | Stop passing a repository to a class built with `new`. |
| `repository-read-invalid` | A | full | A repository read sits inside a `for`, `foreach` or `while` body. | Move the repository read out of the loop body. |
| `query-filter-invalid` | A | traced | A query handler filters a repository list with LINQ in memory. | Filter in the repository instead of in memory. |
| `effect-timing-invalid` | S | records | A command use case declares a `sends` or `calls` effect with `inline` timing. | Send or call after the commit, from an event handler or a job. |
| `partial-missing` | S | full | A marked domain event or rule exception is not declared `partial`, so the generator cannot add its runtime value. | Declare the domain event or rule exception `partial`. |
| `project-invalid` | G | records | The project file breaks the project schema, holds an unfilled template placeholder, or names a release other than the installed tool release. | Write the project file in the declared shape and adopt the installed release. |
| `release-drift` | G | records | The project file names no reviewed LiteSpec release, or a release other than the installed tool release. | Install the reviewed release, or review the installed release and record it in reviewedLiteSpecVersion. |
| `project-path-invalid` | G | records | A configured path is not a path, names no file, or the page scan finds no record or Markdown file. | Point the path at a file that exists. |
| `stack-invalid` | G | records | A named stack ships in no pinned release, a client-only project names a backend stack, the backends are not exactly one, a stack requirement is unnamed, or a frontend stack pairs with the wrong backend. | Name the stacks the pinned release ships, in a valid combination. |
| `extension-invalid` | G | records | A selected extension ships in no pinned release, or supports a stack the project does not name. | Name an extension the pinned release ships for the selected stacks. |
| `stage-deferral-invalid` | G | records | A deferral exception names a provision the pinned release does not ship. | Point the deferral at a provision of the pinned release. |
| `page-invalid` | G | records | A record file holds a document type or an article type without its explanation file, or a Markdown file is neither an explanation beside its record nor a document with a frontmatter block. | Keep records in record files with their explanations beside them, and open every document with a frontmatter block. |
| `page-section-missing` | G | records | A research or decision explanation, or a document, lacks a required H2 section. | Add the required H2 section. |
| `page-heading-invalid` | G | records | A page title or section heading breaks Title Case. | Write the heading in Title Case. |
| `page-example-invalid` | G | records | An Example section runs past the 120 word bound. | Shorten the Example section to 120 words. |
| `glossary-invalid` | G | records | The glossary is not YAML or breaks the glossary schema. | Write the glossary in the declared shape. |
| `frontend-invalid` | G | records | A declared frontend path names no file, or the controlled UI run fails. | Point the frontend path at a folder that exists. |
| `ui-package-invalid` | G | full | No shared UI package is declared for the controlled frontends, or the declaration is incomplete or points outside the package. | Declare one shared package with every key pointing inside it. |
| `ui-baseline-missing` | G | full | The pinned release names no UI baseline for the package platform. | Adopt a release that pins the UI baseline. |
| `ui-frontend-invalid` | G | full | A controlled frontend lacks its `ui` block, names a profile outside the three, or points at a missing path or stylesheet. | Give the frontend a `ui` block with a valid density profile and paths that exist. |
| `ui-components-invalid` | G | full | The committed baseline configuration differs from the manifest baseline or adds a registry without an exception. | Match the committed configuration to the manifest baseline. |
| `ui-sourcelock-invalid` | G | full | The source lock names an unpinned CLI, a preset or registry outside the baseline, or a copied source whose digest changed. | Match the source lock and its digests to the pinned baseline. |
| `ui-design-contract-invalid` | G | full | The design contract lacks its metadata block, breaks the metadata shape, or misses a required section. | Write the contract with its metadata block and required sections. |
| `ui-stylesheet-invalid` | G | full | A stylesheet declares a feature style, a literal color, an unapproved import, or malformed CSS, or a source file carries an inline style. | Keep feature styles in shared components and tokens in the token stylesheet. |
| `ui-class-invalid` | G | full | A class string carries an arbitrary value, a raw palette value, or an important modifier. | Use a semantic token or a declared variant. |
| `ui-import-invalid` | G | full | Application code reaches a primitive library, an internal package path, or a table library directly. | Reach shared UI through the public entry points only. |
| `ui-dependency-invalid` | G | full | A second general-purpose visual dependency lacks an exception. | Depend on one visual system, or record an exception. |
| `ui-catalog-invalid` | G | full | The pattern catalog is missing, breaks its schema, or names an unknown pattern, option, or component path. | Write the catalog against the LiteSpec pattern pages. |
| `ui-route-suite-missing` | G | full | A route has no route suite proving it in a browser. | Prove every route in the route suite. |

The `render` subcommand writes these generated regions, each fenced by `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`: `alert-inventory`, `runbook-index`, and `operations-log`. It also writes `litespec-records.ts` into every React and Svelte frontend frame: the use-case and screen ids of that frontend's own entry points with the `proves` helper browser tests annotate with.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<id>` | for `query` | Prints the record with this identifier or qualified identifier and its links both ways. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | Selects the consumer repository to read. |
| `--format <name>` | human lines | Only `check` reads it. It accepts `json` and writes one JSON object holding the `findings` and `known` arrays, each finding with the fields the Description lists. |
| `--release` | off | Only `check` reads it. Makes the release-readiness findings block: `use-case-unverified`. |
| `--since <ref>` | off | Only `check` reads it. Blocks only on findings about records and code changed since `<ref>`; older findings report. Also reports every removed result, removed refusal code and changed integration event since `<ref>` as a `contract-break` finding. |
| `--all` | off | Only `check` reads it. In text, lists every level A finding one by one. Without it, the level A findings are summarized by code in the counts. In JSON every finding is always listed. |
| `--check` | off | Only `format` reads it. Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The command finished with no open blocking finding. |
| `1` | `check` found an open blocking finding or an expired exception, `query` found no record with the `id`, `format --check` found a file it would change, or the run failed. |
| `2` | The arguments are wrong, the root holds no project record, the `--since` revision names no commit, or `format` found a file that does not parse. |

## Examples

Check the model and read the findings from another program:

```bash
litespec model check --root ../consumer --format json
```

Render the generated outputs after a record change:

```bash
litespec model render --root ../consumer
```

Rewrite every record file into canonical form:

```bash
litespec model format --root ../consumer
```

List the files that would change, without writing:

```bash
litespec model format --root ../consumer --check
```

Print one record with its links:

```bash
litespec model query --root ../consumer use-case/orders.cancel-order
```

Print one record by its qualified identifier:

```bash
litespec model query --root ../consumer finance/problem/organizers-wait-for-ticket-money
```

Check for release, with the release-readiness findings blocking:

```bash
litespec model check --root ../consumer --release
```

Block only on the findings changed since the main branch, and report every breaking contract change since it:

```bash
litespec model check --root ../consumer --since main
```

## Underneath

A web frontend is read by a TypeScript reader that ships with the tool as one bundled script: the tool runs it with Node once per command run that builds the model, sends one versioned JSON request on standard input, and reads one versioned JSON facts document back, so a consumer installs nothing beyond Node itself. The reader opens with a handshake naming the protocol version, the reader, and the TypeScript and Svelte versions; a version mismatch, an absent Node, or a reader failure produces the single `reader-missing` finding for that frontend instead of passing silently. A finding records the reader that produced its facts, and its level takes that reader's qualification: a reader qualifies when its handshake matches; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, which reads Blazor and Android frontends, stays qualified. A finding no reader produced, a record finding, is qualified.

The builder reads `paths`, `shape`, and `stacks` from the consumer's `.litespec/project.yaml`. It reads shared provisions from the release the tool carries and the test run from TRX or JUnit XML files under `paths.testResults`. A JUnit `testcase` proves a Behavior scenario directly by carrying the scenario id in a `litespec.proves` property, one property per scenario or several ids separated by commas in one value: `<properties><property name="litespec.proves" value="scenario/borrow-copy.borrowing-an-available-copy-succeeds" /></properties>`. The case proves each id it names with the same rules as a `proves` mark on a test method, so a runner whose steps carry no mark, or a stack with no code reader, still proves its scenarios. A check `--since` base is read through an archive into a temporary folder, which is removed after the check. Nothing beneath it is separately runnable.
