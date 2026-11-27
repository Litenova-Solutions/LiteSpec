# Initialize Consumer

## Name

`litespec init` starts a consumer repository from a preset: a ready-made selection of shape, stacks, frontends, and layer claims. The `records` preset claims Intent and Behavior only, with no stacks.

## Synopsis

```bash
litespec init --preset <name> --size <small|large> [--install <tool|standalone>] [--root <path>] [--name <project>] [--skill <agent>[,<agent>]] [--skill-global] [--bootstrap] [--help]
```

## Description

Run this command once in an empty folder, then validate the new project with `litespec model check`.

It writes `.litespec/project.yaml` into the consumer root: it reads the consumer template and the named preset from the release the tool carries, overlays the preset's shape, stacks, and frontends, writes the `layers` the preset declares, then writes the project size, the name, and `reviewedLiteSpecVersion`.

When a preset lists no stacks or no frontends, the project file has neither: a server project without a frontend omits `frontends`. The command refuses to overwrite an existing project file.

Three presets ship with the release:

| Preset | Shape | Stacks | Frontend | Layers |
|:---|:---|:---|:---|:---|
| `dotnet-svelte` | `server` | `dotnet`, `svelte` | `web` at `apps/web` on `svelte` | `intent`, `behavior`, `implementation`, `proof` |
| `android-client` | `client-only` | `android` | `android` at `apps/mobile/src/{name}.Android` on `android` | `intent`, `behavior`, `implementation`, `proof` |
| `records` | `server` | none | none | `intent`, `behavior` |

The `records` preset claims `intent` and `behavior` in `layers` and lists no stacks and no frontends. Every other preset keeps the layers of the template, which claims all four. Every written project file validates against `schemas/litespec-project.schema.yaml`.

Beyond the project file, the command copies the starting agent context, the glossary at `.litespec/glossary.yaml`, and the cast at `.litespec/cast.yaml`, and writes `.litespec/.gitignore` listing the generated `model/`, `book/`, `verify/`, `audit/` and `explore/` folders. Files that already exist are left alone.

For a preset that writes the consumer scaffold, it also writes the UI quality requirements of the consumer preset, one folder each under `.litespec/intent/requirements/<name>/`: `ui-text-contrast`, `ui-control-boundary`, `ui-focus-indicator`, and `ui-chart-series`, which the token limit checks measure, plus `ui-reflow` and `ui-target-size`, which the rendered-page audit measures. Each names a constraint or goal the project cites, with the measure and target its check proves. The `android-client` preset writes only the four token limit requirements: a phone client has no browser routes for the audit to visit, so the two route-suite requirements would never gain a run; the device tests beside the screens prove each screen on hardware instead, as the [Mobile Testing](../packs/android/testing.md) page states.

A fresh `AGENTS.md` carries the `agent-protocol` generated region, filled with the release, size, shape, stacks, layers, extensions, and selection index of the new project, and names the command that runs the CLI in the project. When an existing `AGENTS.md` holds no region, the command inserts one after its first heading, or at the end when it holds no heading. Text outside the region is never touched, and a region that is already there is never rewritten; `litespec model render` rewrites the region.

`--install` states how the project installs the CLI, as the [Install the CLI](../guide/install-the-cli.md) guide describes. With `tool`, the default, the command writes `.config/dotnet-tools.json` pinning the `LiteSpec.Cli` tool at the release, unless a tool manifest already exists, and the agent context runs the CLI as `dotnet litespec <command>`. With `standalone`, the project runs the native binary from the GitHub release as `litespec <command>`: no tool manifest is written, and `reviewedLiteSpecVersion` alone pins the release for the install scripts in the project root to read.

With `--skill`, the command also installs the litespec skill for the named agents, one generated copy per agent from the carried release, as the [Skill](skill.md) page describes. Each project-scope copy is stamped with the release. With `--skill-global`, the copies go under the user's home instead and name no release; without `--skill`, the default agents are installed.

Without `--skill` and without `--skill-global`, nothing is installed: the command prints the `skill add` command as its last line instead.

