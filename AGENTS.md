# LiteSpec Agent Protocol

Read this file before you change the LiteSpec repository or a consumer application. It summarizes the provisions that govern agent work. Each section ends with the provisions it draws on, and the provision wins wherever this file is shorter.

## What LiteSpec Covers

LiteSpec 7.0.0 is the contract for one bounded-context business application. A project names one size (small or large), one shape (server or client-only), and its stacks. A server project names one backend stack, and each frontend names one stack. The project claims its record layers in `layers`: intent and behavior, and implementation and proof, which each take `litespec` or `own`.

A stack is a folder with a `stack.md` page: `docs/packs/dotnet/`, `docs/packs/svelte/` and `docs/packs/android/`. Selecting a stack applies its folder, every folder above it, and their subfolders that hold no stack. An extension applies only when the project selects it.

`litespec.manifest.yaml` lists the stacks, shapes, extensions and pinned versions. Structured LiteSpec data is YAML, and so is every record file a consumer writes; explanation files are Markdown. `docs/README.md` is the human index. A provision ID is `litespec/<page>#<slug>` as its `**ID:**` line states it, and `docs/reference/provisions.md` lists them all.

Sources: `litespec/core/scope#keep-one-bounded-context`, `litespec/core/scope#select-a-size-a-shape-and-stacks`, `litespec/core/stacks#apply-the-complete-stack`, `litespec/core/scope#select-conditional-extensions-explicitly`, `litespec/core/scope#claim-the-record-layers-the-project-uses`, `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml`, `litespec/core/authoring#use-the-declared-identifier-grammar`.

## Principles

LiteSpec stays clean, plain, straightforward and consistent. Every change keeps it that way.

1. **Consistent.** Things of one kind look and work the same everywhere. When you change a shape, change every instance in the same change, and make `litespec-maintain validate` enforce it so it cannot drift back.
2. **Named from the intent.** Name each thing for what it really does, from the record or page above it, and carry that one name through every layer below. (`litespec/core/principles#take-every-name-from-the-intent-above-it`)
3. **Clean.** Each fact has one source of truth. Judgments are computed by a tool, never typed. Remove what no longer earns its place instead of keeping it beside its replacement. (`litespec/core/principles#keep-one-authored-source`)
4. **Plain.** No filler, hedging, marketing or history. Do not mark a rule as checked unless a tool checks it, do not leave placeholders, and do not keep compatibility shims between LiteSpec releases.
5. **Straightforward.** The rule comes first, then the reason, then one concrete example from the Claimwick expense product or the library product.
6. **Readable cold.** Every page, record and book page stands alone for a reader who arrives from a link. Records hold every fact; explanation files restate them for people. (`litespec/core/writing#write-human-docs-for-the-cold-reader`, `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy`, `litespec/core/model#keep-the-whole-specification-in-the-records`)
7. **The product, not the company.** LiteSpec specifies the application: behavior it performs, data it holds and limits it enforces. Running the company stays out; it enters only as a constraint or as dated research.
8. **Completion is computed.** The model computes how much of a thing exists and is proven from code, tests and passing scenarios. Nobody types it.
9. **Guidance states its strength.** Every provision says whether it is required, a default or advisory, and where it applies.
10. **Tools stay small.** A command, check, generated file or package exists because the owner approved it. Remove it when nothing uses it.

## Designing LiteSpec

Read the [design rules](docs/core/designing.md) before you add or change a record type, a field, a folder, an explanation section, a check or a command.

