# Docs Review and Render

## Name

`litespec docs review` runs the prose lints on the Markdown and record files changed since a revision and writes a review packet an independent reviewer answers. `litespec docs render --changed` re-renders the book pages of only the records whose content hash changed since the last render.

## Synopsis

```bash
litespec docs review --since <ref> [--root <folder>]
litespec docs render --changed [--root <folder>]
```

## Description

The model check proves what the records state, but it never judges how the words read. The docs commands cover that gap without calling the model or the network: `review` lints the changed prose and hands an independent reviewer a packet, and `render` keeps the rendered pages in step with the records that changed.

`review` reads the files changed since the git revision: tracked files differing between the revision and the working tree, plus untracked files. It keeps only the prose files (`.md`, `.yaml`, `.yml`) and runs four lints on each:

- Paragraph length: a Markdown paragraph holds at most four sentences; a record value holds at most 240 characters. Longer prose is split into shorter sentences.
- LiteSpec-ese: stilted phrases the Style Card replaces with plain words, such as `gains a home`, `unlock the power of`, and `facilitation of`.
- Promotional language: praise instead of observable behavior, such as `cutting-edge`, `robust`, and `powerful`.
- Raw refusal codes: a refusal code such as `spending.receipt-missing` shown without backticks in product copy. Codes wrapped in backticks pass.

The command always writes a review packet to `.litespec/review/docs-<timestamp>.md`, even when no prose files changed. The packet holds each changed file's diff, the eight Style Card rules, the checklist the independent reviewer answers (plain voice, no word shuffling, normative strength kept, cold-reader clarity), and every lint finding with its file, line, and code. Each finding also prints to standard output. The packet names the base revision and the generation time, so a later reader sees what the reviewer saw.

`render --changed` scans every `.yaml` and `.yml` record under the root, except version control, dependencies, build output, and the review outputs themselves. It hashes each record, compares the hash with `.litespec/.cache/render-hashes.json`, and writes one rendered page under `.litespec/render` for each record whose hash is new or changed, mirroring the record's relative path with a Markdown extension. Records whose hash matches the cache are left alone, and records that no longer exist lose their cache entry and their rendered page. The summary line reports how many pages rendered and how many stayed unchanged. Both subcommands take their scope from options.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <folder>` | the working directory | The repository to read and to write the packet under. |
| `--since <ref>` | none, required by `review` | The git revision to diff the working tree against, such as `HEAD~1`. |
| `--changed` | off, required by `render` | Render only the records whose content hash changed since the last render. |
| `--help` | off | Print the usage text and exit 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | `review` found nothing, or `render` finished. |
| `1` | `review` reports a lint finding, or `render` cannot read a record or write a page. |
| `2` | The arguments are wrong, the root is missing, or the changes cannot be read. |

## Examples

Run the prose lints over the change set and write the review packet:

```bash
litespec docs review --since main
```

Re-render the pages of only the records whose content hash changed:

```bash
litespec docs render --changed --root ../consumer
```

## Underneath

`review` reads the change set the same way `rules --since` does: the tracked files differing between the revision and the working tree with the untracked files, minus the review outputs themselves. It keeps only the prose files and runs the four lints in memory before writing the packet. `render --changed` hashes every record, compares each hash with the cache, and writes one page per changed record. Nothing beneath this command is separately runnable.
