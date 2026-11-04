# Provision Scopes

**Stack:** any
**Layer:** workspace

## Intent

Provisions are filed by concern: each page states the rules for one concern, while an agent changing one file needs every rule for that situation. This page states the shared situation vocabulary each provision carries: its stack, its layer, its artifact, and its strength. Authors write these values when they write or grade a provision, and the provision index carries them so the CLI lists the provisions that apply to one path.

## Concepts

### The Four Fields

Each provision carries four scope fields. The page declares the first two once; a provision narrows them or adds the third; the fourth grades how binding the provision is.

| Field | Answers | Values |
|:---|:---|:---|
| `stack` | Which technology | `dotnet`, `react`, `svelte`, `blazor`, `android`, `any` |
| `layer` | Which part of the system | `domain`, `application`, `infrastructure`, `web-api`, `frontend-ui`, `frontend-data`, `tests`, `records`, `workspace`, `operations` |
| `artifact` | Which kind of thing is made | One code kind, or `record:<type>` for one record of that type |
| `strength` | How binding it is | `rule`, `default`, `advice` |

### Stack Values

| Value | Meaning |
|:---|:---|
| `dotnet` | The .NET backend stack |
| `react` | The React web stack |
| `svelte` | The Svelte web stack |
| `blazor` | The Blazor stack |
| `android` | The Android mobile stack |
| `any` | No technology; the provision applies to every stack |

### Layer Values

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
| `command-handler` | The execution path of one command use case |
| `query-handler` | The execution path of one query use case |
| `endpoint` | One route operation and the class serving it |
| `migration` | One storage schema change |
| `job` | Scheduled or manual background work in code |
| `component` | One UI component |
| `route` | One frontend address and its page file |
| `form` | One data-entry form |
| `test` | One test |
| `record:<type>` | One record of that type, such as `record:use-case`. The types are the Type column of the placement table on the [model page](model.md) |

`record:aggregate` names the aggregate record while `aggregate` names its code, and `record:job` names the job record while `job` names its code.

### Strength Values

| Value | Meaning |
|:---|:---|
| `rule` | Follow it; depart from it only through a recorded exception of kind `departure` whose `covers` names the provision, with a reason. |
| `default` | Follow it unless the project chooses otherwise, and record the choice. |
| `advice` | Experience; apply it with judgement and record nothing. |

### Assigning Strength

The first pass over the pages is mechanical: a provision with `Checked by`, or a rule on a checked boundary, takes `rule`; a convention or a pattern takes `default`; general knowledge and a provision whose Statement uses `SHOULD` take `advice`. Each area pass then settles its pages honestly: most provisions earn `rule` or `default`, and generic restatements earn `advice`.

A provision that restates an outside standard is replaced by a link to its source, and a provision that a stack page restates from a base page merges into the base one, so one rule has one home.

### Where Scope Is Written

Each page that owns provisions declares `stack` and `layer` once, in a `**Stack:**` line and a `**Layer:**` line directly under its title. The two lines are the Scope block. A page without provisions, such as a tutorial, a how-to, an explanation, a reference, a command, or an index page, carries no Scope block. Each line names one or more comma-separated values:

```markdown
# Domain Layer

**Stack:** dotnet
**Layer:** domain
```

A provision adds `**Applies to:**` only when it is narrower than its page, naming `stack`, `layer`, or `artifact` in that order, for example `artifact: aggregate`. Narrower means each named field holds a subset of the page values; `artifact` always narrows because the page names none. Every provision carries its `**Strength:**` line directly after its ID line, before `Applies to`. The [authoring standard](authoring.md) states the full provision layout.

`litespec-maintain provisions` writes the scope and strength of every provision into the provision index, so the CLI reads one file instead of parsing every page.

### Scope Diagnostics

These checks report scope defects that no single provision owns, so they are listed here instead of beside a provision.

| Code | Meaning |
|:---|:---|
| `SCOPE_BLOCK` | A page that owns provisions carries no Scope block, or the block names no stack or no layer. |
| `SCOPE_VALUE` | A Scope block or an Applies to line names a value outside this page's tables. |
| `SCOPE_NARROW` | An Applies to line is as wide as its page, widens it, or repeats it. |
| `STRENGTH_LINE` | A provision carries no Strength line, or the line names a value outside `rule`, `default`, and `advice`. |

## Standards

### Declare the Page Scope

**ID:** `litespec/core/scopes#declare-the-page-scope`

**Strength:** rule

**Statement:** Each page that owns provisions MUST declare its stack and layer in a `**Stack:**` line and a `**Layer:**` line directly under its title.

**Rationale:** One block under the title tells every reader and tool which situation the page governs without reading further. A page without it leaves its provisions unfiled: retrieval cannot tell which change they govern, so it shows them everywhere or nowhere. Declaring both fields once keeps the page, not each provision, as the home of the common scope.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_BLOCK` diagnostic.

### Use Only Declared Scope Values

**ID:** `litespec/core/scopes#use-only-declared-scope-values`

**Strength:** rule

**Statement:** A Scope block and an Applies to line MUST use only the stack, layer, and artifact values this page lists.

**Rationale:** Retrieval matches situations to values: a value invented on one page matches nothing, so its provisions never surface for the change they govern. One closed vocabulary keeps every value comparable across pages. A new situation earns a value here first, then uses it on its page.

**Example:** A provision about the expense aggregate narrows with `artifact: aggregate` rather than inventing `artifact: receipt`.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_VALUE` diagnostic.

### State Applies to Only When Narrower

**ID:** `litespec/core/scopes#state-applies-to-only-when-narrower`

**Strength:** rule

**Statement:** A provision MUST carry an Applies to line only when its scope is narrower than its page scope.

**Rationale:** The page scope already governs every provision on it: repeating it on each provision doubles the homes for one fact, and the copies disagree at the next edit. An Applies to line that widens the page scope misfiles the provision under situations the page never governed. One line, only when narrower, keeps retrieval exact.

**Example:** On a page scoped to `layer: domain`, a provision for aggregates carries `**Applies to:** artifact: aggregate`, while a provision for the whole domain layer carries no line.

**Checked by:** `litespec-maintain validate` emits no `SCOPE_NARROW` diagnostic.

### Depart from a Rule Only Through a Recorded Exception

**ID:** `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-exception`

**Strength:** rule

**Statement:** A project that departs from a provision of Strength rule MUST record an exception of kind `departure` whose `covers` names the provision, with a reason.

**Rationale:** A rule binds every project it reaches: a silent departure claims conformance the project does not hold. The exception record names the provision, the reason, and the expiry date, so the departure stays visible, dated, and ends instead of lingering. The [model page](model.md) states how a live exception outranks its provision.

**Example:** Claimwick ships its weekend export without the review step, so it records an exception of kind `departure` whose `covers` names the authorization provision, with the reason and an expiry date.

**Reviewed for:** Each departure from a provision of Strength rule resolves to an exception record under `.litespec/governance/exceptions/` with the provision, a reason, and an expiry date.

### Record Each Replaced Default

**ID:** `litespec/core/scopes#record-each-replaced-default`

**Strength:** rule

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
**Layer:** domain

### Guard Each Aggregate Boundary

**ID:** `litespec/backend/domain#guard-each-aggregate-boundary`

**Strength:** rule

**Applies to:** artifact: aggregate

**Statement:** An aggregate MUST guard every change through its root.

**Rationale:** Changes that pass outside the root skip the checks the aggregate holds.

**Reviewed for:** Which aggregate changes pass outside the root? Look at the callers of each aggregate.
```
