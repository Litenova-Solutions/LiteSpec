# LiteSpec.Cli

## Audience

Developers who build an application to the [LiteSpec](https://litespec.dev) standard, and who run its `litespec` command line in the project. The package carries the LiteSpec release it checks against, so the project needs no copy of the standard.

## What It Is

The `litespec` command line for applications that follow LiteSpec, the specification standard for one bounded-context business application built by people and agents. It scaffolds records, checks them against the code, tests and screens, and generates the project book.

The tool installs as a native executable for the machine: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64` or `osx-arm64`. Any other platform gets a framework-dependent build that runs on the .NET 10 runtime. Installing needs the .NET 10 SDK; running the native executable needs no .NET runtime.

Read [Adopting LiteSpec](https://litespec.dev/6.0.0/learn/adopting-litespec/), [the command reference](https://litespec.dev/6.0.0/tools/) and [the source and issues](https://github.com/Litenova-Solutions/LiteSpec).

## Quick Start

Install the tool in the project, at the release the project follows:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --local --version 6.0.0
```

A teammate or a build restores it with `dotnet tool restore`. A machine without .NET installs the same executable from the [GitHub release](https://github.com/Litenova-Solutions/LiteSpec/releases) with the install script; [installing the CLI](https://litespec.dev/6.0.0/tools/#install-the-cli) lists every method.

Run each command from the project root, the folder that holds `.litespec/project.yaml`:

```bash
dotnet litespec init --preset dotnet-svelte --size small
dotnet litespec model check
```

## Layout

| Command | What it does |
|:---|:---|
| `init` | Writes `.litespec/project.yaml`, the record scaffold, the tool manifest, and the `agent-protocol` generated region of `AGENTS.md`, inserted into an existing `AGENTS.md` that holds none; `--skill` installs the litespec skill for the named agents and `--skill-global` writes the copies under the user's home. |
| `model` | Builds the project model from the records, the code, the screens and the tests. `check` reports where they disagree with the project file, the record pages, the glossary, and the controlled UI in the same run; `format` rewrites record files into canonical form; `render` writes the generated page regions and the records file each web frontend imports; `query` prints one record with its links. |
| `book` | Generates a static site of the records for reviewers. |
| `new` | Scaffolds a record page and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning page. |
| `rename` | Renames a record identifier and rewrites every link. |
| `rules` | Lists the provisions that govern a path or record identifier, grouped by strength. |
| `skill` | Installs or removes the litespec skill. |

Every command accepts `--help`, and the checks accept `--format json`.

## Limits

The tool reads the project's records, code, screens and the latest test results, and writes only the files a command names. It runs no tests, runs no server, and publishes no package on your behalf. Keep `PackageId`, `ToolCommandName` and the command names stable: install scripts and the consumer docs name them.
