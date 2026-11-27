# LiteSpec Commands

The LiteSpec skill names several command forms, and this reference collects them in one place. Below, `litespec` stands for the project's command form from its `AGENTS.md` region: `dotnet litespec` or `litespec`.

## Query a Record

`litespec model query <record-id>` prints one record with its links in both directions, showing its identifier and its folder path. Query the record before changing behavior it describes, and query again whenever a link leads further.

```bash
litespec model query use-case/spending.submit-expense
```

## Search Records

`litespec model query --search <text>` lists the written records whose identifier, title, summary or statement holds the text, case-insensitive, with an exact identifier match first. With no match it prints nothing and exits 0.

```bash
litespec model query --search "receipt threshold"
```

## List the Records of a Type

`litespec model query --list <type>` lists every written record of the type, one line each with identifier, title and path. Adding `--module <module>` narrows the list to the records of that module.

```bash
litespec model query --list use-case --module spending
```

## List the Provisions for a File

`litespec rules <path>` lists the provisions governing a file or folder before it is edited. The target is a path under the consumer root or a record identifier. Add `--full` to print each provision with its rationale and example, or `--all` for the full list with checked finding codes.

```bash
litespec rules apps/api/src/Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

## Read One Provision

`litespec rules <provision-id>` prints one provision in full, with its strength, statement, rationale, example, and verification. It reads the carried release, so it works offline.

## Ask the Warm Model Service

`litespec serve` keeps the project model warm and answers four tools over stdio JSON-RPC, so an agent session asks the service instead of rerunning the checks on every question. Start it once per session:

```bash
litespec serve --root ../consumer
```

The four tools are `litespec_task_packet(target)`, `litespec_check(paths?)`, `litespec_impact(record_id)`, and `litespec_verify_slice(use_case_id)`. The packet names the records a path implements, the judgment provisions for it, and up to 20 neighboring files, capped at about 2,500 tokens.

## Scaffold One Use Case

`litespec scaffold <use-case-id> [--root <consumer>] [--diff]` writes the code files of one use case from its record and never overwrites an existing file. With `--diff` it prints each file it would create instead of writing.

```bash
litespec scaffold use-case/lending.borrow-copy --root ../consumer
```

## Sync Derived Data

`litespec sync [--root <consumer>] [--check]` writes the pure data the records determine into the configured targets. With `--check` it lists each file that would change and exits 1 when any would.

```bash
litespec sync --root ../consumer
```

## List Parallel Slices

`litespec plan-waves [--root <consumer>] [--format json]` prints the conflict-free parallel slices with their exclusive paths and the waves in dependency order. With `--format json` it prints one JSON object with the slices, the waves, and the frozen paths.

```bash
litespec plan-waves --root ../consumer
```

## Report Release Changes

`litespec upgrade --check --root <consumer> [--format json]` reports the release changes between the reviewed LiteSpec version and the carried release, from the embedded manifests. It exits 1 when changes apply.

```bash
litespec upgrade --check --root ../consumer
```

## Review Docs and Routes

`litespec docs review --since <ref> [--root <folder>]` lints the changed prose and writes a review packet. `litespec docs render --changed [--root <folder>]` re-renders only the pages whose record hash changed. `litespec ui review --since <ref> [--root <folder>]` lists the changed routes with their golden baselines and writes a review packet.

```bash
litespec docs review --since HEAD~1
litespec docs render --changed
litespec ui review --since HEAD~1
```

## Check the Change Set

`litespec model check --since <base>` reports the findings about records and code changed since `<base>`.

```bash
litespec model check --since origin/main
```

Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run, local or in the pull-request check. Only a live waiver covers one.

## Check One Module

`litespec model check --module <module>` prints only the findings on the records and files of that module. The exit code follows those findings alone.

```bash
litespec model check --module spending
```

## Report the Module Edges

`litespec model edges` prints each module's written record links to other modules, with counts per link kind and per module, followed by every pair of modules that link each other both ways. With `--module <module>` it prints only that module with the mutual pairs it belongs to. It is read-only.

```bash
litespec model edges
```

## Agent Hooks

The guide [Agent Hooks and MCP](https://litespec.dev/latest/guide/agent-hooks-and-mcp/) shows how the `PostToolUse` hook runs `litespec hook post-edit` after every edit and the `Stop` hook runs `litespec hook pre-stop` at the end of a turn. The same page as plain Markdown lives at `https://litespec.dev/latest/guide/agent-hooks-and-mcp.md`.
