# Generate the Agent Protocol

The agent protocol is the block in a project's `AGENTS.md` file that tells an AI coding agent how to use LiteSpec. It names the commands to run, the order to follow and the rules that an agent cannot guess. This decision generates that block from one template, so every copy matches the release that the project uses.

## Context

The consumer protocol lives in several hand-written copies. The site builds its own block, `init` writes its own template, and `llms.txt` embeds the maintainer file. The copies drift apart, and an agent follows a stale copy even when the code has changed.

The protocol also names context that no command produces. It offers 2 entry points with no order between them, and it leaves several lines uncited.

## Options

LiteSpec compared 2 options for the agent protocol. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Generate the protocol from one template, and report drift | One template change reaches a fresh project, an existing one, the selection page and `llms.txt`, and a hand edit inside the region fails the check. | Every renderer fills the same placeholders, and the region must fit a line budget. |
| Keep a hand-written protocol in each place | Each place words its own copy. | The copies drift apart, and a stale copy misleads the agent. |

## Decision

One template generates the protocol everywhere: `init`, `model render`, the selection page and `llms.txt`. The generated region opens with a sentence that says tool output and records win over the file. It holds the commands and the rules that an agent cannot guess, in 40 lines or fewer.

`model check` reports drift when the region differs from the generated text, and it reports a missing region. An agent follows this order:

1. Query the model for the records with `model query`.
2. Run `rules` for each file to edit.
3. Read the pages that `rules` cites.
4. Read the neighboring code.

## Consequences

`templates/consumer/project-agents.md` is the only source of the consumer protocol. Its LiteSpec part sits between `<!-- generated:agent-protocol -->` fences. The `## Project Commands` part stays outside the region.

`init` writes the region into a fresh `AGENTS.md`, and it inserts the region into an existing file that holds none. `init` never touches text outside the region. `model render` rewrites the region from the carried release and the project file.

`model check` reports `agent-protocol-drift` when the region differs from what `render` writes, and it reports `agent-protocol-missing` when `AGENTS.md` holds no region. Both findings are at level S. The site renders the same template for the chosen selection, and `llms.txt` embeds the filled template under Agent Protocol.

`litespec/core/agent#keep-the-agent-protocol-current` holds the obligation. `litespec/core/agent#select-task-context` states that the agent queries the model for the records and reads the output. The `rules` lines cite `litespec/core/agent#load-applicable-provisions-before-editing`. `validate` reports `TEMPLATE_CITATION_UNKNOWN` for a citation that resolves to no provision.

## Revisit Conditions

This decision reopens when a selection that the project file holds cannot be expressed in the template, or when a renderer needs a placeholder that no other renderer can fill.

Either case leaves one renderer with output that the others cannot produce. The template then cannot stay the only source of the protocol.
