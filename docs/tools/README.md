# Overview

## Intent

The Tools section documents the LiteSpec command line: one command for each consumer check, generator, and lookup, from project validation to generated files and pattern drawing. A developer or coding assistant opens this index to find the command that produces the check or file the task needs. On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail. The commands that maintain the standard itself live in the [Maintain overview](../maintain/README.md).

## Run the CLI

In the LiteSpec repository, run `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`. In a consumer, install the `litespec` command line as a local .NET tool at the adopted release, restore once with `dotnet tool restore`, then run `dotnet litespec <command>` (or `dotnet tool run litespec <command>`) from the consumer root, the folder that holds `litespec.project.json`. The command pages write the consumer form as `litespec <command>`.

The CLI needs the .NET 10 SDK and nothing else: no Node runtime. It carries its release beside its own binary; set `LITESPEC_HOME` to point it at another copy. Set `LITESPEC_TIMING` to print the phase timings of `litespec model` on standard error.

Every command accepts `--help`, and every validator accepts `--format json` for a caller that reads the result rather than the text.

## Consumer Repository

- [Initialize Consumer](init.md): writes `litespec.project.json` for a new consumer from a preset.
- [Scaffold Record](new.md): creates a record page and reserves its identifier, or appends a result or scenario entry to a use-case page.
- [Rename Record](rename.md): renames a record identifier and rewrites every link.
- [Validate Consumer](consumer.md): checks a consumer's pages, project record, and vocabulary.
- [Validate Controlled UI](ui.md): checks the shared UI package and each controlled web frontend.
- [Model Builder](model.md): builds, checks, and diffs the project model, renders generated regions, answers record queries, and prints the names inventory.
- [Book](book.md): generates a static site from `model.json` for reviewers.

## Anywhere

- [Draw Patterns](patterns.md): draws, prints, and checks the UI pattern wireframes.
