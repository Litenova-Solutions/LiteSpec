# Ship a LiteSpec Skill

A skill is a folder of instructions that an AI coding agent loads when a task matches it. This decision ships LiteSpec's task order as one skill named `litespec`, installed as a copy for each agent. An agent in a consumer project then follows the order from the skill, and it does not rely on its memory of the protocol.

## Context

An agent in a consumer project learns the LiteSpec task order from the protocol region in `AGENTS.md`. Claude Code and OpenCode each read their own skill folders, and a global skill takes precedence over the project's copy. Claude Code reads `CLAUDE.md` before the nearest `AGENTS.md`, so a project with a bare `CLAUDE.md` never shows its protocol.

An agent that loads its rules once works from memory for its later edits. A link could share one skill folder across agents, but links need Developer Mode or admin rights on Windows.

## Options

LiteSpec compared 2 options for installing the skill. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One generated skill as a copy per agent, at project and global scope | One template change reaches every agent and scope through `skill add`, and each copy works in git and on Windows with no extra rights. | Each target holds its own copy, so a hand edit in one copy drifts from what the release generates. |
| Link each target at one shared skill folder | One folder holds the whole skill, so no copy can drift from another. | Windows links need Developer Mode or admin rights, and the link target sits outside the repository, so git does not track it. |

## Decision

LiteSpec ships one generated skill named `litespec`, installed as a copy for each agent at project and global scope. The command `litespec skill add` installs it. The project copy carries the release stamp, and the global copy names no release.

Every copy carries the generated marker that the command reads back. `model render` rewrites the project copies, and `model check` reports drift. The edit-time recipes stay hand-installed on their guide page.

## Consequences

`templates/skill/litespec/` holds the only skill source. It has `SKILL.md`, with `references/commands.md` and `references/report.md`. The entry names the skill `litespec`, triggers on work in a repository that holds `.litespec/project.yaml`, and states the task order instead of the rules.

Each target gets its own generated copy. `claude`, `opencode` and `agents` write under `.claude/skills/`, `.opencode/skills/` and `.agents/skills/`. At global scope the same 3 targets write under the home folder. They need no project file and name no release.

Every copy carries `generated-by: litespec` in its frontmatter `metadata`. A project copy adds the release as `litespec-release`. `litespec skill add` writes every selected copy, and it replaces a copy that the tool wrote. It refuses a folder without the marker. With no `--agent`, it selects `agents` and `claude`. `litespec skill remove` deletes only a folder that carries the marker. Tests point the global scope at a temporary home with `LITESPEC_SKILL_HOME`.

`init --skill` installs the skill at project scope during setup, and `init --skill-global` installs it at global scope. Without a flag, `init` prints the `skill add` command as its last line. `model check` reports `skill-drift` at level S when a project copy differs from what `render` writes. Global copies live outside the project and stay outside the check. `skill add` writes Markdown files only, and it installs no hooks.

The top 20 judged provisions across the Claimwick code hold none that a cheap and sure check covers. So no provision gains a check. The entry-file table states which file each agent reads, and how each one bridges to `AGENTS.md`. `init` prints a line when a `CLAUDE.md` does not import `AGENTS.md`, and `model check` reports `agent-protocol-unreached` as advisory for it. `litespec/core/agent#keep-the-agent-protocol-current` holds the drift obligation for the region and the project copies.

## Revisit Conditions

This decision reopens when a supported agent stops reading its skill folder, or when a target forbids generated copies.

Either case removes the reason for a copy per agent. The skill would then need another way to reach that agent.
