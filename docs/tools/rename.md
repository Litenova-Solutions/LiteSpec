# Rename Record

## Name

`litespec rename` renames a record identifier and rewrites every link.

## Synopsis

```bash
litespec rename <old> <new> [--root <path>] [--help]
```

## Description

This command renames one record identifier across the consumer repository. A named record moves with its folder: the record folder moves to the place the new identifier owns, and the folder takes the new name, so the folder name always equals the record file stem. The record file and its explanation file move inside that folder. A requirement's folder sits inside its feature's folder, so renaming a feature moves its folder with every requirement folder inside it. The command updates the record fields and rewrites every citation of the old identifier in record files, explanation files, and documents, so no link points at the old name after the rename. Record files are written back in canonical form. A use case under an aggregate keeps its aggregate's folder and takes the new name. The repository stays resolvable at every commit.

Both arguments are full record identifiers, such as `use-case/lending.borrow-copy`. The command refuses a new identifier a record already carries and an old identifier no record carries.

Run it for every identifier rename, as `litespec/core/model#rewrite-every-link-on-a-rename` states. Never rename by moving the file by hand.

## Arguments

| Argument | Meaning |
|:---|:---|
| `<old>` | The full identifier the record carries today. |
| `<new>` | The full identifier the record takes. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The identifier was renamed and every link rewritten. |
| `1` | The records cannot be read, or the run failed after the checks passed. |
| `2` | The root or the project file is missing or invalid, the old identifier names no record, the new identifier is taken, or an option is unknown. |

## Examples

Rename a use case and rewrite every citation together:

```bash
litespec rename use-case/lending.borrow-copy use-case/lending.lend-copy
```

## Underneath

Nothing beneath this command is separately runnable.
