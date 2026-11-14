# Scaffold Record

## Name

`litespec new` scaffolds a record file, including a requirement's folder, and reserves its identifier. It also appends a result or scenario entry to a use-case file, or appends a rule, permission, or setting entry to its owning list file.

## Synopsis

```bash
litespec new <type> <id> [--root <path>] [--help]
litespec new requirement <feature>.<name> [--root <path>]
litespec new decision <name> [--root <path>]
litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
litespec new entry <page-id> <type> <name> [--root <path>]
```

## Description

This command creates one record file from the template for its type, with every required field already filled. For an article type (research or decision) or for a type on a layer the project lists in `explanations.layers`, it also writes the explanation file beside the record, from the explanation template for its type, `templates/consumer/<type>.md`, with the title filled in. For a document style it writes the `.md` document from its template. It reserves the identifier before any prose exists, so two authors working in parallel never mint the same identifier twice. The second author learns the clash at creation and picks another name.

`<type>` is the record type or page style, such as `reaction`, `job`, or `use-case`. `<id>` is the `home.name` part of the identifier, such as `spending.send-approver-reminder-on-expense-submitted`; the command composes the full identifier as `<type>/<id>`. `new <type> <id>` writes a named record in its own folder named for it, as `<name>/<name>.yaml`, with its explanation `<name>.md` beside the record file. The command writes this folder layout only. The folder sits where its type's home is: a reaction under `.litespec/behavior/modules/<module>/reactions/<name>/`, a job under `.litespec/behavior/modules/<module>/jobs/<name>/`, a notification under `.litespec/behavior/modules/<module>/notifications/<name>/`, and a decision under `.litespec/intent/decisions/<number>-<name>/`. A module file is `.litespec/intent/modules/<module>/module.yaml`; an aggregate file is `.litespec/behavior/modules/<module>/<aggregate>/aggregate.yaml`. A use-case folder starts under `.litespec/behavior/modules/<module>/use-cases/<name>/`; move it under its aggregate's folder once the use case names one. The command refuses an identifier a record already carries.

`new requirement <feature>.<name>` writes the requirement as its own named record in the feature's `requirements/` folder, `.litespec/intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml`, from `templates/consumer/requirement.yaml`, with its identifier `requirement/<feature>.<name>` and its title filled. The feature segment names the feature whose folder holds the requirement.

The command writes no explanation file for a requirement, because the explanation is optional. The author adds `<name>.md` beside the record file only when the statement needs prose.

`new decision` takes only the kebab-case `<name>` and allocates the next decision number from the one decision sequence, so decision numbers stay unique without coordination. The full identifier is `decision/<number>-<name>`, and the file is written as `.litespec/intent/decisions/<number>-<name>/<number>-<name>.yaml` with its explanation file beside it. Before it writes, it prints the provisions whose statement matches the name's words, so a choice the standard already makes is followed instead of recorded.

Run it before writing any new record, as `litespec/core/model#scaffold-pages-and-reserve-identifiers` states. Validate the new file before committing it.

`new result` and `new scenario` append one entry to a use-case file's `records` instead of scaffolding a file. The entry lands in house order, successes first, then refusals in precedence order, then scenarios, so a new refusal follows the existing refusals and a new scenario lands last; the file is written back in canonical form. The entry id is `result/<use-case>.<name>` or `scenario/<use-case>.<name>` from the use case's own name. `<use-case-id>` is the use-case identifier, with or without the `use-case/` prefix. `--code` is required for a refusal and carries `<module>.<reason>`; on a success it becomes the first of `codes`. `--result` names the scenario's result, written or derived. The command refuses a duplicate id and an unknown use case.

`new entry` appends one entry to a list file's `records` instead of scaffolding a file. `<page-id>` is the file's consumer-relative path, because list files carry no record identifier. The type belongs on the file: a rule on a rule-set file, a permission on the permission list, a setting on a setting set. The entry id is `<type>/<home>.<name>` from the file's home, or `permission/<name>`; the home is the file's existing entries' home, else the file stem for a rule and the parent modules folder for a setting. The entry lands last with every required field filled from its template; the file is written back in canonical form. The command refuses a duplicate id and an unknown file.

A requirement is no list entry. `new entry` takes no `requirement`; `new requirement` scaffolds each one as its own record.

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

Append a rule under the page's home:

```bash
litespec new entry .litespec/behavior/rules/spending-rules.yaml rule receipt-filed-once
```

Append a permission to the permission list:

```bash
litespec new entry .litespec/behavior/modules/spending/permissions.yaml permission approve-expense
```

Append a setting under its module:

```bash
litespec new entry .litespec/behavior/modules/spending/settings/receipt-settings.yaml setting receipt-threshold
```

## Underneath

This command reads `templates/consumer/<type>.yaml` from the release the tool carries and writes the record file; it also reads `templates/consumer/<type>.md` and writes the explanation file beside the record, and for a document style it reads `templates/consumer/<type>.md` and writes the document. A requirement writes its record file alone, in the feature's folder. The `decision` path allocates the next number from the one decision sequence, prints the provisions whose statement matches the name's words in identifier order, and then writes. It splits the name on hyphens, skips words shorter than three letters, and matches a plural word against its singular; it prints nothing when no statement matches or the carried release cannot be read. The `result` and `scenario` paths read the use-case file instead and write one entry into its `records`. The `entry` path reads the list file at `<page-id>` instead and writes one entry last into its `records`. Every write lands in canonical form. Nothing beneath this command is separately runnable. Validate the written file before committing it.
