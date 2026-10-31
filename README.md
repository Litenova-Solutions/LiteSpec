# LiteSpec

[![CI](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml/badge.svg)](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml)
[![NuGet](https://img.shields.io/nuget/v/LiteSpec.Cli)](https://www.nuget.org/packages/LiteSpec.Cli)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-litespec.dev-blue.svg)](https://litespec.dev/3.0.0/)

## Intent

LiteSpec is a versioned specification standard for one bounded-context business application. It defines written records for what a product must do, and it ships a .NET command-line tool and Roslyn analyzers that check a project's records, code, and tests against those records.

LiteSpec is for a team that builds one bounded-context business application with people and coding agents working from the same written source. Without a checked link between the written intent and the code, the two drift apart: a record says one thing, the code does another, and a test proves neither. LiteSpec holds the three together by giving each fact one written home and checking the connections between them.

## How It Works

A project describes itself with records: written statements of fixed types, joined by fixed links. Records sit in four layers.

| Layer | What It Holds |
|:---|:---|
| Intent | Goals, requirements, and decisions that people approve. |
| Behavior | Use cases, journeys, and rules that developers review. |
| Implementation | The code and screens that carry the behavior. |
| Proof | The tests and checks that show the code matches the records. |

The `litespec` tool reads the records and the code, builds a project model, and reports where the two disagree. Each check has a level.

| Level | Meaning |
|:---|:---|
| G | Reads records only. Always blocks. |
| S | Reads code through a stack reader. Blocks where the reader is qualified, and reports otherwise. |
| A | Advisory. Reports and never blocks. |

The tool renders the model as a book that a reviewer reads offline. The analyzers apply the same rules inside a .NET build.

## Installation

LiteSpec runs as a local .NET tool. Run these commands from the root of a consumer repository, the folder that will hold `litespec.project.json`.

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 3.0.0
dotnet litespec init --preset dotnet-react --size small
```

Run `dotnet new tool-manifest` only when the repository has no `.config/dotnet-tools.json` yet. `dotnet tool install` records `LiteSpec.Cli` in that manifest at the release named above.

`dotnet litespec init` writes `litespec.project.json` for the project. Both options are required: `--preset` selects the shape, stacks, and frontends, and `--size` selects `small` or `large`. The presets are `dotnet-react`, `dotnet-svelte`, `blazor-client`, and `android-client`. See [Initialize Consumer](docs/tools/init.md) for every option.

## First Check

Run the consumer check from the consumer root after `init` writes the project file.

```bash
dotnet litespec consumer
dotnet litespec model check
```

`dotnet litespec consumer` validates the project file, the records, and the language of the pages. `dotnet litespec model check` builds the project model and reports each finding with its level, code, and location. See [Validate Consumer](docs/tools/consumer.md) and [Model Builder](docs/tools/model.md) for the exit codes and options.

## Stacks Supported

A project names one size (`small` or `large`), one shape (`server` or `client-only`), and its stacks. A server project selects one backend stack, and each frontend selects one stack.

| Stack | Folder | Role |
|:---|:---|:---|
| `dotnet` | `docs/backend/dotnet/` | Backend |
| `react` | `docs/frontend/web/react/` | Web frontend |
| `svelte` | `docs/frontend/web/svelte/` | Web frontend |
| `blazor` | `docs/frontend/blazor/` | Client frontend |
| `android` | `docs/frontend/mobile/android/` | Mobile frontend |

Selecting a stack applies its folder, every folder above it, and their subfolders that hold no stack. [litespec.manifest.json](litespec.manifest.json) lists the stacks, shapes, extensions, and pinned versions for the release.

## Documentation

The documentation for this release is at [litespec.dev/3.0.0](https://litespec.dev/3.0.0/). The same pages live in this repository at [docs/README.md](docs/README.md), which indexes every page a project applies. The command pages are under [docs/tools/](docs/tools/README.md).

## Repository Layout

| Path | What It Holds |
|:---|:---|
| `docs/` | The standard: every page a project applies, plus reference, tool, tutorial, and guide pages. |
| `schemas/` | JSON contracts for the manifest, the project file, records, and UI files. |
| `templates/` | Consumer templates, init presets, and seeds for LiteSpec pages. |
| `apps/cli/` | The `litespec` command line: `src/LiteSpec.Cli` and `tests/LiteSpec.Cli.Tests`. |
| `apps/model/` | The shared model library used by the tool and the analyzers. |
| `apps/analyzers/` | The Roslyn analyzers published as `LiteSpec.Analyzers`. |
| `apps/maintain/` | The `litespec-maintain` tool: authoring checks and the provision index. |
| `apps/site/` | The static site that publishes the standard at litespec.dev. |
| `examples/` | Worked examples, including the Claimwick expense product. |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch, review, and verification steps. Report a defect or propose a change through the issue forms in [.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/).

## Security

See [SECURITY.md](SECURITY.md) for the supported releases and the private reporting route.

## License

LiteSpec is released under the [MIT License](LICENSE).
