# Upgrade Check

## Name

`litespec upgrade --check` reports the release changes between the reviewed LiteSpec version of the consumer and the carried release.

## Synopsis

```bash
litespec upgrade --check --root <consumer> [--format json]
```

## Description

The command reads the reviewed LiteSpec version the consumer records in `reviewedLiteSpecVersion` and the version of the carried release, then reads the embedded release manifests `migrations/<version>.yaml` for every release newer than the reviewed version up to the carried release.

It prints the changes that apply: the schema field renames with the record types that carry each field, the renamed or removed finding codes and analyzer IDs, the retired commands, the removed extensions, and the pack changes, in text and with `--format json`. With `--check` the command exits 1 when the project has unapplied changes, so a gate fails until the contract is reread and the reviewed version is recorded.

A project pinned to the carried release prints that no newer changes hold and exits 0. A project that reviews a version newer than the carried release prints the same, because no carried release has newer changes to report.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--check` | off, and required | Lists the release changes that apply, and exits 1 when any apply. |
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--format json` | text output | Prints one JSON object with the reviewed and carried versions and the applying migrations instead of the text. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | No changes apply. |
| `1` | Changes apply. |
| `2` | The options are wrong, the root holds no project file, or a version cannot be read. |

## Examples

List the release changes that apply to the consumer:

```bash
litespec upgrade --check --root ../consumer
```

List the changes as JSON for a gate:

```bash
litespec upgrade --check --root ../consumer --format json
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses, reads the reviewed version from `.litespec/project.yaml`, and compares it against the carried release version from the manifest beside the tool binary. The migration manifests ship as embedded tool resources, so the report works offline and always matches the carried release. Nothing beneath this command is separately runnable.
