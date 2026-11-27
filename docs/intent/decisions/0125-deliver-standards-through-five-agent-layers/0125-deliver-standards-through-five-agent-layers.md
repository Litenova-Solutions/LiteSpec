# Deliver Standards Through Five Agent Layers

LiteSpec meets agents where they work, in small pieces. No single call carries the whole standard, and no agent grades its own work.

## Context

Decision 0038 narrowed rules output per file and change set. Agents attested their own checklists with `rules --since`.

The output stayed large, the attestation proved nothing, and token cost grew with every edit. Each call re-sent the standard instead of the slice the edit needed.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Deliver the standard through five layers with independent verification | Agents carry small context per call. Deterministic layers enforce what prose cannot. An independent check judges the result. | Five layers to keep: types, analyzers, hooks, MCP, and verification. |
| Keep narrowed rules output with self-attested checklists | One command to keep, and each change prints its own report skeleton. | Large output per call, and a checklist the agent fills itself proves nothing. |
| Send the full standard on every call | Nothing new to build. | Token cost grows with every edit, and agents skim instead of reading. |

## Decision

LiteSpec reaches agents through five layers. Typed record shapes and templates carry the contract.

Roslyn analyzers report violations in the editor. Deterministic hooks run checks on file events. A warm daemon serves a 4-tool MCP surface (`task_packet`, `check`, `verify`, `query`) with path-scoped rules per call.

## Consequences

Independent behavioral and semantic verification judges the result instead of the agent attesting itself. A Claimwick expense change shows the flow: the agent pulls a task packet, edits with hooks watching, and verification confirms the behavior.

This decision supersedes `decision/0038-narrow-rules-output-per-file-and-change-set`: where that decision narrowed one output, this one splits delivery across five layers and verifies independently.

## Revisit Conditions

Reopen this when measured token cost or defect rate shows a layer earns nothing, or when a new model generation removes the need for one layer.
