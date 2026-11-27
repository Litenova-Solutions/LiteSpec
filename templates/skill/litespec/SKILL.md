---
name: litespec
description: Use when working in a repository that holds `.litespec/project.yaml`, for editing code, records, tests, or UI, for citing or looking up a LiteSpec provision, or for writing the completion report. Guides the agent through the LiteSpec task order, which reads the project agent protocol region, works through the warm model service, asks the project tool for the provisions that govern each file, and reports how each was judged.
metadata:
  generated-by: litespec
__LITESPEC_RELEASE_METADATA__
---

# LiteSpec Skill

This skill guides an agent through work in a repository that follows LiteSpec, the contract for one bounded-context business application. It states the order of a task, not the rules themselves; the project's own tool states the rules. That order is: read the project's protocol, work through the warm model service, ask the project tool for the provisions governing each file, and report how each was judged.

Tool output and records outrank this skill: when it disagrees with `litespec serve`, `litespec rules`, `litespec model query`, or a record, those win. This skill only points to them.

Below, `litespec` stands for the project's command form. Take the form (`dotnet litespec` or `litespec`) and the release from the agent protocol region in the project's `AGENTS.md`, and use that form for every command.

## Before the Task

Read the agent protocol region in the project's `AGENTS.md`. It names the release, the size, the shape, the stacks, the extensions, and the record layers, and points to the path-scoped rule files the task may touch.

Read `.litespec/project.yaml`, which states what the project claims: its size, shape, stacks, extensions, and layers.

Check `git status`, and leave unrelated work alone.

Prefer the warm model service over repeated shell calls. Start `litespec serve` once per session and call its four tools: `litespec_task_packet` for the packet before an edit, `litespec_check` for the open findings on touched paths, `litespec_impact` before renaming a record, and `litespec_verify_slice` after changing a use case. Each packet is capped at about 2,500 tokens, so read the packet rather than loading whole files around the target.

## During the Task

Read the packet for the target before editing it, and follow the record links the packet names before changing behavior they describe. Without the service, query the model directly:

```bash
litespec model query <record-id>
```

Search records by text with `litespec model query --search <text>`, list every record of a type with `litespec model query --list <type>`, and report the links between modules with `litespec model edges`.

Run `litespec rules <path>` for each file before editing it. Read the pages `rules` cites first, then look at neighboring code. The path-scoped rule files `litespec agent sync` writes already carry the judgment provisions for each stack; this skill does not restate them.

Read one provision in full when its statement alone does not settle the question:

```bash
litespec rules <provision-id>
```

Stop when two provisions conflict and nothing decides between them. Quote both provision IDs and ask for a decision.

Build only what the specification states. Never delete, skip, or weaken a test that proves a scenario, and never change its expected values unless the scenario changed in the same change. If you believe a test is wrong, report it instead of editing it.

Write new code with `litespec scaffold <use-case-id>`, which writes the files of one use case from its record and never overwrites an existing file. Write derived data with `litespec sync`, which copies the pure data the records determine into the configured targets. List conflict-free parallel slices with `litespec plan-waves`. Read pending release changes with `litespec upgrade --check`. Send changed docs to `litespec docs review --since <base>` and changed routes to `litespec ui review --since <base>`.

`references/commands.md` lists every command form.

## Before Reporting

Run the checks over the change set, where `<base>` names the commit the change starts from:

```bash
litespec model check --since <base>
litespec hook pre-stop
```

`pre-stop` prints the blocking findings on the files changed since `HEAD` and exits 2 while any stands. With no changed files, or no blocking finding on them, it prints one line saying so and exits 0.

Narrow a check to one module with `litespec model check --module <module>`; the exit code then follows that module's findings alone. Check only owned paths with `litespec model check --only-paths <paths...>`. Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run. Only a live waiver covers one.

Write the completion report in the shape that `references/report.md` states.

## References

- `references/commands.md`: every command form this skill names.
- `references/report.md`: the completion report shape.
