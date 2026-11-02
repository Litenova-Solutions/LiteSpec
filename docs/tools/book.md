# Book

## Name

`litespec book` generates the project book: one offline shell built from a consumer's `model.json` that presents the project model for reviewers.

## Synopsis

```bash
litespec book [--model <model.json>] [--out <folder>] [--root <folder>] [--editor <template>]
```

## Description

The book generator reads the `model.json` file that `litespec model build` wrote and writes a self-contained site from it: one `index.html` shell with three columns, and the data as `.js` shards beside it. Every count, stage and status on every page is computed from `model.json` when the site is generated; nothing is typed by hand. The check findings come from the model too: `litespec model build` writes its check result into `model.json` as `findings` and `known`, so the book needs only the model.

The three columns are the navigation sidebar, the record detail, and the connections rail. The sidebar holds four views. The Outline groups records by record layer and places each record once under its computed parent; the record-layer outline follows [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md). The generator contract is the Parents table on the [model page](../core/model.md): the product is the root, each record sits under its computed parent chain with transparent layer and type groups, every non-parent link renders as a connection labelled from the Links table, and a record the checker reports with `record-parent-missing` has no outline node, nor does a transcription record. The Review queue shows the approval batches in the order `litespec model review` prints them. The Security view shows the cross-tenant access, the outside calls, and the personal and sensitive data for the security review. The Ballots view groups the open questions by risk class with the approver beside each group, with each question recommendation and auto-approval date. The keys `1` to `4` switch views in that order, and the shortcuts dialog lists the full keyboard set.

Every record page follows one anatomy, built once for every record type:

1. A breadcrumb.
2. The title.
3. A plain type line that asks the record type's question, for instance `Problem: Which pain does someone have today?`
4. An identity line with the record identifier, its source and Copy Title and Copy ID. Clicking the identifier also copies it, and the page confirms `Copied` beside the button.
5. The lead: one sentence a reader can judge the record by. A screen shows its chosen capture first, at full width.
6. A Completion block, from the model's computed stage, proof, held-by records and checklist counts. When no test run exists, an implemented record reads `Implemented, proof unknown (no test run in the model)`.
7. One aligned two-column field table, below completion: who, place, status and approval, risk, lifecycle and the type's own fields, in that order. Risk names its class and its approver in words, for instance `Money, approved by Ada`; with no risk, no risk row shows. Facts are text in the table rows, never badges, chips or cards.
8. The prose fields in schema order, then the type-specific content, explanations, questions and findings.
9. Links grouped by relation, with the counts in words: `Solved by (2)`, `Cited by (1)`. Long statements expand instead of being cut.

The right rail holds only three things: the goal-to-behavior trace, implementation and proof.

Glossary words in prose link to their definition on first use per section.

- A problem shows its lead as its statement, then affects, affected segment, cost today and workaround today in the field table, its situation, and its evidence as a table of claim, kind, source and read-on with its evidence strength.
- A journey shows its description as the lead, and its steps as numbered chapters. Each step names its performer: an actor as a reference, or the system it names. A step never borrows the journey's actor, and no step carries an avatar.
- An actor shows its kind in the field table and whether it belongs to a team, as text.
- A screen shows one capture at a time under the title, with Desktop or Phone and Light or Dark choices, the route, theme, width and capture date in the caption, and the other variants prefetching. `litespec book` copies each listed capture into `assets/captures/<screen>/` in the output. When no capture exists, the page states the route and capture state the model gives: the route exists but was not captured, or no page carries the screen.

- The list views group records by a record field rather than by type. Requirements group by topic; use cases, rules, reactions and jobs by aggregate, then module; screens by entry point, then floorplan; journeys by lead actor, then goal; problems by primary affected actor, then the goal solved; features by lifecycle, then completion stage; actors by kind. Every group header gives its count and how many are implemented or proven, and refused features start collapsed.
- The ballots group by risk class with the approver beside each group. The review queue groups the same way.
- A question on a record with no risk auto-approves on its decide-by date plus the veto window; a question on a record with risk needs an explicit answer.
- Where a long list would be cut, the page shows `Show all N` instead. Statements are expandable, never silently cut.

