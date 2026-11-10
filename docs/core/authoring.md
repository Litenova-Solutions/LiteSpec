# Authoring Standard

**Stack:** any
**Concern:** workspace

## Intent

Every page in LiteSpec shares one shape: a fixed style, testable rules called provisions, stable identifiers for those rules, and a table naming the evidence each rule needs. This page states that shape, the prose rules the validators enforce on LiteSpec's own pages, and how each release carries the complete contract. The [Project Model](model.md) and [Record Language](language.md) pages state the rules for the records a project writes, and one shared shape lets readers learn pages once.

## Concepts

### Page Authority

Standards state required boundaries, and Conventions state replaceable defaults. Everything else on a page is informative: Intent, Concepts, Rationale, Examples, guides, tutorials, and indexes explain without obliging.

`AGENTS.md` and a consumer's project agents file project the provisions an agent needs for that workspace. A projection cites the provision it projects and adds no obligation of its own. When a projection and its provision disagree, the provision wins.

`CONTRIBUTING.md` owns the contribution workflow. `CHANGELOG.md` describes the current release.

The [Record Language](language.md) page owns the grammar for everything a project writes: field names, values, record names, finding codes, and tool names.

### Documentation Tiers

A reader arrives with one question, and each page answers the one for its documentation tier.

| Documentation tier | Page style | The question it answers |
|:---|:---|:---|
| 1 | Tutorial | How do I get a first working result? |
| 2 | How-to | How do I reach this one goal? |
| 3a | Topic, stack, extension, pattern | What must hold, with which values and defaults? |
| 3b | Reference, command | What are the exact values and how do I run this? |
| 4 | The `Underneath` section of a command page | What does the command run, and how do I run it myself? |

An explanation page builds understanding: what LiteSpec is, why it works the way it does, and how its parts fit. It sits beside the documentation tiers because a reader takes it before or between tasks, not during one. An index routes readers to pages, and the glossary resolves one term each, so neither sits in a documentation tier. A page links down a documentation tier. Any page may link to the glossary. An explanation page is not the explanation file beside a consumer record.

### Page Styles

Every LiteSpec page uses exactly one style, decided by where the page lives.

| Page style | Where it lives | Template | Sections, in order |
|:---|:---|:---|:---|
| Topic | Any area page that owns provisions | None | Intent, optional Concepts, Standards, Conventions, optional Reference Example |
| Stack | `stack.md` in a stack folder | None | Intent, optional Concepts, Standards, Conventions |
| Extension | An `ext/` folder | `templates/standard/extension.md` | Intent, Activation, Baseline Relationship, Standards, Conventions, Dependencies |
| Pattern | `docs/frontend/patterns/` | [UI patterns](../frontend/patterns.md) | Intent, optional Concepts, optional Standards, optional Conventions, Patterns |
| Reference | `docs/reference/` except the generated provision index, and one page per rule in `docs/analyzers/rules/` | `templates/standard/reference.md` | Intent, Reference, optional Notes |
| Command | `docs/tools/` and `docs/maintain/` | `templates/standard/command.md` | Name, Synopsis, Description, Arguments, Options, Exit Codes, Examples, Underneath |
| How-to | `docs/guide/`, and the other pages of `docs/analyzers/` | `templates/standard/how-to.md` | Purpose, optional Prerequisites, Procedure, Check |
| Tutorial | `docs/tutorial/` | `templates/standard/tutorial.md` | Purpose, Prerequisites, Lesson, What You Built |
| Explanation | `docs/learn/` and `docs/example/` | None | Intent, then sections of its own |
| Index | A `README.md` | None | Intent, then navigation groups |
| Glossary | `docs/reference/glossary.md` | None | H2 groups in alphabetical order, each with one intro sentence, then one H3 heading and definition per term |

These rules apply to every Markdown page style:

- A LiteSpec page that owns provisions lives at `docs/<area>/<page>.md` or deeper, with a lowercase file name.
- A page that owns provisions declares its scope in two lines directly under its title, a `**Stack:**` line and a `**Concern:**` line, as [Provision Scopes](scopes.md) states.
- A Markdown page has one H1.
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
- `SHOULD` keeps the standard's rule with a recorded departure; a Convention is a project-owned default the project replaces by naming its own convention, not by recording a departure.

A Convention states a default a project may replace:

```markdown
### Use the Default Location

**Default:** Store the catalog at `.litespec/implementation/patterns/catalog.yaml`.

**Replacement:** A project names another path in `paths.uiPatterns`.

**Rationale:** Why this is the default.
```

