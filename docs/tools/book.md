# Book

## Name

`litespec book` generates the project book: one offline shell built from a consumer's `model.json` that presents the project model for reviewers.

## Synopsis

```bash
litespec book --model <model.json> --out <folder> [--root <folder>] [--editor <template>]
```

## Description

The book generator reads the `model.json` file that `litespec model build` wrote and writes a self-contained site from it: one `index.html` shell with three columns, and the data as `.js` shards beside it. Every count on every page is computed from `model.json` when the site is generated; no count is typed by hand. The check findings come from the model too: `litespec model build` writes its check result into `model.json` as `findings` and `known`, so the book needs only the model.

The three columns are the navigation sidebar, the record detail, and the connections rail. The sidebar holds four views. The Outline groups records by record layer and places each record once under its computed parent; the record-layer outline follows [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md). The generator contract is the Parents table on the [model page](../core/model.md): the product is the root, each record sits under its computed parent chain with transparent layer and type groups, every non-parent link renders as a connection labelled from the Links table, and a record the checker reports with `record-parent-missing` has no outline node, nor does a transcription record. The Review queue shows the approval batches in the order `litespec model review` prints them. The Security view shows the cross-tenant access, the outside calls, and the personal and sensitive data for the security review. The Ballots view groups the open questions by `ballot`, ordered tier 1 first, with each question recommendation and auto-approval date. The keys `1` to `4` switch views in that order, and the shortcuts dialog lists the full keyboard set.

Every record opens in the detail column with the same facts list first: its status, its status reason, its owner, its risk tier, and its approval batch. The status reason reads in words: what the draft waits for, which drafts hold it, or who approved or reviewed it and when. Below the facts the detail renders the record content: a journey shows its `finishedWhen` and its steps as sentences with the use cases and screens serving each step, plus the requirements it cites with their statements and their `except` and fallback inline; a use case shows its results and scenarios as Given, When, Then sentences with their example tables; an aggregate shows its states as cards with a flow table of its transitions, and marks each state past the start that no transition reaches. A finding shows its code meaning from the finding table on the [model page](model.md).

The rail traces the record from its goal down to behavior in the From goal to behavior trace, then shows the Implementation section with the code carrying the record mark, and the Proof section with the tests proving it. The remaining connections follow with the reads-as phrases the Links table on the [model page](../core/model.md) states.

Hovering a mark, a type, a link, a status, a tier, or a finding shows its LiteSpec definition: a glossary definition, a record type question, a model-page section, or a finding-code meaning, with the LiteSpec source named on every popover. Every word the book shows comes from the carried release; nothing is written by hand.

Every record, code element, test, and finding links to the file and line it comes from, opened in the editor. `--root` names the consumer root for those links, and `--editor` names the link template. With `--editor none` the book renders no source links.

The data shards load by inserting script elements, never by fetch, so the site needs no server: open the output folder's `index.html` in a browser. The shell carries a `noscript` note and the manifest summary as static text; there are no static record pages. Record links are hash routes of the form `index.html#<id>`.

Verdicts stay local. A reviewer marks a record approved, reviewed, changes requested, or questioned, and answers ballot questions, all in the browser. Download review writes the verdicts and answers as `review.json`; the book never writes records.

When the model holds no test run, every proof statement reads as not run: a claimed scenario reads as test written, not run, a test reads as passing: not run, and the proof sections read as no test run in the model.

The old shapes are gone: no per-record URLs, no Names page, no lens pages, and no Mermaid. Aggregate lifecycles render as state cards with a flow table.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--model <model.json>` | none | Reads this model file. Required. |
| `--out <folder>` | none | Writes the site to this folder, creating it. Required. |
| `--root <folder>` | the parent of the model folder when the model sits in a `.model` folder, else the model folder | Names the consumer root that source links open. |
| `--editor <template>` | `vscode://file/{root}/{path}:{line}` | Builds source links from this template with the `{root}`, `{path}`, and `{line}` placeholders; `none` renders no source links. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The site was written. |
| `1` | The model file is missing or unreadable, the output folder cannot be written, or a carried LiteSpec page the lexicon needs is missing or lost its table shape; the message names the file. |
| `2` | The arguments are wrong. |

## Examples

Generate the book for a consumer whose model is built:

```bash
litespec book --model ../consumer/.model/model.json --out ../consumer/.book
```

Name the consumer root and the editor that source links open:

```bash
litespec book --model ../consumer/.model/model.json --out ../consumer/.book --root ../consumer --editor "vscode://file/{root}/{path}:{line}"
```

Render the book with no source links:

```bash
litespec book --model ../consumer/.model/model.json --out ../consumer/.book --editor none
```

The review queue and the findings come from the model build recorded in the model:

```bash
litespec model build --root ../consumer
litespec book --model ../consumer/.model/model.json --out ../consumer/.book
```

## Underneath

The command writes `index.html`, the `assets` folder with `book.js`, `book.css`, `book-extra.css`, and `book-fonts.css`, and the `data` folder with the shards: `manifest.js` with the project, the roots, the editor template, the test run, the totals, the shard map, and the unplaced ids; `lexicon.js` with the type, layer, link, field, glossary, section, and finding words parsed from the carried release; one `outline-<layer>-<n>.js` per record layer, sequential past the split budget; `records-<nnn>.js` packs in identifier order; `search-<n>.js` packs loaded on first search; and `queue.js` with the review batches, the ballots, and the security items. Each shard is a script calling `BookData.put`, so the site opens as files. Nothing beneath this command is separately runnable.
