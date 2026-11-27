# Scaffold

## Name

`litespec scaffold` writes the code files of one use case from its record, into the folders the .NET pack states.

## Synopsis

```bash
litespec scaffold <use-case-id> [--root <consumer>] [--diff]
```

## Description

The command reads the built project model and writes normal, single-file, non-partial C# files for the one named use case. Names follow the record id: `use-case/books.post-entry` becomes `PostEntryCommand` in `Application/{Aggregates}/PostEntry/`. An existing file is never overwritten: it is skipped and listed.

For the use case the command writes the command or query message, its handler, a validator for a command with inputs, the result, and one definition file each for authorization, audit, tenancy and, for a command, idempotency, filled from the record: the access grant, audit, tenant and idempotency fields. When a web entry point reaches the use case, it also writes the endpoint with its request and response models. A query with no output groups gets an empty result, and a command with no outputs gets a result carrying the aggregate identity.

The bodies are minimal: a command handler loads the aggregate, calls the act named after the use case verb, and stages it per [Application Layer](../packs/dotnet/application.md), and the command prints the aggregate act it expects. The files compile once that act exists.

Rendering is deterministic: files order by layer and concern, so two runs write byte-identical files. With `--diff` the command writes nothing and prints each file it would create with its full content, one `---` header line per file.

When a record file does not parse, the command writes nothing, names each invalid file, and exits 2, so a scaffolded file is never quietly short. When the use case id lists no use case, or the consumer declares no module project for the module, the command writes nothing and exits 1.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<use-case-id>` | yes | The use case to scaffold, with or without the `use-case/` prefix. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--diff` | off | Writes nothing, prints each file it would create with its full content, and exits 0. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The files are written, or `--diff` printed the files it would create. A second run skips every file and exits 0. |
| `1` | The use case id lists no use case, the consumer declares no module project for the module, or a file cannot be written. |
| `2` | The arguments are wrong, the root holds no project file, or a record file does not parse. |

## Examples

Scaffold one use case from its record:

```bash
litespec scaffold use-case/lending.borrow-copy --root ../consumer
```

Print the files a scaffold would create, without writing:

```bash
litespec scaffold use-case/lending.borrow-copy --root ../consumer --diff
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses, builds the project model, and renders the one use case in memory. Nothing beneath this command is separately runnable.
