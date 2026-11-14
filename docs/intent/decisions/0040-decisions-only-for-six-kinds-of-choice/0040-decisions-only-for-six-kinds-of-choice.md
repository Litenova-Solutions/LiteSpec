# Require Decisions Only for Six Kinds of Choice

A decision record states one technical choice, such as which provider or which package a project uses, and why the team made it. This decision limits technical decision records to expensive-to-reverse choices of 6 kinds. Standing rules that every change follows move to conventions, so a reader meets only the choices that matter.

## Context

One large consumer has 74 decision records. The review keeps 45 of them and sets aside 29: 16 restate a provision that already makes the choice, 6 describe the inside of one class, 6 fold into the decision that absorbs them, and 1 is a business choice that belongs in a requirement.

The same consumer keeps 32 provision-shaped conventions outside `.litespec/governance/conventions/`, so `rules` returns none of them. The book lists decisions as one flat list, with no index by kind or concern.

## Options

LiteSpec compared 3 options for which choices get a decision record. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Record a technical decision only for a choice of one of 6 kinds, and carry standing rules as conventions | A reader meets only expensive-to-reverse choices, each with its kind, its followers and its reopen conditions. | Each existing technical decision needs a kind, and each standing rule moves to the conventions folder. |
| Keep a decision for every technical choice that code follows | No record moves, and each technical class keeps a decision to point at. | Decisions restate provisions and describe single classes, and no rule separates expensive choices from local notes. |
| Group decisions in folders by category | A reader opens one folder per category. | A decision often fits several categories, and each recategorization churns the history. |

## Decision

A technical decision records only an expensive-to-reverse choice of one of 6 kinds: `provider`, `package`, `format`, `boundary`, `topology` or `replacement`. A choice of no kind states its reason in the class documentation.

A standing rule that every change follows is a convention under `.litespec/governance/conventions/`. A `follows` mark can cite that convention.

Categories live in fields and in generated indexes, not in folders. The concern of a decision is computed from the concerns of its followers. `model render` writes the decision and departure indexes. The book groups decisions by kind, then by concern. Claimwick renders each receipt as a PDF file, so `decision/0005-render-receipts-as-pdf` carries `kind: package`.

## Consequences

A technical decision carries `kind`. The field is optional, and a technical decision without one reports `decision-kind-missing`.

`litespec/core/model#record-each-technical-choice-as-a-decision` requires a decision only for a choice of one of the 6 kinds. `litespec/core/agent#search-the-provisions-before-recording-a-decision` asks an agent to query `rules` for the subject first. `litespec new decision <name>` prints the provisions whose statement matches the name before it writes.

A technical decision whose followers all sit in one module, and each of which already carries an `implements` mark, reports `decision-scope-invalid`. A `follows` or `proves` mark may name a project convention as `<project>/conventions#<slug>`, which resolves through the loaded conventions. An unknown convention reports `mark-unknown`.

A Markdown file under `.litespec/` outside `governance/conventions/` that carries a convention `**ID:**` line reports `convention-folder-invalid`. A convention's `Applies to` takes a path glob or the scope keys that provisions use. Anything else reports `convention-scope-invalid`.

Folders carry ownership and layer only. `model render` writes a `decision-index` region grouped by kind, then by concern. It writes a `departure-index` region grouped by gap or permanent and ordered by `expiresOn`. The book groups its decision chapter the same way, lists the followers of each decision by module, and shows the provider decision on each external-system page.

A decision kept at a level that its followers do not match reports `decision-home-drift`. Every new finding is advisory. `templates/consumer/decision.yaml` carries `kind`, and each Claimwick technical decision states its kind.

## Revisit Conditions

This decision reopens when a technical choice outside the 6 kinds proves expensive to reverse.

A choice of that kind would have no `kind` to carry it, so it would fall back to a note in the class documentation. The record would then hide a choice that is hard to undo.
