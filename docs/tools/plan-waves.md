# Plan Waves

## Name

`litespec plan-waves` prints the conflict-free parallel slices of the consumer, one per aggregate use-case set, with the waves in dependency order.

## Synopsis

```bash
litespec plan-waves [--root <consumer>] [--format json]
```

## Description

The command reads the module and aggregate graph and prints one slice per aggregate use-case set: the use cases, reactions, and jobs that one aggregate owns. A use case that sits directly under a module, outside every aggregate, prints in a module-level slice instead.

Each slice lists its exclusive path globs: the record folder of the aggregate, its `Application`, `WebApi`, and `Infrastructure` folders in the module project, and its module folder in each declared frontend. No glob prints on two slices, so two slices never write the same file.

The waves hold the slices in dependency order: a slice prints on the first wave after every slice it depends on. A slice depends on another when its records cite it: a use case that reads, writes, runs, or is triggered by a record of another aggregate, an input that identifies another aggregate, or a module that depends on another module. The slices of one wave share no dependency, so they run beside each other.

Shared paths print once as frozen: the Kernel project, the Platform project, and the Contracts projects that exist. Every wave leaves them alone.

With `--format json`, the command prints one JSON object with the slices, the waves, and the frozen paths, for callers that schedule the waves instead of reading the text.

When a record file does not parse, the command prints nothing, names each invalid file, and exits 1, so a printed slice is never quietly short.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--format json` | text output | Prints one JSON object with the slices, waves, and frozen paths instead of the text. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The slices are printed. |
| `1` | A record file does not parse, so no slice is printed. |
| `2` | The arguments are wrong, the root holds no project file, or the carried release cannot be read. |

## Examples

Print the parallel slices and their waves:

```bash
litespec plan-waves --root ../consumer
```

Print the slices as JSON for a scheduler:

```bash
litespec plan-waves --root ../consumer --format json
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses and builds the project model. It groups the use cases, reactions, and jobs by the aggregate folder their files sit in, orders the slices by the references their records cite, and renders the path globs from the module roots and frontend declarations. Nothing beneath this command is separately runnable.
