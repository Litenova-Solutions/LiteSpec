# Authoring Standard

## Intent

Every page in LiteSpec and in a project built with it shares one shape: a fixed page style, testable rules called provisions, stable identifiers for those rules, and a table naming the evidence each rule needs. This page states that shape, the prose rules for LiteSpec pages that the validators enforce, and how each release carries the complete contract, while the [Record Language](language.md) page states the grammar for everything a project writes. One shared shape lets readers learn pages once and lets validators check every page the same way.

## Concepts

### Page Authority

Standards state required boundaries, and Conventions state replaceable defaults. Everything else on a page is informative: Intent, Concepts, Rationale, Examples, guides, tutorials, and indexes explain without obliging.

`AGENTS.md` and a consumer's project agents file project the provisions an agent needs for that workspace. A projection cites the provision it projects and adds no obligation of its own. When a projection and its provision disagree, the provision wins.

`CONTRIBUTING.md` owns the contribution workflow. `CHANGELOG.md` describes the current release.

The [Record Language](language.md) page owns the grammar for everything a project writes: field names, values, record names, finding codes, and tool names.

### Documentation Layers

A reader arrives with one question, and each page answers the one for its documentation layer.

| Documentation layer | Page style | The question it answers |
|:---|:---|:---|
| 1 | Tutorial | How do I get a first working result? |
| 2 | How-to | How do I reach this one goal? |
| 3a | Topic, stack, extension, pattern | What must hold, with which values and defaults? |
| 3b | Reference, command | What are the exact values and how do I run this? |
| 4 | The `Underneath` section of a command page | What does the command run, and how do I run it myself? |

An explanation page builds understanding: what LiteSpec is, why it works the way it does, and how its parts fit. It sits beside the documentation layers because a reader takes it before or between tasks, not during one. An index routes readers to pages, and the glossary resolves one term each, so neither sits in a documentation layer. A page links down a documentation layer. Any page may link to the glossary.

### Page Styles

Every LiteSpec page uses exactly one style, decided by where the page lives.

| Page style | Where it lives | Template | Sections, in order |
|:---|:---|:---|:---|
| Topic | Any area page that owns provisions | `templates/standard/topic.md` | Intent, optional Concepts, Standards, Conventions, optional Reference Example |
| Stack | `stack.md` in a stack folder | `templates/standard/topic.md` | Intent, optional Concepts, Standards, Conventions |
| Extension | An `ext/` folder | `templates/standard/extension.md` | Intent, Activation, Baseline Relationship, Standards, Conventions, Dependencies |
| Pattern | `docs/frontend/patterns/` | [UI patterns](../frontend/patterns.md) | Intent, optional Concepts, optional Standards, optional Conventions, Patterns |
| Reference | `docs/reference/` except the generated provision index, and one page per rule in `docs/analyzers/rules/` | `templates/standard/reference.md` | Intent, Reference, optional Notes |
| Command | `docs/tools/` and `docs/maintain/` | `templates/standard/command.md` | Name, Synopsis, Description, Arguments, Options, Exit Codes, Examples, Underneath |
| How-to | `docs/guide/`, and the other pages of `docs/analyzers/` | `templates/standard/how-to.md` | Purpose, optional Prerequisites, Procedure, Check |
| Tutorial | `docs/tutorial/` | `templates/standard/tutorial.md` | Purpose, Prerequisites, Lesson, What You Built |
| Explanation | `docs/learn/` and `docs/example/` | None | Intent, then sections of its own |
| Index | A `README.md` | None | Intent, then navigation groups |
| Glossary | `docs/reference/glossary.md` | None | H2 groups in alphabetical order, each with one intro sentence, then one H3 heading and definition per term |

A consumer page is a record or one of these styles. Each one opens with the specification metadata block the record schema defines.

| Page style | Template | Sections, in order |
|:---|:---|:---|
| product-brief | `templates/consumer/product-brief.md` | Summary |
| goal | `templates/consumer/goal.md` | Why It Matters |
| problem | `templates/consumer/problem.md` | Situation, Evidence |
| feature | `templates/consumer/feature.md` | Offering |
| constraint | `templates/consumer/constraint.md` | Summary, What It Requires |
| research | `templates/consumer/research.md` | Answer, optional Notes on Sources |
| decision | `templates/consumer/decision.md` | Context, Decision, Consequences, optional Evidence with Confidence |
| topic | `templates/consumer/topic.md` | Summary, The Situation, What We Do, Why It Is Better, Example, Costs and Risks, How Sure We Are, Position |
| journey | `templates/consumer/journey.md` | Example, optional Before It Starts, Between the Steps |
| module | `templates/consumer/module.md` | optional Example, optional Known Limits, optional Notes |
| aggregate | `templates/consumer/aggregate.md` | Transitions, Data, optional Example, optional Known Limits, optional Notes |
| use-case | `templates/consumer/use-case.md` | Goal, What Happens except on queries, optional Risk, optional Example, optional Notes |
| rule-set | `templates/consumer/rule-set.md` | Summary, optional Notes |
| actor | `templates/consumer/actor.md` | Summary |
| role | `templates/consumer/role.md` | Summary |
| external-system | `templates/consumer/external-system.md` | Summary |
| permission-list | `templates/consumer/permission-list.md` | Entries only |
| entry-point-list | `templates/consumer/entry-point-list.md` | Entries only |
| screen | `templates/consumer/screen.md` | Summary |
| setting-set | `templates/consumer/setting-set.md` | Entries only |
| automation | `templates/consumer/automation.md` | Summary, optional Example, optional Notes |
| notification | `templates/consumer/notification.md` | Summary, optional Example, optional Notes |
| integration-event | `templates/consumer/integration-event.md` | Summary, optional Notes |
| retention-schedule | `templates/consumer/retention-schedule.md` | Entries only |
| runbook | `templates/consumer/runbook.md` | Impact, Prerequisites, Procedure, Verification, Rollback or Recovery, Escalation |
| exception | `templates/consumer/exception.md` | Consequences for a departure, Exit for a gap |
| glossary | `templates/consumer/glossary.md` | Generated region only |
| example-cast | `templates/consumer/example-cast.md` | The Organization, The People, The Occasion, The Amounts, Recurring Situations, optional What This Page Does Not Do |
| tutorial | `templates/consumer/tutorial.md` | Purpose, Prerequisites, Lesson, What You Built |
| how-to | `templates/consumer/how-to.md` | Purpose, optional Prerequisites, Procedure, Check |
| reference | `templates/consumer/reference.md` | Intent, Reference, optional Notes |
| review-report | `templates/consumer/review-report.md` | Verdicts, How the Review Ran, optional Changes Made, optional Held |
| command | `templates/consumer/command.md` | Name, Synopsis, Description, Arguments, Options, Exit Codes, Examples, Underneath |
| configuration | `templates/consumer/configuration.md` | Intent, Settings, Precedence, optional Notes |
| index | `templates/consumer/index.md` | Intent, then navigation groups |

