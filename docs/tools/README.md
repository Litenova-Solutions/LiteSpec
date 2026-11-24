# Overview

The `litespec` command line checks a consumer project, generates its files, and answers lookups about its records. Each command below does one of those jobs: find the one that produces the check or file the task needs.

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
- [Generate](generate.md): writes the code the records fully determine, from the `generate` targets of the consumer declaration.
- [Book](book.md): serves the project book live on localhost, or writes the static book with `--out`.
- [Rules Query](rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
- [Skill](skill.md): installs or removes the litespec skill.
- [Context](context.md): writes the specification as one plain-text file for an AI chat.
- [Api Explore](api-explore.md): walks a running API from its OpenAPI document and reports server faults.
