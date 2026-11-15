# Book Command

## Name

`litespec book` - Serve the project book live from memory on localhost, or write it as a static site with `--out`.

## Synopsis

```bash
litespec book [--root <folder>] [--port <port>] [--links <template>]
litespec book --out <folder> [--root <folder>] [--links <template>]
```

## Description

The book is the model made readable: every record with its completion, its proof and its links. The navigation offers two views: the Outline, which follows the record layers, and the Security view, which lists cross-tenant access, outside calls and personal or sensitive data.

The book is a desktop explorer with a minimum content width of 1024 CSS pixels and no narrow-screen layouts. Its look follows IBM Carbon in IBM Plex type: a dark top bar, the outline on a gray panel, the record page on the white page, and the rail beside it. It follows the reader's light or dark color scheme and keeps no theme toggle. Corners are square, tables have a filled header, and one blue accent marks links and the current item.

Every record type has a short code, such as F for a feature and U for a use case, shown as a badge colored by its record layer wherever a record appears. Hovering or focusing a badge names the type and explains it from the model page, and the About this book page lists every code.

The rail beside each record page holds the record's Trace, its Completion and its On this page list. Trace lists only the records in another record layer that the record serves, reaches or is proven by; a record in the same layer is one step away in the outline. Completion states whether the record has code, whether it has tests, and how many of its scenarios pass. Facts that are the same on every page, such as the build time, the test run and the record counts per type, sit on the About this book page.

The book shows records, not teaching. Explanatory helper text that teaches LiteSpec leaves the pages; record content, labels, counts, and states stay. A term's or a state's definition is available on hover and focus, not printed beside every use.

Three views draw the UI blueprint. The screen page draws the screen's wireframe from its explanation file. Each entry point page draws its navigation map from `leadsTo`: the entry point's screens as places and the connections between them. Each flow page draws its storyboard: the flow's steps' screens in step order. A flow's branches show inside its steps: each branch sits in the step it leaves from with its condition and the step it continues at, and that step names where else it is reached from.

Without `--out` the command serves the book live. It builds the model in memory, listens on localhost, and prints `Serving the book at http://localhost:<port>/`. The server answers only the book's own files by their exact names, with no directory listing and no path read from disk. It watches `.litespec/`, plus the code roots and test results when the project claims Implementation or Proof, and rebuilds in the background after a pause in edits. A rebuild that fails keeps serving the last good book and shows the error in the book. The open page follows a change notification, reloads the manifest and the open record, and shows the build time.

With `--out <folder>` the command writes the static book to the folder and exits. The written book opens from `file://` and from any static host at any base path. The export manifest carries the LiteSpec release and the build time, and it carries no local paths.

Source links come from `--links <template>`, with the placeholders `{root}`, `{path}`, `{line}` and `{commit}`. The command fills `{root}` with the consumer root and `{commit}` with the checked-out commit, or `HEAD` when the folder is no git checkout; the reader fills `{path}` and `{line}` for each source. Live defaults to `vscode://file/{root}/{path}:{line}`. Export defaults to `none`, which suppresses source links. A template names `{path}` and `{line}`, or the run fails as a usage error.

A hosted book shows the Security view with its cross-tenant access, outside calls and personal or sensitive data, the file paths of the records, code and tests, and the test runs. Host it only where every reader may see all three.

The book follows the record layers the project claims in `layers` in `.litespec/project.yaml`. A layer the project does not claim shows as not claimed, never as missing. A project that claims only Intent and Behavior sees the Intent, Behavior and governance records, with no implementing code, no tests, no proof state and no test run. Claiming Implementation adds the code and its marks, and claiming Proof adds the test runs and proof status.

At every level the book shows Intent coverage first on the Intent part: a computed status that states, for each requirement and each flow step, whether some Behavior record reaches it, and the share reached. A requirement is reached when a Behavior record names it in `fulfills`. A flow step is reached when a Behavior record names it in `serves`. The manifest carries the counts, the share and one entry per requirement and flow step with the records that reach it.

The book builds the model in memory from the consumer root, then renders it. It computes nothing the model does not state, and every word it shows about a record, a link or a proof state comes from the carried release or the record itself.

## Arguments

None.

## Options

| Option | Meaning |
|:--|:--|
| `--root <folder>` | The folder to start from. Defaults to the working directory. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--out <folder>` | Write the static book to this folder and exit. Without it the book is served live on localhost. The option has no default folder. |
| `--port <port>` | The localhost port the live book listens on, from 1 to 65535. Defaults to 7420. Live only: the option is refused with `--out`. |
| `--links <template>` | The source link template with `{root}`, `{path}`, `{line}` and `{commit}`, or `none` to suppress links. Live defaults to `vscode://file/{root}/{path}:{line}`; export defaults to `none`. |
| `--help` | Print the usage text and exit. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| 0 | The book is written, or the live server stopped. |
| 1 | The records cannot be read, the output cannot be written, or the port cannot be opened. |
| 2 | A usage error, the root or the project file is missing or invalid, or the carried release or a lexicon page cannot be read. |

## Examples

Serve the book live for the consumer in the working directory:

```bash
litespec book
```

Serve it on another port:

```bash
litespec book --port 8080
```

Write the static book for one consumer to one folder:

```bash
litespec book --root ../consumer --out ../book
```

Write the static book with source links into the repository:

```bash
litespec book --out ../book --links "https://example.com/repo/blob/{commit}/{path}#L{line}"
```

Serve the live book with source links opening in another editor:

```bash
litespec book --links "idea://open?file={root}/{path}&line={line}"
```

## Underneath

The command builds the model in memory, computes the Intent coverage from the built model, then builds one index over the model, then the outline and one detail object per record for the claimed layers, then packs them into shards under `data/`. The shell is one `index.html` with one script and one stylesheet under `assets/`. Live and export share one data format with the same file names: the server holds the shards in memory and adds one change notification, and the export writes them to disk. Shards arrive on demand: the manifest and the lexicon first, then the outline and records of the layers the reader opens.

The shards are:

- `manifest.js`: the project, the source links, the LiteSpec release, the build time, the claimed layers, Intent coverage, the test run, totals, the layers with their outline shards, the shard map, the search shards and the unplaced ids. The Intent coverage holds `reached`, `total` and `share`, and the `requirements` and `steps` counts, with no entries. When Proof is not claimed the test run is null.
- `lexicon.js`: LiteSpec's own words: type labels, layer writers, link phrases, sections, glossary, field labels and finding meanings.
- `outline-<layer>-<n>.js`: the outline nodes of one layer, split past the outline budget.
- `coverage.js`: the Intent coverage entries, one per requirement and flow step, with its `id`, `kind`, `reached` flag and `reachedBy` records.
- `records-<n>.js`: the record details, packed to about 256 KB per shard and never past 512 KB. A detail carries its sections and its fields, and no whole-page HTML.
- `search-<n>.js`: the search rows: id, title, type and excerpt each.
- `security.js`: the security items: cross-tenant access, outside calls and personal or sensitive data.

Records that no parent rule places are unplaced: the manifest lists them, and the export names them on the console with the failed `record-parent-missing` finding.
