# LiteSpec

[![CI](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml/badge.svg)](https://github.com/Litenova-Solutions/LiteSpec/actions/workflows/litespec.yml)
[![NuGet](https://img.shields.io/nuget/v/LiteSpec.Cli)](https://www.nuget.org/packages/LiteSpec.Cli)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-litespec.dev-blue.svg)](https://litespec.dev/7.0.0/)

LiteSpec is a standard for specification-driven development of one bounded-context business application by people and coding agents. Without a checked link between the written intent and the code, the two drift apart: a record says one thing, the code does another, and a test proves neither. A team writes what the product must do as records, and the `litespec` tool checks the specification and its links and helps agents find the provisions that apply.

The standard has two parts. The specification model holds the record types in which a product states what it must do, linked from Intent through Behavior to the code and tests that carry it. The engineering provisions hold the experience of building such an application, each marked as required, a default, or advisory. Each fact has one written home, and the tool checks the connections between homes, so people and coding agents work from the same written source.

## How It Works

A project describes itself with records, which are written statements of fixed types joined by fixed links. Each record page is a YAML record file with an optional Markdown explanation file beside it. All records live in four layers under the product record.

| Layer | What It Holds |
|:---|:---|
| Intent | Goals, problems, features, flows, requirements, constraints, research, and decisions. These state what the product promises. |
| Behavior | Modules, aggregates, rules, use cases, read models, reactions, jobs, notifications, integration events, entry points, and screens. These state how the system keeps its promises. |
| Implementation | The code that carries the behavior. |
| Proof | The tests and checks that show the code matches the records. |

The `litespec` tool reads the records and the code, builds a project model, and reports where the two disagree.

Each project declares the record layers it claims in the `layers` key of `.litespec/project.yaml`, along with the practice each claimed layer follows. The layers build on each other: claiming `behavior` requires `intent`, and claiming `implementation` or `proof` requires `behavior`.

| Layer | Allowed Values | What the project claims |
|:---|:---|:---|
| `intent` | `litespec` | Its Intent records follow the record model. |
| `behavior` | `litespec` | Its Behavior records follow the record model. |
| `implementation` | `litespec` or `own` | Its code marks its records, and follows either LiteSpec's architecture pages or the team's own architecture. |
| `proof` | `litespec` or `own` | Its tests prove scenarios, and follow either LiteSpec's test pages or the team's own test practice. |

`litespec model check`, the book, `litespec rules`, and the analyzers report only what the claimed layers cover.

Each check carries a check level, which states what its finding rests on. Whether a finding blocks follows from the strength of the provision it reports: a finding on a required provision blocks unless a waiver covers it, and a finding on an advisory provision never blocks.

Findings about links between records always sit at level G or S, and always block.

The part of the tool that reads one stack's code and tests and follows their marks back to records is called a stack reader. A reader counts as qualified once it has run and its handshake has matched.

| Check Level | Meaning |
|:---|:---|
| G | Rests on records only. |
| S | Rests on code read through a stack reader. The finding counts only where the reader is qualified. |
| A | Rests on prose, names, or proof state, and never blocks. |

`litespec book` turns the model into readable pages: it serves the book live by default, and `--out` writes a book safe to host. The analyzers apply the same rules inside a .NET build.

## How Agents Use It

An agent asks the tool which provisions apply instead of loading standard pages. Before it edits a file, it asks which provisions govern that file: `litespec rules <path>` lists them grouped by strength, with the reason for each, after the decisions and waivers the file follows. If a statement alone does not settle the question, `litespec rules <provision-id>` prints the full provision.

To learn what the specification says, the agent queries the model: `litespec model query <record-id>` returns a single record with its computed links, while `litespec model query --search <text>` and `litespec model query --list <type>` find records. For a whole session, `litespec serve` keeps the model warm and answers four tools: the task packet, the open findings, the impact of a record, and the slice check of one use case. The agent builds only what the specification states, and when something looks missing, it asks the owner or files a backlog item; it never writes code for what the specification does not state.

The project's `AGENTS.md` carries the agent protocol the tool keeps current through `litespec agent sync`, with one path-scoped rule file per selected stack and hook settings that remind each edit and gate the stop. `litespec skill add` installs a LiteSpec skill for Claude Code and OpenCode that leads an agent through this order.

## Installation

Install the `litespec` command line in one of two ways. A repository with the .NET SDK installs it as a local .NET tool. A repository without .NET installs the standalone native executable for Windows, Linux, or macOS, which needs no .NET runtime. Run the install from the consumer repository root, which is the folder that will hold `.litespec/project.yaml`.

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --version 7.0.0
dotnet litespec init --preset dotnet-svelte --size small
```

Run `dotnet new tool-manifest` only when the repository has no `.config/dotnet-tools.json` yet. The `dotnet tool install` step records `LiteSpec.Cli` in that manifest at the named release.

Without the .NET SDK, install the standalone binary from the GitHub release and run it as `litespec`:

```bash
curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
export PATH="$HOME/.litespec/bin:$PATH"
litespec init --preset dotnet-svelte --size small --install standalone
```

On Windows, the install command is `irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex`. The [CLI overview](docs/tools/README.md#install-the-cli) covers each install method, how it pins the release, and the platform requirements.

`init` writes `.litespec/project.yaml` for the project, and it needs both `--preset` and `--size`. The `--preset` flag selects the shape, stacks, frontends, and layer claims, and `--size` selects `small` or `large`. The presets are `dotnet-react`, `dotnet-svelte`, `blazor-client`, `android-client`, and `records`. See [Initialize Consumer](docs/tools/init.md) for every option.

## First Check

After `init` writes the project file, run the model check from the consumer root.

```bash
dotnet litespec model check
```

The command validates the project file, the record files, and the language of the explanation files and documents. Then it builds the project model and reports each finding with its check level, code, and location. See [Model Builder](docs/tools/model.md) for the exit codes and options.

## Commands

These are the CLI commands, with each `model` and `docs` subcommand on its own row:

| Command | What It Does |
|:---|:---|
| `litespec init` | Writes `.litespec/project.yaml` for a new consumer from a preset. |
| `litespec new` | Creates a record file in its one legal folder and reserves its identifier. |
| `litespec rename` | Renames an identifier and rewrites every citation of it. |
| `litespec model check` | Checks the specification, its links, and its marks. This is the one command CI runs. |
| `litespec model format` | Rewrites record files into canonical form. |
| `litespec model query` | Returns one record with its computed links, or finds records with `--search` and `--list`. |
| `litespec model edges` | Prints each module's links to other modules. |
| `litespec model render` | Rewrites the generated regions: the agent protocol in `AGENTS.md`, the project skill copies, and the indexes. |
| `litespec scaffold` | Writes the code files of one use case from its record, and never overwrites a file. |
| `litespec sync` | Writes the pure data the records determine into the configured targets. |
| `litespec serve` | Keeps the project model warm and answers the four MCP tools an agent calls. |
| `litespec agent sync` | Writes the thin root agent context, the path-scoped rule files, and the hook settings. |
| `litespec hook` | Reminds one edit with its provisions, or gates the stop on the blocking findings. |
| `litespec plan-waves` | Lists conflict-free parallel slices with their exclusive paths from the module graph. |
| `litespec upgrade` | Reports the release changes the project has not applied yet. |
| `litespec docs review` | Lints changed docs and records and writes a review packet. |
| `litespec docs render` | Re-renders the book pages of only the records whose hash changed. |
| `litespec ui review` | Lists the changed routes with their golden baselines and writes a review packet. |
| `litespec book` | Serves the project book live on localhost, or writes the static book with `--out`. |
| `litespec rules <path>` | Lists the provisions that apply to a file, grouped by strength. |
| `litespec skill add` | Installs the LiteSpec skill for Claude Code, OpenCode and the shared `.agents` folder. |
| `litespec context` | Writes the specification as one plain-text file for an AI chat. |

See the [command index](docs/tools/README.md) for every option.

## Stacks Supported

A project names one size (`small` or `large`), one shape (`server` or `client-only`), and its stacks. A server project selects one backend stack, and each frontend selects one stack.

| Stack | Folder | Role |
|:---|:---|:---|
| `dotnet` | `docs/packs/dotnet/` | Backend |
| `svelte` | `docs/packs/svelte/` | Web frontend |
| `android` | `docs/packs/android/` | Mobile frontend |

Selecting a stack applies its folder, every folder above it, and their subfolders that hold no stack. The [release manifest](litespec.manifest.yaml) lists the stacks, shapes, extensions, and pinned versions for the release.

## Documentation

The documentation for this release lives at [litespec.dev/7.0.0](https://litespec.dev/7.0.0/). The same pages live in this repository, indexed from [docs/README.md](docs/README.md), which lists every page a project applies. The command pages sit under [the tools index](docs/tools/README.md).

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

[CONTRIBUTING.md](CONTRIBUTING.md) covers the branch and review steps, and the [Verification section of AGENTS.md](AGENTS.md#verification) lists the checks to run. To report a defect or propose a change, use the issue forms in [.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/).

## Security

[SECURITY.md](SECURITY.md) names the supported releases and the private reporting route.

## License

LiteSpec is released under the [MIT License](LICENSE).
