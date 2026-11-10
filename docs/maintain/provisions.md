# Generate Provisions

## Name

`litespec-maintain provisions` writes the provision index and its data file, resolving every identifier to its page, record layer, trace, and checked share, and writes the finding code table, the record folder table, and the manifest pin table.

## Synopsis

```bash
litespec-maintain provisions [--root <path>] [--check] [--help]
```

## Description

This command rebuilds `docs/reference/provisions.md` and `docs/reference/provisions.json` from the active standards, so a citation reaches its heading in one step. The page and the data file carry each provision with its scope, layer, trace, strength, and the mark and evidence from its inline verification line, with the layer and trace right after the artifact. The page also states the checked share of required provisions for each page: the provisions at strength `required` that carry a `Checked by` line, as a share of the provisions at strength `required` on that page. Each provision takes the layer its page gives, or the layer its Applies to line states when that layer differs, and its trace is `yes` only where its Applies to line states it, as the [Provision Scopes](../core/scopes.md) page states.

The finding code table comes from the finding code table the tool reads, the record folder table from the record folder map, and the pin table from the release manifest. Run it after adding, renaming, or removing a provision, a finding code, a record folder, or a pin. The next run overwrites a hand edit, so never edit the page or the data file directly.

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

`litespec-maintain validate` reads the same builders and reports a stale generated file as `PROVISIONS_STALE`. Nothing beneath this command is separately runnable.
