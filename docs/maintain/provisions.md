# Generate Provisions

## Name

`litespec-maintain provisions` rebuilds the provision index and its data file from the active standards, resolving every identifier to its page, record layer, trace, and checked share. The same run writes the finding code table, the record folder table, and the manifest pin table.

## Synopsis

```bash
litespec-maintain provisions [--root <path>] [--check] [--help]
```

## Description

This command rebuilds the provision index from the active standards: `docs/reference/provisions.md` and `docs/reference/provisions.json`. A citation reaches its heading in one step because the index resolves it.

Both files carry each provision with its scope, layer, trace, strength, and the mark and evidence from its inline verification line; the layer and trace sit right after the artifact. For each page, the index states its checked share: the provisions at strength `required` that carry a `Checked by` line, as a share of the provisions at strength `required` on that page.

A provision takes its layer from its page, or from its Applies to line when that layer differs, and its trace is `yes` only where its Applies to line states it, as the [Provision Scopes](../core/scopes.md) page states.

The same run also writes the finding code table, the record folder table from the record folder map, and the pin table from the release manifest. Run it after adding, renaming, or removing a provision, a finding code, a record folder, or a pin. Never edit the page or the data file directly: the next run overwrites a hand edit.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the LiteSpec checkout | Selects the LiteSpec repository to read. |
| `--check` | off | Compares every generated file with the sources it is written from and writes nothing. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:--|:--|
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

The same builders feed `litespec-maintain validate`, which reports a stale generated file as `PROVISIONS_STALE`. Nothing beneath this command is separately runnable.
