# Provision Scopes

**Stack:** any
**Concern:** workspace

Each provision carries scope fields filing it by situation: the stack, the concern, the artifact, the record layer it governs, whether it traces that layer to the records, and how binding it is. This page states those fields and the scope block each page declares. Each page states the rules for one concern, but an agent changing one file needs every rule for that situation. Set these values when writing or grading a provision. The provision index carries them, so the CLI lists the provisions applying to one path and shows which record layer each provision belongs to.

## Concepts

### The Six Fields

A provision is one rule in the standard, and each provision carries six scope fields naming the situation it governs. The page declares the stack and concern once for all its provisions. A single provision narrows the stack or names an artifact where it governs less than the whole page.

```yaml
paths:
  behaviorDocs: .litespec/behavior
```

The project record above points at one Behavior tree, the same way each provision points at its situation: the `layer` field names the record layer the provision governs, and `trace` says whether the provision ties that layer back to the records. The [project scope page](scope.md#layer-claims) states which layers a project claims.

| Field | Answers | Values |
|:---|:---|:---|
| `stack` | Which technology | `dotnet`, `svelte`, `android`, `any` |
| `concern` | Which part of the system | `domain`, `application`, `infrastructure`, `web-api`, `frontend-ui`, `frontend-data`, `tests`, `records`, `workspace`, `operations` |
| `artifact` | Which kind of thing is made | `any`, one code kind, several code kinds separated by a comma, or `record:<type>` for one record of that type |
| `layer` | Which record layer the provision governs | `any`, `intent`, `behavior`, `implementation`, `proof` |
| `trace` | Whether it ties its layer to the records | `yes`, `no` |
| `strength` | How binding it is | `required`, `default`, `advisory` |

### Stack Values

The `stack` field names the technology a provision governs:

| Value | Meaning |
|:---|:---|
| `dotnet` | The .NET backend stack |
| `svelte` | The Svelte web stack |
| `android` | The Android mobile stack |
| `any` | No technology; the provision applies to every stack |

### Concern Values

The `concern` field names the part of the system a provision governs:

| Value | Meaning |
|:---|:---|
| `domain` | Business state, rules, and language |
| `application` | Use-case coordination and the pipeline |
| `infrastructure` | Storage, messaging, and outside calls |
| `web-api` | HTTP endpoints and their contracts |
| `frontend-ui` | Components, routes, and rendering |
| `frontend-data` | Client state, the cache, and generated clients |
| `tests` | The suites proving behavior |
| `records` | Specification records and standards pages |
| `workspace` | Repository shape, packages, and configuration |
| `operations` | Releases, deployment, and support |

### Artifact Values

The `artifact` field names the kind of thing a provision governs:

| Value | Meaning |
|:---|:---|
| `aggregate` | The aggregate root with the entities it guards |
| `state-record` | One named state of an aggregate and its data |
| `value-object` | One immutable type defined by its values |
| `domain-event` | One fact completed inside the bounded context |
| `knowledge-model` | One knowledge model project: the row types, roles, keys, lookups and validation of one area |
| `knowledge-set` | One knowledge set project: the dated rows of one variant of an area |
| `use-case` | The message and result types of one use case |
| `command-handler` | The execution path of one command use case |
| `query-handler` | The execution path of one query use case |
| `validator` | The input validation of one command or query use case |
| `reaction` | The handler class of one reaction |
| `port` | One declared port interface |
| `repository` | One aggregate repository, its interface or its store implementation |
| `endpoint` | One route operation and the class serving it |
| `migration` | One storage schema change |
| `job` | Scheduled or one-off background work in code |
| `component` | One UI component |
| `route` | One frontend address and its page file |
| `form` | One data-entry form |
| `test` | One test |
| `any` | Every file of the provision layer |
| `record:<type>` | One record of that type, such as `record:use-case`. The types are the Type column of the placement table on the [model page](model.md) |

The `record:` prefix marks the difference between a record and its code. The value `record:aggregate` names the aggregate record, while `aggregate` names its code. The value `record:job` names the job record, while `job` names its code.

When one rule governs several code kinds, the provision lists them separated by commas, for example `artifact: command-handler, query-handler`. A file matches when its kind is one of the listed kinds.

The value `any` stands alone and carries no kind. A file matches when its concern is the provision's concern. On a page where another provision names a kind, `artifact: any` marks a provision with concern-wide scope.

Every code kind names files the CLI classifies from the path, so a kind with no classifiable files gets no value in this table.

### Layer Values

The `layer` field names the record layer a provision governs:

| Value | Meaning |
|:---|:---|
| `any` | Every record layer: a provision about the record model as a whole |
| `intent` | The Intent records: the outcomes and problems the product answers |
| `behavior` | The Behavior records: what the software does, holds, and enforces |
| `implementation` | The code serving the Behavior records, and the practice LiteSpec gives for that code |
| `proof` | The tests and runs proving the Behavior scenarios, and the practice LiteSpec gives for them |

A trace of `yes` marks a provision tying its layer back to the records, such as a code mark naming its Behavior record or a test run proving a scenario. A trace of `no` marks LiteSpec's own practice for the layer without such a link.

For a project claiming Implementation or Proof with `own`, only provisions with trace `yes` apply. For a project claiming the layer with `litespec`, provisions apply whatever their trace. The [project scope page](scope.md#layer-claims) states the layer claims.

### Page Layer

Each page gives its provisions one default record layer. The default comes from the page path and its concern. The table below repeats the defaults the provision index builder uses, so the generated index and the standard agree.

| Page | Layer |
|:---|:---|
| `docs/core/intent.md` | `intent` |
| `docs/core/behavior.md`, `docs/core/use-cases.md`, and `docs/core/access.md` | `behavior` |
| `docs/core/testing.md` and `docs/core/ui-verification.md` | `proof` |
| `docs/core/release.md`, `docs/core/backend-*.md`, `docs/core/knowledge.md`, `docs/core/audit.md`, `docs/core/concurrency.md`, `docs/core/integrations.md`, `docs/core/jobs.md`, `docs/core/lifecycle.md`, `docs/core/locale.md`, `docs/core/outbox.md`, `docs/core/tenancy.md`, and `docs/core/ui-ia.md` | `implementation` |
| Any other `docs/core/` page | `any` |
| Any other page whose concern is `tests` | `proof` |
| Every other standards page | `implementation` |

Every provision takes its trace from its page, which is `no`, unless it states otherwise. A provision states its own layer on its Applies to line when that layer differs from the page default. It states `trace: yes` where it ties its layer back to the records. A stack page takes its layer from this table like any other page: `implementation`, or `proof` when its concern is `tests`.

### Strength Values

The `strength` field grades how binding a provision is:

| Value | Meaning |
|:---|:---|
| `required` | Follow it; set it aside through a [waiver](waivers.md) whose `waives` names the provision, with a reason. |
| `default` | Follow it unless the project chooses otherwise, and record the choice. |
| `advisory` | Experience; apply it with judgement and record nothing. |

Grading starts with a mechanical pass. A provision with `Checked by`, or a rule on a checked boundary, takes `required`. A convention or a pattern takes `default`. General knowledge, and a provision whose Statement uses `SHOULD`, takes `advisory`. Each area pass then settles its pages honestly: most provisions bind, and generic restatements earn `advisory`.

A provision restating an outside standard becomes a link to its source instead. A provision a stack page repeats from a base page merges into the base one. Each rule keeps one home.

A project setting a rule aside records a waiver naming it with a reason. A project choosing otherwise than a `default` provision records its choice where the provision names. An `advisory` provision asks for no record, which sets the two strengths apart.

### Where Scope Is Written

Each page owning provisions declares its `stack` and `concern` once. The two lines sit directly under its title: a `**Stack:**` line and a `**Concern:**` line. Together they form the Scope block. Each line names one or more values separated by commas:

```markdown
# Domain Layer

**Stack:** dotnet
**Concern:** domain
```

A page without provisions carries no Scope block, such as a tutorial, a how-to, an explanation, a reference, a command, or an index page.

A provision adds an `**Applies to:**` line when it is narrower than its page, or when its layer or trace differs from its page. The line names `stack`, `artifact`, `layer`, or `trace` in that order, separated by `; `. For example, `artifact: aggregate` narrows to aggregates, and `artifact: aggregate; layer: implementation; trace: yes` also moves the layer.

An `artifact` field names one kind, several kinds separated by commas, `any`, or one or more `record:<type>` values separated by commas. Narrower means each named field holds a subset of the page values. The `artifact` field narrows in every case, since the page names none.

The value `any` holds the page scope in the field. A provision governing every file of its layer states `artifact: any` on a page where another provision names a kind, and carries no line on a page where no provision names one. A provision states `layer` when it differs from its page layer, and `trace` when it is `yes`, since the page trace is `no`. Each of these fields names exactly one value.

Every provision carries its `**Strength:**` line directly after its ID line, before Applies to. The [authoring standard](authoring.md) states the full provision layout.

`litespec-maintain provisions` writes the stack, concern, artifact, layer, trace, and strength of every provision into the provision index and its data file. It places the layer and trace right after the artifact. It also states the checked share of rules for each page in the index, so the CLI reads one file instead of parsing every page.

### Scope Diagnostics

These checks report scope defects belonging to no single provision. They are listed here instead of beside one.

| Code | Meaning |
|:---|:---|
| `SCOPE_ARTIFACT` | A provision names no artifact on a page where another provision names one. |
| `SCOPE_BLOCK` | A page owning provisions carries no Scope block, or the block names no stack or no concern. |
| `SCOPE_VALUE` | A Scope block or an Applies to line names a value outside this page's tables, or an Applies to line names a `layer` or `trace` value outside its values, or names more than one value for either. |
| `SCOPE_NARROW` | An Applies to line is as wide as its page, widens it, or repeats the layer or trace of its page. |
| `STRENGTH_LINE` | A provision carries no Strength line, or the line names a value outside `required`, `default`, and `advisory`. |

## Standards

### Declare the Page Scope

**ID:** `litespec/core/scopes#declare-the-page-scope`

**Strength:** required

**Statement:** Each page that owns provisions MUST declare its stack and concern in a `**Stack:**` line and a `**Concern:**` line directly under its title.

**Rationale:** Without the block, the page's provisions stay unfiled. Retrieval has no way to tell which change they govern, so it shows them everywhere or nowhere. One block under the title tells every reader and tool which situation the page governs, with no need to read further. Declaring both fields once keeps the page, rather than each provision, as the home of the shared scope.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_BLOCK` diagnostic.

### Use Only Declared Scope Values

**ID:** `litespec/core/scopes#use-only-declared-scope-values`

**Strength:** required

**Statement:** A Scope block and an Applies to line MUST use only the stack, concern, artifact, layer, and trace values this page lists.

**Rationale:** Retrieval matches situations to values. A value invented on one page matches nothing, and its provisions surface for no change they govern. One closed vocabulary keeps every value comparable across pages. A new situation earns its value on this page first, then uses it on its page.

**Example:** A provision about the expense aggregate narrows with `artifact: aggregate` rather than inventing `artifact: receipt`. A provision about marks on a record-model page states `trace: yes` rather than inventing a trace value.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_VALUE` diagnostic.

### State Applies to Only When Narrower

**ID:** `litespec/core/scopes#state-applies-to-only-when-narrower`

**Strength:** required

**Statement:** A provision MUST carry an Applies to line only when its scope is narrower than its page scope, its layer or trace differs from its page, or it marks its concern-wide scope with `artifact: any` on a page where another provision names a kind.

**Rationale:** The page scope already governs every provision on the page. Repeating it on each provision gives one fact two homes, and the copies disagree at the next edit. Widening it misfiles the provision under situations outside the page. A line appearing only when narrower, at a different layer or trace, or marking concern-wide scope beside a named kind keeps retrieval exact.

**Example:** On a page scoped to `concern: domain`, a provision for aggregates carries `**Applies to:** artifact: aggregate`. A provision for the whole domain concern carries no line. On an application page where a provision names `artifact: validator`, a provision for the whole application concern carries `**Applies to:** artifact: any`. On a record-model page, a provision about marks carries `**Applies to:** layer: implementation; trace: yes`.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_NARROW` diagnostic.

### Name the Artifact of Every Provision on a Page That Names One

**ID:** `litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one`

**Strength:** required

**Statement:** On a page where one provision names an `artifact`, every provision MUST name the kinds of file it governs, or `artifact: any` when it governs every file of its layer.

**Rationale:** The `litespec rules` command matches a provision to a file by the kind of file the provision names. A provision naming none on a page of named kinds matches every file of the layer. A handler receives the validator and port provisions beside its own, and an agent reads provisions outside the file in front of it.

**Example:** On the application page, the provision for validators names `artifact: validator`. The provision for the whole application concern names `artifact: any`. Running `litespec rules` on a command handler returns the second and leaves out the first.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_ARTIFACT` diagnostic.

### Give Every Provision Its Layer and Trace

**ID:** `litespec/core/scopes#give-every-provision-its-layer-and-trace`

**Strength:** required

**Statement:** Every provision MUST carry the record layer of its page, or state its own layer as a `layer` field on its Applies to line when that layer differs from its page layer; a provision that ties its layer to the records states `trace: yes` on that line.

**Rationale:** A tool reporting the part of the record model a project claims reads each provision's layer and trace from the provision index. A provision with no layer, or with a layer copied from its page, gives the tool no exact part to report. The page gives the default, one field states the exception, and the index carries the result beside the artifact, so no reader composes the layer by hand.

**Example:** A provision about expense records on a page under `docs/core/` carries `any` from its page and states no field. A provision requiring code marks on the same page states `**Applies to:** layer: implementation; trace: yes`. Its index row carries `implementation` and `yes`.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_VALUE` diagnostic for a `layer` or `trace` value.

### Set a Rule Aside Only Through a Recorded Waiver

**ID:** `litespec/core/scopes#set-a-rule-aside-only-through-a-recorded-waiver`

**Strength:** required

**Statement:** A project that sets aside a provision of Strength `required` MUST record a waiver whose `waives` names the provision, with a reason.

**Rationale:** A rule binds every project it reaches. A project quietly skipping a binding rule claims it follows the standard in records saying otherwise. The [waiver record](waivers.md) names the provision and the reason. A gap waiver's expiry date marks a gap the team means to close, so the waiver stays visible and ends instead of lingering.

**Example:** Claimwick ships its weekend export without the review step. It records a waiver whose `waives` names the authorization provision and whose reason states the choice.

**Reviewed for:** Does every waiver from a provision of Strength `required` name a provision, page, or finding code, and give a reason? Check each file under `.litespec/governance/waivers/`.

### Record Each Replaced Default

**ID:** `litespec/core/scopes#record-each-replaced-default`

**Strength:** required

**Statement:** A project that chooses otherwise than a provision of Strength default MUST record its choice where the provision names.

**Rationale:** Without the record, a reader cannot tell a deliberate choice from a provision nobody read. A default holds until the project says otherwise in writing. The provision names the recording place in its Replacement, so each choice sits where its provision points. An advisory provision asks for no record, which sets the two strengths apart.

**Example:** A project keeps its pattern catalog outside the default path, and records the path in `paths.uiPatterns`, as the provision's Replacement names.

**Reviewed for:** Is every choice against a default recorded where its provision's Replacement names? Check each replaced default's recording place.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/scopes#declare-the-page-scope`, `litespec/core/scopes#use-only-declared-scope-values`, and `litespec/core/scopes#state-applies-to-only-when-narrower`.

A .NET domain page opens with a Scope block naming `dotnet` and `domain`. A provision governing aggregates carries `**Applies to:** artifact: aggregate`, using a value from the artifact table, while a provision governing the whole domain concern carries no Applies to line. On a record-model page, a provision about code marks carries `**Applies to:** layer: implementation; trace: yes`.