1. **One thing per record.** A second thing with its own refusal, value, dates or proof is a second record. (`litespec/core/designing#hold-one-thing-in-each-record`)
2. **A record with its own reasoning gets its own folder.** It lives at `<name>/<name>.yaml` with `<name>.md` beside it. Only closed lists with no reasoning per entry stay as entries: permissions, refusals, entry points, results, scenarios and cast entries. (`litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder`)
3. **Shape a type for its next field.** Adding a field or a section changes one record's file, never a whole set. (`litespec/core/designing#shape-each-record-type-for-its-next-field`)
4. **One home for each fact.** Every other appearance is computed, or a copy held to its source by a check. Facts a reader or tool needs are fields, never prose. (`litespec/core/principles#keep-one-authored-source`, `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy`, `litespec/core/model#keep-the-whole-specification-in-the-records`)
5. **One parent, one reason.** The folder comes from the record's content and the link field from its reason. Links sit on the record that points, and the model computes the inverse. (`litespec/core/model#place-a-record-by-its-content-and-link-it-by-its-reason`, `litespec/core/model#write-links-on-the-record-that-points`)
6. **Values that vary live in data.** Rates, thresholds and deadlines go in dated reference data that rules cite. (`litespec/core/domain-model#cite-varying-values-from-reference-data`)
7. **Every type, field and value has a reader.** That reader is a check, the book, a command or a citing record. Remove a shape when its last reader goes. (`litespec/core/designing#give-every-type-field-and-value-a-reader`)
8. **Checks match their certainty.** The schema holds shape and the model check holds the graph. Add a check only where it is cheap and sure; otherwise the provision is reviewed. Broken links, contracts and naming block; prose quality is reviewed through `litespec model review`. (`litespec/core/model#keep-shape-rules-in-the-schema`, `litespec/core/model#mark-every-check-with-its-level`)
9. **Ship a record type complete.** In the same change, add the schema definition, its home and its explanation sections, its link phrases and field-table rows, a template copied from Claimwick, a Claimwick instance, its book placement, the checks that read it, and the pages that teach it. (`litespec/core/designing#deliver-each-record-type-complete`)
10. **Change a shape everywhere at once.** Change the tools, templates, Claimwick, fixtures and pages in one change, with no compatibility path. Add a validator so the old shape cannot return. (`litespec/core/designing#change-every-instance-of-a-shape-in-one-change`, `litespec/core/authoring#exclude-cross-release-compatibility-work`)
11. **A consumer's request is evidence of a need, not a design.** Check the rule against products unlike that consumer. (`litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`)

Treat each rule as binding at its stated strength. When a task needs a departure, state it in the decision record, naming the provision and the reason (`litespec/core/designing#state-each-deviation-from-a-design-rule`). When the same rule has been set aside twice, propose an amendment to the design page instead of a third exception (`litespec/core/designing#update-a-design-rule-that-keeps-bending`).

## Spec Gaps

When an agent reports that LiteSpec cannot express a concept, model the concept as a record type, a field, or a provision before the consumer writes more code around it, and cite the gap in the change. (`litespec/core/maintaining#model-a-reported-spec-gap-before-more-code`)

A check that keeps forcing an awkward shape is a design defect in LiteSpec, not a consumer workaround. Fix the check, the record type, or the provision so the domain has a home, and check the rule against products unlike the one that reported it. (`litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`, `litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code`)

Sources: `litespec/core/maintaining#model-a-reported-spec-gap-before-more-code`, `litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code`

## Serving Many Consumers

LiteSpec serves many projects, so never name a consumer project in it. Keep every consumer's domain vocabulary, records, flows, figures, people, and products out of every page, provision, example, test fixture, template, decision, changelog entry, and tool message. Draw examples only from the Claimwick expense product or the library product. When one consumer's need prompts a change, state the general need, check the rule against products unlike that consumer, and apply it only where it makes sense across them. A need that holds for one consumer alone stays in that consumer as a project convention or waiver, not in LiteSpec.

Sources: `litespec/core/maintaining#keep-consumers-out-of-litespec`, `litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`.

## Which Repository You Are In

| Repository | You change | You run |
|:---|:---|:---|
| LiteSpec | The standard (`docs/`, `schemas/`, `templates/`, the manifest), the CLI in `apps/cli/`, the shared rule library in `apps/model/`, the analyzers in `apps/analyzers/`, and the site in `apps/site/` | `litespec-maintain validate`, `litespec-maintain provisions`, `litespec-maintain changelog`, `litespec-maintain publish-local`, the tests |
| A consumer | Its records, code, tests and contracts, against the LiteSpec release its local `litespec` tool carries | `dotnet litespec model check`, `dotnet litespec book`, `dotnet litespec rules`, and the gates its CI selects |

In this repository, run the CLI with `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`. The [command index](docs/tools/README.md) shows how to run it from a consumer. Run the maintainer tool with `dotnet run --project apps/maintain/src/LiteSpec.Maintain -- <command>`; its commands are `validate`, `provisions`, `changelog` and `publish-local`. Use `publish-local` to hand a consumer the current branch's CLI and analyzers through a local feed. With `--from-ci <run>`, it pins the consumer to the `release/*` build CI published to GitHub Packages instead of packing locally.

## Loading Context

In a consumer:

1. Read the consumer's `AGENTS.md` and `.litespec/project.yaml`.
2. Find the records the task touches with `litespec model query <id>`, read the query output, then the YAML record files themselves, which hold the whole specification, and their explanation files when you need the context they add.
3. Before you generate files or change a public boundary, read the full standards pages those records cite.
4. Before you edit inside an extension's boundary, read the extension.
5. Then look at neighboring files and follow a local pattern that complies.

