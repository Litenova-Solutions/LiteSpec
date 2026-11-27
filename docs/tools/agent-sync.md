# Agent Sync

## Name

`litespec agent sync` writes the thin root agent context, the path-scoped rule files, and the hook settings from the project's stacks and selected extensions only.

## Synopsis

```bash
litespec agent sync [--root <consumer>] [--check]
```

## Description

The command keeps the agent-facing files in step with the standard the project selected, so an agent that loads only the rules for the files it edits still answers every judgment provision that governs them.

The root `AGENTS.md` stays under 100 lines: what the project is, the stacks and extensions it selects, the commands to run, where the path-scoped rules live, and the golden slice pointers. The generated text lives inside the `agent-sync` marked region; every line outside that region is left byte for byte as it was. When `AGENTS.md` holds no region, the command inserts it after the first heading, or at the end when no heading exists; when the file is missing, the command writes it from the project title with the region after the heading.

Each selected stack gets one rule file per host: `.claude/rules/<area>.md`, `.cursor/rules/<area>.mdc`, and `.github/instructions/<area>.instructions.md`, where `<area>` is the stack id. Each file scopes itself to the stack's source folders through its host frontmatter: `paths:` globs for Claude, `globs:` for Cursor, and `applyTo:` for GitHub. Each file holds only the unchecked judgment provisions of its area as one line each with its identifier, such as `- litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage: ...`. A project that selects no stack from a host's family writes no file for that family.

Two shared rule files cover the cross-cutting work. `.claude/rules/spec-writing.md` scopes to `.litespec/**/*.yaml` and carries the YAML clarity rules of the [Writing](../core/writing.md) page with the project's brand voice from `.litespec/product.yaml`. `.claude/rules/ui-routes.md` scopes to the frontend route folders and carries the single-job route guidance with the route provisions.

The command merges the hook block into `.claude/settings.json`: a `PostToolUse` entry on `Edit|Write` running `litespec hook post-edit`, and a `Stop` entry running `litespec hook pre-stop`. Existing settings are kept: the command adds only the entries that are missing and never removes or rewrites an entry it did not write.

With `--check` the command writes nothing, lists each file it would write, rewrite, or delete, and exits 1 when any would change.

When the project drops a stack, the command deletes the generated rule files of that stack's area that no longer apply; a file without the sync marker is never deleted. A stale agent context is the defect this command removes: run it after changing the stacks or the extensions, and `model check` reports a drifted agent file the same way it reports any other drifted generated file.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--check` | off | Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The files are written, or `--check` found every file current. |
| `1` | A file would change with `--check`, or a file cannot be read or written. |
| `2` | The arguments are wrong, or the root, the project file, or the carried release cannot be read. |

## Examples

Write the agent files from the selected stacks:

```bash
litespec agent sync --root ../consumer
```

List the files that would change, without writing:

```bash
litespec agent sync --root ../consumer --check
```

## Underneath

The command reads the provisions from the generated `docs/reference/provisions.json` in the carried release, the same catalog `litespec rules` reads, and keeps only the provisions the consumer's stacks, shape, and selected extensions apply. Each area holds its stack folder's provisions with the shared provisions that govern every stack: a page under a stack folder belongs to that stack's area alone, and a page outside every stack folder belongs to every area.

The area globs come from the consumer declaration: the module, WebApi, worker, kernel, and platform roots for a backend stack, and each matching frontend's folder for a frontend stack, with the stack's conventional folders when the declaration names none. The brand voice comes from the `brand:` block of `.litespec/product.yaml`; when the product declares none, the file states the universal baseline instead.

Nothing beneath this command is separately runnable.