A project that departs from a Standard records a departure whose `covers` names the provision, in `.litespec/governance/departures/`. `rules <path>` shows the departure in place of the provision. A project that replaces a Convention names its own convention in its documentation. A Replacement names the local convention or its recording place, not a departure. A convention with no specific recording place uses `A consumer can replace this default with an explicit local convention.` as its whole Replacement. The [departures page](departures.md) states the departure record.

A pattern provision carries its question, its default option, its compact behavior, and its rationale, and then its options with their wireframes. [UI patterns](../frontend/patterns.md) states the pattern contract and the wireframe language.

### Provision Identifiers

Every provision ID is `litespec/<page>#<slug>`:

| Segment | Source |
|:---|:---|
| `litespec` | The source that owns the provision |
| `<page>` | The page path under `docs/`, without `.md` |
| `<slug>` | The identifier slug as the ID line states it |

The `**ID:**` line under each provision heading is the provision's identity. A heading may change without changing the ID, so fixing a heading's wording breaks no citation. Headings keep a stable verb-noun head so readers find the provision, not to protect the ID.

The heading carries no identifier text. No two provisions share an ID. Consumer record identifiers follow [Identifier Forms](language.md#identifier-forms).

### Provision Layout

Every provision has the same parts in the same order:

```markdown
### Keep Shared UI in One Package

**ID:** `litespec/frontend/components#keep-shared-ui-in-one-package`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Shared primitives and composites MUST live in the one shared UI package.

**Rationale:** ...

**Example:** ...

**Reviewed for:** Which shared primitives or composites live outside the shared package? Look in the workspace packages.
```

A provision adds `**Applies to:**` only when it is narrower than its page, or when its record layer or trace differs from its page, for example `artifact: aggregate` or `layer: implementation; trace: yes`.

1. The heading names the provision.
2. The ID line holds the provision's identity, so a reader copies it from where the provision stands; the heading may change without changing it.
3. The Strength line grades how binding the provision is: `required`, `default`, or `advisory`, as [Provision Scopes](scopes.md) states.
4. `Applies to` follows the Strength line only when the provision is narrower than its page, or its record layer or trace differs from its page; otherwise the page scope applies.
5. A rule states its `Statement`; a convention states its `Default` and `Replacement`; a pattern states its `Question`, `Default`, `Compact` and `Rationale`.
6. `Rationale` follows, and `Example` is optional and follows `Rationale` when one clarifies the rule.
7. The verification line comes last among the fields. `Checked by` marks a provision a tool checks (`checkable`) and starts with the checking command in backticks, then the code the tool reports where the tool emits one, then the scope. `Reviewed for` marks a provision a person judges (`guidance`) and asks the one question a reviewer answers, naming the observable evidence and where to look. Evidence like "verify compliance" says nothing and is refused.

A Standard carries Strength `required` or `advisory`; a Convention or a pattern carries Strength `default`. A pattern's options follow its verification line. `litespec-maintain provisions` writes the [provision index](../reference/provisions.md) from these lines, with each provision's scope and strength, so the CLI reads one file instead of parsing every page; nothing else repeats them.

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

A generated region is a section a tool writes, between `<!-- generated:<name> -->` and `<!-- /generated:<name> -->`, in an explanation file or a document. Only the owning tool writes inside it, and an edit there is lost at the next generation. Authoring checks skip generated regions. A region that renders facts about its record finds the record through the shared file name.

### Release Model

Each LiteSpec release is a complete contract that states every active provision. LiteSpec publishes no compatibility guarantee, migration path, deprecation period, or identifier alias between its own releases. A consumer pins one release, keeps it for as long as it chooses, and adopts a later release by accepting that release's whole contract.

Version numbers name complete pinned contracts. A patch release corrects a narrow area, a minor release makes one coherent change, and a major release replaces scope, method, or a stack. Within a major version, provision identifiers stay stable: a minor release adds provisions and changes wording, and removes or renames no identifier. A major release may remove or rename identifiers, and its changelog carries the generated list of every added, removed and renamed identifier as the one piece of history material the release keeps.

This model governs LiteSpec itself. A consumer product has its own users, so its API compatibility, migration, deprecation and rollback provisions still apply.

## Standards

### Keep Authored Prose ASCII-Safe

**ID:** `litespec/core/authoring#keep-authored-prose-ascii-safe`

**Strength:** required

**Statement:** Authored prose MUST contain only ASCII unless code, data, or mathematical content requires another character.

**Rationale:** Non-ASCII characters break in terminals, diffs, generated context, and plain-text tools: a curly quote becomes unreadable in one reader and a silent mismatch in another. ASCII prose renders the same everywhere the standard is read or checked.

**Example:** Write a hyphen or a comma where a typographic dash would go.

**Checked by:** `litespec-maintain validate` emits no `PROSE_NON_ASCII` diagnostic.

### Use One Normative Vocabulary

**ID:** `litespec/core/authoring#use-one-normative-vocabulary`

**Strength:** required

**Statement:** A Standards provision MUST use exactly one listed capitalized modal and no other capitalized normative term.

**Rationale:** Strength words work only when each one means exactly one thing. The provision contract defines the listed modals with their RFC meaning, so a reader knows whether a word obliges or suggests, and no second capitalized term sneaks in a different shade of obligation.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_MODAL_COUNT` or `STANDARD_MODAL_VOCABULARY` diagnostic.

### Restrict Normative Language to Standards and Conventions

**ID:** `litespec/core/authoring#restrict-normative-language-to-standards-and-conventions`

**Strength:** required

**Statement:** A capitalized modal MUST appear only in a Statement, a Deviation, a Default, or a Replacement.

**Rationale:** Obligation hides when it sits in Concepts, Rationale, or an example: a project replaces a default it saw nowhere in the labeled assertions. One location for every modal lets readers and tools find each obligation in the same place. A prohibition written as `cannot` and a rule written as `never` belong in the same labels.

**Checked by:** `litespec-maintain validate` emits no `PROSE_NORMATIVE_LOCATION` or `STANDARD_INFORMATIVE_NORMATIVE` diagnostic.

### Use One Term for One Concept

**ID:** `litespec/core/authoring#use-one-term-for-one-concept`

**Strength:** required

**Statement:** Authored prose MUST name each concept with its one glossary term.

**Rationale:** A second name for one concept reads as a second concept: the reader hunts for a distinction the writer did not mean. One term per concept means each term is defined once and checked once. Settled by [decision/0018-one-record-language](decision/0018-one-record-language) on the evidence in [research/naming-grammar-evidence](research/naming-grammar-evidence).

**Reviewed for:** Which concepts appear under more than one name? Look at the terms the change adds, beside the glossary.

### Exclude Generated Regions from Prose Checks

**ID:** `litespec/core/authoring#exclude-generated-regions-from-prose-checks`

**Strength:** required

**Statement:** An authoring check MUST skip every generated region.

**Rationale:** A generated region holds tool output that no author wrote and no author can fix by editing the page. Checking it would fail pages for words outside author control and train writers to ignore diagnostics.

**Reviewed for:** Which findings point inside generated-region fences? Look at the findings the checks report.

### Use Controlled Capitalization

**ID:** `litespec/core/authoring#use-controlled-capitalization`

**Strength:** required

**Statement:** A page MUST use Title Case for its title and every heading.

**Rationale:** Titles and headings read as names in indexes, tabs, and search results. Title Case capitalizes every word except articles (`a`, `an`, `the`), coordinating conjunctions (`and`, `but`, `for`, `nor`, `or`, `so`, `yet`), and prepositions of four letters or fewer (`as`, `at`, `by`, `for`, `from`, `in`, `into`, `of`, `off`, `on`, `onto`, `over`, `per`, `to`, `up`, `upon`, `via`, `with`); the first and last words stay capitalized, and code, identifiers, and file names keep their own case.

**Checked by:** `litespec-maintain validate` emits no `PAGE_TITLE_CASE` or `PAGE_HEADING_CASE` diagnostic.

### Use the Declared Page Contract

**ID:** `litespec/core/authoring#use-the-declared-page-contract`

**Strength:** required

**Statement:** An authored page MUST use the sections, in order, of its page style.

**Rationale:** A fixed shape puts the authority, the context, the defaults, and the evidence in the same place on every page. Readers learn the shape once, and tools find each section without guessing. A page that invents its own order sends both searching. A record file carries its type's fields in canonical key order instead of sections.

**Checked by:** `litespec-maintain validate` emits no `PAGE_MISSING_SECTION`, `PAGE_SECTION_ORDER` or `PAGE_UNKNOWN_SECTION` diagnostic.

### Limit Each Intent to Three Sentences

**ID:** `litespec/core/authoring#limit-each-intent-to-three-sentences`

**Strength:** required

**Statement:** An Intent section MUST hold at most three sentences.

**Rationale:** The first sentence states the scope and boundary. The second states who applies the page and when. An optional third sentence relates the page to its neighbors.

**Reviewed for:** Which Intent sections run past three sentences? Look at the Intent section of each page the change touches.

### Write None in an Empty Required Section

**ID:** `litespec/core/authoring#write-none-in-an-empty-required-section`

**Strength:** required

**Statement:** A required section with nothing to say MUST contain only `None.`.

**Rationale:** One fixed marker reads the same on every page and gives the validator one shape to accept.

**Checked by:** `litespec-maintain validate` emits no `PAGE_EMPTY_SECTION` or `PAGE_EMPTY_SECTION_TEXT` diagnostic.

### Route the Reader Before Listing Pages

**ID:** `litespec/core/authoring#route-the-reader-before-listing-pages`

**Strength:** required

**Statement:** A documentation root index MUST state which page style answers which reader question before its first navigation group.

**Rationale:** A reader without a map from question to page opens both and trusts neither. Naming each style in the reader's terms before the first navigation group sends each question to the page that answers it.

**Example:** A `Which Page Do You Want` section names each style in the reader's terms, one sentence each.

**Checked by:** `litespec-maintain validate` emits no `INDEX_MISSING_ROUTING` diagnostic.

### Name the Tool Rather Than the Mechanism

**ID:** `litespec/core/authoring#name-the-tool-rather-than-the-mechanism`

**Strength:** required

**Statement:** A tutorial step or how-to step MUST name the repository command that performs it, where one exists.

**Rationale:** A procedure that lists what a command already does becomes a second copy of that command's behavior. The copy drifts from the tool, and readers follow stale steps. Naming the repository command delegates the steps to the tool that performs them.

**Example:** A step reads ``Run `./shop up` `` rather than listing the compose start, the readiness wait, and each process launch.

**Reviewed for:** Which procedure steps lack a repository command? Look at the steps the change adds.

### State One Tier per Page

**ID:** `litespec/core/authoring#state-one-tier-per-page`

**Strength:** required

**Statement:** A page MUST NOT describe both a command and the mechanism that command runs, outside an `Underneath` section.

**Rationale:** A reader who wanted the command reads mechanism detail as noise, and a reader who wanted the mechanism reads the command wrapper as an obstacle. One documentation tier per page serves each reader, and the `Underneath` section holds the mechanism for the reader who asks for it.

**Reviewed for:** Which command pages mix mechanism into other sections? Look outside the Underneath section of each command page the change touches.

### Name the Escape from Every Abstraction

**ID:** `litespec/core/authoring#name-the-escape-from-every-abstraction`

**Strength:** required

**Statement:** A command page MUST state under `Underneath` what the command runs or the sentence `Nothing beneath this command is separately runnable.`.

**Rationale:** A wrapper nobody can see through leaves a failure with no command to run by hand and no documentation tier to inspect. Stating what the command runs, or that nothing beneath it is separately runnable, gives every failure a next step.

**Example:** A command with nothing separately runnable writes `Nothing beneath this command is separately runnable.` under `Underneath`.

**Checked by:** `litespec-maintain validate` emits no `PAGE_MISSING_SECTION` diagnostic for a command page.

### Record a Setting as Reference

**ID:** `litespec/core/authoring#record-a-setting-as-reference`

**Strength:** required

**Statement:** A configuration setting a reader can change MUST appear on a configuration page with its default, its scope, and what overrides it.

**Rationale:** A setting introduced inside a procedure is found only by the reader who already knows which procedure mentioned it. A configuration page with the default, scope, and overrides puts every setting where a reader looks it up.

**Reviewed for:** Which procedure-named settings lack a configuration page row? Look at the settings the change adds, beside the configuration pages.

### Cite Owned Content Instead of Copying It

**ID:** `litespec/core/authoring#cite-owned-content-instead-of-copying-it`

**Strength:** required

**Statement:** A page MUST link to content another page owns instead of restating it.

**Rationale:** A copy drifts from its source with the next unrelated edit, and readers follow the stale one. One home for each fact keeps every reader on the current text. A link to a record names its identifier, not its file.

**Reviewed for:** Which passages restate content another page owns? Compare repeated passages with the owning page.

### Keep Principle Pages Free of Stack Detail

**ID:** `litespec/core/authoring#keep-principle-pages-free-of-stack-detail`

**Strength:** required

**Statement:** A principle page MUST NOT name a framework, a package, or a language-specific construct.

**Rationale:** Technology lives in stack pages, so a principle holds for every stack. A framework name in a principle binds every stack to one choice.

**Reviewed for:** Which principle pages name stack detail? Look for framework, package, and language names in principle pages.

### Place Each Stack Binding on Its Stack Page

**ID:** `litespec/core/authoring#place-each-stack-binding-on-its-stack-page`

**Strength:** required

**Statement:** A stack-specific section MUST live on its stack page rather than on a shared page.

**Rationale:** Shared pages stay neutral across stacks. Each stack page carries its own bindings, so a reader finds one stack's detail in one place.

**Reviewed for:** Which shared pages carry stack-specific sections? Look for stack sections outside stack pages.

### Split Extension Dependencies into Three Labeled Lines

**ID:** `litespec/core/authoring#split-extension-dependencies-into-three-labeled-lines`

**Strength:** required

**Statement:** An extension Dependencies section MUST carry the lines Packages, Related extensions, and Guides.

**Rationale:** Fixed labels separate package needs from related extensions and guides. A line with nothing to name holds `None.`.

**Reviewed for:** Which Dependencies sections lack a labeled line? Look in each page under an `ext/` folder.

### Define Each Glossary Term Under Its Heading

**ID:** `litespec/core/authoring#define-each-glossary-term-under-its-heading`

**Strength:** required

**Statement:** A glossary entry MUST hold its definition in the paragraph under its term heading.

**Rationale:** The heading anchor already carries the term identifier, so a `Term` line repeats it. Lines after the definition name the record layer, established aliases, the source, and rejected synonyms where they exist.

**Reviewed for:** Which glossary entries open with something other than the definition, or carry a Term line? Look in `docs/reference/glossary.md`.

### Order Glossary Groups Alphabetically

**ID:** `litespec/core/authoring#order-glossary-groups-alphabetically`

**Strength:** required

**Statement:** Glossary groups MUST stand in alphabetical order, each opening with one intro sentence.

**Rationale:** Groups in a fixed order with one opening sentence tell a reader where a term lives before any term is read. An unordered group hides its terms from a reader who guesses the wrong place, and a group without its sentence opens on terms with no shared context.

**Example:** The code, product, and standard term groups open the glossary in that order, each with one sentence saying what its terms name.

**Checked by:** `litespec-maintain validate` emits no `GLOSSARY_ORDER` or `GLOSSARY_DEFINITION` diagnostic.

### Write Atomic Standards Provisions

**ID:** `litespec/core/authoring#write-atomic-standards-provisions`

**Strength:** required

**Statement:** A Standards provision MUST follow the Standard contract this page defines.

**Rationale:** One provision with one obligation has one authority, one identifier, and one verification line. A provision that bundles two obligations leaves a project half-compliant with no identifier for the half it missed.

**Example:** `### Keep Business Invariants in Domain` owns one obligation about where invariants are enforced.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_LABEL_INVALID`, `STANDARD_LABEL_ORDER`, `STANDARD_LABEL_DUPLICATE`, `STANDARD_SENTENCE_COUNT`, `STANDARD_MODAL_COUNT`, `STANDARD_MODAL_VOCABULARY`, `STANDARD_MISSING_DEVIATION`, `STANDARD_UNEXPECTED_DEVIATION`, `STANDARD_DEVIATION_NORMATIVE`, `STANDARD_DEVIATION_SENTENCE`, `STANDARD_MISSING_REQUIREMENT`, `STANDARD_REQUIREMENT_COUNT`, `STANDARD_UNLABELED_CONTENT`, or `PROVISION_RESTATES_HEADING` diagnostic.

### Identify Actionable Conventions

**ID:** `litespec/core/authoring#identify-actionable-conventions`

**Strength:** required

**Statement:** A Convention provision MUST follow the Convention contract this page defines.

**Rationale:** A Convention heading under `## Conventions` makes each replaceable default traceable: a project cites exactly the default it replaces. Without the contract, defaults hide in prose and replacements cite paragraphs instead of identifiers.

**Checked by:** `litespec-maintain validate` emits no `CONVENTION_MISSING_DEFAULT`, `CONVENTION_MISSING_REPLACEMENT`, `CONVENTION_DEFAULT_SENTENCE`, `CONVENTION_REPLACEMENT_SENTENCE`, `CONVENTION_REPLACEMENT_DEVIATION`, or `PROVISION_RESTATES_HEADING` diagnostic.

### Attach Examples to Their Provisions

**ID:** `litespec/core/authoring#attach-examples-to-their-provisions`

**Strength:** required

**Statement:** An informative example MUST belong to its provision or to a Reference Example that names every provision it demonstrates.

**Rationale:** An example floating free of its provision reads as a second rule: readers obey the illustration instead of the obligation. Belonging to its provision, or to a Reference Example that names every provision it demonstrates, marks it as illustration.

**Checked by:** `litespec-maintain validate` emits no `STANDARD_EXAMPLE_LABEL`, `PAGE_UNKNOWN_SECTION`, or `REFERENCE_EXAMPLE_DECLARATION` diagnostic.

### Use the Declared Identifier Grammar

**ID:** `litespec/core/authoring#use-the-declared-identifier-grammar`

**Strength:** required

**Statement:** A provision ID MUST be `litespec/<page>#<slug>` as its ID line states it.

**Rationale:** The page names the file and the ID line states the slug, so a citation resolves with no lookup table. One grammar for every identifier keeps citations writable by hand and checkable by tool. The ID line is the identity: a heading reword leaves it unchanged, and no two provisions share one.

**Example:** `litespec/frontend/components#compose-composites-in-page-code` is the ID line under its heading on the frontend components page.

**Checked by:** `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` or `ID_DUPLICATE` diagnostic.

### Name the Owning Page in the Identifier

**ID:** `litespec/core/authoring#name-the-owning-page-in-the-identifier`

**Strength:** required

**Statement:** A provision ID's page segment MUST be its page's path under `docs/` without the `.md` extension.

**Rationale:** The page segment is the file a reader opens and the address the provision carries on the published site. Deriving it from the path keeps the identifier and the file from disagreeing.

**Example:** `litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case` and `litespec/frontend/mobile/structure#organize-screens-by-module` are on two different pages.

**Checked by:** `litespec-maintain validate` emits no `ID_PAGE_FILENAME` or `ID_DUPLICATE` diagnostic.

### Keep Identifiers Out of Provision Headings

**ID:** `litespec/core/authoring#keep-identifiers-out-of-provision-headings`

**Strength:** required

**Statement:** A provision heading MUST carry no identifier text.

**Rationale:** The page path and the heading already state the identifier. An identifier written into the heading repeats it at best and contradicts it at worst, after a rename touches one and misses the other.

**Checked by:** `litespec-maintain validate` emits no `ID_IN_HEADING` diagnostic.

### Exclude a Sequence Number from the Identifier

**ID:** `litespec/core/authoring#exclude-a-sequence-number-from-the-identifier`

**Strength:** required

**Statement:** A provision ID MUST NOT contain a sequence number.

**Rationale:** A counter orders a provision among its siblings and says nothing about what it requires. Words name the obligation; numbers would renumber every sibling on each insertion and break citations for no gain.

**Reviewed for:** Which identifiers carry a sequence number? Look at the provision IDs the change adds.

### Place the ID Under Every Provision Heading

**ID:** `litespec/core/authoring#place-the-id-under-every-provision-heading`

**Strength:** required

**Statement:** Every provision heading MUST be followed by an ID line holding the provision's identity.

**Rationale:** A reader who cites a provision copies its ID from where the provision stands, instead of deriving a slug by hand or looking it up elsewhere. The ID line is the identity: a heading may change without changing it, so an ID line that reads differently from its heading still holds.

**Example:** `### Keep Shared UI in One Package` on `docs/frontend/components.md` is followed by the ID line `litespec/frontend/components#keep-shared-ui-in-one-package`.

**Checked by:** `litespec-maintain validate` emits no `ID_LINE_MISSING` or `ID_LINE_MISMATCH` diagnostic.

### Map Provisions to Evidence

**ID:** `litespec/core/authoring#map-provisions-to-evidence`

**Strength:** required

**Statement:** Every provision MUST end its fields with exactly one verification line.

**Rationale:** A provision with no verification line is a rule nobody checks: people assume a tool covers it while no tool was told. One line per provision, beside the rule it checks, makes each rule reviewable by a person or a tool.

**Checked by:** `litespec-maintain validate` emits no `VERIFY_MISSING_ID`, `VERIFY_DUPLICATE_ID`, `VERIFY_ORDER`, or `VERIFY_HEADER` diagnostic.

### Mark Every Provision Checkable or Guidance

**ID:** `litespec/core/authoring#mark-every-provision-checkable-or-guidance`

**Strength:** required

**Statement:** A verification line MUST open with `Checked by` for a provision a tool checks or `Reviewed for` for one a person judges.

**Rationale:** The label tells a reader who judges the provision: a tool (`checkable`) or a person (`guidance`). Without it, a team automates a judgment call or hand-reviews what a command already proves.

**Checked by:** `litespec-maintain validate` reports `VERIFY_MISSING_ID` for a provision whose verification line opens with any other label.

### Name Observable Evidence in Verification Lines

**ID:** `litespec/core/authoring#name-observable-evidence-in-verification-lines`

**Strength:** required

**Statement:** A verification line MUST name the observable evidence and where the reviewer looks.

**Rationale:** A line that restates its Statement sends the reviewer in a circle: the evidence confirms that the rule says what it says. Naming the artifact and its location turns the line into an instruction the reviewer follows.

**Checked by:** `litespec-maintain validate` emits no `VERIFY_GENERIC_EVIDENCE`, `VERIFY_TEMPLATED_EVIDENCE`, `VERIFY_DUPLICATE_EVIDENCE`, `VERIFY_COMMAND`, or `CHECKED_BY_UNKNOWN` diagnostic.

### Give One Observable Fact One Mark Across Pages

**ID:** `litespec/core/authoring#give-one-observable-fact-one-mark-across-pages`

**Strength:** required

**Statement:** One observable fact MUST carry one mark across pages.

**Rationale:** A fact checked by a tool on one page and judged by a person on another leaves each reviewer unsure which judgment holds.

**Reviewed for:** Which shared facts carry different labels? Compare the verification lines that name one check, using the provision index.

### Regenerate the Provision Index

**ID:** `litespec/core/authoring#regenerate-the-provision-index`

**Strength:** required

**Statement:** A standards change MUST leave `docs/reference/provisions.md` equal to the output of `litespec-maintain provisions`.

**Rationale:** One generated page resolves every identifier to its heading and page, so a reader follows a citation in one step. It carries each provision's scope and strength, so the CLI reads one file instead of parsing every page. Regenerating it with `litespec-maintain provisions` on every standards change keeps the index and the pages from disagreeing.

**Checked by:** `litespec-maintain provisions --check` exits zero.

### Run Repeatable Authoring Checks

**ID:** `litespec/core/authoring#run-repeatable-authoring-checks`

**Strength:** required

**Statement:** A standards change MUST pass the repository validator, the test suite, and `git diff --check`.

**Rationale:** The repository validator, the test suite, and `git diff --check` each catch what the others miss: structure, behavior, and whitespace. Passing all three on every standards change keeps the standard checkable by anyone who runs the same commands. The tools check every schema with LiteSpec's own evaluator, so running them needs no paid licence, settled by [decision/0022-own-schema-evaluator](decision/0022-own-schema-evaluator) on the evidence in [research/schema-evaluator-evidence](research/schema-evaluator-evidence). The change report lists every skipped check with its reason. The [command index](../tools/README.md) and the [maintainer index](../maintain/README.md) name each check and its command.

**Checked by:** `litespec-maintain validate`, `dotnet test --solution LiteSpec.slnx` and `git diff --check` exit zero.

### Validate Current Standards Material

**ID:** `litespec/core/authoring#validate-current-standards-material`

**Strength:** required

**Statement:** The standards validator MUST evaluate only current standards material.

**Rationale:** Each pinned release is a complete snapshot, so the validator judges the working tree alone. Reading history would let a provision pass on the strength of a release nobody pinned. Git history and the changelog give release context to readers outside validation.

**Reviewed for:** Which validator inputs come from outside the working tree? Look at the paths the validator reads.

### Exclude Historical Transition Material

**ID:** `litespec/core/authoring#exclude-historical-transition-material`

**Strength:** required

**Statement:** Current standards material MUST NOT contain history-specific paths, IDs, terms, aliases, replacement maps, release migration material, compatibility rules, or transition checks other than the generated provision identifier list in the changelog.

**Rationale:** A snapshot that carries aliases, maps, migration notes, or transition checks for other releases forces every reader to learn releases they did not pin. The published snapshot states its own contract and nothing else. An extension's `Baseline Relationship` section names a provision it replaces inside this release, which is a current replacement rather than history. The changelog's generated list of added, removed and renamed provision identifiers states a fact about the release it ships in; it offers no alias, mapping or migration path, so it stands as the one exception this provision allows.

**Reviewed for:** Which active files carry history-specific paths, aliases, or migration material? Look at the files the change adds.

### Publish Each Release as a Complete Contract

**ID:** `litespec/core/authoring#publish-each-release-as-a-complete-contract`

**Strength:** required

**Statement:** A LiteSpec release MUST state every active provision without depending on an earlier release.

**Rationale:** A consumer pins one snapshot and reads it as the whole obligation. Depending on another release would force the consumer to pin and read two contracts to know either. Stating every active provision in each release keeps one snapshot sufficient.

**Checked by:** `litespec-maintain validate` checks every page of the release in one run, and the manifest names one version for the whole contract.

### Exclude Cross-Release Compatibility Work

**ID:** `litespec/core/authoring#exclude-cross-release-compatibility-work`

**Strength:** required

**Statement:** The LiteSpec repository MUST NOT publish a compatibility guarantee, migration path, deprecation period, or identifier alias between its own releases.

**Rationale:** LiteSpec is a pinned contract, not a running service: consumers adopt a release whole, on their own schedule. Compatibility guarantees, migration paths, deprecation periods, and identifier aliases between LiteSpec releases would promise a continuity the model does not offer. A consumer product keeps its own API compatibility, migration, deprecation, and rollback provisions for its own users.

**Reviewed for:** Which pages promise migration, mapping, or alias between releases? Look at the release material the change adds.

### Generate the Provision Diff for Each Release

**ID:** `litespec/core/authoring#generate-the-provision-diff-for-each-release`

**Strength:** required

**Statement:** The changelog for each LiteSpec release MUST list the provision identifiers the release adds, removes, and renames, generated from the provision index at the previous tag and the provision index of the release.

**Rationale:** A consumer cites provision identifiers in code marks, records, and departures, so a release that removes or renames an identifier without naming it leaves every citation pointing at nothing with no way to find what changed. The generated list states a fact about the release it ships in and nothing more: it carries no alias, no replacement map, and no migration path, and no provision depends on an earlier release to state its own contract. Because the list derives from the two provision indexes, a tool generates it; a list typed by hand goes stale with the next provision edit. This provision is the exception to `litespec/core/authoring#exclude-historical-transition-material`, and it extends to no other history material.

**Example:** `litespec-maintain changelog --from v3.0.0` writes the list into the changelog entry for a release, with `litespec/core/scope#claim-the-record-layers-the-project-uses` under Added when that provision first ships, and a major release lists each identifier it removes or renames under Removed and Renamed in the same entry.

**Reviewed for:** Which changelog identifiers were typed by hand, or name an identifier that a comparison of the provision index at the previous tag with the provision index of the release does not produce? Look at the changelog entry beside the two provision indexes.

### Keep a Pinned Release for as Long as It Serves

**ID:** `litespec/core/authoring#keep-a-pinned-release-for-as-long-as-it-serves`

**Strength:** required

**Statement:** A consumer MAY keep any published LiteSpec release for as long as it chooses.

**Rationale:** Adoption is the consumer's decision. If a repository change could oblige a move, every LiteSpec edit would become a deadline for every consumer. Keeping any published release for as long as it serves decouples the consumer's schedule from the standard's.

**Reviewed for:** Which provisions oblige a move to a later release? Look at the Statements the change adds.

### Record the Reviewed LiteSpec Release

**ID:** `litespec/core/authoring#record-the-reviewed-litespec-release`

**Strength:** required

**Statement:** A consumer MUST record the LiteSpec release it adopts in `reviewedLiteSpecVersion` within `.litespec/project.yaml`.

**Rationale:** A provision can gain force while keeping its ID, so a departure written against one release could otherwise apply to a rule nobody reread. Recording the adopted release in `reviewedLiteSpecVersion` makes adoption an explicit act and tells every reader which contract the project reviewed.

**Checked by:** `litespec model check` reports `release-drift` when `reviewedLiteSpecVersion` names no release or a release other than the installed tool release.

## Conventions

### Prefer Direct Action Headings

**ID:** `litespec/core/authoring#prefer-direct-action-headings`

**Strength:** default

**Default:** Start a Standard or Convention heading with a concrete action verb.

**Replacement:** Use a noun heading only for a pattern, an informative definition, or an index entry.

**Checked by:** `litespec-maintain validate` emits no `HEADING_ACTION` diagnostic.
