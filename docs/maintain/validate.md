# Validate Standards

## Name

`litespec-maintain validate` checks the LiteSpec repository against its own authoring contract.

## Synopsis

```bash
litespec-maintain validate [--root <path>] [--format json] [--help]
```

## Description

This command is the repository gate for every page, template, and identifier under `docs/`. It checks page contracts and section order, atomic Standards and Convention blocks, provision identity, inline verification lines, pattern pages and their wireframes, normative-language placement, heading capitalization, and ASCII prose. It also checks internal links, manifest paths, extension declarations, and the generated tables. A page may cite only a finding code the finding code table holds, a template under `templates/`, and a record of the Claimwick example, and a `Checked by` line that names `litespec model check` must name a finding code. A pattern offers at most three options. A page under `docs/` is indexed when a link from `docs/README.md`, followed through links, reaches it; a generated page is exempt, as is a record's explanation file beside its record, since the record reaches it.

Run it after changing any page, any template, or any authoring rule.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the LiteSpec checkout | Selects the LiteSpec repository to read. |
| `--format json` | off | Writes one JSON object on standard output instead of human-readable lines, with `tool`, `root`, `ok`, and an `errors` array whose entries carry `file`, `line`, `code`, and `message`. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | Every authoring check passed. |
| `1` | At least one check reported a defect. |
| `2` | The usage is invalid, or an input file is unreadable. |

## Examples

Validate the repository from its root:

```bash
litespec-maintain validate
```

Validate another checkout and write the result as JSON:

```bash
litespec-maintain validate --root ../standards --format json
```

## Underneath

The generated-file comparison is separately runnable. It reports a stale provision index, provision data, finding code table, record folder table, or manifest pin table, the condition this command reports as `PROVISIONS_STALE`:

```bash
litespec-maintain provisions --check
```

The remaining checks have no separate entry point. They run only as part of this command.
