# LiteSpec.Cli

The `litespec` command line for applications that follow [LiteSpec](https://litespec.dev), the specification standard for one bounded-context business application built by people and agents. The package carries the LiteSpec release it checks against, so a project needs no copy of the standard.

## Install

Install the tool in the project, at the release the project follows:

```bash
dotnet new tool-manifest
dotnet tool install LiteSpec.Cli --local --version 4.1.0
```

A teammate or a build restores it with `dotnet tool restore`.

## Use

Run each command from the project root, the folder that holds `.litespec/project.json`:

```bash
dotnet litespec init --preset dotnet-svelte --size small
dotnet litespec model check
```

| Command | What it does |
|:---|:---|
| `init` | Writes `.litespec/project.json`, the record scaffold, the tool manifest, and the `## LiteSpec` block of `AGENTS.md`. |
| `model` | Builds the project model from the records, the code, the screens and the tests; `check` reports where they disagree with the project file, the record pages, the glossary, and the controlled UI in the same run, `format` rewrites record files into canonical form, `render` writes the generated page regions and the records file each web frontend imports, and `query` prints one record with its links. |
| `book` | Generates a static site of the records for reviewers. |
| `new` | Scaffolds a record page and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning page. |
| `rename` | Renames a record identifier and rewrites every link. |
| `rules` | Lists the provisions that govern a path or record identifier, grouped by strength. |

Every command accepts `--help`, and the checks accept `--format json`.

## Learn More

- [Adopting LiteSpec](https://litespec.dev/4.1.0/learn/adopting-litespec/)
- [The command reference](https://litespec.dev/4.1.0/tools/)
- [The source and issues](https://github.com/Litenova-Solutions/LiteSpec)