In this repository, read the [authoring standard](docs/core/authoring.md) and the whole page you are changing first.

Do not load conventions the task does not touch, or extensions the project has not selected.

Sources: `litespec/core/agent#select-task-context`, `litespec/core/agent#read-the-full-record-before-changing-a-boundary`, `litespec/core/agent#load-applicable-extensions`, `litespec/core/agent#inspect-local-examples-after-standards`, `litespec/core/model#keep-the-whole-specification-in-the-records`, `litespec/core/writing#write-human-docs-for-the-cold-reader`.

## Precedence

When guidance overlaps, apply it in this order: a project waiver, a selected extension's replacement, a deeper page, then a shallower page. A waiver names the provisions it covers and carries a reason and a scope. A project never edits a provision to depart from it.

Between a record and its explanation file, the record wins; a disagreement is a defect in the explanation.

If two requirements conflict and nothing above decides, stop. Quote both provision IDs and their paths, and ask for a decision.

Sources: `litespec/core/agent#apply-guidance-precedence`, `litespec/core/agent#stop-on-unresolved-conflict`, `litespec/core/scopes#set-a-rule-aside-only-through-a-recorded-waiver`, `litespec/core/model#keep-the-whole-specification-in-the-records`, `litespec/core/writing#write-human-docs-for-the-cold-reader`.

## Placing Records

A record's folder says where its content belongs, and a link field says why the record exists. Nest a record under another record only when its whole content lies within that record. A record whose content spans several records sits at the lowest node that holds all of it and names the record it exists for in a link field: a feature stands in the folder of the problem it answers and names the goal it advances in `advances`, and a flow sits under the product and names its goal in `serves` or its problem or constraint in `answers`.

Apply this when you add a record type, give a type a new parent or home in the Placement Table, or choose between the homes a type allows. A folder chosen for the reason a record exists, rather than for its content, is the defect.

Sources: `litespec/core/model#place-a-record-by-its-content-and-link-it-by-its-reason`, `litespec/core/model#give-every-record-one-parent`, `litespec/core/model#write-links-on-the-record-that-points`, `litespec/core/model#place-child-records-in-typed-folders`.

## Making a Change

1. Confirm the scope of the request and which repositories it touches.
2. Read `git status` and leave unrelated work alone.
3. Check the manifest before you change a dependency. Add a package only with owner approval.
4. Run `litespec rules <path>` before editing a file.
5. Change the record files, explanation files, code, tests, contracts and operations that belong together in one change.
6. Keep your working files under `.agents/`.

Do not add a package the owner has not approved, a schema migration, an authentication change, a public break or an external side effect.

Sources: `litespec/core/agent#make-coherent-scoped-changes`, `litespec/core/agent#inspect-existing-work-before-editing`, `litespec/workspace/dependencies#approve-new-packages-explicitly`, `litespec/core/system#update-records-with-behavior`, `litespec/core/agent#keep-agent-working-files-under-agents`, `litespec/core/agent#restrict-unapproved-side-effects`, `litespec/core/agent#load-applicable-provisions-before-editing`.

## Writing

Write every page, example, template, README and tool message by the [LiteSpec writing and tone guidelines](WRITING.md): plainspoken technical writing, a lead paragraph, claims at their real strength, and normative meaning preserved. Records follow the [writing standard](docs/core/writing.md). Records hold every fact; explanation files restate them for people (`litespec/core/model#keep-the-whole-specification-in-the-records`, `litespec/core/writing#write-human-docs-for-the-cold-reader`). The [authoring standard](docs/core/authoring.md) states what the validators check on LiteSpec's own pages: ASCII only, one term per concept, Title Case on every heading, and nothing about earlier releases. The [Project Model](docs/core/model.md) and [Record Language](docs/core/language.md) pages state the rules a consumer's YAML record files follow.

Each page uses the sections of its page style. A Standard states one testable obligation. Every provision states its strength and where it applies, and has the same layout, in this order:

```markdown
### Keep Shared UI in One Package

**ID:** `litespec/frontend/components#keep-shared-ui-in-one-package`

**Strength:** required

**Statement:** ...

**Rationale:** ...

**Example:** ...

