# Provision Scopes

**Stack:** any
**Concern:** workspace

## Intent

Provisions are filed by concern: each page states the rules for one concern, while an agent changing one file needs every rule for that situation. This page states the shared situation vocabulary each provision carries: its stack, its concern, its artifact, the record layer it governs, its trace, and its strength. Authors write these values when they write or grade a provision, and the provision index carries them so the CLI lists the provisions that apply to one path and the record layer each provision belongs to.

## Concepts

### The Six Fields

Each provision carries six scope fields. The page declares the first two once: a provision narrows `stack` and adds `artifact`. The `layer` field names the record layer the provision governs, the `trace` field says whether it ties that layer to the records, and the `strength` field grades how binding it is. The [scope page](scope.md#layer-claims) states which layers a project claims.

| Field | Answers | Values |
|:---|:---|:---|
| `stack` | Which technology | `dotnet`, `react`, `svelte`, `blazor`, `android`, `any` |
| `concern` | Which part of the system | `domain`, `application`, `infrastructure`, `web-api`, `frontend-ui`, `frontend-data`, `tests`, `records`, `workspace`, `operations` |
| `artifact` | Which kind of thing is made | `any`, one code kind, several code kinds separated by a comma, or `record:<type>` for one record of that type |
| `layer` | Which record layer the provision governs | `any`, `intent`, `behavior`, `implementation`, `proof` |
| `trace` | Whether it ties its layer to the records | `yes`, `no` |
| `strength` | How binding it is | `required`, `default`, `advisory` |

### Stack Values

| Value | Meaning |
|:---|:---|
| `dotnet` | The .NET backend stack |
| `react` | The React web stack |
| `svelte` | The Svelte web stack |
| `blazor` | The Blazor stack |
| `android` | The Android mobile stack |
| `any` | No technology; the provision applies to every stack |

### Concern Values

| Value | Meaning |
|:---|:---|
| `domain` | Business state, rules, and language |
| `application` | Use-case coordination and the pipeline |
| `infrastructure` | Storage, messaging, and outside calls |
| `web-api` | HTTP endpoints and their contracts |
| `frontend-ui` | Components, routes, and rendering |
| `frontend-data` | Client state, the cache, and generated clients |
| `tests` | The suites that prove behavior |
| `records` | Specification records and standards pages |
| `workspace` | Repository shape, packages, and configuration |
| `operations` | Releases, deployment, and support |

### Artifact Values

| Value | Meaning |
|:---|:---|
| `aggregate` | The aggregate root with the entities it guards |
| `state-record` | One named state of an aggregate and its data |
| `value-object` | One immutable type defined by its values |
| `domain-event` | One fact completed inside the bounded context |
| `use-case` | The message and result types of one use case |
| `command-handler` | The execution path of one command use case |
| `query-handler` | The execution path of one query use case |
| `validator` | The input validation of one command or query use case |
| `reaction` | The handler class of one reaction |
| `port` | One declared port interface |
| `repository` | One aggregate repository, its interface or its store implementation |
| `endpoint` | One route operation and the class serving it |
| `migration` | One storage schema change |
| `job` | Scheduled or manual background work in code |
| `component` | One UI component |
| `route` | One frontend address and its page file |
| `form` | One data-entry form |
| `test` | One test |
| `any` | Every file of the provision layer |
| `record:<type>` | One record of that type, such as `record:use-case`. The types are the Type column of the placement table on the [model page](model.md) |

`record:aggregate` names the aggregate record while `aggregate` names its code, and `record:job` names the job record while `job` names its code. A provision names several code kinds separated by a comma when one rule governs several kinds, for example `artifact: command-handler, query-handler`; a file matches when its kind is one of the named kinds. The value `any` stands alone: it names no kind, and a file matches when its concern is the provision's concern, so `artifact: any` states the concern-wide scope on a page where another provision names a kind. Each code kind names files the CLI classifies from the path, so a kind with no classifiable files earns no value here.

### Layer Values

| Value | Meaning |
|:---|:---|
| `any` | Every record layer: a provision about the record model as a whole |
| `intent` | The Intent records: the outcomes and problems the product answers |
| `behavior` | The Behavior records: what the software does, holds, and enforces |
| `implementation` | The code that serves the Behavior records, and the practice LiteSpec gives for that code |
| `proof` | The tests and runs that prove the Behavior scenarios, and the practice LiteSpec gives for them |

A trace of `yes` marks a provision that ties its layer to the records: a code mark that names its Behavior record, or a test run that proves a scenario. A trace of `no` marks LiteSpec's practice for the layer. A provision in the Implementation or Proof layer applies to a project that claims the layer with `own` only when its trace is `yes`, and it applies to a project that claims the layer with `litespec` in either case, as the [scope page](scope.md#layer-claims) states.

### Page Layer

A page gives every provision on it one record layer by default, from the page path and its concern. The default table lives in the provision index builder and on this page, so the generated index and the standard state the same table.

| Page | Layer |
|:---|:---|
| `docs/core/intent.md` | `intent` |
| `docs/core/behavior.md`, `docs/core/use-cases.md`, and `docs/core/access.md` | `behavior` |
| `docs/core/release.md` | `implementation` |
| Any other `docs/core/` page | `any` |
| Any other page whose concern is `tests` | `proof` |
| Every other standards page | `implementation` |

Every page's provisions take their trace from the page, which is `no`. A provision states its own layer on its Applies to line only when that layer differs from its page layer, and states `trace: yes` only where it ties its layer to the records. A stack page takes its layer from this table like any other page: `implementation`, or `proof` when its concern is `tests`.

### Strength Values

| Value | Meaning |
|:---|:---|
| `required` | Follow it; depart from it only through a [departure](departures.md) whose `covers` names the provision, with a reason. |
| `default` | Follow it unless the project chooses otherwise, and record the choice. |
| `advisory` | Experience; apply it with judgement and record nothing. |

### Assigning Strength

The first pass over the pages is mechanical: a provision with `Checked by`, or a rule on a checked boundary, takes `required`; a convention or a pattern takes `default`; general knowledge and a provision whose Statement uses `SHOULD` take `advisory`. Each area pass then settles its pages honestly: most provisions earn `required` or `default`, and generic restatements earn `advisory`.

A provision that restates an outside standard is replaced by a link to its source, and a provision that a stack page restates from a base page merges into the base one, so one rule has one home.

### Where Scope Is Written

Each page that owns provisions declares `stack` and `concern` once, in a `**Stack:**` line and a `**Concern:**` line directly under its title. The two lines are the Scope block. A page without provisions, such as a tutorial, a how-to, an explanation, a reference, a command, or an index page, carries no Scope block. Each line names one or more comma-separated values:

```markdown
# Domain Layer

**Stack:** dotnet
**Concern:** domain
```

A provision adds `**Applies to:**` only when it is narrower than its page, or when its layer or trace differs from its page, naming `stack`, `artifact`, `layer`, or `trace` in that order, separated by `; `, for example `artifact: aggregate` or `artifact: aggregate; layer: implementation; trace: yes`. An `artifact` field names one kind, several kinds separated by a comma, `any`, or one or more `record:<type>` values separated by a comma. Narrower means each named field holds a subset of the page values; `artifact` always narrows because the page names none. The value `any` holds the page scope in the field, so a provision that governs every file of its layer states `artifact: any` on a page where another provision names a kind, and carries no line on a page where no provision names a kind. A provision states `layer` only when it differs from its page layer, and `trace` only when it is `yes`, the page trace being `no`. Each of these fields names exactly one value. Every provision carries its `**Strength:**` line directly after its ID line, before `Applies to`. The [authoring standard](authoring.md) states the full provision layout.

`litespec-maintain provisions` writes the stack, concern, artifact, layer, trace, and strength of every provision into the provision index and its data file, with the layer and trace right after the artifact, and states the checked share of rules for each page in the index, so the CLI reads one file instead of parsing every page.

### Scope Diagnostics

These checks report scope defects that no single provision owns, so they are listed here instead of beside a provision.

| Code | Meaning |
|:---|:---|
| `SCOPE_ARTIFACT` | A provision names no artifact on a page where another provision names one. |
| `SCOPE_BLOCK` | A page that owns provisions carries no Scope block, or the block names no stack or no concern. |
| `SCOPE_VALUE` | A Scope block or an Applies to line names a value outside this page's tables, or an Applies to line names a `layer` or `trace` value outside its values, or names more than one value for either. |
| `SCOPE_NARROW` | An Applies to line is as wide as its page, widens it, or repeats the layer or trace of its page. |
| `STRENGTH_LINE` | A provision carries no Strength line, or the line names a value outside `required`, `default`, and `advisory`. |

## Standards

### Declare the Page Scope

**ID:** `litespec/core/scopes#declare-the-page-scope`

**Strength:** required

**Statement:** Each page that owns provisions MUST declare its stack and concern in a `**Stack:**` line and a `**Concern:**` line directly under its title.

**Rationale:** One block under the title tells every reader and tool which situation the page governs without reading further. A page without it leaves its provisions unfiled: retrieval cannot tell which change they govern, so it shows them everywhere or nowhere. Declaring both fields once keeps the page, not each provision, as the home of the common scope.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_BLOCK` diagnostic.

### Use Only Declared Scope Values

**ID:** `litespec/core/scopes#use-only-declared-scope-values`

**Strength:** required

**Statement:** A Scope block and an Applies to line MUST use only the stack, concern, artifact, layer, and trace values this page lists.

**Rationale:** Retrieval matches situations to values: a value invented on one page matches nothing, so its provisions never surface for the change they govern. One closed vocabulary keeps every value comparable across pages. A new situation earns a value here first, then uses it on its page.

**Example:** A provision about the expense aggregate narrows with `artifact: aggregate` rather than inventing `artifact: receipt`. A provision about marks on a record-model page states `trace: yes` rather than inventing a trace value.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_VALUE` diagnostic.

### State Applies to Only When Narrower

**ID:** `litespec/core/scopes#state-applies-to-only-when-narrower`

**Strength:** required

**Statement:** A provision MUST carry an Applies to line only when its scope is narrower than its page scope, its layer or trace differs from its page, or it marks its concern-wide scope with `artifact: any` on a page where another provision names a kind.

**Rationale:** The page scope already governs every provision on it: repeating it on each provision doubles the homes for one fact, and the copies disagree at the next edit. An Applies to line that widens the page scope misfiles the provision under situations the page never governed. One line, only when narrower, at a different layer or trace, or marking the concern-wide scope beside a named kind, keeps retrieval exact.

**Example:** On a page scoped to `concern: domain`, a provision for aggregates carries `**Applies to:** artifact: aggregate`, while a provision for the whole domain concern carries no line. On an application page where a provision names `artifact: validator`, a provision for the whole application concern carries `**Applies to:** artifact: any`. On a record-model page, a provision about marks carries `**Applies to:** layer: implementation; trace: yes`.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_NARROW` diagnostic.

### Name the Artifact of Every Provision on a Page That Names One

**ID:** `litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one`

**Strength:** required

**Statement:** On a page where one provision names an `artifact`, every provision MUST name the kinds of file it governs, or `artifact: any` when it governs every file of its layer.

**Rationale:** `litespec rules` matches a provision to a file by the kind of file it names. A provision that names none on a page of named kinds matches every file of the layer, so a handler receives the validator and port provisions beside its own and an agent reads provisions that do not govern the file in front of it.

**Example:** On the application page, the provision for validators names `artifact: validator` and the provision for the whole application concern names `artifact: any`, so `litespec rules` on a command handler returns the second and leaves out the first.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_ARTIFACT` diagnostic.

### Give Every Provision Its Layer and Trace

**ID:** `litespec/core/scopes#give-every-provision-its-layer-and-trace`

**Strength:** required

**Statement:** Every provision MUST carry the record layer of its page, or state its own layer as a `layer` field on its Applies to line when that layer differs from its page layer; a provision that ties its layer to the records states `trace: yes` on that line.

**Rationale:** A tool that reports only the part of the record model a project claims reads the layer and trace of each provision from the provision index: a provision with no layer, or with a layer copied from its page, gives the tool no exact part to report. The page gives the default, one field states the exception, and the index carries the result beside the artifact, so no reader composes the layer by hand.

**Example:** A provision about expense records on a page under `docs/core/` carries `any` from its page and states no field. A provision that requires code marks on the same page states `**Applies to:** layer: implementation; trace: yes`, and its index row carries `implementation` and `yes`.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_VALUE` diagnostic for a `layer` or `trace` value.

### Depart from a Rule Only Through a Recorded Departure

**ID:** `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure`

**Strength:** required

**Statement:** A project that departs from a provision of Strength `required` MUST record a departure whose `covers` names the provision, with a reason.

**Rationale:** A rule binds every project it reaches: a silent departure claims conformance the project does not hold. The [departure record](departures.md) names the provision and the reason, and an expiry date marks a gap the team means to close, so the departure stays visible and ends instead of lingering.

**Example:** Claimwick ships its weekend export without the review step, so it records a departure whose `covers` names the authorization provision and whose reason states the choice.

**Reviewed for:** Which departures from a provision of Strength `required` name no provision, page or finding code, or give no reason? Look at the files under `.litespec/governance/departures/`.

### Record Each Replaced Default

**ID:** `litespec/core/scopes#record-each-replaced-default`

**Strength:** required

**Statement:** A project that chooses otherwise than a provision of Strength default MUST record its choice where the provision names.

**Rationale:** A default holds until the project says otherwise in writing: without the record, a reader cannot tell a deliberate choice from a provision nobody read. The provision names the recording place in its Replacement, so each choice sits where its provision points. Advice asks for no record, which is what sets the two strengths apart.

**Example:** A project that keeps its pattern catalog outside the default path records the path in `paths.uiPatterns`, as the provision's Replacement names.

**Reviewed for:** Each choice against a provision of Strength default is recorded in the place its Replacement names.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/scopes#declare-the-page-scope`, `litespec/core/scopes#use-only-declared-scope-values`, and `litespec/core/scopes#state-applies-to-only-when-narrower`.

A domain page for the .NET stack opens with its Scope block, and its aggregate provision narrows to the aggregate artifact:

```markdown
# Domain Layer

**Stack:** dotnet
**Concern:** domain

### Guard Each Aggregate Boundary

**ID:** `litespec/backend/domain#guard-each-aggregate-boundary`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate MUST guard every change through its root.

**Rationale:** Changes that pass outside the root skip the checks the aggregate holds.

**Reviewed for:** Which aggregate changes pass outside the root? Look at the callers of each aggregate.
```
