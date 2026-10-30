# Book

## Name

`litespec book` generates the project book: a static website built from a consumer's `model.json` that presents the project model as linked pages for reviewers.

## Synopsis

```bash
litespec book --model <model.json> --out <folder>
```

## Description

The book generator reads the `model.json` file that `litespec model build` wrote and writes a self-contained static site from it. The site holds one page per record, one index per record type, one chapter per journey, a ballots page, a goals index, a review page, a glossary, and a search page. Every count on every page is computed from `model.json` when the site is generated; no count is typed by hand. The check findings on the review page come from the model too: `litespec model build` writes its check result into `model.json` as `findings` and `known`, so the book needs only the model. When the model holds no test run, every proof statement in the book reads as not run and names the `paths.testResults` setting that reads one, and a journey step the model records as exercised reads as walked by a passing test.

The book renders each record's prose body, and every field has a typed renderer that writes words and tables, so raw JSON never reaches a reviewer. Field tables render as tables, scenarios group under their result, and field names never render literally in Intent chapters.

Each journey chapter holds the goal, the `finishedWhen`, the steps as sentences with their `ifItGoesWrong`, the requirements it fulfils with their statements and their `except` and fallback inline, and the open questions. For each step it names the serving use case's goal in one line, or reads as served by automation with a link to the serving reaction or job, and says whether a passing test walked the step; the Then clauses of its scenarios follow as context. A consequences box states the new refusals in product words, the data kept and for how long, and the money effects, marked as context and outside the approval hash.

The ballots page lists the open questions grouped by `ballot`, ordered tier 1 first, with each question's recommendation and auto-approval date.

An aggregate page shows its lifecycle as a generated state diagram with a From, To, Triggered By table of its transitions, and marks each state past the start that no transition reaches. Its data table names each field with its meaning and its handling in words. A use case page lists its results and scenarios as Given, When, Then sentences with their example tables and each scenario's last proof date.

Reviewer pages show no identifiers, codes, file paths, or check names: record links show titles, and refusals and checks read as words. A Developer toggle in the header reveals the identifiers, codes, file paths, and check names, with a Developer section on each record page. A Review toggle shows the review notes that touch each page: its draft state, its known gaps, and its findings. The review page always shows everything, with or without the toggle: every known gap with owner and end date, expired gaps first; every draft and every reopened record grouped into the review batches `litespec model review` computes, in the same order, each batch with the command that approves it; every open question; and every finding.

Search covers titles with statements, meanings, and scenario text. The site needs no server: open the output folder's `index.html` in a browser.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--model <model.json>` | none | Reads this model file. Required. |
| `--out <folder>` | none | Writes the static site to this folder, creating it. Required. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The site was written. |
| `1` | The model file is missing or unreadable, or the output folder cannot be written. |
| `2` | The arguments are wrong. |

## Examples

Generate the book for a consumer whose model is built:

```bash
litespec book --model ../consumer/.model/model.json --out ../consumer/.book
```

The review page shows the check findings the model build recorded in the model:

```bash
litespec model build --root ../consumer
litespec book --model ../consumer/.model/model.json --out ../consumer/.book
```

## Underneath

The command copies its stylesheet, its script, and the vendored Mermaid script with its version file into the output folder's `assets` beside the pages it writes. It writes `search.json`, an index of record titles with statements, meanings, and scenario text, and embeds that same index in the search page, so search works when the site is opened as files. Nothing beneath this command is separately runnable.