These rules apply to every page style:

- A page that owns provisions lives at `docs/<area>/<page>.md` or deeper, with a lowercase file name.
- A page has one H1.
- The index page of a section or folder, its `README.md`, is titled `Overview`, so a reader tells a section from its first page. The documentation root index keeps its own title. A link to a section index names its section: `[Backend overview](../backend/README.md)`.
- A required section with nothing to say contains only `None.`
- An example sits inside the provision it illustrates, or in a Reference Example that names every provision it demonstrates.
- A tutorial, how-to, explanation, reference, command, or index page owns no provisions.
- A tutorial follows one path. An option or an alternative belongs on a how-to page.
- A Standards section opens directly with its first provision heading; provision headings are the content.
- A Concepts section holds definitions, diagrams, and vocabulary; obligations belong in Standards.
- An Activation section states the activation scope, the kinds, and the criterion that selects the extension.
- A Baseline Relationship section compares against the baseline it replaces; obligations belong in Standards.

### Provisions

A provision is a `###` heading inside one of three sections:

| Section | Kind | Heading |
|:---|:---|:---|
| `## Standards` | rule | Starts with an action verb, such as `Keep Shared UI in One Package` |
| `## Conventions` | convention | Starts with an action verb and names the replaceable default |
| `## Patterns` | pattern | The pattern's name, such as `Record header` |

A Standard states one testable obligation:

```markdown
### Keep Shared UI in One Package

**Statement:** A workspace MUST hold its shared primitives and composites in one shared UI package.

**Deviation:** Only with SHOULD or SHOULD NOT: the bounded reason that permits a departure.

**Rationale:** Why the rule exists.

**Example:** What following it looks like.
```

