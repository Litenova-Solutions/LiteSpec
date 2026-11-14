# Decision Records

## Intent

A decision record pins one expensive-to-reverse choice down with the options weighed and the evidence cited, so a later team sees why the product looks the way it does instead of relitigating it. A technical decision pins a choice of one of six kinds: provider, package, format, boundary, topology, or replacement. This page states where each record lives in the consumer repository (the application repository built against one pinned LiteSpec release), what fields and explanation sections it holds, and how a record changes: in place, with git keeping the history.

## Reference

The [Intent layer](../core/intent.md) states what a decision record means. The [model page](../core/model.md) states every home a decision can take, and the [scaffold command](../tools/new.md) states the command's full syntax. This page states the home the command writes and what the record holds.

### Where a Record Lives

Each record is one record file in its own folder, with its explanation file beside it in that folder, in the consumer repository. `litespec new decision <name>` scaffolds both files in that folder and reserves the identifier; it takes a kebab-case name without a number and allocates the next free number from the one decision sequence. Before it writes the files, it prints the provisions whose statement matches the name's words, so a choice a provision already makes follows that provision instead of becoming a decision. Before recording a decision, query `rules` for its subject; where a provision makes the choice, the code follows that provision instead, as `litespec/core/agent#search-the-provisions-before-recording-a-decision` states. A technical decision lives under `.litespec/implementation/decisions/` or `.litespec/implementation/modules/<m>/decisions/` instead, as the [model page](../core/model.md) states.

| Item | Value |
|:---|:---|
| Path | `.litespec/intent/decisions/<number>-<name>/<number>-<name>.yaml` with `.litespec/intent/decisions/<number>-<name>/<number>-<name>.md` beside it in the consumer repository |
| Record template | `templates/consumer/decision.yaml` |
| Explanation template | `templates/consumer/decision.md` |
| Record type | `decision` |

### Identifier Form

The identifier is `decision/<number>-<name>`, such as `decision/0001-receipt-threshold`. The name states the choice in words, in at most six words, and the number counts as one of those words. `litespec new decision` allocates the next free number from one sequence across every home. The number keeps proposal order only: it orders records by when they were proposed and carries no other meaning.

### What a Record Contains

The explanation file opens with one paragraph, then carries these sections, in order:

| Section | What it states |
|:---|:---|
| Context | The situation that forced a choice, in the past tense. |
| Options | Each real option compared on the same points, with what speaks for it and against it. |
| Decision | The selected option. |
| Consequences | What follows from the choice for the product, the team and the code. |
| Evidence | The research and constraints the choice rests on, each with its confidence; the section is optional. |
| Revisit Conditions | The condition that would reopen the choice, or that no known condition reopens it. |

The record file carries the fields tools read: `options` with one entry per weighed option (`name`, `pros`, `cons`), `choice` with the selected option, `reopenedWhen` with what would overturn the choice, and `consequences` with the required work and the accepted cost. It also carries `decides` with the records this choice sets and `basedOn` with the constraints and research behind it. A technical decision also carries `kind`, one of the six kinds the next section names. A product decision carries no `kind`. The field is optional: a technical decision with no `kind` is reported as `decision-kind-missing`, and a choice of no kind is not a decision. A record present in git is decided.

### The Six Kinds

Each technical decision takes one kind, which is the category and the filter at once.

| Kind | Meaning |
|:---|:---|
| `provider` | An outside service. |
| `package` | A third-party package outside the manifest baseline. It pairs with `admits`. |
| `format` | A stored, published, or wire shape, such as a token layout or a document serialization. |
| `boundary` | A security or trust boundary. |
| `topology` | A deployment or hosting shape. |
| `replacement` | The project takes another way than a manifest baseline. |

A technical choice of none of these kinds states its reason in the class documentation, and the mark on the class names the record the class serves. A technical decision whose followers all sit in one module, and each already carry an `implements` mark, is reported as `decision-scope-invalid`: the reason belongs in the class documentation instead.

### Computed Concern

A technical decision takes no written category. Its concern is the set of concerns of its followers' paths, using the classification `rules` applies: `domain`, `application`, `infrastructure`, `web-api`, `frontend-ui`, `frontend-data`, `tests`, `workspace` and `operations`. Nobody writes the concern, so it cannot drift. Claimwick follows `decision/0005-render-receipts-as-pdf` from its receipt renderer, so the decision carries concern `application`.

### The Decision Index

`litespec model render` writes a `decision-index` region into the README of each decisions folder that carries the fence. The region holds one table grouped by `kind`, then by concern. Each row holds the number and title, the choice in one line, the follower count with the modules the followers sit in, `reopenedWhen`, and a marker for `decision-unimplemented`, `decision-basis-missing` and `decision-scope-invalid`. The region lists only the decisions in its own folder. Claimwick reads its topology choice in `.litespec/implementation/decisions/README.md` and its package choice, which only the Spending module follows, in `.litespec/implementation/modules/spending/decisions/README.md`.

### Decisions and Conventions

A decision chooses between options at one moment, and its `reopenedWhen` names what would reopen it. A convention is a standing rule every change follows, and its `Reviewed for` question names how a reviewer finds a breach. A convention states a judgment rule, never a list of words: product language lives in the glossary, where the checks read it. A convention lives under `.litespec/governance/conventions/`, where `rules` reads it, and code cites it in a `follows` or `proves` mark as `<project>/conventions#<slug>`. Its `Applies to` takes a path glob or the scope keys provisions use, such as `stack: dotnet; artifact: command-handler`.

### Changed in Place

A decision record changes in place: a new decision edits the record, and git keeps the earlier text. Two decisions deciding the same record are a blocking finding, not a tie to resolve. A departure from a shared standard is a departure record rather than a decision.

## Notes

The outside conventions for this artifact are [Nygard's architecture decision record](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) and [MADR](https://adr.github.io/). Both record context, decision, and consequences in one record per choice, and this record keeps that shape while git keeps the history.
