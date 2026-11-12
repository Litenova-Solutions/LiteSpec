# Ship a LiteSpec Skill

## Context

An agent in a consumer project learns the LiteSpec task order from the protocol region in `AGENTS.md`. Claude Code and OpenCode each read their own skill folders, and a global skill can shadow the project's copy. A link would share one folder, but links need Developer Mode or admin rights on Windows. Claude Code reads `CLAUDE.md` before the nearest `AGENTS.md`, so a project with a bare `CLAUDE.md` never shows its protocol. An agent that loads its rules once works from memory by its later edits.

## Decision

LiteSpec ships one generated skill named `litespec` as a copy per agent, at project and global scope, installed by `litespec skill add`. The project copy carries the release stamp, the global copy names no release, and every copy carries the generated marker the command reads back. `model render` rewrites the project copies, `model check` reports drift, and the edit-time recipes stay hand-installed on their guide page. The entry-file table bridges each agent to `AGENTS.md`, and the check reports a `CLAUDE.md` that leaves `AGENTS.md` out.
