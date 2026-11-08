# Book Command

## Name

`litespec book` - Generate the project book, one offline shell with sharded data, from a consumer root.

## Synopsis

```bash
litespec book [--root <folder>] [--out <folder>] [--editor <template>]
```

## Description

The book is the model made readable: every record with its completion, its proof and its links, in one static site that opens without a server. Each record page leads with computed completion: whether the record has code, whether it has tests, and how many of its scenarios pass. The navigation offers two views: the Outline, which follows the record layers, and the Security view, which lists cross-tenant access, outside calls and personal or sensitive data.

The book follows the conformance level the project claims in `.litespec/project.json`. At `records` it shows the Intent, Behavior and governance records and leaves out code, proof and level S findings: no implementing code, no tests, no proof state and no test run. At `traced` it adds code and proof status. At `full` it shows everything.

At every level the book shows Intent coverage first on the Intent part: a computed status that states, for each requirement and each journey step, whether some Behavior record reaches it, and the share reached. A requirement is reached when a Behavior record names it in `fulfills`. A journey step is reached when a Behavior record names it in `serves`. The manifest carries the counts, the share and one entry per requirement and journey step with the records that reach it.

The book builds the model in memory from the consumer root, then renders it. It computes nothing the model does not state, and every word it shows about a record, a link or a proof state comes from the carried release or the record itself.

## Arguments

None.

## Options

| Option | Meaning |
|:--|:--|
| `--root <folder>` | The consumer root to read. Defaults to the consumer root found from the working directory. |
| `--out <folder>` | The folder to write the static site to. Defaults to `.litespec/book` under the consumer root. |
| `--editor <template>` | The editor link template with `{root}`, `{path}` and `{line}`, or `none` to suppress links. Defaults to `vscode://file/{root}/{path}:{line}`. |
| `--help` | Print the usage text and exit. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| 0 | The book is written. |
| 1 | The input is unreadable or the output is unwritable. |
| 2 | A usage error, a missing carried release, or a missing or broken lexicon page. |

## Examples

Write the book for the consumer in the working directory:

```bash
litespec book
```

Write the book for one consumer to one folder:

```bash
litespec book --root ../consumer --out ../book
```

Write the book with source links opening in another editor:

```bash
litespec book --editor "idea://open?file={root}/{path}&line={line}"
```

## Underneath

The command builds the model in memory, computes the Intent coverage from the built model, then builds one index over the model, then the outline and one detail object per record at the claimed conformance level, then packs them into shards under `data/`. The shell is one `index.html` with one script and one stylesheet under `assets/`. Shards arrive on demand: the manifest and the lexicon first, then the outline and records of the layers the reader opens.

The shards are:

- `manifest.js`: the project, roots, editor template, conformance level, Intent coverage, test run, totals, layers with their outline shards, the shard map, the search shards, the unplaced ids and the generation date. The Intent coverage holds `reached`, `total` and `share`, the `requirements` and `steps` counts, and one `entries` entry per requirement and journey step with its `id`, `kind`, `reached` flag and `reachedBy` records. At the `records` level the test run is null.
- `lexicon.js`: LiteSpec's own words: type labels, layer writers, link phrases, sections, glossary, field labels and finding meanings.
- `outline-<layer>-<n>.js`: the outline nodes of one layer, split past the outline budget.
- `records-<n>.js`: the record details, fifty records per shard.
- `search-<n>.js`: the search rows: id, title, type and excerpt each.
- `security.js`: the security items: cross-tenant access, outside calls and personal or sensitive data.

Records that no parent rule places are unplaced: the manifest lists them, and the console line names them with the failed `record-parent-missing` rule.
