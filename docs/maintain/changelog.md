# Generate Provision Changelog

## Name

`litespec-maintain changelog` writes the provision changes since a git ref into the changelog.

## Synopsis

```bash
litespec-maintain changelog --from <ref> [--root <path>] [--check] [--help]
```

## Description

This command compares the provision index at a git ref with the current one, so a reader of an earlier release sees which provision IDs were added, removed, or renamed, and which extensions arrived or left. It writes those lists into the marked provision-diff section of `CHANGELOG.md`. An ID counts as renamed when one removed ID and one added ID share the same page and the same statement.

Run it after `litespec-maintain provisions`, as the last step before tagging a release. The next run overwrites the marked section, so never edit that section directly.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--from <ref>` | none, required | Selects the git tag or commit to compare with. |
| `--root <path>` | the LiteSpec checkout | Selects the LiteSpec repository to read. |
| `--check` | off | Compares the marked section with the computed lists and writes nothing. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The section was written, or `--check` found it current. |
| `1` | `--check` found the section stale. |
| `2` | The usage is invalid, or the ref, the index, or the changelog is unreadable. |

## Examples

Write the changes since release 3.0.0:

```bash
litespec-maintain changelog --from v3.0.0
```

Compare without writing:

```bash
litespec-maintain changelog --from v3.0.0 --check
```

## Underneath

The command reads the old copies with `git show <ref>:docs/reference/provisions.json` and `git show <ref>:litespec.manifest.yaml`, run from the repository root. The comparison and the section rewrite have no separate entry point. They run only as part of this command.
