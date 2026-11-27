# Agent Hooks and MCP

Keep the governing provisions in front of the agent: after each file edit the agent sees the short `rules` output for that file, and before it stops the model check runs over its change set. An agent that loads the provisions once at the start of a task works from memory by its later edits, and memory drifts from the standard as the session grows.

This guide wires the hooks through `litespec agent sync` and `litespec hook`. Run the sync once per consumer; it writes the path-scoped rule files and the Claude settings beside them. This guide adds no rule of its own.

## Prerequisites

Start with:

- The `litespec` command from [Install the CLI](install-the-cli.md). The recipes spell it `litespec`, and a local .NET tool install spells it `dotnet litespec`.
- A consumer root holding `.litespec/project.yaml`, because both commands resolve the project from the working directory.
- `git`, because `hook pre-stop` scopes the change set from git.
- No extra install for the OpenCode plugin, which runs inside the OpenCode server and calls the CLI through `node:child_process`.

Read the [Rules Query](../tools/rules.md) and [Model Builder](../tools/model.md) pages before this guide, because they own the model the hooks read. The hooks automate `litespec/core/agent#load-applicable-provisions-before-editing`, which asks the agent to run `rules <path>` before editing a file and to answer each judgment provision `rules --since <base>` lists before reporting done. Read the [Agent Sync](../tools/agent-sync.md) and [Hook](../tools/hook.md) pages for the commands this guide installs.

## Procedure

Run `litespec agent sync` from the consumer root first. It writes the thin root `AGENTS.md`, one path-scoped rule file per selected stack for Claude, Cursor, and GitHub, the shared record-writing and route files, and the hook entries in `.claude/settings.json` below. Commit every file it writes, so the project shares them through git. Re-run it after changing the stacks or the extensions.

### Remind Each Edit in Claude Code

`agent sync` writes this `PostToolUse` hook on `Edit|Write` into `.claude/settings.json`, so the project shares it through git. After each file edit the hook feeds the hook JSON to `litespec hook post-edit`, which returns the judgment provisions and the open findings for that file as added context the model reads beside its edit. For example, after an edit to the Claimwick SubmitExpense command handler, the hook returns the provisions that govern that file, so the agent fixes the handler shape before moving on.

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "litespec hook post-edit"
          }
        ]
      }
    ]
  }
}
```

`post-edit` prints at most 40 lines and always exits 0, so a file outside the consumer or a file with no governing provisions adds nothing and never breaks the session. The [Hook](../tools/hook.md) page describes the 40 lines: one judgment provision per line with its identifier, one collapsed line per record holding provisions about the file, and the open findings for it.

### Gate the Stop in Claude Code

`agent sync` writes this `Stop` hook into `.claude/settings.json` beside the entry above. It runs `litespec hook pre-stop` over the files changed since `HEAD`. When the change set carries a blocking finding, the command prints it and exits 2, so the agent continues the turn and reads the findings. When the change set is clean, the command prints one line saying so and exits 0, and the turn ends. For example, a turn that leaves a missing mark gets the finding text back and keeps working; a clean turn ends.

```json
{
  "hooks": {
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "litespec hook pre-stop"
          }
        ]
      }
    ]
  }
}
```

A `Stop` hook times out after 60 seconds, so set `timeout` on the hook entry when the check needs longer.

### Remind Each Edit in OpenCode

Write `.opencode/plugins/litespec-rules.ts`. OpenCode loads each file under `.opencode/plugins/` as a plugin. The setup registers an `execute.after` tool hook that skips failed tools and tools other than edit and write, runs the short `rules` form over the edited path, and appends the output to the tool result the model reads. For example, the Claimwick handler edit returns its governing provisions beside the edit result. A failed `rules` run resolves to empty text, so a broken CLI never breaks the session.

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
      return { ...record, content: content + [...content, { type: "text", text: rules }] };
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

OpenCode has no stop hook, so the end-of-turn check stays manual. Run `litespec hook pre-stop` from the consumer root before reporting done. It prints the blocking findings on the files changed since `HEAD` and exits 2 while any stands; with none it prints one line and exits 0. For example, the Claimwick turn ends only after the pre-stop run reports no blocking finding.

The hooks above and the MCP service are independent pieces. The [AI and Model Context Protocol](../ext/ai-mcp.md) extension separately covers a shipped MCP server, a local process that an LLM client starts on the user's own machine so the model can call the product's public API. Install the recipes on this page whether or not your project ships that server.

### Serve the Warm Model to Agents

When an agent works through the MCP tools instead of the command line, run `litespec serve` from the consumer root. It loads the project model once, keeps it warm across edits, and answers the four agent tools: the task packet, the open findings, the impact of a record, and the slice verification of one use case. The [Serve](../tools/serve.md) page states the tools and their shapes.

## Check

- Run `litespec agent sync --check` from the consumer root and confirm it reports no file would change.
- Edit one consumer file and confirm the agent sees the `hook post-edit` output for that file beside the edit result.
- Run `litespec hook pre-stop` from the consumer root. With no blocking finding it prints one line and exits 0, and with one it prints the findings and exits 2.
- Start `litespec serve` from the consumer root and confirm `tools/list` answers the four agent tools.
