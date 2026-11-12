# Skill

## Name

`litespec skill` installs or removes the litespec skill, the folder of guidance a coding agent loads when its description matches the task.

## Synopsis

```bash
litespec skill add [--agent <name>]... [--global] [--root <path>] [--help]
litespec skill remove [--agent <name>]... [--global] [--root <path>] [--help]
litespec skill --help
```

## Description

A skill is a folder holding `SKILL.md` (YAML frontmatter, then Markdown) with optional `references/` files beside it. An agent loads it on demand when its description matches the task. This command installs the litespec skill from the release the tool carries, one copy per selected agent.

Each agent reads its own folder:

| Agent | Project scope | Global scope |
|:---|:---|:---|
| `claude` | `.claude/skills/litespec/` | `~/.claude/skills/litespec/` |
| `opencode` | `.opencode/skills/litespec/` | `~/.config/opencode/skills/litespec/` |
| `agents` | `.agents/skills/litespec/` | `~/.agents/skills/litespec/` |

With no `--agent`, the command selects `agents` and `claude`, which together reach Claude Code and OpenCode. Each target gets its own generated copy: a copy works in git and on Windows without Developer Mode, and no copy is ever hand-edited.

At project scope the command selects the consumer from `--root` with the usual walk-up, and the project file must exist. Each copy is stamped with the carried release. At global scope the command writes under the user's home, needs no project file, and the copy names no release, so one global skill serves projects on different releases.

`add` writes every selected copy and replaces a copy this tool wrote. A folder without its marker is left alone, and the run fails: the marker is `generated-by: litespec` in the `metadata` of the copy's frontmatter. `remove` deletes only a folder carrying the marker.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `add` | one of `add`, `remove` | Installs the skill at every selected target, replacing a copy this tool wrote. |
| `remove` | one of `add`, `remove` | Deletes the skill at every selected target when this tool wrote it. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--agent <name>` | `agents` and `claude` | Selects the targets: `claude`, `opencode`, or `agents`. Repeat the option for more than one. |
| `--global` | off | Writes under the user's home instead of the consumer. Needs no project file, and cannot be combined with `--root`. |
| `--root <path>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The skill was installed or removed at every selected target. |
| `1` | A target holds a folder this tool did not write, or a release or target file cannot be read or written. |
| `2` | The subcommand or an agent is unknown, the root or the project file is missing or invalid, `--global` is combined with `--root`, or an option is unknown or empty. |

## Examples

Install the skill for the default agents:

```bash
litespec skill add
```

Install the skill for OpenCode only:

```bash
litespec skill add --agent opencode
```

Install the skill for two agents:

```bash
litespec skill add --agent claude --agent opencode
```

Install the skill for every project of the user:

```bash
litespec skill add --global
```

Remove the skill again:

```bash
litespec skill remove
```

## Underneath

The command reads `templates/skill/litespec/` from the release the tool carries: `SKILL.md` with `references/commands.md` and `references/report.md`. The template carries the placeholder `__LITESPEC_RELEASE_METADATA__` on its own line inside `metadata`. At project scope the command replaces it with the carried release; at global scope it removes the line. Every copy carries `generated-by: litespec` in its frontmatter `metadata`, and the command reads that marker back to decide whether a folder is its own: `add` replaces a marked folder and refuses any other, and `remove` deletes a marked folder and leaves any other alone.

`LITESPEC_SKILL_HOME` points the command at another home folder for the global scope. Only the skill command reads it. Nothing beneath this command is separately runnable.
