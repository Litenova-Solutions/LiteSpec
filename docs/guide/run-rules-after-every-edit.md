# Run Rules After Every Edit

## Purpose

An agent that loads the governing provisions once at the start of a task works from memory by its later edits, and memory drifts from the standard as the session grows. This guide brings the provisions back at the moment they matter: after each file edit the agent sees the short `rules` output for that file, and before it stops it faces the model check over its change set. It covers Claude Code hooks and an OpenCode plugin. `skill add` writes Markdown files only and installs no hooks, so each recipe below is installed by hand. The guide adds no rule of its own.

## Prerequisites

Start with:

- The `litespec` command from [Install the CLI](install-the-cli.md): the recipes spell it `litespec`, and a local .NET tool install spells it `dotnet litespec`.
- A consumer root holding `.litespec/project.yaml`, since both commands resolve the project from the working directory.
- `git`, since `model check --since` reads the change set from git.
- `jq` on the PATH for the Claude Code scripts, which parse hook input as JSON.
- No extra install for the OpenCode plugin: it runs inside the OpenCode server and calls the CLI through `node:child_process`.

Read the [Rules Query](../tools/rules.md) and [Model Builder](../tools/model.md) pages before this guide: they own the commands the recipes run. The recipes automate `litespec/core/agent#load-applicable-provisions-before-editing`, which asks the agent to run `rules <path>` before editing a file and to answer each judgment provision `rules --since <base>` lists before reporting done.

## Procedure

### Remind Each Edit in Claude Code

Add a `PostToolUse` hook on `Edit|Write` in `.claude/settings.json`, so the project shares it through git. The hook fires after each file edit, runs `rules` in short form over the edited file, and returns the output as added context the model reads beside its edit.

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": ".claude/hooks/litespec-rules.sh"
          }
        ]
      }
    ]
  }
}
```

Write the script at `.claude/hooks/litespec-rules.sh` and mark it executable. It reads the absolute file path from the hook JSON on standard input, runs the short `rules` form without `--full`, and prints the output back as `additionalContext`. It exits silently when the path is missing or `rules` fails, such as for a file outside the consumer.

```bash
#!/bin/sh
input=$(cat)
path=$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')
if [ -z "$path" ]; then exit 0; fi
output=$(litespec rules "$path" 2>/dev/null) || exit 0
if [ -z "$output" ]; then exit 0; fi
jq -n --arg ctx "$output" '{hookSpecificOutput: {hookEventName: "PostToolUse", additionalContext: $ctx}}'
```

After an edit to the Claimwick SubmitExpense command handler, the hook returns the provisions that govern that file, so the agent corrects the handler shape before moving on. The [Rules Query](../tools/rules.md) page states the short form: judgment statements grouped by strength, checked provisions as one line each, and concern-wide provisions by identifier.

### Gate the Stop in Claude Code

Add a `Stop` hook that runs `model check --since <base>` over the change set. When the check reports a blocking finding, the hook returns the output as added context so the agent continues the turn and reads the findings. When the check is clean, the hook exits silently and the turn ends.

```json
{
  "hooks": {
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": ".claude/hooks/litespec-stop.sh"
          }
        ]
      }
    ]
  }
}
```

Write the script at `.claude/hooks/litespec-stop.sh` and mark it executable. It stands down when `stop_hook_active` is already set, so the hook continues the turn at most once. It compares against `main` below; use the branch the team compares against instead.

```bash
#!/bin/sh
input=$(cat)
active=$(printf '%s' "$input" | jq -r '.stop_hook_active // false')
if [ "$active" = "true" ]; then exit 0; fi
output=$(litespec model check --since main 2>&1) || {
  jq -n --arg ctx "$output" '{hookSpecificOutput: {hookEventName: "Stop", additionalContext: $ctx}}'
  exit 0
}
exit 0
```

A `Stop` hook times out after 60 seconds; set `timeout` on the hook entry when the check needs longer.

### Remind Each Edit in OpenCode

Write `.opencode/plugins/litespec-rules.ts`. OpenCode loads each file under `.opencode/plugins/` as a plugin. The setup registers an `execute.after` tool hook that keeps failed tools and tools other than edit and write out, runs `rules` in short form over the edited path, and appends the output to the tool result the model reads. A failed `rules` run resolves to empty text, so a broken CLI never breaks the session.

```ts
import { execFile } from "node:child_process";
import { Plugin } from "@opencode/plugin";

function run(command: string, args: string[], cwd: string): Promise<string> {
  return new Promise((resolve) => {
    execFile(command, args, { cwd, timeout: 60000 }, (error, stdout) => {
      resolve(error ? "" : stdout.trim());
    });
  });
}

function withRules(result: unknown, rules: string): unknown {
  if (typeof result === "string") {
    return result + "\n\n" + rules;
  }
  if (result && typeof result === "object" && !Array.isArray(result)) {
    const record = result as Record<string, unknown>;
    const content = record.content;
    if (typeof content === "string") {
      return { ...record, content: content + "\n\n" + rules };
    }
    if (Array.isArray(content)) {
      return { ...record, content: [...content, { type: "text", text: rules }] };
    }
    return { ...record, content: rules };
  }
  return result;
}

export default Plugin.define({
  id: "litespec-rules",
  async setup(ctx) {
    const directory = ctx.location.directory;
    await ctx.tool.hook("execute.after", async (event) => {
      if (event.status !== "completed") {
        return;
      }
      const tool = String(event.tool ?? "").toLowerCase();
      if (tool !== "edit" && tool !== "write") {
        return;
      }
      const input = (event.input ?? {}) as Record<string, unknown>;
      const path = input.path ?? input.filePath ?? input.file_path;
      if (typeof path !== "string" || path.length === 0) {
        return;
      }
      const rules = await run("litespec", ["rules", path], directory);
      if (rules.length > 0 && event.result) {
        event.result = withRules(event.result, "LiteSpec rules for the edited file:\n" + rules);
      }
    });
  },
});
```

On OpenCode 1, the same logic lives in the `tool.execute.after` entry of the object the plugin returns. It reads the tool from `input.tool` and appends the `rules` output to `output.output` when that is a string.

```ts
import { execFile } from "node:child_process";
import { type Plugin } from "@opencode-ai/plugin";

export const LitespecRules: Plugin = async () => {
  return {
    "tool.execute.after": async (input, output) => {
      const tool = String(input.tool ?? "").toLowerCase();
      if (tool !== "edit" && tool !== "write") {
        return;
      }
      const args = (output.args ?? {}) as Record<string, unknown>;
      const path = args.path ?? args.filePath;
      if (typeof path !== "string" || path.length === 0) {
        return;
      }
      const rules: string = await new Promise((resolve) => {
        execFile("litespec", ["rules", path], (error, stdout) => {
          resolve(error ? "" : stdout.trim());
        });
      });
      if (rules.length > 0 && typeof output.output === "string") {
        output.output += "\n\nLiteSpec rules for the edited file:\n" + rules;
      }
    },
  };
};
```

### Check the Change Set in OpenCode

OpenCode has no stop hook, so the end-of-turn check stays a manual step. Run `litespec model check --since main` from the consumer root before reporting done, replacing `main` with the branch the team compares against.

## Check

- Edit one consumer file and confirm the agent sees the `rules` output for that file beside the edit result.
- Run `litespec rules <path>` on the same file by hand and confirm the text matches what the hook or plugin returned.
- Run `litespec model check --since main` from the consumer root: with no blocking finding the Stop hook stays silent, and with one the turn continues with the check output.
