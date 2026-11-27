# Decision Records

A decision record pins one expensive-to-reverse choice with the options weighed and the evidence cited, so a later team sees why the product looks the way it does. A technical decision pins a choice of one of six kinds: provider, package, format, boundary, topology, or replacement. This page lists where each record lives in a consumer repository, which fields and explanation sections it holds, and how a record changes, with git keeping the history.

## Reference

The [Intent layer](../core/intent.md) defines what a decision record means. The [model page](../core/model.md) lists every home a decision takes, and the [scaffold command](../tools/new.md) documents the full command syntax. What follows is the home the command writes with what the record holds.

### Where a Record Lives

Each decision is one record file in its own folder in the consumer repository, with its explanation file beside it in the same folder. `litespec new decision <name>` writes both files there and reserves the identifier. The command takes a kebab-case name with no number, and allocates the next free number from the one decision sequence shared across every home.

The rules come first: where a provision settles the choice, the code follows that provision instead of recording a decision, as `litespec/core/agent#search-the-provisions-before-recording-a-decision` states. Before writing the files, the command prints the provisions with statements matching the words of the name.

A technical decision takes its home under `.litespec/implementation/decisions/` or `.litespec/implementation/modules/<m>/decisions/` instead, as the [model page](../core/model.md) states.

The [record folders](folders.md) state every home a decision takes. The record starts from `templates/consumer/decision.yaml` and its explanation from `templates/consumer/decision.md`.

### Identifier Form

The identifier reads `decision/<number>-<name>`, such as `decision/0001-receipt-threshold`. The name states the choice in at most six words, and the number counts as one of them. `litespec new decision` allocates the next free number from the one sequence shared across every home. The number keeps proposal order only: it records when each record was proposed, and carries no other meaning.

### What a Record Contains

The explanation file opens with one paragraph, then these sections in order:

| Section | What it states |
|:---|:---|
| Context | The situation that forced a choice, in the past tense. |
| Options | Each real option compared on the same points, with what speaks for it and against it. |
| Decision | The selected option. |
| Consequences | What follows from the choice for the product, the team and the code. |
| Evidence | The research and constraints the choice rests on, each with its confidence; the section is optional. |
| Revisit Conditions | The condition that would reopen the choice, or that no known condition reopens it. |

The record file holds the fields tools read. `options` carries one entry per weighed option (`name`, `pros`, `cons`), `choice` carries the selected option, `reopenedWhen` carries what reopens the choice, and `consequences` carries the work to do with the cost accepted.

It also carries `basedOn`, naming the constraints and research behind it. The records the choice settles name the decision in their own `basedOn`, so the decision lists no followers itself. It carries `supersedes`, naming the `decision/...` identifiers this decision replaces; the link reads `supersedes` and reads back `is superseded by`. A technical decision also carries `kind`, one of the six kinds the next section lists; a product decision carries no `kind`. A technical decision with no kind is reported as `decision-kind-missing`, since a choice of no kind is no decision. A record present in git counts as decided.

### Decision Kinds

Each technical decision takes exactly one kind. The kind labels the category and filters the index at once.

| Kind | Meaning |
|:---|:---|
| `provider` | An outside service. |
| `package` | A third-party package outside the manifest baseline. It pairs with `admits`. |
| `format` | A stored, published, or wire shape, such as a token layout or a document serialization. |
| `boundary` | A security or trust boundary. |
| `topology` | A deployment or hosting shape. |
| `replacement` | The project takes another way than a manifest baseline. |

A technical choice outside these six kinds keeps its reason in the class documentation, with the mark on the class naming the record the class serves. A technical decision with every follower in one module, where each follower already carries an `implements` mark, is reported as `decision-scope-invalid`. The reason belongs in the class documentation there.

### Computed Concern

No person writes a concern on a technical decision, so the value stays exact. It is the set of concerns of its followers' paths, using the classification `rules` applies (`domain`, `application`, `infrastructure`, `web-api`, `frontend-ui`, `frontend-data`, `tests`, `workspace`, and `operations`). Claimwick follows `decision/0005-render-receipts-as-pdf` from its receipt renderer, so that decision carries concern `application`.

### Decision Index

`litespec model render` writes a `decision-index` region into the README of each decisions folder carrying the fence. The region holds one table, grouped by `kind` and then by concern, listing only the decisions in its own folder. Each row shows the number and title, the choice in one line, the follower count with the modules holding the followers, `reopenedWhen`, and a marker for `decision-unimplemented`, `decision-basis-missing`, and `decision-scope-invalid`. Claimwick lists its topology choice in `.litespec/implementation/decisions/README.md`, and its package choice, followed only by the Spending module, in `.litespec/implementation/modules/spending/decisions/README.md`.

### Decisions and Conventions

A decision settles options at one moment, and its `reopenedWhen` names what reopens it. A convention is a standing rule every change follows, and its `Reviewed for` question tells a reviewer how to spot a breach. A convention states a judgment rule about what holds, not a word list: product language lives in the glossary, where the checks read it.

A convention lives under `.litespec/governance/conventions/`, where `rules` reads it, and code cites it in a `follows` or `proves` mark as `<project>/conventions#<slug>`. Its `Applies to` takes a path glob or the scope keys provisions use, such as `stack: dotnet` with `artifact: command-handler`.

### Changed in Place

A decision record changes in place: a new decision edits the record, and git keeps the earlier text. Several decisions decide one record: a retention decision and a rounding decision both decide `aggregate/spending.expense`. A decision replacing another names it in `supersedes`; the superseded decision stays in the repository as context, and the book shows it as superseded, computed from the inverse link. Setting a shared standard aside calls for a waiver record instead of a decision.

## Notes

This page follows two outside conventions: [Nygard's architecture decision record](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) and [MADR](https://adr.github.io/). Both keep context, decision, and consequences in one record per choice. This record keeps that shape and lets git keep the history.
