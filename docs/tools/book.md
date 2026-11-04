# Book Command

## Name

`litespec book` - Generate the project book, one offline shell with sharded data, from a consumer's `model.json`.

## Synopsis

```bash
litespec book --model <model.json> --out <folder> [--root <folder>] [--editor <template>]
```

## Description

The book is the model made readable: every record with its completion, its proof and its links, in one static site that opens without a server. Each record page leads with computed completion: whether the record has code, whether it has tests, and how many of its scenarios pass. The navigation offers two views: the Outline, which follows the record layers, and the Security view, which lists cross-tenant access, outside calls and personal or sensitive data.

The book reads the model only. It computes nothing the model does not state, and every word it shows about a record, a link or a proof state comes from the carried release or the record itself.

## Arguments

None.

## Options

| Option | Meaning |
|:--|:--|
| `--model <file>` | The `model.json` written by the model builder. Defaults to `.litespec/model/model.json` under the consumer root. |
| `--out <folder>` | The folder to write the static site to. Defaults to `.litespec/book` under the consumer root. |
| `--root <folder>` | The consumer root for source links. Defaults to the repository root when the model sits in a `.litespec/model` folder, else the model's folder. |
| `--editor <template>` | The editor link template with `{root}`, `{path}` and `{line}`, or `none` to suppress links. Defaults to `vscode://file/{root}/{path}:{line}`. |
| `--help` | Print the usage text and exit. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| 0 | The book is written. |
| 1 | The input is unreadable or the output is unwritable. |
| 2 | A usage error, a missing carried release, or a missing or broken lexicon page. |

## Examples

Write the book for the default model and output:

```bash
litespec book
```

Write the book for one model to one folder:

```bash
litespec book --model .litespec/model/model.json --out .litespec/book
```

Write the book with source links opening in another editor:

```bash
litespec book --editor "idea://open?file={root}/{path}&line={line}"
```

## Underneath

The command builds one index over the model, then the outline and one detail object per record, then packs them into shards under `data/`. The shell is one `index.html` with one script and one stylesheet under `assets/`. Shards arrive on demand: the manifest and the lexicon first, then the outline and records of the layers the reader opens.

The shards are:

- `manifest.js`: the project, roots, editor template, test run, totals, layers with their outline shards, the shard map, the search shards, the unplaced ids and the generation date.
- `lexicon.js`: LiteSpec's own words: type labels, layer writers, link phrases, sections, glossary, field labels and finding meanings.
- `outline-<layer>-<n>.js`: the outline nodes of one layer, split past the outline budget.
- `records-<n>.js`: the record details, fifty records per shard.
- `search-<n>.js`: the search rows: id, title, type and excerpt each.
- `security.js`: the security items: cross-tenant access, outside calls and personal or sensitive data.

Records that no parent rule places are unplaced: the manifest lists them, and the console line names them with the failed `record-parent-missing` rule.