**Reviewed for:** ...   (or Checked by: the command, the finding code and the scope)
```

The ID line holds the ID the heading derives, and the Strength line states `required`, `default` or `advisory`. A Standard is `required` or `advisory`; a Convention or a pattern is `default`. `litespec-maintain provisions` writes the index from these lines. When you rename a provision, replace every citation of the old ID in the same change.

Sources: `litespec/core/authoring#keep-authored-prose-ascii-safe`, `litespec/core/authoring#use-one-normative-vocabulary`, `litespec/core/authoring#use-one-term-for-one-concept`, `litespec/core/authoring#use-controlled-capitalization`, `litespec/core/authoring#use-the-declared-page-contract`, `litespec/core/authoring#keep-each-standard-to-one-obligation`, `litespec/core/authoring#place-the-id-under-every-provision-heading`, `litespec/core/authoring#regenerate-the-provision-index`, `litespec/core/principles#state-the-strength-of-every-provision`, `litespec/core/maintaining#replace-every-citation-on-a-rename`, `litespec/core/model#keep-the-whole-specification-in-the-records`, `litespec/core/writing#write-human-docs-for-the-cold-reader`, `litespec/core/language#read-each-field-name-in-its-record-type`.

## Releases

Each LiteSpec release is a complete contract. Do not add compatibility, migration, deprecation or alias work between LiteSpec releases, and keep no history-specific paths, IDs or transition checks. That rule covers LiteSpec's own releases only: a consumer product's API compatibility, migration and rollback provisions still apply.

The highest git tag is the current release. The manifest, `CHANGELOG.md` and the consumer template carry the release being prepared, and tagging it makes it current. A consumer keeps a pinned release for as long as it wants and records it in `reviewedLiteSpecVersion` within `.litespec/project.yaml`.

Sources: `litespec/core/authoring#publish-each-release-as-a-complete-contract`, `litespec/core/authoring#exclude-cross-release-compatibility-work`, `litespec/core/authoring#exclude-historical-transition-material`, `litespec/core/maintaining#publish-a-litespec-release-from-a-tag`, `litespec/core/authoring#record-the-reviewed-litespec-release`.

## High-Risk Boundaries

Read the full provision before you apply any of these:

- Use the Kernel, the Platform, and one project per module, with one folder per use case inside each layer that serves it. (`litespec/packs/dotnet/architecture#separate-the-kernel-the-platform-and-the-modules`)
- Put a type that is not a use case's own model in the lowest folder that holds every consumer naming it, and give each use case its own messages, results and transport models. (`litespec/core/backend-architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`, `litespec/core/backend-architecture#give-each-use-case-its-own-models`)
- Keep Domain free of persistence, web, mediator, logging and dependency injection. (`litespec/core/backend-architecture#point-dependencies-inward`)
- Model each aggregate lifecycle with an abstract state and sealed state records. (`litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records`)
- Model each closed set in Domain as a record hierarchy instead of an enum. (`litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums`)
- Give each architecture layer its own messages, results and transport models. (`litespec/core/backend-architecture#own-each-layers-contract-types`)
- Give each rejected Domain rule its own exception type and stable refusal code. (`litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions`)
- Organize code by module, aggregate and use case, with one folder per use case inside each of `Application/`, `WebApi/` and `Infrastructure`, and place each reaction and job with the use case it runs. (`litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case`, `litespec/workspace/naming#place-each-trigger-with-what-it-runs`)
- Write commands through repositories, send no message from a handler, and commit once in the command pipeline. (`litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage`, `litespec/core/backend-persistence#commit-each-command-pipeline-once`)
- Publish integration events only from the domain events they translate, never from user code. (`litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit`)
- Read another module's state through the module's own read model, or ask one question through a decision port. (`litespec/core/backend-application#read-another-modules-state-through-a-read-model`, `litespec/core/backend-application#ask-another-module-one-question-through-a-decision-port`)
- Derive the actor from verified claims, and authorize each target from declared permissions. (`litespec/core/backend-application#enforce-target-authorization-in-the-pipeline`)
- Use the controlled shadcn-svelte baseline for Svelte. (`litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives`, `litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream`)

## User Interfaces

Each screen record names one job and one archetype, and each browser entry point declares its navigation. A route composes exactly one locked archetype from the project's UI package and fills only its slots, offers at most four commands outside a hub, and stays small. Every route runs the geometry sensors, and changed routes go to an independent visual review against the golden baselines.

If you see how an archetype could be better, finish the task on the locked one and put the proposal in your completion report. Ask nothing mid-task.

Sources: `litespec/core/ui-ia#declare-navigation-on-each-browser-entry-point`, `litespec/core/ui-ia#declare-each-screen-job-and-archetype`, `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`, `litespec/core/ui-ia#cap-each-non-hub-screen-at-four-offered-commands`, `litespec/core/ui-ia#keep-route-files-small`, `litespec/core/ui-verification#run-geometry-sensors-on-every-route`, `litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic`.

## The Book

