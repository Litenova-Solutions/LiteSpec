# Overview

## Intent

The Tools section documents the LiteSpec command line: one command for each consumer check, generator, and lookup, from project validation to generated files. A developer or coding assistant opens this index to find the command that produces the check or file the task needs. On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail. The commands that maintain the standard itself live in the [Maintain overview](../maintain/README.md).

## Install the CLI

Each LiteSpec release ships the `litespec` command line as the `LiteSpec.Cli` .NET tool and as a standalone archive per platform. The [Install the CLI](../guide/install-the-cli.md) guide covers every install method, the platform requirements, and continuous integration.

## Run the CLI

Run every command from the consumer root, the folder that holds `.litespec/project.yaml`. The command pages write the consumer form as `litespec <command>`; with the local .NET tool, type `dotnet litespec <command>`. In the LiteSpec repository, run `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`.

A command that reads or changes a consumer starts at `--root`, or the working directory when `--root` is absent, and uses the first folder at or above it that holds `.litespec/project.yaml`. `litespec init` uses the `--root` folder as written, with no walk-up. A command exits with code 2 when the root or the project file is missing or invalid, and with code 1 when the records cannot be read.

The CLI carries its release beside its own executable; set `LITESPEC_HOME` to point it at another copy.

Every command accepts `--help`, and `model check` accepts `--format json` for a caller that reads the result rather than the text.

## Consumer Repository

- [Initialize Consumer](init.md): writes `.litespec/project.yaml` for a new consumer from a preset.
- [Scaffold Record](new.md): creates a record file and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning file.
- [Rename Record](rename.md): renames a record identifier and rewrites every link.
- [Model Builder](model.md): checks the project model with the consumer pass, formats record files, renders generated outputs, and answers record queries.
- [Book](book.md): generates the project book from the consumer root: one offline shell with the outline and security views.
- [Rules Query](rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
