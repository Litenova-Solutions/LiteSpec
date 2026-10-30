# Contracts Between Parallel Slices

The judge fixed these shapes so slices that run at the same time agree. A slice that writes one side
writes exactly this; a slice that reads it codes against exactly this. Part 0 of `spec.md` still wins.

## 1. The Links Table in `docs/core/model.md`

Section `### Links` keeps one table, now with five columns, header exactly:

```markdown
| Field | Written on | Points at | Reads as | Read back as |
|:---|:---|:---|:---|:---|
```

- `Field` is the field name in backticks (`` `advances` ``, `` `access.grant` ``).
- `Reads as` and `Read back as` are plain lowercase phrases, no backticks, no trailing period.
- A row with two carrier readings (the `actor` row) writes both phrases separated by ` / ` in the
  same cell, in the order of the carriers in `Written on`: `is walked by / acts as`.
- 35 rows, exactly the rows and phrases of spec.md Part B Section 2. Several rows may share a
  field name (`serves` has three rows); a reader keys a row by field plus its `Written on` text.

## 2. The Parent Table in `docs/core/model.md`

A new Concepts subsection `### Parents`, placed directly after `### Links`, holds the parent rule
(Part C P1 to P5, without any `product` field, per correction C1) and one table, header exactly:

```markdown
| Record type | Parent | Found through |
|:---|:---|:---|
```

One row per record type in `RecordTypes.All` (31 rows), the type in backticks. `Parent` is one or
more record types in backticks joined by `, ` or ` or `, or the words `the product`, or `none` for
`product` itself. `Found through` is one of these phrases (a reader matches on them):

| Type | Parent | Found through |
|:---|:---|:---|
| `product` | none | the project that declares it |
| `goal`, `problem`, `constraint`, `research`, `decision`, `actor`, `term`, `module`, `role`, `permission`, `entry-point`, `external-system`, `runbook`, `integration-event`, `deviation`, `known-gap` | the product | the product |
| `feature` | `problem` | `solves`, first entry |
| `requirement` | `feature`, `constraint`, `research` or `decision` | `features`, first entry; else `basedOn`, first entry; else a decision naming it in `decides` |
| `journey` | `goal` | `serves` |
| `aggregate` | `module` | identifier home |
| `use-case` | `aggregate` or `module` | the aggregate holding its page; else identifier home |
| `result` | `use-case` | identifier home |
| `scenario` | `result` | `result` |
| `rule` | `aggregate` or `module` | identifier home, an aggregate before a module |
| `event` | `aggregate` | identifier home |
| `reaction`, `job`, `notification`, `setting` | `module` | identifier home |
| `screen` | `entry-point` | `entryPoint`, agreeing with the identifier home |

(The page writes one row per type; the grouped rows above are for brevity here only.)

## 3. Checker Foundations (Already Written by the Judge)

- `Finding.TierOverride` (`string?`, init) in `apps/model/src/LiteSpec.Model.Core/Finding.cs`;
  `Finding.Tier` returns it when set.
- `Finding.Tiers` holds `record-without-parent` = G, `feature-without-goal` = A,
  `result-without-scenario` = A; `Checker.Codes` holds all three.
- `Checker.Tiered(string code, string message, Record record, ProjectModel model)` in
  `Checker.Graph.cs` returns a `Finding` with `TierOverride = "G"` for a tier 1 record.
- `Checker.Check` already calls, in this order and with this exact signature
  `private static void CheckX(ProjectModel model, List<Finding> findings)`:
  `CheckResultWithoutScenario` (after `CheckProof`), `CheckMissingParent` and `CheckFeatureGoal`
  (after `CheckOrphaned`). Each new check slice writes its method in its own partial file
  (`Checker.Parents.cs`, `Checker.Features.cs`, `Checker.Results.cs`) inside
  `static partial class Checker` in namespace `LiteSpec.Model`.
