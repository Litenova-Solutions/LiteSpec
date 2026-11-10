# Generate Provisions

## Name

`litespec-maintain provisions` writes the provision index and its data file, resolving every identifier to its page, record layer, trace, and checked share.

## Synopsis

```bash
litespec-maintain provisions [--root <path>] [--check] [--help]
```

## Description

This command rebuilds `docs/reference/provisions.md` and `docs/reference/provisions.json` from the active standards, so a citation reaches its heading in one step. The page and the data file carry each provision with its scope, record layer, trace, strength, and the mark and evidence from its inline verification line, with the layer and trace right after the artifact. The page also states the checked share of rules for each page: the provisions at strength `rule` that carry a `Checked by` line, as a share of the provisions at strength `rule` on that page. Each provision takes the layer its page gives, or the layer its Applies to line states when that layer differs, and its trace is `yes` only where its Applies to line states it, as the [Provision Scopes](../core/scopes.md) page states.

Run it after adding, renaming, or removing a provision. The next run overwrites a hand edit, so never edit the page or the data file directly.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the LiteSpec checkout | Selects the LiteSpec repository to read. |
| `--check` | off | Compares the page and the data file with the active standards and writes nothing. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The page and the data file were written, or `--check` found them current. |
| `1` | `--check` found the page or the data file stale. |
| `2` | The usage is invalid. |

## Examples

Rebuild the page and the data file after a provision change:

```bash
litespec-maintain provisions
```

Compare without writing, which is what continuous integration runs:

```bash
litespec-maintain provisions --check
```

## Underneath

`litespec-maintain validate` reads the same builder and reports a stale page or data file as `PROVISIONS_STALE`. Nothing beneath this command is separately runnable.
