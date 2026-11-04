# Decision Records

## Intent

A decision record pins one expensive-to-reverse choice (a data model, a provider, a security boundary) down with the options weighed and the evidence cited, so a later team sees why the product looks the way it does instead of relitigating it. This page states where each record lives in the consumer repository (the application repository built against one pinned LiteSpec release), what fields and explanation sections it holds, and how a record changes: in place, with git keeping the history.

## Reference

The [product page](../core/intent.md) states what a decision record means; this page states where it lives and what it holds.

### Where a Record Lives

Each record is one record file with its explanation file beside it in the consumer repository. Start it with `litespec new decision <number>-<name>`, which scaffolds both files and reserves its identifier, then fill in the record fields and the explanation sections below.

| Item | Value |
|:---|:---|
| Path | `.litespec/intent/decisions/<number>-<name>.yaml` with `.litespec/intent/decisions/<number>-<name>.md` beside it in the consumer repository |
| Record template | `templates/consumer/decision.yaml` |
| Explanation template | `templates/consumer/decision.md` |
| Record type | `decision` |

### Identifier Form

The identifier is `decision/<number>-<name>`, such as `decision/0001-receipt-threshold`. The name states the choice in words, in at most six words, and the number counts as one of those words. Decision numbers come from one sequence across every home, allocated by `litespec new decision`, which takes the next free number. The number keeps proposal order only: it orders records by when they were proposed and carries no other meaning.

### What a Record Contains

The explanation file carries three sections:

| Section | What it states |
|:---|:---|
| Context | The concrete constraint or conflict forcing a choice. |
| Decision | The selected behavior. |
| Evidence | The research and constraints the choice rests on, each with its confidence. |

The record file carries the fields tools read: `options` with one entry per weighed option (`name`, `pros`, `cons`), `choice` with the selected option, `reopenedWhen` with what would overturn the choice, and `consequences` with the required work and the accepted cost. It also carries `decides` with the records this choice sets and `basedOn` with the constraints and research behind it. A record present in git is decided.

### Changed in Place

A record changes in place: a new decision edits the record, and git keeps the earlier text. Two decisions deciding the same record are a blocking finding, not a tie to resolve. A departure from a shared standard is an exception of kind `departure` rather than a decision.

## Notes

The outside conventions for this artifact are [Nygard's architecture decision record](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) and [MADR](https://adr.github.io/). Both record context, decision, and consequences in one record per choice, and this record keeps that shape while git keeps the history.