- The schema renames land in the same wave: features write `lifecycle`, `advances`, `plannedFor`,
  `solves`; requirements write `features` (plural) and `dueBy`; journeys write `aim` and
  `serves: ["goal/<name>"]`; goals `dueBy`; decisions `decidedOn`; constraints `authority`, `kind`;
  settings may write `readBy`; research writes `conclusion`. The judge renames the reads in
  `Checker.cs` (`CheckTypeFields`, `CheckFulfilment`), `Approver.cs` and `LinkTargets`
  (`advances`, `readBy`, `features` replace `feature`).

## 4. The Book's Data Contract

The book is one `index.html` plus script shards (spec.md Part E). Every shard file is
`data/<key>.js` holding exactly one call `BookData.put("<key>", <json>);`. The shell defines
`window.BookData` with `put(key, value)`, `get(key)` and `load(key) -> Promise` (inserts
`<script src="data/<key>.js">` once, resolves when `put` for that key arrives). No `fetch`.

Keys and contents:

| Key | Contents |
|:---|:---|
| `manifest` | `{ project: {id, title}, roots: {consumer, litespec}, editor: "<template or none>", testRun: {...} or null, totals: {<type>: count}, layers: [{key, label, outline: ["outline-intent-0", ...]}], shardOf: {<record id>: "records-000"}, search: ["search-0", ...], unplaced: [<record id>], generatedOn }` |
| `lexicon` | `{ types: {<type>: {label, plural, layer, question}}, layers: {<key>: {label, writes, reviews}}, links: [{field, carriers, forward, inverse}], fields: {<field>: label}, glossary: {<lowercase term>: {term, definition, layer, source}}, sections: {<section title>: {text, source}}, findings: {<code>: {tier, meaning}} }` |
| `outline-<layer>-<n>` | `[node]`, node = `{ id, kind: "record" or "group", label, type, status, children: [node] }`; a group node's `id` is `<layer>/<type>`, its `label` the type's plural from the lexicon or the layer label |
| `records-<nnn>` | `{ <record id>: record }`, record = the mockup's record shape (`type, title, owner, status, effective, decider, decideBy, fields, src`) plus Part E Section 3 fields (`parent, layer, proof, machine, reachable, keptBy, stepServers, stepScreens, exercised, route, calls, impl, tests, expect, lacks, heldBy, findings, bodyHtml, sections, out, in`) |
| `search-<n>` | `[[id, title, type, excerpt]]` |
| `queue` | `{ batches, ballots, security }` |

Layer keys: `intent`, `behavior`, `implementation`, `proof`, `governance`, `glossary`.

C# types, namespace `LiteSpec.Book` (the namespace of the existing book code), all `internal`:

- `BookLexicon.Load(string liteSpecRoot)` returns a `BookLexicon`; `ToJson()` returns the
  `lexicon` value as `System.Text.Json.Nodes.JsonObject`. A missing page or table throws
  `BookLexiconException` whose `File` names the path; `BookCommand` maps it to exit 1.
- `BookParents.Rules` is `IReadOnlyDictionary<string, BookParentRule>` keyed by record type;
  `BookParentRule(string Type, IReadOnlyList<string> ParentTypes, string FoundThrough)`.
  `BookParents.ParentOf(JsonObject model, string recordId)` returns the parent id or null.
- `BookOutline.Build(JsonObject model)` returns `BookOutlineResult` with
  `IReadOnlyDictionary<string, string?> ParentOf`, `IReadOnlyList<BookOutlineNode> Layers`
  (one root node per layer key, children nested) and `IReadOnlyList<string> Unplaced`.
  `BookOutlineNode(string Id, string Kind, string Label, string? Type, string? Status, List<BookOutlineNode> Children)`.
- `BookEnrich.Build(JsonObject model, BookOutlineResult outline)` returns
  `IReadOnlyDictionary<string, JsonObject>`: one record detail object per record id.
- `BookShards.Write(string outDir, ...)` and `BookShell` (wave 2) write the files above.

The `model` argument is the parsed `model.json` (`JsonNode.Parse(...)!.AsObject()`), the same file
`litespec book --model` reads today.