When a target holds a folder this tool did not write, that folder is left alone and the run fails, while the project file stays written.

When the root holds a `CLAUDE.md` that does not import `AGENTS.md`, the command prints a line saying so, since Claude Code reads `AGENTS.md` only through that import.

With `--bootstrap`, valid only with `--preset dotnet-svelte`, the command also copies the runnable starter from `templates/bootstrap/dotnet-svelte/` in the carried release into the root after writing the project file. It fills three tokens in file contents and in path segments: `__PROJECT__` with the project name, `__NAMESPACE__` with the PascalCase name with letters and digits only (`Team Expenses` becomes `TeamExpenses`), and `__SCOPE__` with the lowercase package scope (`team-expenses`). Binary files are copied unchanged. The template file `.litespec/project.bootstrap.yaml` is not copied: its top-level keys (such as `verify`) are merged into the written project file after filling the tokens. The command checks every bootstrap target before writing anything and refuses with exit code 1 when any target file already exists; `--bootstrap` with another preset is a usage error.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--preset <name>` | required | Selects the preset: `android-client`, `dotnet-svelte`, or `records`. |
| `--size <size>` | required | Sets the project size: `small` or `large`. |
| `--install <install>` | `tool` | States how the project installs the CLI: `tool`, a local .NET tool pinned in `.config/dotnet-tools.json`, or `standalone`, the native binary from the GitHub release. |
| `--root <path>` | the working directory | Selects the folder to write into. The command uses this folder as written, with no walk-up. |
| `--name <project>` | the root folder's name | Sets the project name. |
| `--skill <agent>[,<agent>]` | off | Installs the skill at project scope for the named agents: one or more of `claude`, `opencode`, `agents`, comma separated. With `--skill-global` the copies are written at global scope instead. |
| `--skill-global` | off | Writes the skill copies under the user's home instead of the consumer. Without `--skill` it installs the default agents. |
| `--bootstrap` | off | Copies the runnable dotnet-svelte starter from the carried release into the root. Valid only with `--preset dotnet-svelte`. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The project file was written. |
| `1` | The release holds no readable template, preset, scaffold file, skill file, bootstrap file, or version, a skill target holds a folder this tool did not write, or a bootstrap target file already exists. |
| `2` | The root folder is missing or invalid, the project file already exists, the preset, size, install, or agent is invalid, `--bootstrap` names another preset, or an option is unknown, repeated, or empty. A usage error is reported before `--help` is honored. |

## Examples

Start a small Svelte consumer in a new folder:

```bash
litespec init --preset dotnet-svelte --size small --root ./shop
```

Start a large Android client and name the project:

```bash
litespec init --preset android-client --size large --name shop
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
litespec init --preset dotnet-svelte --size small --root ./shop --skill agents,claude
```

Start a small consumer and install the skill for every project of the user:

```bash
litespec init --preset dotnet-svelte --size small --root ./shop --skill agents --skill-global
```

Start a small consumer with the runnable starter:

```bash
litespec init --preset dotnet-svelte --size small --name "Team Expenses" --bootstrap
```

## Underneath

The command merges `templates/consumer/project.yaml` with `templates/presets/<name>.yaml` from the release the tool carries and writes the result. The scaffold files come from `templates/consumer/`: the glossary as `.litespec/glossary.yaml` and the cast as `.litespec/cast.yaml`. The command writes `.litespec/.gitignore` with literal content listing the generated `model/`, `book/`, `verify/`, `audit/` and `explore/` folders. A tool install adds `.config/dotnet-tools.json`. The `agent-protocol` region comes from `templates/consumer/project-agents.md`.

With `--skill`, the skill copies come from `templates/skill/litespec/` in the same way the [Skill](skill.md) page states, and `LITESPEC_SKILL_HOME` points the global scope at another home folder. With `--bootstrap`, the starter copies come from `templates/bootstrap/dotnet-svelte/`, with the three tokens filled in contents and path segments, binary files passed through unchanged, and `.litespec/project.bootstrap.yaml` merged into the written project file instead of copied. Nothing beneath this command is separately runnable. Validate the written file with `litespec model check`.
