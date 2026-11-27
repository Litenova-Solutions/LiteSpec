# Hook

## Name

`litespec hook` runs the agent harness hooks: it reminds each file edit with the provisions that govern the file, and it gates the stop on the blocking findings of the change set.

## Synopsis

```bash
litespec hook post-edit [--root <consumer>]
litespec hook pre-stop [--root <consumer>]
```

## Description

`post-edit` reads the Claude Code PostToolUse JSON on standard input, takes the edited file from `tool_input.file_path`, and prints at most 40 lines: the judgment provisions `rules` returns for that file, one line each with its identifier, and the open findings for it. The provisions print first with one line naming the file, then one collapsed line per record holding provisions about the file, then the findings newest-blocking first under their own heading. When the input states no file, or no provision and no finding applies, the command prints nothing. The output is added context beside the edit: it never blocks the agent, so it always exits 0 once the input is read.

`pre-stop` runs the model check scoped to the files changed since `HEAD` and prints the blocking findings on them, one per line with its path, line, level, code, and message, with a count line last. With no changed files, or no blocking finding on them, it prints one line saying so and exits 0. With a blocking finding it exits 2, so the agent keeps working through the turn instead of stopping. A check the tool cannot run, such as a root outside a git repository, prints one line saying so and exits 0 rather than trapping the agent.

Install both through `litespec agent sync`, which writes the `PostToolUse` entry on `Edit|Write` running `litespec hook post-edit` and the `Stop` entry running `litespec hook pre-stop` into `.claude/settings.json`. The [Agent Hooks and MCP](../guide/agent-hooks-and-mcp.md) guide shows the resulting settings and the OpenCode pairing.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `post-edit` | one of `post-edit`, `pre-stop` | Prints the judgment provisions and the open findings for the edited file. |
| `pre-stop` | one of `post-edit`, `pre-stop` | Prints the blocking findings on the files changed since `HEAD`. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. For `post-edit` with no `--root`, the consumer is found from the edited file's folder first. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The hook printed its context, or `pre-stop` found no blocking finding. |
| `1` | The hook input or a file cannot be read. |
| `2` | The arguments are wrong, or `pre-stop` found a blocking finding. |

## Examples

Remind one edit with the provisions that govern it, as the hook does:

```bash
echo '{"tool_input": {"file_path": "apps/api/src/Modules/Library.Lending/Application/Copies/RegisterCopy/RegisterCopyCommandHandler.cs"}}' | litespec hook post-edit --root ../consumer
```

Gate the stop on the change set:

```bash
litespec hook pre-stop --root ../consumer
```

## Underneath

`post-edit` accepts the Claude Code hook shapes: `tool_input.file_path` with `filePath` and `path` as fallbacks, and a top-level path when the payload carries no `tool_input`. The provisions come from the same retrieval `litespec rules` prints: unchecked judgment provisions that match the file kind or its marks, with page-scope provisions by concern left out as in the short `rules` form. The findings come from the same collection `model check` reports, minus the covered findings, filtered to the edited file or its subject record and ordered blocking first. The 40-line cap counts every printed line; when the lines run over, the last line says how many were withheld.

`pre-stop` reads the change set the same way `rules --since` does: the tracked files differing between `HEAD` and the working tree with the untracked files, minus the tool output folder. The check is the model check scoped to those files, and only a blocking outcome fails the run.

Nothing beneath this command is separately runnable.