Hovering a mark, a type, a link, a status, a risk class, or a finding shows its LiteSpec definition: a glossary definition, a record type question, a model-page section, or a finding-code meaning, with the LiteSpec source named on every popover. Every word the book shows comes from the carried release.

Every record links to its record file and line; every code element, test, and finding links to the file and line it comes from, opened in the editor. `--root` names the consumer root for those links, and `--editor` names the link template. With `--editor none` the book renders no source links.

A link taken from record data renders as a link only for an `http:`, `https:`, or `mailto:` target, an in-page fragment, a record identifier, a research source key cited as `[text][key]`, a relative path with no scheme, or the configured editor scheme. Any other target renders as plain text. Records order by ordinal title, then identifier.

The data shards load by inserting script elements, never by fetch, so the site needs no server: open the output folder's `index.html` in a browser. The shell carries a `noscript` note and the manifest summary as static text; there are no static record pages. Record links are hash routes of the form `index.html#<id>`.

Verdicts stay local. A reviewer marks a record approved, reviewed, changes requested, or questioned, and answers ballot questions, all in the browser. Download review writes the verdicts and answers as `review.json`; the book never writes records.

When the model holds no test run, completion reads implemented with proof unknown, and the proof sections read as no test run in the model.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--model <model.json>` | `.litespec/model/model.json` in the consumer found from the working folder | Reads this model file. |
| `--out <folder>` | `.litespec/book` in the same consumer | Writes the site to this folder, creating it. |
| `--root <folder>` | the repository root when the model sits in a `.litespec/model` folder, else the model folder | Names the consumer root that source links open. |
| `--editor <template>` | `vscode://file/{root}/{path}:{line}` | Builds source links from this template with the `{root}`, `{path}`, and `{line}` placeholders; `none` renders no source links. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The site was written. |
| `1` | The model file is missing or unreadable, or the output folder cannot be written. |
| `2` | The arguments are wrong, the carried LiteSpec release is missing, or a carried LiteSpec page the lexicon needs is missing or lost its table shape; the message names the file. |

## Examples

Generate the book for a consumer whose model is built:

```bash
litespec book --model ../consumer/.litespec/model/model.json --out ../consumer/.litespec/book
```

Name the consumer root and the editor that source links open:

```bash
litespec book --model ../consumer/.litespec/model/model.json --out ../consumer/.litespec/book --root ../consumer --editor "vscode://file/{root}/{path}:{line}"
```

Render the book with no source links:

```bash
litespec book --model ../consumer/.litespec/model/model.json --out ../consumer/.litespec/book --editor none
```

The review queue and the findings come from the model build recorded in the model:

```bash
litespec model build --root ../consumer
litespec book --model ../consumer/.litespec/model/model.json --out ../consumer/.litespec/book
```

## Underneath

The command writes `index.html`, the `assets` folder with `book.js`, `book.css`, `book-extra.css`, and `book-fonts.css`, and the `data` folder with the shards: `manifest.js` with the project, the roots, the editor template, the test run, the project approval table, the totals, the shard map, and the unplaced ids; `lexicon.js` with the type, layer, link, field, glossary, section, and finding words parsed from the carried release; one `outline-<layer>-<n>.js` per record layer, sequential past the split budget; `records-<nnn>.js` packs in identifier order, carrying fields, computed completion, approval, captures and evidence strength; `search-<n>.js` packs loaded on first search; `assets/captures/<screen>/` with the copied screen captures; and `queue.js` with the review batches by risk class and approver, the ballots, and the security items. Each shard is a script calling `BookData.put`, so the site opens as files. Nothing beneath this command is separately runnable.
