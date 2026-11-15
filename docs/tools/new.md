# Scaffold Record

## Name

`litespec new` scaffolds a record file, including a requirement's folder, and reserves its identifier. It also appends a result or scenario entry to a use-case file, or appends a rule, permission, or setting entry to its owning list file.

## Synopsis

```bash
litespec new <type> <id> [--root <path>] [--help]
litespec new <document-style> <name> [--root <path>]
litespec new requirement <feature>.<name> [--root <path>]
litespec new requirement <name> [--root <path>]
litespec new decision <name> [--root <path>]
litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
litespec new use-case <id> --kind command|query [--root <path>]
litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
litespec new entry <page-id> <type> <name> [--root <path>]
```

## Description

Run this command before writing any new record, as `litespec/core/model#scaffold-pages-and-reserve-identifiers` states. Validate the new file before committing it. From the template for its type, the command creates one record file with every required field already filled, and reserves the identifier before any prose exists. That reservation is what keeps two authors working in parallel from minting the same identifier twice: whoever creates second learns of the clash at creation and picks another name.

`<type>` names the record type or page style, such as `reaction`, `job`, or `use-case`, and `<id>` gives the `home.name` part of the identifier, such as `spending.send-approver-reminder-on-expense-submitted`. The full identifier is `<type>/<id>`. The record lands in its own folder named for it, as `<name>/<name>.yaml`, with its explanation `<name>.md` beside the record file, and the command writes this folder layout only. Each type has its home: a reaction under `.litespec/behavior/modules/<module>/reactions/<name>/`, a job under `.litespec/behavior/modules/<module>/jobs/<name>/`, a notification under `.litespec/behavior/modules/<module>/notifications/<name>/`, a read model under `.litespec/behavior/modules/<module>/read-models/<name>/`, and a decision under `.litespec/intent/decisions/<number>-<name>/`. A module is declared only in Behavior, so its file is `.litespec/behavior/modules/<module>/module.yaml`; an aggregate file is `.litespec/behavior/modules/<module>/aggregates/<aggregate>/aggregate.yaml`. A use-case folder starts under `.litespec/behavior/modules/<module>/use-cases/<name>/`; move it under its aggregate's `use-cases/` folder once the use case names one. An identifier a record already carries is refused.

Record and explanation share one Title Case title, such as `Decided Within Five Days`, written to the record's YAML `title` and to the H1 of its explanation file. The explanation file comes from the explanation template for its type, `templates/consumer/<type>.md`, with the title filled in, and is written for an article type (research or decision) or for a type on a layer the project lists in `explanations.layers`. For a document style, the command writes the `.md` document from its template.

A requirement under a feature is scaffolded with `new requirement <feature>.<name>`: its own named record in the feature's `requirements/` folder, `.litespec/intent/features/<feature>/requirements/<name>/<name>.yaml`, from `templates/consumer/requirement.yaml`, with its identifier `requirement/<feature>.<name>` and its title filled. The feature segment names the feature whose folder holds the requirement, so the feature record must exist first. A product-level requirement is scaffolded with `new requirement <name>` at `.litespec/intent/requirements/<name>/<name>.yaml`, with its identifier `requirement/<name>`. Neither form writes an explanation file, since the explanation is optional; add `<name>.md` beside the record file only when the statement needs prose.

`new decision` takes only the kebab-case `<name>`. The number comes from the one decision sequence, so decision numbers stay unique without coordination: the full identifier is `decision/<number>-<name>`, written as `.litespec/intent/decisions/<number>-<name>/<number>-<name>.yaml` with its explanation file beside it. Before writing, the command prints the provisions whose statement matches the name's words, so you follow a choice the standard already makes instead of recording it again.

`new result` and `new scenario` append one entry to a use-case file's `records` instead of scaffolding a file. The entry lands in house order: successes first, then refusals in precedence order, then scenarios. A new refusal follows the existing refusals, a new scenario lands last, and the file is written back in canonical form. The entry id builds from the use case's own name as `result/<use-case>.<name>` or `scenario/<use-case>.<name>`. `<use-case-id>` is the use-case identifier, with or without the `use-case/` prefix. `--code` carries `<module>.<reason>`; it is required for a refusal, and on a success it becomes the first of `codes`. `--result` names the scenario's result, written or derived. A duplicate id and an unknown use case are both refused.

