# Scaffold Explicit Files and Sync Pure Data

LiteSpec creates code with two small commands that do different jobs. Scaffold writes the normal files a person owns, and sync writes data files a tool can rewrite.

## Context

Partial-file codegen split each use case across normal files and generated partial twins. A rename touched two files, and a review read halves.

The generator code grew past 6,000 lines while the output still needed hand edits after every run. Owners could not tell which half held the truth.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Scaffold writes normal files once, sync writes pure data on demand | Each use case lives in one explicit file a person can read and edit. Generated output carries data only, so reruns never fight hand edits. | Two commands to learn, and templates to keep for each stack. |
| Keep partial-file codegen with generated twins | One command writes both halves, and no new workflow to teach. | Every use case ships as two halves that drift, and the generator stays large. |
| Hand-write every file with no tool | No generator to keep and no output to review. | Every consumer repeats the same wiring by hand, and shared fixes never land everywhere. |

## Decision

The command `litespec scaffold` writes one explicit normal file per use case that the owner edits freely.

The command `litespec sync` writes pure-data files such as refusal codes, permission lists, and contracts. It never writes a partial twin beside a hand-edited file.

## Consequences

No `*.generated.cs` file ships in any stack. A Claimwick expense approval use case shows the shape: one explicit handler file the team owns, plus synced data files the tool rewrites on demand.

Scaffold templates live per stack, and each template is copied from a Claimwick file. Sync output carries version-free names so reruns do not churn file names.

## Revisit Conditions

Reopen this when a stack needs output that pure data cannot carry, or when scaffold templates drift from the records they were copied from.
