# Validate Standards

## Name

`litespec-maintain validate` checks the LiteSpec repository against its own authoring contract.

## Synopsis

```bash
litespec-maintain validate [--root <path>] [--format json] [--help]
```

## Description

This command gates the repository: every page, template, and identifier under `docs/` passes through it. It checks page contracts and section order, atomic Standards and Convention blocks, provision identity, inline verification lines, pattern pages and their wireframes, normative-language placement, strength and wording agreement, heading capitalization, and ASCII prose. Internal links, manifest paths, extension declarations, and the generated tables are checked too.

Citations stay inside what each file may name. A page may cite only a finding code the finding code table holds, a template under `templates/`, and a record of the Claimwick example; a `Checked by` line naming `litespec model check` must name a finding code. A template under `templates/` may cite only a provision the index holds. A required Statement carries the capitalized obligation word or its negation, while an advisory Statement carries neither, and a provision with Strength default holds a Default line. A pattern offers at most three options. A page under `docs/` counts as indexed when following links from `docs/README.md` reaches it. Generated pages are exempt, as is a record's explanation file beside its record, which the record reaches.

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
|:--|:--|
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

One part is separately runnable: the generated-file comparison. It reports a stale provision index, provision data, finding code table, record folder table, or manifest pin table, the condition this command reports as `PROVISIONS_STALE`:

```bash
litespec-maintain provisions --check
```

The remaining checks have no separate entry point; they run only as part of this command.
