# Scaffold Record

## Name

`litespec new` scaffolds a record file and reserves its identifier, appends a result or scenario entry to a use-case file, or appends a requirement, rule, permission, or setting entry to its owning list file.

## Synopsis

```bash
litespec new <type> <id> [--root <path>] [--help]
litespec new decision <name> [--root <path>]
litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
litespec new entry <page-id> <type> <name> [--root <path>]
```

## Description

This command creates one record file from the template for its type, with every required field already filled. For an article type (research or decision) it also writes the explanation file from its explanation template. For a document style it writes the `.md` document from its template. It reserves the identifier before any prose exists, so two authors working in parallel never mint the same identifier twice. The second author learns the clash at creation and picks another name.

`<type>` is the record type or page style, such as `reaction`, `job`, or `use-case`. `<id>` is the `home.name` part of the identifier, such as `spending.send-approver-reminder-on-expense-submitted`; the command composes the full identifier as `<type>/<id>`. The file is written at the home its type owns: a reaction under `modules/<module>/reactions/`, a job under `modules/<module>/jobs/`, a notification under `modules/<module>/notifications/`, a decision under the decisions folder with its explanation file beside it. A module file is `modules/<module>/module.yaml`; an aggregate file is `modules/<module>/<aggregate>/aggregate.yaml`. A use-case file starts under `reads/` beside its module; move the file beside its aggregate once the use case names one. The command refuses an identifier a record already carries.

`new decision` takes only the kebab-case `<name>` and allocates the next decision number from the one decision sequence, so decision numbers stay unique without coordination. The full identifier is `decision/<number>-<name>`, and the file is written under the decisions folder with its explanation file beside it.

Run it before writing any new record, as `litespec/core/model#scaffold-pages-and-reserve-identifiers` states. Validate the new file before committing it.

`new result` and `new scenario` append one entry to a use-case file's `records` instead of scaffolding a file. The entry lands in house order, successes first, then refusals in precedence order, then scenarios, so a new refusal follows the existing refusals and a new scenario lands last; the file is written back in canonical form. The entry id is `result/<use-case>.<name>` or `scenario/<use-case>.<name>` from the use case's own name. `<use-case-id>` is the use-case identifier, with or without the `use-case/` prefix. `--code` is required for a refusal and carries `<module>.<reason>`; on a success it becomes the first of `codes`. `--result` names the scenario's result, written or derived. The command refuses a duplicate id and an unknown use case.

`new entry` appends one entry to a list file's `records` instead of scaffolding a file. `<page-id>` is the file's consumer-relative path, because list files carry no record identifier. The type must belong on the file: a requirement on a feature file, a rule on a rule-set file, a permission on the permission list, a setting on a setting set. The entry id is `<type>/<home>.<name>` from the file's home, or `permission/<name>`; the home is the file's existing entries' home, else the file stem for a requirement or rule and the parent modules folder for a setting. The entry lands last with every required field filled from its template; the file is written back in canonical form. The command refuses a duplicate id and an unknown file.

A release record starts from `templates/consumer/release-record.md`: copy it to `.litespec/releases/<date>.md` and fill every placeholder. The template carries the source commit and artifact reference, the included journeys, use cases, and extensions, the evidence results, and the operating readiness a release record holds.

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
| `--outcome success` or `refusal` | none | For `result`: whether the entry ends in success or refusal. |
| `--code <code>` | none | For `result`: the refusal code as `<module>.<reason>`; required for a refusal, the first of `codes` on a success. |
| `--result <result-id>` | none | For `scenario`: the written or derived result the scenario ends in. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
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

Append a requirement to its page:

```bash
litespec new entry .litespec/intent/features/approval.yaml requirement decided-within-five-days
```

Append a rule under the page's home:

```bash
litespec new entry .litespec/behavior/rules/spending-rules.yaml rule receipt-filed-once
```

Append a permission to the permission list:

```bash
litespec new entry .litespec/behavior/access/permissions.yaml permission approve-expense
```

Append a setting under its module:

```bash
litespec new entry .litespec/behavior/modules/spending/settings.yaml setting receipt-threshold
```

## Underneath

This command reads `templates/consumer/<type>.yaml` from the release the tool carries and writes the record file; for an article type it also reads `templates/consumer/<type>.md` and writes the explanation file, and for a document style it reads `templates/consumer/<type>.md` and writes the document. The `decision` path allocates the next number from the one decision sequence before writing. The `result` and `scenario` paths read the use-case file instead and write one entry into its `records`. The `entry` path reads the list file at `<page-id>` instead and writes one entry last into its `records`. Every write lands in canonical form. Nothing beneath this command is separately runnable. Validate the written file before committing it.