The book is the model made readable, so it follows LiteSpec and adds nothing of its own:

1. Outline it by the record layers (Intent, Behavior, Implementation, Proof) with the governance records beside them, and place each record under the record its folder and links name.
2. Label everything with a record type, a field name, a computed state, or a glossary term.
3. Explain each term with its glossary definition or its record type's question.
4. Show only statuses, counts, orders and findings that the builder or a check computes.
5. When the book needs a fact LiteSpec does not define, add it to LiteSpec first as a record field, a computed status or a check.
6. Link every record, code element, test and finding to its file and line.

Sources: `litespec/core/model#organize-the-book-by-the-record-layers`, `litespec/core/model#name-book-labels-with-litespec-terms`, `litespec/core/model#explain-the-book-from-litespec-definitions`, `litespec/core/model#show-only-computed-judgments-in-the-book`, `litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it`, `litespec/core/model#point-every-book-entry-to-its-source`.

## Tests

Agents write most of this repository, so a test only counts when its expected value comes from somewhere the agent cannot bend: the specification, a fixed data contract, or what a user sees in the running product.

- Prove a change by running the real thing: a smoke run of the CLI against an example project, and for the book and the site a browser check of the built pages that asserts what a reader sees after the change, by role and text, never by class names or markup.
- Write a unit test only where the expected value is a fixed contract: a parser or the data a second program reads, a security rule such as link sanitizing, or an invariant that holds for every input. Include one negative and one boundary case.
- Write the expected value from the specification or the contract before reading the implementation, and check that the test fails without the change.
- Never write a test that pins wording, markup or layout, and never a test that passes against both the old and the new code.
- Never edit, skip or loosen a failing test in the change that makes it fail. Report it instead, unless the specification it proves changed in the same change.
- The book client has no unit tests: svelte-check, the build and a browser check of a real book verify it.

Sources, read 2026-11-15: Playwright best practices (https://playwright.dev/docs/best-practices) on testing user-visible behavior; Martin Fowler and Ham Vocke, The Practical Test Pyramid (https://martinfowler.com/articles/practical-test-pyramid.html), on contract tests between programs; Kent C. Dodds, The Testing Trophy (https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications), on confidence per test; Google Testing Blog, Just Say No to More End-to-End Tests (https://testing.googleblog.com/2015/04/just-say-no-to-more-end-to-end-tests.html), on keeping end-to-end checks few because they are slow and flaky; freeCodeCamp, How to Make Your Coding Agent Check Its Work in the Browser (https://www.freecodecamp.org/news/how-to-make-your-coding-agent-check-its-work-in-the-browser/), on browser checks catching defects that green unit suites missed.

## Verification

In this repository, run these before a change is done:

```bash
dotnet build LiteSpec.slnx
dotnet test --solution LiteSpec.slnx
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions --check
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
git diff --check
pnpm install --frozen-lockfile
pnpm --dir apps/cli/book check && pnpm --dir apps/cli/book build
pnpm --dir apps/cli/fe-reader check
pnpm --dir apps/site test && pnpm --dir apps/site build
```

`provisions --check` fails when the provision index is stale, so run `provisions` without `--check` to write it. The pnpm lines are the web jobs CI runs; a change that passes the .NET checks and fails them is not done.

The full .NET suite is heavy on memory, so let CI run it: push the branch and read the run. CI splits the suite by test class into ten parallel shards with `.github/scripts/test-shard.sh`. Locally, run the build and only the tests of the classes you changed, with `dotnet test --project <test project> --filter-class "<namespace>.<class>"`.

`litespec-maintain validate` checks schemas, manifest references, provision IDs, page contracts, pattern pages and their wireframes, links and verification marks.

In a consumer, run the gates that the job table in [continuous integration](docs/quality/ci.md) selects for each changed area, Playwright for the browser flows the change affects, and the checks of every selected extension.

Sources: `litespec/core/authoring#run-repeatable-authoring-checks`, `litespec/core/authoring#regenerate-the-provision-index`, `litespec/quality/ci#run-applicable-gates-on-every-pull-request`, `litespec/core/release#run-boundary-selected-checks`.

## Finishing

Compare what the change does with the active records. Report each exact command, its result, what the evidence covers, and every check you skipped with the reason. Name the provisions `rules` returned and how each was judged.

Leave no placeholder implementation, `TODO` or untracked generated output.

Sources: `litespec/core/release#report-verification-exactly`, `litespec/core/system#deliver-one-complete-use-case`, `litespec/core/release#regenerate-application-contracts`, `litespec/core/agent#name-judged-provisions-in-the-completion-report`.
