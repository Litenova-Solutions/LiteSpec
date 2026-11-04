# Overview

## Intent

The Tools section documents the LiteSpec command line: one command for each consumer check, generator, and lookup, from project validation to generated files. A developer or coding assistant opens this index to find the command that produces the check or file the task needs. On a command page, Name and Synopsis give the quick form, and Description and Examples give the detail. The commands that maintain the standard itself live in the [Maintain overview](../maintain/README.md).

## Run the CLI

In the LiteSpec repository, run `dotnet run --project apps/cli/src/LiteSpec.Cli -- <command>`. In a consumer, install the `litespec` command line as a local .NET tool at the adopted release, restore once with `dotnet tool restore`, then run `dotnet litespec <command>` (or `dotnet tool run litespec <command>`) from the consumer root, the folder that holds `.litespec/project.json`. The command pages write the consumer form as `litespec <command>`.

The CLI needs the .NET 10 SDK. A project with a React or Svelte frontend also needs Node at the version the manifest pins (`stack.node`), because the tool reads those frontends with a bundled Node reader. It carries its release beside its own binary; set `LITESPEC_HOME` to point it at another copy. Set `LITESPEC_TIMING` to print the phase timings of `litespec model` on standard error.

Every command accepts `--help`, and `model check` accepts `--format json` for a caller that reads the result rather than the text.

## Consumer Repository

- [Initialize Consumer](init.md): writes `.litespec/project.json` for a new consumer from a preset.
- [Scaffold Record](new.md): creates a record file and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning file.
- [Rename Record](rename.md): renames a record identifier and rewrites every link.
- [Model Builder](model.md): checks the project model with the consumer pass, formats record files, renders generated outputs, and answers record queries.
- [Book](book.md): generates the project book from the consumer root: one offline shell with the outline and security views.
- [Rules Query](rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
