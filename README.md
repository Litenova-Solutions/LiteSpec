# LiteSpec

[![CI](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml/badge.svg)](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml)
[![NuGet](https://img.shields.io/nuget/v/LiteSpec.Cli)](https://www.nuget.org/packages/LiteSpec.Cli)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-litespec.dev-blue.svg)](https://litespec.dev/4.0.0/)

## Intent

LiteSpec is a standard for specification-driven development of one bounded-context business application by people and coding agents. It defines two things. The specification model is the record types in which a product states what it must do, linked from Intent through Behavior to the code and tests that carry it. The engineering provisions are the experience of how to build such an application, each marked as a rule, a default or advice. Its tool checks the specification and its links, and helps agents find the provisions that apply.

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

A project states the part of the contract it claims in the `conformance` field of `.litespec/project.json`. A conformance level is that claim. The levels nest: `traced` claims everything `records` claims, and `full` claims everything `traced` claims.

| Conformance level | What the project claims |
|:---|:---|
| `records` | Its Intent, Behavior, governance and glossary records follow the record model. |
| `traced` | Also: code and tests point to their records with marks, and test runs prove scenarios, in a code layout of the project's own. |
| `full` | Also: the selected stacks, LiteSpec's code layout, the analyzers and the operations provisions. |

`litespec model check`, the book, `litespec rules` and the analyzers report only the part the claimed level covers.

Each check also has a check level. A stack reader is the part of the tool that reads one stack's code and tests and follows their marks to the records. A reader is qualified when it ran and its handshake matched; the .NET reader stays qualified.

| Check level | Meaning |
|:---|:---|
| G | Reads records only. Always blocks. |
| S | Reads code through a stack reader. Blocks where the reader is qualified, and reports otherwise. |
| A | Advisory. Reports and never blocks. |

The tool renders the model as a book that a reader reads offline. The analyzers apply the same rules inside a .NET build.

## How Agents Use It

An agent asks the tool which provisions apply to the file at hand instead of loading pages. Before editing a file, it runs `litespec rules <path>` and reads the returned provisions grouped by strength, each with why it was retrieved. When a statement is not enough, it runs `litespec rules <path> --full` for rationale and example. It answers each returned provision's Reviewed for question before reporting done, and names the provisions it judged and how in its completion report.

It loads record context through `litespec model query <record-id>`, which returns one record with its computed links. It builds only what the specification states; something that seems missing becomes a question to the owner or a backlog item, never code.

## Installation

LiteSpec runs as a local .NET tool. Run these commands from the root of a consumer repository, the folder that will hold `.litespec/project.json`.

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 4.0.0
dotnet litespec init --preset dotnet-react --size small
```

Run `dotnet new tool-manifest` only when the repository has no `.config/dotnet-tools.json` yet. `dotnet tool install` records `LiteSpec.Cli` in that manifest at the release named above.

`dotnet litespec init` writes `.litespec/project.json` for the project. Both options are required: `--preset` selects the shape, stacks, and frontends, and `--size` selects `small` or `large`. The presets are `dotnet-react`, `dotnet-svelte`, `blazor-client`, and `android-client`. See [Initialize Consumer](docs/tools/init.md) for every option.

## First Check

Run the model check from the consumer root after `init` writes the project file.

```bash
dotnet litespec model check
```

`dotnet litespec model check` validates the project file, the record files, and the language of the explanation files and documents, builds the project model, and reports each finding with its check level, code, and location. See [Model Builder](docs/tools/model.md) for the exit codes and options.

## Commands

The CLI has eight commands.

| Command | Does |
|:---|:---|
| `litespec init` | Writes `.litespec/project.json` for a new consumer from a preset. |
| `litespec new` | Creates a record file in its one legal folder and reserves its identifier. |
| `litespec rename` | Renames an identifier and rewrites every citation. |
| `litespec model check` | Checks the specification, its links, and its marks. The one command CI runs. |
| `litespec model format` | Rewrites record files into canonical form. |
| `litespec model query` | Returns one record with its computed links. |
| `litespec book` | Generates the project book from the model. |
| `litespec rules <path>` | Lists the provisions that apply to a file, grouped by strength. |

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

Selecting a stack applies its folder, every folder above it, and their subfolders that hold no stack. [litespec.manifest.json](litespec.manifest.json) lists the stacks, shapes, extensions, and pinned versions for the release.

## Documentation

The documentation for this release is at [litespec.dev/4.0.0](https://litespec.dev/4.0.0/). The same pages live in this repository at [docs/README.md](docs/README.md), which indexes every page a project applies. The command pages are under [docs/tools/](docs/tools/README.md).

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