`new entry` appends one entry to a list file's `records` instead of scaffolding a file. `<page-id>` is the file's consumer-relative path, since list files carry no record identifier. The type must belong on the file: a rule on a rule-set file, a permission on the permission set, a setting on a setting set. The entry id builds from the file's home as `<type>/<home>.<name>`, or `permission/<name>`. The home is the file's existing entries' home; when the file holds no entries yet, it is the file stem for a rule and the parent modules folder for a setting. The entry lands last with every required field filled from its template, and the file is written back in canonical form. A duplicate id and an unknown file are both refused.

A requirement is no list entry: `new entry` takes no `requirement`, and each requirement is scaffolded as its own record with `new requirement`.

A release record starts from `templates/consumer/release-record.md`. Copy it to `.litespec/releases/<date>.md` and fill every placeholder: the source commit and artifact reference, the included flows, use cases, and extensions, the evidence results, and the operating readiness a release record holds.

## Arguments

| Argument | Meaning |
|:---|:---|
| `<type>` | The record type or page style to scaffold; for `entry`, the entry type to append. |
| `<id>` | The `home.name` part of the identifier to reserve. |
| `<use-case-id>` | For `result` and `scenario`: the use case holding the new entry. |
| `<page-id>` | For `entry`: the owning file's consumer-relative path. |
| `<name>` | For `decision`, `result`, `scenario`, and `entry`: the kebab-case name of the new record or entry. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--kind command` or `query` | `command` | For `use-case`: whether the file is written from the command template or the query template. |
| `--outcome success` or `refusal` | none | For `result`: whether the entry ends in success or refusal. |
| `--code <code>` | none | For `result`: the refusal code as `<module>.<reason>`; required for a refusal, the first of `codes` on a success. |
| `--result <result-id>` | none | For `scenario`: the written or derived result the scenario ends in. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The file was written and the identifier reserved, or the entry was appended. |
| `1` | The records cannot be read, or the release holds no readable template for the type. |
| `2` | The root or the project file is missing or invalid, the identifier is taken, the use case or file is unknown, the type is unknown or does not belong on the file, or an option is invalid. |

## Examples

Scaffold a reaction and reserve its identifier:

```bash
litespec new reaction spending.send-approver-reminder-on-expense-submitted
```

Scaffold a use case in the orders module:

```bash
litespec new use-case orders.cancel-order
```

Scaffold a decision with the next number:

```bash
litespec new decision receipt-kept-once
```

Print the provisions whose statement matches the name before scaffolding the decision:

```bash
litespec new decision render-receipts
```

Append a refusal result to the cancel-order page:

```bash
litespec new result use-case/orders.cancel-order already-shipped --code orders.already-shipped --outcome refusal
```

Append a success result carrying its first code:

```bash
litespec new result orders.cancel-order cancelled --code orders.cancelled --outcome success
```

Append a scenario ending in the new refusal:

```bash
litespec new scenario use-case/orders.cancel-order cancelling-a-shipped-order-is-refused --result result/cancel-order.already-shipped
```

Scaffold a requirement under its feature:

```bash
litespec new requirement approval.decided-within-five-days
```

Scaffold a product-level requirement:

```bash
litespec new requirement monthly-close-checklist
```

Append a rule under the page's home:

```bash
litespec new entry .litespec/behavior/rules/spending-rules.yaml rule receipt-filed-once
```

Append a permission to the permission set:

```bash
litespec new entry .litespec/behavior/modules/spending/permissions.yaml permission approve-expense
```

Append a setting under its module:

```bash
litespec new entry .litespec/behavior/modules/spending/settings/receipt-settings.yaml setting receipt-threshold
```

## Underneath

The record file comes from `templates/consumer/<type>.yaml` in the release the tool carries; a use case reads `use-case-command.yaml` or `use-case-query.yaml` by its `--kind`. It opens with a marker line naming its template, and `model check` reports `template-value-unchanged` for a scaffold that keeps a text or number the template copied. The explanation file beside the record comes from `templates/consumer/<type>.md`, and for a document style the document comes from `templates/consumer/<type>.md`. A requirement writes its record file alone, in its feature's `requirements/` folder or at product level. The `decision` path allocates the next number from the one decision sequence, prints the provisions whose statement matches the name's words in identifier order, and then writes; it splits the name on hyphens, skips words shorter than three letters, and matches a plural word against its singular, and prints nothing when no statement matches or the carried release cannot be read. The `result` and `scenario` paths read the use-case file instead and write one entry into its `records`, and the `entry` path reads the list file at `<page-id>` instead and writes one entry last into its `records`. Every write lands in canonical form. Nothing beneath this command is separately runnable. Validate the written file before committing it.
