# Generate the Agent Protocol

## Context

The consumer protocol lived in several hand-written copies: the site built its own block, `init` wrote its own template, and `llms.txt` embedded the maintainer file. The copies drifted apart, and a stale copy beat working code. The protocol named context no command produced, offered two entry points with no order, and left several lines uncited.

## Decision

One template generates the protocol everywhere: `init`, `model render`, the selection page and `llms.txt`. The generated region opens with the precedence sentence, carries commands and the rules an agent cannot guess within 40 lines, and `model check` reports drift or a missing region. The agent queries the model for the records, runs `rules` for each file to edit, reads the pages `rules` cites, then reads neighboring code. Every citation in the template resolves to a provision, and `validate` reports one that does not.
