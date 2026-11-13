---
name: litespec
description: Use when working in a repository that holds `.litespec/project.yaml`, for editing code, records, tests, or UI, for citing or looking up a LiteSpec provision, or for writing the completion report. Guides the agent through the LiteSpec task order, which reads the project agent protocol region, queries the model, asks the project tool for the provisions that govern each file, and reports how each was judged.
metadata:
  generated-by: litespec
  litespec-release: 5.3.0
---

# LiteSpec Skill

This skill guides an agent through work in a repository that follows LiteSpec, the contract for one bounded-context business application. It states the order of a task, not the rules: the project's own tool states the rules.

When this skill disagrees with `litespec rules`, `litespec model query`, or a record, the tool output and the record win; this skill only points to them.

Below, `litespec` stands for the project's command form. Read the form (`dotnet litespec` or `litespec`) and the release from the agent protocol region in the project's `AGENTS.md`, and use that form for every command.

## Before the Task

Read the agent protocol region in the project's `AGENTS.md`. It names the release, the size, the shape, the stacks, the extensions, and the record layers, and it points to the selection index for the pages the task may touch.

Read `.litespec/project.yaml`. It states what the project claims: its size, shape, stacks, extensions, and layers.

Read `git status`, and leave unrelated work alone.

## During the Task

Query the model for each record the task touches, and follow its links before changing behavior it describes:

```bash
litespec model query <record-id>
```

Run `litespec rules <path>` for each file before editing it. Read the pages `rules` cites, then look at neighboring code.

Read one provision in full when a statement is not enough:

```bash
litespec rules <provision-id>
```

When two provisions conflict and nothing decides between them, stop. Quote both provision IDs and ask for a decision.

Never delete, skip, or weaken a test that proves a scenario, and never change its expected values unless the scenario changed in the same change. Report a test you believe is wrong instead of editing it.

See `references/commands.md` for every command form.

## Before Reporting

Run the checks over the change set, with `<base>` naming the commit the change starts from:

```bash
litespec model check --since <base>
litespec rules --since <base>
```

Answer each judgment provision `rules --since <base>` lists. It prints a report skeleton with one Reviewed for question and one empty How judged line per provision.

Write the completion report in the shape `references/report.md` states.

## References

- `references/commands.md`: the command forms this skill names.
- `references/report.md`: the completion report shape.