- The Statement is one sentence with exactly one capitalized modal, as [RFC 2119](https://www.rfc-editor.org/info/rfc2119/) and [RFC 8174](https://www.rfc-editor.org/info/rfc8174/) define them.
- A different obligation, prohibition, or permission is a separate provision.
- Conditions and scope belong in the Statement sentence.
- Every other paragraph of the provision sits under a Rationale or an Example label and adds no obligation.
- A Deviation appears if and only if the Statement uses `SHOULD` or `SHOULD NOT`.
- `SHOULD` keeps the standard's rule with a deviation; a Convention is a project-owned default the project replaces by naming its own convention, not by recording a deviation.

A Convention states a default a project may replace:

```markdown
### Use the Default Location

**Default:** Store the catalog at `docs/ui/patterns.json`.

**Replacement:** A project names another path in `paths.uiPatterns`.

**Rationale:** Why this is the default.
```

A project that departs from a Standard records a deviation that cites the provision. A project that replaces a Convention names its own convention in its documentation. A Replacement names the local convention or its recording place, not a deviation. A convention with no specific recording place uses `A consumer can replace this default with an explicit local convention.` as its whole Replacement. The [model page](model.md) states the deviation record.

A pattern provision carries its question, its default option, its compact behavior, and its rationale, and then its options with their wireframes. [UI patterns](../frontend/patterns.md) states the pattern contract and the wireframe language.

### Provision Identifiers

Every provision ID is `litespec/<page>#<slug>`:

| Segment | Source |
|:---|:---|
| `litespec` | The source that owns the provision |
| `<page>` | The page path under `docs/`, without `.md` |
| `<slug>` | The heading: lowercase, punctuation removed, spaces as hyphens |

`### Keep business invariants in Domain` on `docs/backend/architecture.md` is `litespec/backend/architecture#keep-business-invariants-in-domain`. The same text is the heading's address on the published site. Headings keep a stable verb-noun head, because a reword renames the ID.

The heading carries no identifier text. A renamed heading renames the ID, so the two stay in agreement. No two pages share a path, so no two provisions share an ID. Consumer record identifiers follow the [identifier page](../backend/identifiers.md).

### Record Identifiers

A consumer record identifier is `type/home.name` in kebab-case: the type names the record kind, and `home.name` names the module or set that owns it. The [model page](model.md) states the form each type uses, and the [identifier page](../backend/identifiers.md) states the name rules, the checks, and how citations resolve. The [Record Language](language.md) page states the name form each record type uses.

### Provision Layout

Every provision has the same parts in the same order:

```markdown
### Keep Shared UI in One Package

**ID:** `litespec/frontend/components#keep-shared-ui-in-one-package`

**Statement:** Shared primitives and composites MUST live in the one shared UI package.

**Rationale:** ...

**Example:** ...

**Reviewed for:** Which shared primitives or composites live outside the shared package? Look in the workspace packages.
```

1. The heading names the provision.
2. The ID line holds the ID the heading derives, so a reader copies it from where the provision stands.
3. A rule states its `Statement`; a convention states its `Default` and `Replacement`; a pattern states its `Question`, `Default`, `Compact` and `Rationale`.
4. `Rationale` and `Example` follow.
5. The verification line comes last among the fields. `Checked by` marks a provision a tool checks (`checkable`) and starts with the checking command in backticks, then the code the tool reports where the tool emits one, then the scope. `Reviewed for` marks a provision a person judges (`guidance`) and asks the one question a reviewer answers, naming the observable evidence and where to look. Evidence like "verify compliance" says nothing and is refused.

A pattern's options follow its verification line. `litespec-maintain provisions` writes the [provision index](../reference/provisions.md) from these lines; nothing else repeats them.

### Validator Diagnostics

These checks report page defects that no single provision owns, so they are listed here instead of beside a provision.

| Code | Meaning |
|:---|:---|
| `LINK_BROKEN` | A link names a file that does not exist or a malformed percent escape. |
| `ANCHOR_BROKEN` | A link anchor names no heading in its target file. |
| `PAGE_TITLE_COUNT` | A page carries other than one H1. |
| `HEADING_EMPTY_BODY` | A heading carries no body and no nested heading. |
| `PATTERN_INVALID` | A pattern page or wireframe breaks the pattern contract in [UI patterns](../frontend/patterns.md). |

### Generated Regions

A generated region is a section a tool writes, between `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`. Only the owning tool writes inside it, and an edit there is lost at the next generation. Authoring checks skip generated regions. Inside Intent and Behavior records, a generated region derives from other records only.

### Authoring at Scale

Many authors, often agents, write records in parallel. `litespec new` scaffolds a page and reserves its identifier before prose exists, and `litespec rename` rewrites every link when an identifier changes. Validating one page before committing keeps each commit sound without walking the whole repository.

### Release Model

Each LiteSpec release is a complete contract that states every active provision. LiteSpec publishes no compatibility guarantee, migration path, deprecation period, or identifier alias between its own releases. A consumer pins one release, keeps it for as long as it chooses, and adopts a later release by accepting that release's whole contract.

Version numbers name complete pinned contracts. A patch release corrects a narrow area, a minor release makes one coherent change, and a major release replaces scope, method, or a stack.

This model governs LiteSpec itself. A consumer product has its own users, so its API compatibility, migration, deprecation and rollback provisions still apply.

## Standards

### Keep Authored Prose ASCII-Safe

**ID:** `litespec/core/authoring#keep-authored-prose-ascii-safe`

**Statement:** Authored prose MUST contain only ASCII unless code, data, or mathematical content requires another character.

**Rationale:** Non-ASCII characters break in terminals, diffs, generated context, and plain-text tools: a curly quote becomes unreadable in one reader and a silent mismatch in another. ASCII prose renders the same everywhere the standard is read or checked.

**Example:** Write a hyphen or a comma where a typographic dash would go.

**Checked by:** `litespec-maintain validate` emits no `PROSE_NON_ASCII` diagnostic.

### Exclude Declared Content Paths from the ASCII Check

**ID:** `litespec/core/authoring#exclude-declared-content-paths-from-the-ascii-check`

**Statement:** An ASCII check MUST exclude only the natural-language content paths that the project record declares.

**Rationale:** A product whose subject is another language carries that language in its content files, where a diacritic is part of a word rather than a styling choice. Excluding only the declared content paths keeps the check strict everywhere else: documents about that content stay in scope, so styled prose filed beside content still meets the check.

**Reviewed for:** Which non-ASCII prose sits outside the paths the project record declares? Look at the content files beside the declaration.

### Use One Normative Vocabulary

**ID:** `litespec/core/authoring#use-one-normative-vocabulary`

**Statement:** A Standards provision MUST use exactly one approved capitalized modal and no other capitalized normative term.

**Rationale:** Strength words work only when each one means exactly one thing. The provision contract defines the approved modals with their RFC meaning, so a reader knows whether a word obliges or suggests, and no second capitalized term sneaks in a different shade of obligation.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_MODAL_COUNT` or `STANDARD_MODAL_VOCABULARY` diagnostic.

### Restrict Normative Language to Standards and Conventions

**ID:** `litespec/core/authoring#restrict-normative-language-to-standards-and-conventions`

**Statement:** A capitalized modal MUST appear only in a Statement, a Deviation, a Default, or a Replacement.

**Rationale:** Obligation hides when it sits in Concepts, Rationale, or an example: a project replaces a default it saw nowhere in the labeled assertions. One location for every modal lets readers and tools find each obligation in the same place. A prohibition written as `cannot` and a rule written as `never` belong in the same labels.

**Checked by:** `litespec-maintain validate` emits no `PROSE_NORMATIVE_LOCATION` or `STANDARD_INFORMATIVE_NORMATIVE` diagnostic.

### Use One Term for One Concept

**ID:** `litespec/core/authoring#use-one-term-for-one-concept`

**Statement:** Authored prose MUST name each concept with its one glossary term.

**Rationale:** A second name for one concept reads as a second concept: the reader hunts for a distinction the writer did not mean. One term per concept means each term is defined once and checked once.

**Reviewed for:** Which concepts appear under more than one name? Look at the terms the change adds, beside the glossary.

### Record the Project Vocabulary as Data

**ID:** `litespec/core/authoring#record-the-project-vocabulary-as-data`

**Statement:** A consumer MUST record each term, its rejected synonyms, the scope of each rejection, and its mannered terms in the project glossary satisfying `schemas/glossary.schema.json`.

**Rationale:** A glossary column headed "Avoid" speaks to a reader and to nothing else: no tool can check it. A rule held as data reaches the reader and the validator alike, so the same rejection appears in prose guidance and in diagnostics.

**Example:** A rejection carries the scope that binds it, because a word that is right in one module is wrong in another.

```json
{ "id": "term/customer", "term": "customer", "meaning": "A person or company that buys.",
  "owner": "__OWNER__", "status": "approved",
  "rejected": [{ "word": "client", "scope": "^domain/modules/orders/",
    "reason": "client names the API consumer" }] }
```

**Checked by:** `litespec consumer` validates the project glossary against the glossary schema.

### Reject a Recorded Synonym Inside Its Scope

**ID:** `litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope`

**Statement:** Authored prose MUST NOT use a synonym that the project glossary rejects for the scope the page sits in.

**Rationale:** Term drift is invisible one page at a time and plain across a module: each page looks fine while the project slowly speaks two languages. The expensive case is a synonym that is a defined term elsewhere, because one word then names two things and every reader guesses which one.

**Checked by:** `litespec consumer` reports no `GLOSSARY_REJECTED_SYNONYM` finding.

### State Mannered Terms Literally

**ID:** `litespec/core/authoring#state-mannered-terms-literally`

**Statement:** Authored prose MUST NOT use a mannered term the project glossary lists.

**Rationale:** A metaphor or stock phrase reads smoothly while hiding what the project means: each reader pictures a different act behind the same flourish. The glossary lists each mannered term with the literal phrase that carries its meaning, so the check points at the flourish and names the plain words to reach for.

**Example:** The glossary lists `{ "term": "leverage", "instead": "use" }`, so prose reads "use the retry queue" rather than "leverage the retry queue".

**Checked by:** `litespec consumer` reports no `GLOSSARY_MANNERED_TERM` finding.

### Check the Vocabulary on Every Surface a Reader Meets

**ID:** `litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets`

**Statement:** A consumer MUST name every non-Markdown surface its vocabulary reaches in `paths.glossaryScan`, including source, interface copy, the API contract, and acceptance tests.

**Rationale:** Documentation is often the smallest surface a reader meets; names in code, interface copy, contracts, and tests carry the vocabulary further. A rejection that stops at prose lets `ClientOrderId` breach a rejection of `client` where readers meet it most. A pattern that matches no file fails, so every listed surface meets the check.

**Example:** A pattern that matches no file is an error, so every listed surface meets the check.

```json
{ "paths": { "glossaryScan": ["apps/*/lib/i18n/dictionaries/*.json", "apps/api/tests/**/*.feature"] } }
```

**Checked by:** `litespec consumer` scans every `glossaryScan` pattern and fails a pattern that matches no file.

### Exclude the Compound That Names Another Concept

**ID:** `litespec/core/authoring#exclude-the-compound-that-names-another-concept`

**Statement:** A glossary rejection MUST list, in `except`, every compound in which a rejected word names a concept other than the one the rejection is about.

**Rationale:** A rejection matches a word, and a word can belong to more than one term: Claimwick rejects `docket` in favor of `receipt` for payment proof while a docket still names the shipping list. Without the `except` list, the check would force the project to rename an unrelated concept or drop a sound rejection.

**Example:** The exemption covers the compound only, so the bare word stays rejected.

```json
{ "word": "docket", "reason": "one word for a payment receipt and a shipping list",
  "scope": "^docs/domain/modules/spending/", "except": ["shipping docket"] }
```

**Checked by:** `litespec consumer` reports no `GLOSSARY_REJECTED_SYNONYM` finding inside an exempt compound.

### Exclude Generated Regions from Prose Checks

**ID:** `litespec/core/authoring#exclude-generated-regions-from-prose-checks`

**Statement:** An authoring check MUST skip every generated region.

**Rationale:** A generated region holds tool output that no author wrote and no author can fix by editing the page. Checking it would fail pages for words outside author control and train writers to ignore diagnostics.

**Reviewed for:** Which findings point inside generated-region fences? Look at the findings the checks report.

### Use Controlled Capitalization

**ID:** `litespec/core/authoring#use-controlled-capitalization`

**Statement:** A page MUST use Title Case for its title and every heading.

**Rationale:** Titles and headings read as names in indexes, tabs, and search results. Title Case capitalizes every word except articles (`a`, `an`, `the`), coordinating conjunctions (`and`, `but`, `for`, `nor`, `or`, `so`, `yet`), and prepositions of four letters or fewer (`as`, `at`, `by`, `for`, `from`, `in`, `into`, `of`, `off`, `on`, `onto`, `over`, `per`, `to`, `up`, `upon`, `via`, `with`); the first and last words stay capitalized, and code, identifiers, and file names keep their own case.

**Checked by:** `litespec-maintain validate` emits no `PAGE_TITLE_CASE` or `PAGE_HEADING_CASE` diagnostic.

### Use the Declared Page Contract

**ID:** `litespec/core/authoring#use-the-declared-page-contract`

**Statement:** An authored page MUST use the sections, in order, of its page style.

**Rationale:** A fixed shape puts the authority, the context, the defaults, and the evidence in the same place on every page. Readers learn the shape once, and tools find each section without guessing. A page that invents its own order sends both searching.

**Checked by:** `litespec-maintain validate` emits no `PAGE_MISSING_SECTION`, `PAGE_SECTION_ORDER` or `PAGE_UNKNOWN_SECTION` diagnostic.

### Limit Each Intent to Three Sentences

**ID:** `litespec/core/authoring#limit-each-intent-to-three-sentences`

**Statement:** An Intent section MUST hold at most three sentences.

**Rationale:** The first sentence states the scope and boundary. The second states who applies the page and when. An optional third sentence relates the page to its neighbors.

**Reviewed for:** Which Intent sections run past three sentences? Look at the Intent section of each page the change touches.

### Write None in an Empty Required Section

**ID:** `litespec/core/authoring#write-none-in-an-empty-required-section`

**Statement:** A required section with nothing to say MUST contain only `None.`.

**Rationale:** One fixed marker reads the same on every page and gives the validator one shape to accept.

**Checked by:** `litespec-maintain validate` emits no `PAGE_EMPTY_SECTION` or `PAGE_EMPTY_SECTION_TEXT` diagnostic.

### Route the Reader Before Listing Pages

**ID:** `litespec/core/authoring#route-the-reader-before-listing-pages`

**Statement:** A documentation root index MUST state which page style answers which reader question before its first navigation group.

**Rationale:** A reader without a map from question to page opens both and trusts neither. Naming each style in the reader's terms before the first navigation group sends each question to the page that answers it.

**Example:** A `Which Page Do You Want` section names each style in the reader's terms, one sentence each.

**Checked by:** `litespec-maintain validate` emits no `INDEX_MISSING_ROUTING` diagnostic.

### Name the Tool Rather Than the Mechanism

**ID:** `litespec/core/authoring#name-the-tool-rather-than-the-mechanism`

**Statement:** A tutorial step or how-to step MUST name the repository command that performs it, where one exists.

**Rationale:** A procedure that lists what a command already does becomes a second copy of that command's behavior. The copy drifts from the tool, and readers follow stale steps. Naming the repository command delegates the steps to the tool that performs them.

**Example:** A step reads ``Run `./shop up` `` rather than listing the compose start, the readiness wait, and each process launch.

**Reviewed for:** Which procedure steps lack a repository command? Look at the steps the change adds.

### State One Layer per Page

**ID:** `litespec/core/authoring#state-one-layer-per-page`

**Statement:** A page MUST NOT describe both a command and the mechanism that command runs, outside an `Underneath` section.

**Rationale:** A reader who wanted the command reads mechanism detail as noise, and a reader who wanted the mechanism reads the command wrapper as an obstacle. One documentation layer per page serves each reader, and the `Underneath` section holds the mechanism for the reader who asks for it.

**Reviewed for:** Which command pages mix mechanism into other sections? Look outside the Underneath section of each command page the change touches.

### Name the Escape from Every Abstraction

**ID:** `litespec/core/authoring#name-the-escape-from-every-abstraction`

**Statement:** A command page MUST state under `Underneath` what the command runs or the sentence `Nothing beneath this command is separately runnable.`.

**Rationale:** A wrapper nobody can see through leaves a failure with no command to run by hand and no documentation layer to inspect. Stating what the command runs, or that nothing beneath it is separately runnable, gives every failure a next step.

**Example:** A command with nothing separately runnable writes `Nothing beneath this command is separately runnable.` under `Underneath`.

**Checked by:** `litespec-maintain validate` emits no `PAGE_MISSING_SECTION` diagnostic for a command page.

### Record a Setting as Reference

**ID:** `litespec/core/authoring#record-a-setting-as-reference`

**Statement:** A configuration setting a reader can change MUST appear on a configuration page with its default, its scope, and what overrides it.

**Rationale:** A setting introduced inside a procedure is found only by the reader who already knows which procedure mentioned it. A configuration page with the default, scope, and overrides puts every setting where a reader looks it up.

**Reviewed for:** Which procedure-named settings lack a configuration page row? Look at the settings the change adds, beside the configuration pages.

### Cite Owned Content Instead of Copying It

**ID:** `litespec/core/authoring#cite-owned-content-instead-of-copying-it`

**Statement:** A page MUST link to content another page owns instead of restating it.

**Rationale:** A copy drifts from its source with the next unrelated edit, and readers follow the stale one. One home for each fact keeps every reader on the current text.

**Reviewed for:** Which passages restate content another page owns? Compare repeated passages with the owning page.

### Keep Principle Pages Free of Stack Detail

**ID:** `litespec/core/authoring#keep-principle-pages-free-of-stack-detail`

**Statement:** A principle page MUST NOT name a framework, a package, or a language-specific construct.

**Rationale:** Technology lives in stack pages, so a principle holds for every stack. A framework name in a principle binds every stack to one choice.

**Reviewed for:** Which principle pages name stack detail? Look for framework, package, and language names in principle pages.

### Place Each Stack Binding on Its Stack Page

**ID:** `litespec/core/authoring#place-each-stack-binding-on-its-stack-page`

**Statement:** A stack-specific section MUST live on its stack page rather than on a shared page.

**Rationale:** Shared pages stay neutral across stacks. Each stack page carries its own bindings, so a reader finds one stack's detail in one place.

**Reviewed for:** Which shared pages carry stack-specific sections? Look for stack sections outside stack pages.

### Split Extension Dependencies into Three Labeled Lines

**ID:** `litespec/core/authoring#split-extension-dependencies-into-three-labeled-lines`

**Statement:** An extension Dependencies section MUST carry the lines Packages, Related extensions, and Guides.

**Rationale:** Fixed labels separate package needs from related extensions and guides. A line with nothing to name holds `None.`.

**Reviewed for:** Which Dependencies sections lack a labeled line? Look in each page under an `ext/` folder.

### Define Each Glossary Term Under Its Heading

**ID:** `litespec/core/authoring#define-each-glossary-term-under-its-heading`

**Statement:** A glossary entry MUST hold its definition in the paragraph under its term heading.

**Rationale:** The heading anchor already carries the term identifier, so a `Term` line repeats it. Lines after the definition name the record layer, established aliases, the source, and rejected synonyms where they exist.

**Reviewed for:** Which glossary entries open with something other than the definition, or carry a Term line? Look in `docs/reference/glossary.md`.

### Order Glossary Groups Alphabetically

**ID:** `litespec/core/authoring#order-glossary-groups-alphabetically`

**Statement:** Glossary groups MUST stand in alphabetical order, each opening with one intro sentence.

**Rationale:** Groups in a fixed order with one opening sentence tell a reader where a term lives before any term is read. An unordered group hides its terms from a reader who guesses the wrong place, and a group without its sentence opens on terms with no shared context.

**Example:** The code, product, and standard term groups open the glossary in that order, each with one sentence saying what its terms name.

**Checked by:** `litespec-maintain validate` emits no `GLOSSARY_ORDER` or `GLOSSARY_DEFINITION` diagnostic.

### Write Atomic Standards Provisions

**ID:** `litespec/core/authoring#write-atomic-standards-provisions`

**Statement:** A Standards provision MUST follow the Standard contract this page defines.

**Rationale:** One provision with one obligation has one authority, one identifier, and one verification line. A provision that bundles two obligations leaves a project half-compliant with no identifier for the half it missed.

**Example:** `### Keep Business Invariants in Domain` owns one obligation about where invariants are enforced.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_LABEL_INVALID`, `STANDARD_LABEL_ORDER`, `STANDARD_LABEL_DUPLICATE`, `STANDARD_SENTENCE_COUNT`, `STANDARD_MODAL_COUNT`, `STANDARD_MODAL_VOCABULARY`, `STANDARD_MISSING_DEVIATION`, `STANDARD_UNEXPECTED_DEVIATION`, `STANDARD_DEVIATION_NORMATIVE`, `STANDARD_DEVIATION_SENTENCE`, `STANDARD_MISSING_REQUIREMENT`, `STANDARD_REQUIREMENT_COUNT`, `STANDARD_UNLABELED_CONTENT`, or `PROVISION_RESTATES_HEADING` diagnostic.

### Identify Actionable Conventions

**ID:** `litespec/core/authoring#identify-actionable-conventions`

**Statement:** A Convention provision MUST follow the Convention contract this page defines.

**Rationale:** A Convention heading under `## Conventions` makes each replaceable default traceable: a project cites exactly the default it replaces. Without the contract, defaults hide in prose and replacements cite paragraphs instead of identifiers.

**Checked by:** `litespec-maintain validate` emits no `CONVENTION_MISSING_DEFAULT`, `CONVENTION_MISSING_REPLACEMENT`, `CONVENTION_DEFAULT_SENTENCE`, `CONVENTION_REPLACEMENT_SENTENCE`, `CONVENTION_REPLACEMENT_DEVIATION`, or `PROVISION_RESTATES_HEADING` diagnostic.

### Attach Examples to Their Provisions

**ID:** `litespec/core/authoring#attach-examples-to-their-provisions`

**Statement:** An informative example MUST belong to its provision or to a Reference Example that names every provision it demonstrates.

**Rationale:** An example floating free of its provision reads as a second rule: readers obey the illustration instead of the obligation. Belonging to its provision, or to a Reference Example that names every provision it demonstrates, marks it as illustration.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_EXAMPLE_LABEL`, `PAGE_UNKNOWN_SECTION`, or `REFERENCE_EXAMPLE_DECLARATION` diagnostic.

### Use the Declared Identifier Grammar

**ID:** `litespec/core/authoring#use-the-declared-identifier-grammar`

**Statement:** A provision ID MUST be `litespec/<page>#<slug>`, computed from its page path and its heading.

**Rationale:** The page names the file and the slug names the heading, so a citation resolves with no lookup table. One grammar for every identifier keeps citations writable by hand and checkable by tool.

**Example:** `litespec/frontend/components#compose-composites-in-page-code` is a heading on the frontend components page.

**Checked by:** `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Name the Owning Page in the Identifier

**ID:** `litespec/core/authoring#name-the-owning-page-in-the-identifier`

**Statement:** A provision ID's page segment MUST be its page's path under `docs/` without the `.md` extension.

**Rationale:** The page segment is the file a reader opens and the address the provision carries on the published site. Deriving it from the path keeps the identifier and the file from disagreeing.

**Example:** `litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case` and `litespec/frontend/blazor/structure#organize-features-by-module-and-use-case` are on two different pages.

**Checked by:** `litespec-maintain validate` emits no `ID_PAGE_FILENAME` or `ID_DUPLICATE` diagnostic.

### Keep Identifiers Out of Provision Headings

**ID:** `litespec/core/authoring#keep-identifiers-out-of-provision-headings`

**Statement:** A provision heading MUST carry no identifier text.

**Rationale:** The page path and the heading already state the identifier. An identifier written into the heading repeats it at best and contradicts it at worst, after a rename touches one and misses the other.

**Checked by:** `litespec-maintain validate` emits no `ID_IN_HEADING` diagnostic.

### Exclude a Sequence Number from the Identifier

**ID:** `litespec/core/authoring#exclude-a-sequence-number-from-the-identifier`

**Statement:** A provision ID MUST NOT contain a sequence number.

**Rationale:** A counter orders a provision among its siblings and says nothing about what it requires. Words name the obligation; numbers would renumber every sibling on each insertion and break citations for no gain.

**Reviewed for:** Which identifiers carry a sequence number? Look at the provision IDs the change adds.

### Use the Declared Record Identifier Grammar

**ID:** `litespec/core/authoring#use-the-declared-record-identifier-grammar`

**Statement:** A consumer record identifier MUST be `type/home.name` in kebab-case, using the form the Project Model states for its type.

**Rationale:** The type names the record kind and `home.name` names the owning module or set, so an identifier resolves with no lookup table. One grammar for every record kind keeps identifiers writable by hand and checkable by tool. The [model page](model.md) states the form each type uses, and the [identifier page](../backend/identifiers.md) states the name rules and checks.

**Example:** `reaction/spending.send-approver-reminders-on-expense-submitted` is a reaction owned by the spending module, and `journey/submit-and-reimburse#s2` is the second step of a journey.

**Checked by:** `litespec consumer` fails a record identifier outside the declared grammar.

### Place the ID Under Every Provision Heading

**ID:** `litespec/core/authoring#place-the-id-under-every-provision-heading`

**Statement:** Every provision heading MUST be followed by an ID line holding the ID the heading derives.

**Rationale:** A reader who cites a provision copies its ID from where the provision stands, instead of deriving a slug by hand or looking it up elsewhere. The heading stays the source: an ID line that disagrees with its heading is refused.

**Example:** `### Keep Shared UI in One Package` on `docs/frontend/components.md` is followed by the ID line `litespec/frontend/components#keep-shared-ui-in-one-package`.

**Checked by:** `litespec-maintain validate` emits no `ID_LINE_MISSING` or `ID_LINE_MISMATCH` diagnostic.

### Map Provisions to Evidence

**ID:** `litespec/core/authoring#map-provisions-to-evidence`

**Statement:** Every provision MUST end its fields with exactly one verification line.

**Rationale:** A provision with no verification line is a rule nobody checks: people assume a tool covers it while no tool was told. One line per provision, beside the rule it checks, makes each rule reviewable by a person or a tool.

**Checked by:** `litespec-maintain validate` emits no `VERIFY_MISSING_ID`, `VERIFY_DUPLICATE_ID`, `VERIFY_ORDER`, or `VERIFY_HEADER` diagnostic.

### Mark Every Provision Checkable or Guidance

**ID:** `litespec/core/authoring#mark-every-provision-checkable-or-guidance`

**Statement:** A verification line MUST open with `Checked by` for a provision a tool checks or `Reviewed for` for one a person judges.

**Rationale:** The label tells a reader who judges the provision: a tool (`checkable`) or a person (`guidance`). Without it, a team automates a judgment call or hand-reviews what a command already proves.

**Checked by:** `litespec-maintain validate` reports `VERIFY_MISSING_ID` for a provision whose verification line opens with any other label.

### Name Observable Evidence in Verification Lines

**ID:** `litespec/core/authoring#name-observable-evidence-in-verification-lines`

**Statement:** A verification line MUST name the observable evidence and where the reviewer looks.

**Rationale:** A line that restates its Statement sends the reviewer in a circle: the evidence confirms that the rule says what it says. Naming the artifact and its location turns the line into an instruction the reviewer follows.

**Checked by:** `litespec-maintain validate` emits no `VERIFY_GENERIC_EVIDENCE`, `VERIFY_TEMPLATED_EVIDENCE`, `VERIFY_DUPLICATE_EVIDENCE`, or `VERIFY_COMMAND` diagnostic.

### Give One Observable Fact One Mark Across Pages

**ID:** `litespec/core/authoring#give-one-observable-fact-one-mark-across-pages`

**Statement:** One observable fact MUST carry one mark across pages.

**Rationale:** A fact checked by a tool on one page and judged by a person on another leaves each reviewer unsure which judgment holds.

**Reviewed for:** Which shared facts carry different labels? Compare the verification lines that name one check, using the provision index.

### Regenerate the Provision Index

**ID:** `litespec/core/authoring#regenerate-the-provision-index`

**Statement:** A standards change MUST leave `docs/reference/provisions.md` equal to the output of `litespec-maintain provisions`.

**Rationale:** One generated page resolves every identifier to its heading and page, so a reader follows a citation in one step. Regenerating it with `litespec-maintain provisions` on every standards change keeps the index and the pages from disagreeing.

**Checked by:** `litespec-maintain provisions --check` exits zero.

### Declare Structured Specification Metadata

**ID:** `litespec/core/authoring#declare-structured-specification-metadata`

**Statement:** A consumer page MUST open with a specification metadata block satisfying the record schema for its declared type.

**Rationale:** The record schema owns the field list of every record type and page style. The block is one JSON object between two lines that hold only `---`, which people read as a header and tools parse as data. The [model page](model.md) states the record types and their homes.

**Example:** A use-case page opens with this block.

```markdown
---
{
  "type": "use-case",
  "id": "use-case/spending.submit-expense",
  "title": "Submit Expense",
  "owner": "__OWNER__",
  "status": "approved"
}
---
```

**Checked by:** `litespec consumer` validates each opening specification metadata block against the record schema.

### Use One Metadata Carrier

**ID:** `litespec/core/authoring#use-one-metadata-carrier`

**Statement:** A consumer page MUST use its opening specification metadata block as its only metadata carrier.

**Rationale:** Two metadata carriers on one page disagree sooner or later: source links, paths, and evidence drift between the header and the body. One opening block keeps metadata in one place, while the body sections carry the links, paths, and evidence beside what they describe.

**Reviewed for:** Which pages carry metadata outside the opening block? Look at the body sections of each page the change touches.

### Classify Every Specification File

**ID:** `litespec/core/authoring#classify-every-specification-file`

**Statement:** Every Markdown file under the consumer documentation root MUST declare a record type or page style, or sit under a declared unstructured path.

**Rationale:** A file the validator leaves unclassified is skipped, and a skipped file looks sound: its errors stay hidden. Requiring every file to declare a type or sit under a declared unstructured path leaves no page unchecked. A directory index that owns no record uses the index style.

**Checked by:** `litespec consumer` fails a Markdown file with no declared type outside a declared unstructured path.

### Derive a Computed Fact Instead of Restating It

**ID:** `litespec/core/authoring#derive-a-computed-fact-instead-of-restating-it`

**Statement:** A specification MUST name the source or the command that produces any count, census, or membership list derived between records.

**Rationale:** A restated number is right until the next unrelated edit changes the records it counts, and nothing reports when it goes stale. Naming the source or the command gives every reader the current figure instead of the figure from commit day. Derivation between records is allowed: generic refusals derive from `access` and `input`, and computed statuses appear in the book and the model output only.

**Example:** A chapter states where its numbers come from rather than copying them.

```markdown
The figures below render from the retention schedule at build time.
```

**Reviewed for:** Which counts, censuses, or membership lists lack a source or command? Look at the numbers the change adds.

### Keep Code Out of Intent and Behavior Fields

**ID:** `litespec/core/authoring#keep-code-out-of-intent-and-behavior-fields`

**Statement:** No field on an Intent or Behavior record MUST be filled from code.

**Rationale:** Code implements Behavior, so a field filled from an implementation restates what already ships instead of stating the promise under review. A fact learned from the world enters Intent as research or a constraint, cited and dated, and a need the upper record layer does not state travels up as a draft question. Derivation between records stays allowed.

**Example:** A positivity guard found in a handler becomes a draft rule with its question, while the code stays untouched until the rule is reviewed.

**Reviewed for:** Which Intent or Behavior fields copy values from code? Look at the fields the change fills, beside the implementation.

### Scaffold Pages and Reserve Identifiers

**ID:** `litespec/core/authoring#scaffold-pages-and-reserve-identifiers`

**Statement:** An author MUST scaffold a new page with `litespec new <type> <id>`, which reserves its identifier.

**Rationale:** Two agents working in parallel mint the same identifier when each writes a file by hand, and the collision surfaces only at merge time. Scaffolding reserves the identifier before any prose exists, so the second author learns the clash at creation and picks another name.

**Example:** `litespec new reaction spending.send-approver-reminders-on-expense-submitted` creates the reaction page with its metadata block and reserved identifier.

**Reviewed for:** Which new pages lack a reserved identifier? Look for pages created without `litespec new`.

### Rewrite Every Link on a Rename

**ID:** `litespec/core/authoring#rewrite-every-link-on-a-rename`

**Statement:** An author MUST rename a record identifier with `litespec rename <old> <new>`, which rewrites every link.

**Rationale:** An identifier rename touches the page, its metadata, and every record that points at it. Rewriting every link in the same command leaves no citation pointing at the old name, so the repository stays resolvable at every commit.

**Example:** `litespec rename use-case/spending.close-run use-case/spending.complete-run` updates the page and every citation together.

**Checked by:** `litespec rename` leaves no citation of the old identifier, and `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Validate a Single Page Before Committing

**ID:** `litespec/core/authoring#validate-a-single-page-before-committing`

**Statement:** An author MUST validate a single page before committing it.

**Rationale:** Full validation walks the whole repository, which grows slow exactly when many authors commit often. A single-page check reports shape and link errors for the page at hand in seconds, so each commit arrives already sound.

**Example:** The author validates the touched page, fixes the reported diagnostics, and commits only when the page passes.

**Reviewed for:** The change report shows each touched page passing single-page validation before commit.

### Run Repeatable Authoring Checks

**ID:** `litespec/core/authoring#run-repeatable-authoring-checks`

**Statement:** A standards change MUST pass the repository validator, the test suite, and `git diff --check`.

**Rationale:** The repository validator, the test suite, and `git diff --check` each catch what the others miss: structure, behavior, and whitespace. Passing all three on every standards change keeps the standard checkable by anyone who runs the same commands. The change report lists every skipped check with its reason. The [command index](../tools/README.md) and the [maintainer index](../maintain/README.md) name each check and its command.

**Checked by:** `litespec-maintain validate`, `dotnet test --solution LiteSpec.slnx` and `git diff --check` exit zero.

### Validate Current Standards Material

**ID:** `litespec/core/authoring#validate-current-standards-material`

**Statement:** The standards validator MUST evaluate only current standards material.

**Rationale:** Each pinned release is a complete snapshot, so the validator judges the working tree alone. Reading history would let a provision pass on the strength of a release nobody pinned. Git history and the changelog give release context to readers outside validation.

**Reviewed for:** Which validator inputs come from outside the working tree? Look at the paths the validator reads.

### Exclude Historical Transition Material

**ID:** `litespec/core/authoring#exclude-historical-transition-material`

**Statement:** Current standards material MUST NOT contain history-specific paths, IDs, terms, aliases, replacement maps, release migration material, compatibility rules, or transition checks.

**Rationale:** A snapshot that carries aliases, maps, migration notes, or transition checks for other releases forces every reader to learn releases they did not pin. The published snapshot states its own contract and nothing else. An extension's `Baseline Relationship` section names a provision it replaces inside this release, which is a current replacement rather than history.

**Reviewed for:** Which active files carry history-specific paths, aliases, or migration material? Look at the files the change adds.

### Publish Each Release as a Complete Contract

**ID:** `litespec/core/authoring#publish-each-release-as-a-complete-contract`

**Statement:** A LiteSpec release MUST state every active provision without depending on an earlier release.

**Rationale:** A consumer pins one snapshot and reads it as the whole obligation. Depending on another release would force the consumer to pin and read two contracts to know either. Stating every active provision in each release keeps one snapshot sufficient.

**Checked by:** `litespec-maintain validate` checks every page of the release in one run, and the manifest names one version for the whole contract.

### Exclude Cross-Release Compatibility Work

**ID:** `litespec/core/authoring#exclude-cross-release-compatibility-work`

**Statement:** The LiteSpec repository MUST NOT publish a compatibility guarantee, migration path, deprecation period, or identifier alias between its own releases.

**Rationale:** LiteSpec is a pinned contract, not a running service: consumers adopt a release whole, on their own schedule. Compatibility guarantees, migration paths, deprecation periods, and identifier aliases between LiteSpec releases would promise a continuity the model does not offer. A consumer product keeps its own API compatibility, migration, deprecation, and rollback provisions for its own users.

**Reviewed for:** Which pages promise migration, mapping, or alias between releases? Look at the release material the change adds.

### Keep a Pinned Release for as Long as It Serves

**ID:** `litespec/core/authoring#keep-a-pinned-release-for-as-long-as-it-serves`

**Statement:** A consumer MAY keep any published LiteSpec release for as long as it chooses.

**Rationale:** Adoption is the consumer's decision. If a repository change could oblige a move, every LiteSpec edit would become a deadline for every consumer. Keeping any published release for as long as it serves decouples the consumer's schedule from the standard's.

**Reviewed for:** Which provisions oblige a move to a later release? Look at the Statements the change adds.

### Record the Reviewed LiteSpec Release

**ID:** `litespec/core/authoring#record-the-reviewed-litespec-release`

**Statement:** A consumer MUST record the LiteSpec release it adopts in `reviewedLiteSpecVersion` within `litespec.project.json`.

**Rationale:** A provision can gain force while keeping its ID, so a deviation written against one release could otherwise apply to a rule nobody reread. Recording the adopted release in `reviewedLiteSpecVersion` makes adoption an explicit act and tells every reader which contract the project reviewed.

**Checked by:** `litespec consumer` fails when `reviewedLiteSpecVersion` differs from the pinned release.

### Review Each Term for Its Lookalikes

**ID:** `litespec/core/authoring#review-each-term-for-its-lookalikes`

**Statement:** A project MUST resolve each pair of terms that differ only in hyphenation, spelling variant, plurality, or a compound split.

**Rationale:** Two spellings of one concept split every search and every listing, and a reader cannot tell whether they name one concept or two. One spelling per concept keeps each term defined once and checked once. A hyphenated pair whose first part is a function word is two words side by side, not one compound split in two, so the check skips it: `a`, `an`, `as`, `at`, `by`, `in`, `of`, `on`, `or`, `to`, and `with`.

**Example:** The notification `send-approvalreminder-confirmation` and the reaction `send-approval-reminder-on-expense-submitted` name one concept with two spellings; the project picks one and renames the other. `send-on-line-notice` beside `send-online-notice` is not reported, since `on` opens no compound.

**Checked by:** `litespec model check` reports `term-clash` (tier A) for a joined and hyphenated pair, a British and American spelling pair, or a singular and plural in one home, when both forms occur, except a hyphenated pair opening with a function word.

### State What Happens in Every Behavior Statement

**ID:** `litespec/core/authoring#state-what-happens-in-every-behavior-statement`

**Statement:** A rule statement, scenario step, and result meaning SHOULD avoid words that hide behavior.

**Deviation:** A quoted name, an interface label, or a cited source keeps its fixed wording.

**Rationale:** The vague words are handle, handles, handled, process, processes, manage, manages, various, etc., appropriate, appropriately, properly, as needed, and so on: each says that something happens without saying what. A reader cannot check a statement that hides its behavior, so each statement names the act in words a test could assert.

**Example:** A scenario step reads "The member borrows the copy" rather than "The member handles the copy".

**Checked by:** `litespec model check` reports `statement-word-invalid` (tier A) for a listed vague word in a rule statement, scenario step, or result meaning.

## Conventions

### Prefer Direct Action Headings

**ID:** `litespec/core/authoring#prefer-direct-action-headings`

**Default:** Start a Standard or Convention heading with a concrete action verb.

**Replacement:** Use a noun heading only for a pattern, an informative definition, or an index entry.

**Checked by:** `litespec-maintain validate` emits no `HEADING_ACTION` diagnostic.
