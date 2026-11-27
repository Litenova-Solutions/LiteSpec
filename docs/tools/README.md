# Overview

The `litespec` command line checks a consumer project, writes its scaffolded and synced files, keeps its agent files current, serves the warm model to agents, and answers lookups about its records. Each command below does one of those jobs: find the one that produces the check or file the task needs.

On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail. The commands that maintain the standard itself live in the [Maintain overview](../maintain/README.md).

## Install the CLI

Each LiteSpec release ships the command line two ways: as the `LiteSpec.Cli` .NET tool and as a standalone archive per platform. The [Install the CLI](../guide/install-the-cli.md) guide covers every install method, the platform requirements, and continuous integration.

## Run the CLI

Run every command from the consumer root, the folder that holds `.litespec/project.yaml`. The command pages write the consumer form as `litespec <command>`; with the local .NET tool, type `dotnet litespec <command>`. In the LiteSpec repository itself, run `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`.

A command that reads or changes a consumer starts at `--root`, or at the working directory when `--root` is absent, and uses the first folder at or above it that holds `.litespec/project.yaml`. `litespec init` is the exception: it writes into the `--root` folder as given, with no walk-up. A command exits with code 2 when the root or the project file is missing or invalid, and with code 1 when the records cannot be read.

Each CLI runs the LiteSpec release installed beside its executable. Set `LITESPEC_HOME` to make it use a copy installed elsewhere.

Every command accepts `--help`. `model check` also accepts `--format json`, which writes one JSON object for callers that read the result instead of the text.

## Consumer Repository

- [Initialize Consumer](init.md): writes `.litespec/project.yaml` for a new consumer from a preset.
- [Scaffold Record](new.md): creates a record file and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning file.
- [Rename Record](rename.md): renames a record identifier and rewrites every link to it.
- [Model Builder](model.md): checks the project model with the consumer pass, reviews its prose, formats record files, renders generated outputs, answers record queries, and reports module edges.
- [Scaffold](scaffold.md): writes the normal per-use-case code files for one use case record, and never overwrites an existing file.
- [Sync](sync.md): writes the pure data the records determine, from the `sync` targets and the frontend clients of the consumer declaration.
- [Serve](serve.md): keeps the project model warm and answers the four MCP tools an agent calls: task packet, check, impact and slice verification.
- [Agent Sync](agent-sync.md): writes the thin root agent context, the path-scoped rule files and the hook settings.
- [Hook](hook.md): runs the agent harness hooks for one edit or the stop.
- [Plan Waves](plan-waves.md): lists conflict-free parallel slices with their exclusive paths from the module graph.
- [Upgrade](upgrade.md): reports the release changes the project has not applied yet.
- [Docs Review and Render](docs.md): lints changed docs and records and writes a review packet, or re-renders the book pages of changed records.
- [UI Review](ui-review.md): lists the changed routes with their golden baselines and writes a review packet.
- [Book](book.md): serves the project book live on localhost, or writes the static book with `--out`.
- [Rules Query](rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
- [Skill](skill.md): installs or removes the litespec skill.
- [Context](context.md): writes the specification as one plain-text file for an AI chat.
- [Api Explore](api-explore.md): walks a running API from its OpenAPI document and reports server faults.
- [UI Audit](ui-audit.md): visits each route of a running application in a browser and reports what the rendered pages show wrong.
