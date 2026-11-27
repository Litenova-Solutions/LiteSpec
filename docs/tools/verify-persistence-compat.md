# Verify Persistence Compat

## Name

`litespec verify persistence-compat` checks that every stored persistence snapshot still reads through the current serializer.

## Synopsis

```bash
litespec verify persistence-compat [--root <consumer>] [--record --release <name>]
```

## Description

The command runs the persistence harness the project file names in `verify.persistence.command` and reports the stored snapshots that no longer read. Snapshots live at `.litespec/proof/persistence-snapshots/<release>/<name>.json`, one folder per release. Without `--record` the command checks every release folder; with `--record --release <name>` it records the snapshots of one release, creating the folder first.

The command runs the harness from the consumer root through the platform shell with `record <folder>` or `check <folder>`, once per release folder on a check. The harness prints one JSON object per line with the snapshot file, whether it reads, and why not: `{"file": "<relative snapshot path>", "status": "ok" | "failed", "message": "<reason>"}`.

A failed line is a `snapshot-read-failed` finding, and a release folder with no snapshot is a `snapshot-missing` finding. A project file that states no `verify.persistence.command` is a usage error, because there is no harness to run.

In Claimwick, the team records the v1 snapshots, then changes how the expense stores its amount. The next check prints a failed line for the v1 expense snapshot, and the command reports it as incompatible, so the team adds the converter that reads the earlier shape before the release.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--record` | off | Records the snapshots of one release instead of checking them. Needs `--release`. |
| `--release <name>` | none | The release folder the record writes, under `.litespec/proof/persistence-snapshots/`. Needs `--record`. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | Every snapshot reads. |
| `1` | A snapshot is incompatible, a release folder holds no snapshot, the harness cannot be checked, or the report cannot be written. |
| `2` | The arguments are wrong, the root holds no project file, or the project file names no persistence harness. |

## Examples

Check every stored release:

```bash
litespec verify persistence-compat --root ../consumer
```

Record the snapshots of the next release:

```bash
litespec verify persistence-compat --root ../consumer --record --release v2
```

## Underneath

The command runs the harness once per release folder from the consumer root and parses every JSON line it prints. It prints each finding in the model check line format, writes the findings as a JSON report to `.litespec/verify/persistence-compat.json`, and exits 0 when it raised no finding, 1 when it raised one, and 2 on a usage error. Nothing beneath this command is separately runnable.
