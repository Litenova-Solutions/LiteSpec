# Rename Record

## Name

`litespec rename` renames a record identifier and rewrites every link.

## Synopsis

```bash
litespec rename <old> <new> [--move] [--root <path>] [--help]
```

## Description

This command renames one record identifier across the consumer repository. A plain rename keeps the record at its level and in its owner's folder. A named record's own folder takes the new name, so the folder name always equals the record file stem, and the record file and its explanation file take that stem inside it. The folder moves whole, so every supporting file the record folder holds moves with it. A use case under an aggregate stays in that aggregate's `use-cases/` folder, an aggregate stays in its module's `aggregates/` folder with its use cases, and a requirement stays in its feature's `requirements/` folder. A feature folder moves whole with its requirement folders, and every requirement identifier of the feature takes the new feature name.

A new identifier that names another owner, such as a use case under a different module or a requirement under a different feature, is refused without `--move`. With `--move` the record moves to the folder that its new owner holds. An aggregate moves with the use cases in its folder. A use case moves alone and takes its new owner's `use-cases/` folder, so it leaves its old aggregate behind.

The command checks every destination before it moves anything, and it refuses a folder or file that already exists. A move that fails part way is reported with its error, and the command does not roll it back. The command updates the record fields and rewrites every citation of the old identifier in record files, explanation files, and documents, so no link points at the old name after the rename. Record files are written back in canonical form. The repository stays resolvable at every commit when the rename completes.

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
| `--move` | off | Accepts a new identifier that names another owner, and moves the record into the folder that owner holds. Without it, a rename that changes the owner is refused. |
| `--root <path>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The identifier was renamed and every link rewritten. |
| `1` | The records cannot be read, or a move was refused because a destination exists or could not be made. |
| `2` | The root or the project file is missing or invalid, the old identifier names no record, the new identifier is taken or names another owner without `--move`, or an option is unknown. |

## Examples

Rename a use case and rewrite every citation together:

```bash
litespec rename use-case/lending.borrow-copy use-case/lending.lend-copy
```

Move a use case to the module that now owns it:

```bash
litespec rename use-case/lending.borrow-copy use-case/billing.borrow-copy --move
```

## Underneath

Nothing beneath this command is separately runnable.
