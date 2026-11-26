# Generate

## Name

`litespec generate` writes the code the records fully determine, from the `generate` targets of the consumer declaration.

## Synopsis

```bash
litespec generate [--root <consumer>] [--check]
```

## Description

The command reads the built project model and writes one file set per `generate` target in `.litespec/project.yaml`. Each target names a `kind`, a `stack`, and the `path` folder the files go in:

- `refusal-codes` writes the refusal codes the records declare. For a .NET stack each module with refusals gets one C# file with a public static class holding one public const string per refusal, named from the refusal key in PascalCase with the code as its value and the refusal's meaning as its documentation. For a TypeScript stack the target writes one file with a const object of codes, their union type, and a map from each code to the English sentence its refusal results state.
- `permissions` writes the permission ids the same way: one C# constant class per module holding permissions, or one TypeScript file with a const object of ids and their union type.
- `event-decisions` writes, per module with domain events, one C# file with a static table holding each domain event the records declare with the outcome the outbox decision check derives from the records: `ignored` when the event record states `ignored: true`, `consumed` when a reaction or a translating event consumes it, and `missing` when the records state neither. A consumer's startup check compares its own decisions against this table instead of typed lists.
- `closed-sets` writes, per module or knowledge area holding closed value sets, one C# file with a static class holding one nested class per set, each with one const string per allowed value. A set is a reference-data table or a setting stating `choices`.

Every file opens with a header line saying `litespec generate` wrote it from the records and it must not be edited. Rendering is deterministic: modules, files, and entries order by identifier, so two runs write byte-identical files. A target with no entries writes no files.

For a .NET stack the `path` may hold `{module}` for the record module name or `{Module}` for the same name in PascalCase, and each module writes its file into its own resolved folder. Without a placeholder each module writes into `<path>/<Module>/`. The `namespace` names the C# namespace with the same placeholders. For a TypeScript stack the target writes its one file directly into `path`, which holds no placeholder.

With `--check` the command writes nothing, lists each file it would write or rewrite, and exits 1 when any would change.

When a record file does not parse, the command writes nothing, names each invalid file, and exits 2, so a generated file is never quietly short. When a target names an unsupported kind or stack combination, the command writes nothing and names each invalid target.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--check` | off | Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The files are written, or `--check` found every file current. |
| `1` | A file would change with `--check`, a target is invalid, the records cannot be read, or a file cannot be written. |
| `2` | The arguments are wrong, the root holds no project file, or a record file does not parse. |

## Examples

Write the code the records determine:

```bash
litespec generate --root ../consumer
```

List the files that would change, without writing:

```bash
litespec generate --root ../consumer --check
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses, builds the project model, and renders every configured target in memory. `litespec model check` renders the same plan and reports each missing or differing file as a `generated-drift` finding, so the check fails exactly when `generate` would write. Nothing beneath this command is separately runnable.
