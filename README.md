# LiteSpec

[![CI](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml/badge.svg)](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml)
[![NuGet](https://img.shields.io/nuget/v/LiteSpec.Cli)](https://www.nuget.org/packages/LiteSpec.Cli)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-litespec.dev-blue.svg)](https://litespec.dev/6.0.0/)

## Intent

LiteSpec is a standard for specification-driven development of one bounded-context business application by people and coding agents. It defines two things. The specification model is the record types in which a product states what it must do, linked from Intent through Behavior to the code and tests that carry it. The engineering provisions are the experience of how to build such an application, each marked as required, a default, or advisory. Its tool checks the specification and its links, and helps agents find the provisions that apply.

LiteSpec is for a team that builds one bounded-context business application with people and coding agents working from the same written source. Without a checked link between the written intent and the code, the two drift apart: a record says one thing, the code does another, and a test proves neither. LiteSpec holds the three together by giving each fact one written home and checking the connections between them.

## How It Works

A project describes itself with records: written statements of fixed types, joined by fixed links. Each record page is a YAML record file with an optional Markdown explanation file beside it. Records sit in four layers.

| Layer | What It Holds |
|:---|:---|
| Intent | Goals, requirements, and decisions that state what the product promises. |
| Behavior | Use cases, journeys, and rules that state how the system keeps the promises. |
| Implementation | The code and screens that carry the behavior. |
| Proof | The tests and checks that show the code matches the records. |

The `litespec` tool reads the records and the code, builds a project model, and reports where the two disagree.

A project names the record layers it claims in `layers` in `.litespec/project.yaml`, with the practice each claimed layer follows. A lower layer needs the one above it: `behavior` needs `intent`, and `implementation` and `proof` each need `behavior`.

| Layer | Values | What the project claims |
|:---|:---|:---|
| `intent` | `litespec` | Its Intent records follow the record model. |
| `behavior` | `litespec` | Its Behavior records follow the record model. |
| `implementation` | `litespec` or `own` | Code marks its records, and follows LiteSpec's architecture pages or the team's own architecture. |
| `proof` | `litespec` or `own` | Tests prove scenarios, and follow LiteSpec's test pages or the team's own test practice. |

`litespec model check`, the book, `litespec rules` and the analyzers report only what applies to the claimed layers.

Each check also has a check level. A stack reader is the part of the tool that reads one stack's code and tests and follows their marks to the records. A reader is qualified when it ran and its handshake matched.

| Check level | Meaning |
|:---|:---|
| G | Reads records only. Always blocks. |
| S | Reads code through a stack reader. Blocks where the reader is qualified, and reports otherwise. |
| A | Advisory. Reports; `--release` makes the release-readiness findings block. |

The tool renders the model as a book that a reader reads offline. The analyzers apply the same rules inside a .NET build.

## How Agents Use It

An agent asks the tool which provisions apply to the file at hand instead of loading pages. Before editing a file, it runs `litespec rules <path>` and reads the returned provisions grouped by strength, each with why it was retrieved, after the decisions and departures the file follows. When a statement is not enough, it runs `litespec rules <provision-id>` for the whole provision. Before reporting done, it runs `litespec rules --since <base>`, answers the Reviewed for question of each provision the change set returns, and names the provisions it judged and how in its completion report.

It loads record context through `litespec model query <record-id>`, which returns one record with its computed links. It builds only what the specification states; something that seems missing becomes a question to the owner or a backlog item, never code.

The project's `AGENTS.md` carries the agent protocol as a region the tool generates and keeps current. `litespec skill add` installs a LiteSpec skill for Claude Code and OpenCode that walks an agent through this order.

## Installation

The `litespec` command line is a native executable for Windows, Linux and macOS that needs no .NET runtime. A repository with the .NET SDK installs it as a local .NET tool; run these commands from the root of a consumer repository, the folder that will hold `.litespec/project.yaml`.

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 6.0.0
dotnet litespec init --preset dotnet-react --size small
```

Run `dotnet new tool-manifest` only when the repository has no `.config/dotnet-tools.json` yet. `dotnet tool install` records `LiteSpec.Cli` in that manifest at the release named above.

A repository without .NET installs the standalone binary from the GitHub release instead, and runs it as `litespec`:

```bash
curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
export PATH="$HOME/.litespec/bin:$PATH"
litespec init --preset dotnet-svelte --size small --install standalone
```

On Windows, `irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex` installs it. The [CLI overview](docs/tools/README.md#install-the-cli) lists every install method, how each pins the release, and the platform requirements.

`init` writes `.litespec/project.yaml` for the project. Both `--preset` and `--size` are required: `--preset` selects the shape, stacks, frontends, and layer claims, and `--size` selects `small` or `large`. The presets are `dotnet-react`, `dotnet-svelte`, `blazor-client`, `android-client`, and `records`. See [Initialize Consumer](docs/tools/init.md) for every option.

## First Check

Run the model check from the consumer root after `init` writes the project file.

```bash
dotnet litespec model check
```

`dotnet litespec model check` validates the project file, the record files, and the language of the explanation files and documents, builds the project model, and reports each finding with its check level, code, and location. See [Model Builder](docs/tools/model.md) for the exit codes and options.

## Commands

The CLI commands, with the `model` subcommands on their own rows:

| Command | Does |
|:---|:---|
| `litespec init` | Writes `.litespec/project.yaml` for a new consumer from a preset. |
| `litespec new` | Creates a record file in its one legal folder and reserves its identifier. |
| `litespec rename` | Renames an identifier and rewrites every citation. |
| `litespec model check` | Checks the specification, its links, and its marks. The one command CI runs. |
| `litespec model format` | Rewrites record files into canonical form. |
| `litespec model query` | Returns one record with its computed links. |
| `litespec book` | Generates the project book from the model. |
| `litespec model render` | Rewrites the generated regions: the agent protocol in `AGENTS.md`, the project skill copies, and the indexes. |
| `litespec rules <path>` | Lists the provisions that apply to a file, grouped by strength, or to a change set with `--since`. |
| `litespec skill add` | Installs the LiteSpec skill for Claude Code, OpenCode and the shared `.agents` folder. |

See the [command index](docs/tools/README.md) for every option.

## Stacks Supported

A project names one size (`small` or `large`), one shape (`server` or `client-only`), and its stacks. A server project selects one backend stack, and each frontend selects one stack.

| Stack | Folder | Role |
|:---|:---|:---|
| `dotnet` | `docs/backend/dotnet/` | Backend |
| `react` | `docs/frontend/web/react/` | Web frontend |
| `svelte` | `docs/frontend/web/svelte/` | Web frontend |
| `blazor` | `docs/frontend/blazor/` | Client frontend |
| `android` | `docs/frontend/mobile/android/` | Mobile frontend |

Selecting a stack applies its folder, every folder above it, and their subfolders that hold no stack. [litespec.manifest.yaml](litespec.manifest.yaml) lists the stacks, shapes, extensions, and pinned versions for the release.

## Documentation

The documentation for this release is at [litespec.dev/6.0.0](https://litespec.dev/6.0.0/). The same pages live in this repository at [docs/README.md](docs/README.md), which indexes every page a project applies. The command pages are under [docs/tools/](docs/tools/README.md).

## Repository Layout

| Path | What It Holds |
|:---|:---|
| `docs/` | The standard: every page a project applies, plus reference, tool, tutorial, and guide pages. |
| `schemas/` | YAML schemas for the manifest, the project file, records, and UI files. |
| `templates/` | Consumer templates, init presets, and seeds for LiteSpec pages. |
| `apps/cli/` | The `litespec` command line: `src/LiteSpec.Cli` and `tests/LiteSpec.Cli.Tests`. |
| `apps/model/` | The shared model library used by the tool and the analyzers. |
| `apps/analyzers/` | The Roslyn analyzers published as `LiteSpec.Analyzers`. |
| `apps/maintain/` | The `litespec-maintain` tool: authoring checks and the provision index. |
| `apps/site/` | The static site that publishes the standard at litespec.dev. |
| `examples/` | Worked examples, including the Claimwick expense product. |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch and review steps, and the [Verification section of AGENTS.md](AGENTS.md#verification) for the checks to run. Report a defect or propose a change through the issue forms in [.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/).

## Security

See [SECURITY.md](SECURITY.md) for the supported releases and the private reporting route.

## License

LiteSpec is released under the [MIT License](LICENSE).
