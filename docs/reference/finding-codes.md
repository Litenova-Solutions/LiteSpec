# Finding Codes

## Intent

Use this generated page to read what a finding code means and how to resolve it. A reader who sees a code in `litespec model check` output, or in a `Checked by` line of a standards page, looks it up here. The table holds every code the tools emit, with its level, the record layer it governs, whether it ties that layer to the records, and the extension that adds it.

## Reference

| Code | Level | Layer | Trace | Extension | Meaning | Fix |
|:---|:---|:---|:---|:---|:---|:---|
| `record-invalid` | G | any | no | - | A record file fails to parse, or a record fails the shape its type requires. | Correct the file so it parses and matches the shape its type requires. |
| `identifier-invalid` | G | any | no | - | An identifier breaks the form its type requires. | Rename the identifier to the required form. |
| `identifier-duplicate` | G | any | no | - | One identifier names two records. | Give one of the records a new identifier. |
| `identifier-folder-drift` | G | any | no | - | An identifier's module segment disagrees with its folder. | Rename the identifier to its folder's module and update every citation. |
| `name-duplicate` | G | any | no | - | One name segment names two records in the product. | Give one of the records a new name. |
| `field-invalid` | G | any | no | - | A field holds a value outside its shape. | Write the field in its declared shape. |
| `reference-unknown` | G | any | no | - | A citation names no written record or shared provision of the installed release. | Point the citation at a written record or a provision of the installed release. |
| `link-unknown` | G | any | no | - | A Markdown link in an explanation file or a document resolves to no file and cites no record. | Point the link at a file or a record that exists. |
| `product-count-invalid` | G | any | no | - | The project holds no single product record. | Write one product record. |
| `size-type-missing` | G | any | no | - | The size requires a type the project lacks. | Add a record of the missing type. |
| `requirement-fulfillment-missing` | G | behavior | no | - | A requirement is fulfilled by no rule, use case, reaction, job, notification or setting; a quality requirement is also fulfilled by a technical decision naming it in `basedOn`. | Fulfill the requirement from a Behavior record, or name a quality requirement from a technical decision in `basedOn`. |
| `requirement-measure-missing` | G | proof | yes | - | A measured requirement names no check, or its check has no passing run; a statement naming a number with no `measure` reports at level A. | Name the check and record a passing run, or add the missing `measure`. |
| `requirement-cites-missing` | A | any | no | - | A requirement cites no goal, problem, feature, constraint, research or decision. | Cite the reason the requirement serves. |
| `constraint-unreached` | G | any | no | - | A constraint with `applies: true` is cited by no requirement. | Cite the constraint from a requirement, or set `applies` to false. |
| `intent-unreached` | G | behavior | no | - | An Intent record other than a vision reaches no Behavior. | Reach Behavior from the record, or move the record to the backlog. |
| `decision-decides-missing` | G | any | no | - | A product decision names nothing it decides. | Name the records the decision decides. |
| `decision-number-duplicate` | G | any | no | - | Two decisions share one number. | Give one decision the next free number. |
| `decision-decides-duplicate` | G | any | no | - | Two decisions decide one record. | Keep one decision on the record and move the other. |
| `use-case-journey-missing` | G | any | no | - | A public use case sits in no journey, and no reaction, job or other use case runs it. It runs at large size only. | Walk the use case in a journey, or name what runs it. |
| `state-unreached` | G | any | no | - | A state is reached by no transition. | Add a transition that reaches the state, or remove the state. |
| `transition-unknown` | G | any | no | - | A transition names a missing state or use case. | Point the transition at a written state and a written use case. |
| `use-case-success-missing` | G | any | no | - | A use case ends with no success result. | Add the success result to the use case. |
| `scenario-result-unknown` | G | any | no | - | A scenario ends in a result outside its own use case and outside the derived results. | End the scenario in a result of its own use case. |
| `calculation-table-missing` | G | any | no | - | A calculation rule has no scenario examples. | Add the scenario examples to the rule. |
| `rule-name-invalid` | G | any | no | - | A rule statement negates what its name does not keep. | Keep the negation word in the rule name. |
| `departure-expired` | G | any | no | - | A departure stands past its expiresOn date. | Fix the finding, or extend the departure with a new expiresOn date. |
| `departure-invalid` | G | any | no | - | A departure covers a finding code no check emits, a provision or page the installed release does not ship, or a code the model needs in order to load. | Name a finding code a check emits, or a provision or page of the installed release, and no code the model needs in order to load. |
| `job-name-invalid` | G | any | no | - | A job name does not start with the verb of a use case it runs. | Start the job name with the verb of a use case it runs. |
| `template-key-invalid` | A | any | no | - | A notification template key ends with no entry of `addresses`. | End the key with an entry of `addresses`. |
| `setting-unit-missing` | A | any | no | - | A setting whose value is a number or a duration declares no `unit`. | Declare the `unit` of the setting. |
| `integration-event-name-invalid` | G | any | no | - | An outbound integration event is named with an internal word such as `staged`, `dispatch` or `outbox`. | Rename the event without the internal word. |
| `reaction-trigger-unknown` | G | any | no | - | A reaction names a trigger no domain event or integration event carries. | Point the trigger at a written domain event or integration event. |
| `erasure-undeclared` | G | any | no | - | A personal or sensitive field stands outside a declared erasure position. | Declare the erasure position of the field. |
| `record-link-missing` | G | any | no | - | A use case, reaction, job, notification or setting names neither `serves` nor `fulfills`. | Link the record with `serves` or `fulfills`. |
| `record-parent-missing` | G | any | no | - | A feature `solves` names no problem, a journey `serves` names no goal, a scenario names no result, a screen `entryPoint` names no entry point, or a result, rule, event, or module-homed record names an identifier home no record holds. | Name the missing parent. |
| `folder-invalid` | G | any | no | - | A folder sits outside the closed set of its layer and level. | Move the folder into the closed set its layer and level allow, or remove it. |
| `module-undeclared` | G | any | no | - | A `modules/<m>/` folder names no declared module. | Declare the module with a module record, or remove the folder. |
| `record-placement-invalid` | G | any | no | - | A record sits outside the folder and level its type allows, or a `module.yaml` holds rules. | Move the record to its home, or move the rules to a rule-set file. |
| `record-folder-drift` | G | any | no | - | A record's folder name differs from the stem of its record file. | Rename the folder or the file so both carry the record's name. |
| `role-orphaned` | G | any | no | - | A role is held by no actor. | Hold the role from an actor, or remove the role. |
| `permission-orphaned` | G | any | no | - | A permission is allowed by no role. | Allow the permission from a role, or remove the permission. |
| `problem-sufferer-invalid` | G | any | no | - | A problem names a sufferer outside the actors. | Name a sufferer the actors hold. |
| `problem-evidence-missing` | G | any | no | - | A problem cites no evidence. | Cite the evidence of the problem. |
| `actor-unused` | A | any | no | - | An actor is named by no journey or problem. | Walk the actor in a journey, name it in a problem, or remove the actor. |
| `goal-mover-missing` | G | behavior | no | - | No use case moves the goal measure. | Move the measure from a use case. |
| `term-missing` | A | any | no | - | A glossary term appears in no record summary or statement. | Use the term in a summary or statement, or remove it. |
| `result-scenario-missing` | A | any | no | - | A written result is shown by no scenario; a success result reports at level G. | Write a scenario that shows the result. |
| `step-use-case-missing` | G | behavior | no | - | A journey step is served by no use case, reaction or job. Its pair `step-screen-missing` names a step a browser or device use case serves that no screen serves. | Serve the step from a use case, a reaction or a job. |
| `effect-loop-invalid` | G | any | no | - | The effect graph cycles outside a domain event with a stated end condition. | Break the cycle, or route it through a domain event with a stated end condition. |
| `module-dependency-invalid` | G | any | no | - | Module `dependsOn` links form a cycle. | Break the cycle so one module stops depending on the other. |
| `link-locality-invalid` | G | any | no | - | Within one layer, a module record is linked from another module outside a declared `dependsOn` or an integration event. | Move the record to product level, or declare the module tie. |
| `event-publisher-invalid` | G | any | no | - | An integration event has more than one publishing module. | Keep one publishing module and read the event from the other modules. |
| `consistency-cites-missing` | G | any | no | - | An atomic use case changing more than one aggregate names no rule in `cites`. | Cite the atomicity rule in `cites`. |
| `query-output-missing` | G | any | no | - | A query declares no `output`. | Declare the `output` of the query. |
| `paging-undeclared` | G | any | no | - | A collection query declares no `paging`. | Declare the `paging` of the query. |
| `widening-cites-missing` | G | any | no | - | A query group visible beyond its `reads` names no requirement in `cites`. | Cite the widening requirement in `cites`. |
| `retention-enforcement-missing` | G | any | no | - | A data class names no `enforcedBy`. | Name the enforcement in `enforcedBy`. |
| `setting-requirement-missing` | G | any | no | - | A commercial, legal, or tenant-varying default fulfills no requirement. | Fulfill a requirement from the setting. |
| `reservation-release-missing` | G | any | no | - | A reservation rule names no `releasedBy`, or names never with no reason. | Name the releasing record in `releasedBy`, or never with a reason. |
| `notification-sender-missing` | G | any | no | - | A notification is sent by no reaction, job or use case. | Send the notification from a reaction, a job or a use case. |
| `except-fallback-missing` | G | any | no | - | An `except` names no fallback requirement, or the fallback carries its own `except`. | Name a fallback requirement that carries no `except` of its own. |
| `job-guard-missing` | G | any | no | - | A manual job states no `guard`. | State the `guard` of the job. |
| `schedule-invalid` | G | any | no | - | A job schedule is neither an `every` duration nor a `cron` expression. | Write the schedule as an `every` duration or a `cron` expression. |
| `atomic-group-drift` | G | any | no | - | An atomic use case's `changes` differ from its atomicity rule's `binds`. | Match `changes` to the rule's `binds`, or update the rule. |
| `atomic-group-invalid` | A | any | no | - | An atomic group binds more than 4 aggregates. | Split the group into use cases that react to each other's events. |
| `behavior-unrecorded` | S | implementation | yes | - | A refusal code in a declaring position, a screen call to no endpoint, or (the frontend case) a route with no screen mark or a mark naming no screen is named by no record. | Delete the code, or write its parent record from a requirement, drafting the requirement when none exists. |
| `mark-missing` | S | implementation | yes | - | A code element the marks table names carries no mark. | Add the mark the marks table names. |
| `mark-unknown` | S | implementation | yes | - | A mark names no record, or a record of a type its code element does not take, or sits on an element that takes no mark; or a `follows` mark names no provision of the carried release, no decision, departure or project convention, or a provision on a page the project's stacks do not apply. | Point the mark at a record its element takes, or remove the mark. |
| `decision-unimplemented` | S | implementation | yes | - | A technical decision under `.litespec/implementation/decisions/` is followed by no code element. | Follow the decision from a code element, or retire the decision. |
| `decision-basis-missing` | A | any | no | - | A technical decision names no constraint or requirement in `basedOn`. | Name the constraint or requirement in `basedOn`. |
| `decision-kind-missing` | A | any | no | - | A technical decision states no `kind`. | Give the decision its `kind`: provider, package, format, boundary, topology or replacement, or record the reason in class documentation when the choice has no kind. |
| `decision-scope-invalid` | A | implementation | yes | - | A technical decision is followed only from one module, and each follower already carries an `implements` mark. | State the reason in the class documentation and retire the decision, or show the choice is wider than one module. |
| `decision-home-drift` | A | implementation | yes | - | A module-level technical decision is followed from more than one module, or a product-level technical decision is followed only from one module. | Move the decision to the product decisions folder when several modules follow it, or into the one module that does. |
| `convention-folder-invalid` | A | any | no | - | A Markdown file under `.litespec/` outside `governance/conventions/` carries an `**ID:**` line of the convention form, so `rules` never returns it. | Move the convention into `.litespec/governance/conventions/`. |
| `convention-scope-invalid` | A | any | no | - | A convention's `Applies to` is neither a path glob nor the scope vocabulary provisions use, so `rules` never returns it. | Write `Applies to` as a path glob or as scope keys such as `stack: dotnet; artifact: command-handler`. |
| `agent-protocol-missing` | S | any | no | - | The project's `AGENTS.md` exists and holds no `agent-protocol` generated region; a project with no `AGENTS.md` is not reported. | Run `litespec model render` to insert the region. |
| `agent-protocol-drift` | S | any | no | - | The `agent-protocol` region in `AGENTS.md` differs from what `model render` writes for the carried release and the project file. | Run `litespec model render` to rewrite the region. |
| `agent-protocol-unreached` | A | any | no | - | The project holds a `CLAUDE.md` that does not import `AGENTS.md`, so Claude Code never reads the agent protocol. | Add the line `@AGENTS.md` to `CLAUDE.md`. |
| `skill-drift` | S | any | no | - | A project-scope copy of the LiteSpec skill carrying the generated marker differs from what the carried release generates. | Run `litespec model render` to rewrite the copy. |
| `constituent-parent-missing` | A | implementation | no | - | An unmarked Domain type sits in a folder that holds no aggregate root, under no aggregate folder, and outside the shared kernel. | Move the type under its aggregate, or mark it. |
| `mark-count-invalid` | S | implementation | yes | - | A code element carries more or fewer marks than its kind takes in the marks table, or names one record twice. | Carry the marks the marks table names, each once. |
| `mark-style-invalid` | S | implementation | yes | - | A C# mark is written in the style the project did not declare. | Rewrite the mark in the declared style. |
| `mark-folder-drift` | S | implementation | no | - | A handler, endpoint, event handler, job host or aggregate root marks a use case, reaction, job or aggregate outside the module folder it sits in, or a handler or endpoint marks a use case outside the use-case folder it sits in. | Move the element to the folder its mark names, or point the mark at the record its folder holds. |
| `code-placement-drift` | S | implementation | no | - | A code element, test, or folder sits outside the code folder its record's placement names, or a folder names no declared record. | Move the element to the folder its mark names, or point the mark at the record its folder holds. |
| `runtime-value-drift` | S | implementation | yes | - | A hand-written event `Classification` or exception `Code` differs from what its record states. | Match the value to what the record states. |
| `act-event-undeclared` | S | implementation | yes | - | A root method marked with a use case raises an event the use case does not list in `raises`. | List the event in `raises`, or stop raising it. |
| `act-event-unimplemented` | S | implementation | yes | - | A use case lists an event in `raises` that no root method marked with it raises. | Raise the event from a marked root method, or remove it from `raises`. |
| `use-case-act-missing` | S | implementation | yes | - | A command handler changing an entity aggregate calls no root method marked with its use case. | Call a root method marked with the use case. |
| `aggregate-unimplemented` | S | implementation | yes | - | No aggregate root carries the mark of an entity aggregate. | Mark the aggregate root. |
| `repository-unimplemented` | S | implementation | no | - | An aggregate a use case changes has no Infrastructure repository implementing its domain repository. | Write the Infrastructure repository. |
| `reaction-unimplemented` | S | implementation | yes | - | No event handler carries the reaction mark. | Write the event handler, or remove the reaction. |
| `job-unimplemented` | S | implementation | yes | - | No job host carries the job mark. | Write the job host, or remove the job. |
| `external-system-unimplemented` | S | implementation | yes | - | No port carries the external system mark. | Write the port, or remove the external system. |
| `use-case-endpoint-missing` | S | implementation | yes | - | A use case reached through a `browser` or `api` entry point has no endpoint carrying its mark. | Expose the endpoint and carry the use case mark. |
| `effect-unimplemented` | A | implementation | yes | - | A declared effect edge is taken by no code. | Take the edge in code, or remove the declaration. |
| `use-case-unreached` | A | implementation | yes | - | A use case reached through a browser or device entry point is offered by no screen of that entry point. It reports and does not block: a use case with no screen does not block a check or a release. | Offer the use case from a screen of the entry point. |
| `screen-unimplemented` | S | implementation | yes | - | No route carries the screen's mark. | Carry the screen mark from the route. |
| `screen-offers-drift` | S | implementation | yes | - | A screen's `offers` differs from the use cases its route reaches through the generated client. | Match `offers` to the use cases the route reaches. |
| `screen-serves-drift` | G | implementation | yes | - | A screen serves a journey step no use case it offers serves. | Serve the step from an offered use case, or stop serving it. |
| `step-screen-missing` | G | implementation | yes | - | A journey step a browser or device use case serves is served by no screen. Its pair `step-use-case-missing` names a step no use case serves. | Serve the step from a screen. |
| `refusal-sentence-missing` | A | implementation | yes | - | A refusal a screen can reach has neither a dictionary sentence nor a browser test asserting its code. | Write the dictionary sentence and the browser test. |
| `reader-missing` | S | implementation | yes | - | A reader could not read what it was given: a frontend reader could not run (Node is absent, its handshake names another protocol, or it failed), a .NET reader could not read or parse a backend, test or screen file, a declared stack root resolved to no folder, a `server` project selected no backend reader, or a configured bill of materials was missing, not JSON, not SPDX or CycloneDX, or yielded no packages. The finding names the cause and the file or path; a frontend's S findings report instead of blocking. | Restore the reader: install Node, match the handshake, or fix the failure the finding names. |
| `use-case-unimplemented` | S | implementation | yes | - | No code implements the use case and no departure covers it. | Implement the use case, or record a departure. |
| `refusal-unimplemented` | S | implementation | yes | - | A refusal result names a code the code never throws, or a code matching a `refusals.composed` template whose key no code declares. | Throw the named code, or remove the refusal result. |
| `proves-duplicate` | A | proof | yes | - | A scenario carries `proves` marks on more than one test. | Keep the `proves` mark on one test. |
| `proves-unreached` | S | proof | yes | - | No proving step of a claimed scenario reaches its use case, no assertion matches its refusal by code, client alias, or HTTP status, and no integration fact drives its use case or rule. The frontend case: a browser test reaches no screen offering the claimed scenario's use case by a `page.goto` literal or template matching the screen's `address`, or no assertion in the same test holds the refusal code or the success result's declared text or code. A step reaches a use case when it belongs to the use case's steps class, calls its generated-client operation, requests its endpoint route through its own helpers, or looks its argument up in a probe table of routes in its steps class. A scenario proves the use case a when step reaches; the use case a given step reaches, when a later when step and then step observe it; or a use case a when step sets off through a declared producer, whose effects run it or whose event a reaction running it reacts to, with an effect asserted. Codes and statuses asserted inside a then step's method count as the scenario's own. | Prove the scenario from a test that reaches its use case and asserts its outcome. |
| `use-case-scenario-missing` | S | any | no | - | A use case has no scenario. It runs at large size only. | Write a scenario for the use case. |
| `use-case-unverified` | A | proof | yes | - | A use case holds scenarios with no passing proof. | Prove every scenario through the public boundary. |
| `proof-invalid` | A | proof | yes | - | A test passed and failed within the runs read. | Stabilize the test so it passes on every run. |
| `proof-hash-drift` | A | proof | yes | - | A scenario's proving test method changed since the `--since` base while the scenario did not. It runs on `check --since` only. | Change the scenario in the same change, or restore the proving test. |
| `write-undeclared` | S | implementation | yes | - | The code writes an aggregate outside `changes`. | Declare the aggregate in `changes`, or stop writing it. |
| `result-undeclared` | S | implementation | yes | - | The code returns an outcome no result declares. | Declare the outcome in a result, or stop returning it. |
| `effect-undeclared` | S | implementation | yes | - | The code takes an edge no record declares, naming a one-hop helper as `via 'Type.Method'`. | Declare the edge on the record, or stop taking it. |
| `prose-missing` | A | any | no | - | A record holds no `summary` field, or a goal, vision, problem, requirement or rule holds no `statement` field. | Write the missing `summary` or `statement` field. |
| `explanation-orphaned` | A | any | no | - | A `.md` file under a record root has no frontmatter and no record file beside it, and is not `README.md`. | Pair the file with a record file of the same name, or give it a document frontmatter block. |
| `explanation-missing` | A | any | no | - | A research or decision record has no explanation file, or its explanation lacks a required section. | Write the explanation file with its required sections. |
| `format-drift` | A | any | no | - | A record file differs from canonical form. | Run `litespec model format`. |
| `source-link-bare` | A | any | no | - | A research explanation links a URL directly instead of citing a source key. | Cite the source with a `[text][key]` reference link. |
| `source-key-unknown` | A | any | no | - | A research explanation cites a source key the record's `sources` do not hold. | Point the link at a declared source key, or add the source. |
| `source-unused` | A | any | no | - | A research source's `key` is cited nowhere in its explanation. | Cite the source from the explanation, or remove the source. |
| `atomic-group-duplicate` | A | any | no | - | One command saves more than one atomic write group. | Save one atomic write group per command, or split the work into use cases that react to each other's events. |
| `operation-id-duplicate` | S | implementation | no | - | Two endpoints declare one operation id, which the API document and every generated client refuse. | Give one endpoint a new operation id. |
| `operation-use-case-drift` | S | implementation | no | - | An operation in the committed contract `paths.contract` names carries no `x-litespec-use-case`, or names another use case than its endpoint's mark; an unreadable contract reports once. | Name the endpoint use case in `x-litespec-use-case`, or fix the unreadable contract. |
| `contract-break` | S | implementation | no | - | A result or refusal code the `--since` base held no longer exists, or an integration event changed since the base. It runs on `check --since` only. | Keep the result, refusal code and integration event the base published. |
| `analyzer-lowering-unrecorded` | S | implementation | no | - | A LiteSpec analyzer rule is lowered or switched off in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable` or `[SuppressMessage]` while no live departure covers the provision the rule enforces. | Remove the lowering, or record a departure for the provision. |
| `alert-link-missing` | S | any | no | delivery | An alert rule in `paths.alerts` has no `requirement` label naming a quality requirement or no `runbook` label naming a runbook. | Label the alert rule with the requirement and the runbook. |
| `alert-threshold-drift` | A | any | no | delivery | An alert rule's own text carries none of the decimal numbers its requirement's `measure` names. | Carry a decimal number from the `measure` in the alert text. |
| `quality-alert-missing` | G | any | no | delivery | A quality requirement no alert names, whose `check` matches no measured-check or check result, and no open departure covers. | Name the requirement from an alert, or record a departure. |
| `runbook-unused` | A | any | no | delivery | A runbook no alert, job or use case names. | Name the runbook from an alert, a job or a use case, or remove the runbook. |
| `setting-key-missing` | G | any | no | - | A setting with `setBy: operator` names no `key`. | Name the `key` of the setting. |
| `setting-unimplemented` | S | implementation | yes | - | A setting with a `key` is marked by no options class. | Mark an options class with the setting. |
| `setting-key-drift` | S | implementation | yes | - | A marked options class's `SectionName` and property do not spell the setting's `key`. | Spell the setting `key` in the `SectionName` and property. |
| `template-unimplemented` | S | implementation | yes | - | A notification declares a `template` key the template store lacks. | Add the key to the template store, or remove it from the notification. |
| `template-unrecorded` | S | implementation | yes | - | The template store holds a key no notification declares. | Declare the key on a notification, or remove it from the store. |
| `template-param-drift` | S | implementation | yes | - | A notification declares a parameter none of its template's texts uses as a placeholder. | Use the parameter as a placeholder in the template text, or remove it. |
| `dependency-undeclared` | S | implementation | no | - | A direct package in the bill of materials is pinned by no manifest entry and admitted by no decision. | Pin the package in the manifest and admit it in a decision. |
| `dependency-version-drift` | S | implementation | no | - | A direct package ships at a version other than its manifest pin. | Ship the package at its manifest pin. |
| `dependency-license-unknown` | S | implementation | no | - | A direct package's license is not in `dependencies.licenses` and no `admits` entry names it. | List the license in `dependencies.licenses`, or name the package in an `admits` entry. |
| `admission-invalid` | G | any | no | - | An `admits` entry misses its layer, license, location, account owner, failure behavior, or operating cost. | Complete the `admits` entry with its layer, license, location, account owner, failure behavior, and operating cost. |
| `name-reason-missing` | S | implementation | no | - | A folder or type is named `Shared`, `Common`, `Helper`, `Manager` or the like outside the declared shared kernel. | Rename the folder or type without the shared word, or declare the shared kernel. |
| `collaborator-repository-invalid` | A | implementation | no | - | A class built with `new` takes a repository in its own constructor. | Stop passing a repository to a class built with `new`. |
| `repository-read-invalid` | A | implementation | no | - | A repository read sits inside a `for`, `foreach` or `while` body. | Move the repository read out of the loop body. |
| `query-filter-invalid` | A | implementation | no | - | A query handler filters a repository list with LINQ in memory. | Filter in the repository instead of in memory. |
| `effect-timing-invalid` | S | any | no | - | A command use case declares a `sends` or `calls` effect with `inline` timing. | Send or call after the commit, from an event handler or a job. |
| `partial-missing` | S | implementation | no | - | A marked domain event or rule exception is not declared `partial`, so the generator cannot add its runtime value. | Declare the domain event or rule exception `partial`. |
| `project-invalid` | G | any | no | - | The project file breaks the project schema, holds an unfilled template placeholder, or names a release other than the installed tool release. | Write the project file in the declared shape and adopt the installed release. |
| `release-drift` | G | any | no | - | The project file names no reviewed LiteSpec release, or a release other than the installed tool release. | Install the reviewed release, or review the installed release and record it in reviewedLiteSpecVersion. |
| `project-path-invalid` | G | any | no | - | A configured path is not a path, names no file, or the page scan finds no record or Markdown file. | Point the path at a file that exists. |
| `stack-invalid` | G | any | no | - | A named stack ships in no pinned release, a client-only project names a backend stack, the backends are not exactly one, a stack requirement is unnamed, or a frontend stack pairs with the wrong backend. | Name the stacks the pinned release ships, in a valid combination. |
| `extension-invalid` | G | any | no | - | A selected extension ships in no pinned release, or supports a stack the project does not name. | Name an extension the pinned release ships for the selected stacks. |
| `stage-deferral-invalid` | G | any | no | - | A deferral departure names a provision the pinned release does not ship. | Point the deferral at a provision of the pinned release. |
| `page-invalid` | G | any | no | - | A record file holds a document type or an article type without its explanation file, or a Markdown file is neither an explanation beside its record nor a document with a frontmatter block. | Keep records in record files with their explanations beside them, and open every document with a frontmatter block. |
| `page-section-missing` | G | any | no | - | A research or decision explanation, or a document, lacks a required H2 section. | Add the required H2 section. |
| `page-heading-invalid` | G | any | no | - | A page title or section heading breaks Title Case. | Write the heading in Title Case. |
| `page-example-invalid` | G | any | no | - | An Example section runs past the 120 word bound. | Shorten the Example section to 120 words. |
| `glossary-invalid` | G | any | no | - | The glossary is not YAML or breaks the glossary schema. | Write the glossary in the declared shape. |
| `frontend-invalid` | G | any | no | - | A declared frontend path names no file, or the controlled UI run fails. | Point the frontend path at a folder that exists. |
| `ui-package-invalid` | G | implementation | no | - | No shared UI package is declared for the controlled frontends, or the declaration is incomplete or points outside the package. | Declare one shared package with every key pointing inside it. |
| `ui-baseline-missing` | G | implementation | no | - | The pinned release names no UI baseline for the package platform. | Adopt a release that pins the UI baseline. |
| `ui-frontend-invalid` | G | implementation | no | - | A controlled frontend lacks its `ui` block, names a profile outside the three, or points at a missing path or stylesheet. | Give the frontend a `ui` block with a valid density profile and paths that exist. |
| `ui-components-invalid` | G | implementation | no | - | The committed baseline configuration differs from the manifest baseline or adds a registry without a departure. | Match the committed configuration to the manifest baseline. |
| `ui-sourcelock-invalid` | G | implementation | no | - | The source lock names an unpinned CLI, a preset or registry outside the baseline, or a copied source whose digest changed. | Match the source lock and its digests to the pinned baseline. |
| `ui-design-contract-invalid` | G | implementation | no | - | The design contract lacks its metadata block, breaks the metadata shape, or misses a required section. | Write the contract with its metadata block and required sections. |
| `ui-stylesheet-invalid` | G | implementation | no | - | A stylesheet declares a feature style, a literal color, an unapproved import, or malformed CSS, or a source file carries an inline style. | Keep feature styles in shared components and tokens in the token stylesheet. |
| `ui-class-invalid` | G | implementation | no | - | A class string carries an arbitrary value, a raw palette value, or an important modifier. | Use a semantic token or a declared variant. |
| `ui-import-invalid` | G | implementation | no | - | Application code reaches a primitive library, an internal package path, or a table library directly. | Reach shared UI through the public entry points only. |
| `ui-dependency-invalid` | G | implementation | no | - | A second general-purpose visual dependency lacks a departure. | Depend on one visual system, or record a departure. |
| `ui-catalog-invalid` | G | implementation | no | - | The pattern catalog is missing, breaks its schema, or names an unknown pattern, option, or component path. | Write the catalog against the LiteSpec pattern pages. |
| `ui-route-suite-missing` | G | proof | no | - | A route has no route suite proving it in a browser. | Prove every route in the route suite. |

## Notes

The `litespec-maintain provisions` command writes this page from the finding code table, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.
