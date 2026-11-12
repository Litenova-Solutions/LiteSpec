# Initialize Consumer

## Name

`litespec init` starts a consumer repository from a preset, a ready-made selection of shape, stacks, frontends, and layer claims, or the records preset, which claims the record model with no stacks.

## Synopsis

```bash
litespec init --preset <name> --size <small|large> [--install <tool|standalone>] [--root <path>] [--name <project>] [--skill <agent>[,<agent>]] [--skill-global] [--help]
```

## Description

This command writes `.litespec/project.yaml` into the consumer root. It reads the consumer template and the named preset from the release the tool carries, overlays the preset's shape, stacks, and frontends, writes the `layers` the preset declares, and then writes the project size, name, and `reviewedLiteSpecVersion`. A preset that names no stacks or no frontends writes a project with none, so a server project with no frontend omits `frontends`. It refuses to overwrite an existing project file.

It also copies the starting agent context, the glossary at `.litespec/glossary.yaml`, and the example cast at `.litespec/example-cast.md` into the LiteSpec folder, leaving existing files alone, and writes `.litespec/.gitignore` listing the generated `model/` and `book/` folders. A fresh `AGENTS.md` carries the `agent-protocol` generated region filled with the release, size, shape, stacks, layers, extensions, and selection index of the new project, and names the command that runs the CLI in the project. An existing `AGENTS.md` that holds no region gains the region after its first heading, or at the end when it holds no heading. The command never touches text outside the region, and it never rewrites a region that is already there; `litespec model render` rewrites the region.

`--install` states how the project installs the CLI, as the [Install the CLI](../guide/install-the-cli.md) guide describes. With `tool`, the default, the command writes `.config/dotnet-tools.json` pinning the `LiteSpec.Cli` tool at the release, leaving an existing tool manifest alone, and the agent context runs the CLI as `dotnet litespec <command>`. With `standalone`, the project runs the native binary from the GitHub release as `litespec <command>`: the command writes no tool manifest, and `reviewedLiteSpecVersion` alone pins the release, which the install scripts read in the project root.

With `--skill`, the command also installs the litespec skill for the named agents, one generated copy per agent from the carried release, as the [Skill](skill.md) page describes. At project scope each copy is stamped with the release. With `--skill-global`, the copies are written under the user's home instead and name no release; without `--skill` it installs the default agents. Without `--skill` and without `--skill-global`, the command installs nothing and prints the `skill add` command as its last line instead. A target that holds a folder this tool did not write is left alone and fails the run, and the project file stays written.

When the root holds a `CLAUDE.md` that does not import `AGENTS.md`, the command prints a line saying so. Claude Code reads `AGENTS.md` only through that import.

Five presets ship with the release:

| Preset | Shape | Stacks | Frontend | Layers |
|:---|:---|:---|:---|:---|
| `dotnet-react` | `server` | `dotnet`, `react` | `web` at `apps/web` on `react` | `intent`, `behavior`, `implementation`, `proof` |
| `dotnet-svelte` | `server` | `dotnet`, `svelte` | `web` at `apps/web` on `svelte` | `intent`, `behavior`, `implementation`, `proof` |
| `blazor-client` | `client-only` | `blazor` | `app` at `apps/app` on `blazor` | `intent`, `behavior`, `implementation`, `proof` |
| `android-client` | `client-only` | `android` | `android` at `apps/mobile/src/{name}.Android` on `android` | `intent`, `behavior`, `implementation`, `proof` |
| `records` | `server` | none | none | `intent`, `behavior` |

The `records` preset claims `intent` and `behavior` in `layers` and names no stacks and no frontends. Every other preset keeps the layers of the template, which claims all four. Every written project file validates against `schemas/litespec-project.schema.yaml`.

Run this command once in an empty folder, then run `litespec model check` to validate the new project.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--preset <name>` | required | Selects the preset: `android-client`, `blazor-client`, `dotnet-react`, `dotnet-svelte`, or `records`. |
| `--size <size>` | required | Sets the project size: `small` or `large`. |
| `--install <install>` | `tool` | States how the project installs the CLI: `tool`, a local .NET tool pinned in `.config/dotnet-tools.json`, or `standalone`, the native binary from the GitHub release. |
| `--root <path>` | the working directory | Selects the folder to write into. The command uses this folder as written, with no walk-up. |
| `--name <project>` | the root folder's name | Sets the project name. |
| `--skill <agent>[,<agent>]` | off | Installs the skill at project scope for the named agents: one or more of `claude`, `opencode`, `agents`, comma separated. With `--skill-global` the copies are written at global scope instead. |
| `--skill-global` | off | Writes the skill copies under the user's home instead of the consumer. Without `--skill` it installs the default agents. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The project file was written. |
| `1` | The release holds no readable template, preset, scaffold file, skill file, or version, or a skill target holds a folder this tool did not write. |
| `2` | The root folder is missing or invalid, the project file already exists, the preset, size, install, or agent is invalid, or an option is unknown, repeated, or empty. A usage error is reported before `--help` is honored. |

## Examples

Start a small React consumer in a new folder:

```bash
litespec init --preset dotnet-react --size small --root ./shop
```

Start a large Blazor client and name the project:

```bash
litespec init --preset blazor-client --size large --name shop
```

Start a small Svelte consumer that runs the standalone binary, with no .NET tool manifest:

```bash
litespec init --preset dotnet-svelte --size small --install standalone --root ./shop
```

Start a small records-only project with no stacks:

```bash
litespec init --preset records --size small --root ./shop
```

Start a small consumer and install the skill for two agents:

```bash
litespec init --preset dotnet-react --size small --root ./shop --skill agents,claude
```

Start a small consumer and install the skill for every project of the user:

```bash
litespec init --preset dotnet-react --size small --root ./shop --skill agents --skill-global
```

## Underneath

This command merges `templates/consumer/project.yaml` with `templates/presets/<name>.yaml` from the release the tool carries and writes the result. It copies the scaffold files from `templates/consumer/`, including the glossary as `.litespec/glossary.yaml`, the example cast as `.litespec/example-cast.md`, and `.litespec/.gitignore`, and writes `.config/dotnet-tools.json` for a tool install. It reads the `agent-protocol` region from `templates/consumer/project-agents.md`. With `--skill`, it reads the skill copies from `templates/skill/litespec/` in the same way the [Skill](skill.md) page states, and `LITESPEC_SKILL_HOME` points the global scope at another home folder. Nothing beneath this command is separately runnable. Validate the written file with `litespec model check`.
