# Scaffold Record

## Name

`litespec new` scaffolds a record page and reserves its identifier, appends a result or scenario entry to a use-case page, or appends a requirement, rule, permission, or setting entry to its owning list page.

## Synopsis

```bash
litespec new <type> <id> [--root <path>] [--help]
litespec new result <use-case-id> <name> --outcome success|refusal [--code <code>] [--root <path>]
litespec new scenario <use-case-id> <name> --result <result-id> [--root <path>]
litespec new entry <page-id> <type> <name> [--root <path>]
```

## Description

This command creates one record page from the template for its type, with the opening specification metadata block already filled. It reserves the identifier before any prose exists, so two authors working in parallel never mint the same identifier twice. The second author learns the clash at creation and picks another name.

`<type>` is the record type or page style, such as `automation`, `use-case`, or `decision`. `<id>` is the `home.name` part of the identifier, such as `spending.send-approver-reminder-on-expense-submitted`; the command composes the full identifier as `<type>/<id>`. The page is written at the home its type owns: an automation under `modules/<module>/automations/`, a notification under `modules/<module>/notifications/`, a decision under the decisions folder. A use-case page starts under `reads/` beside its module; move the file beside its aggregate once the use case names one. The command refuses an identifier a record already carries.

Run it before writing any new record, as `litespec/core/authoring#scaffold-pages-and-reserve-identifiers` states. Validate the new page before committing it.

`new result` and `new scenario` append one entry to a use-case page's frontmatter records instead of scaffolding a page. The entry lands in house order, successes first, then refusals in precedence order, then scenarios, so a new refusal follows the existing refusals and a new scenario lands last; every other byte of the page stays untouched. The entry id is `result/<use-case>.<name>` or `scenario/<use-case>.<name>` from the use case's own name. `<use-case-id>` is the use-case identifier, with or without the `use-case/` prefix. `--code` is required for a refusal and carries `<module>.<reason>`; on a success it becomes the first of `codes`. `--result` names the scenario's result, written or derived. The command refuses a duplicate id and an unknown use case.

`new entry` appends one entry to a list page's frontmatter records instead of scaffolding a page. `<page-id>` is the page's consumer-relative path, because list pages carry no record identifier. The type must belong on the page: a requirement on a topic page, a rule on a rule-set page, a permission on the permission list, a setting on a setting set. The entry id is `<type>/<home>.<name>` from the page's home, or `permission/<name>`; the home is the page's existing entries' home, else the file stem for a requirement or rule and the parent modules folder for a setting. The entry lands last with every required field filled with its template placeholder; every other byte of the page stays untouched. The command refuses a duplicate id and an unknown page.

A release record starts from `templates/consumer/release-record.md`: copy it to `docs/releases/<date>.md` and fill every placeholder. The template carries the source commit and artifact reference, the included journeys, use cases, and extensions, the evidence results, and the operating readiness a release record holds.

## Arguments

| Argument | Meaning |
|:---|:---|
| `<type>` | The record type or page style to scaffold; for `entry`, the entry type to append. |
| `<id>` | The `home.name` part of the identifier to reserve. |
| `<use-case-id>` | For `result` and `scenario`: the use case holding the new entry. |
| `<page-id>` | For `entry`: the owning page's consumer-relative path. |
| `<name>` | For `result`, `scenario`, and `entry`: the kebab-case name of the new entry. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <path>` | the working directory | Selects the consumer repository to write into. |
| `--outcome success` or `refusal` | none | For `result`: whether the entry ends in success or refusal. |
| `--code <code>` | none | For `result`: the refusal code as `<module>.<reason>`; required for a refusal, the first of `codes` on a success. |
| `--result <result-id>` | none | For `scenario`: the written or derived result the scenario ends in. |
| `--help` | off | Prints the synopsis, the options, and the exit codes, then exits zero. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The page was written and the identifier reserved, or the entry was appended. |
| `1` | The release holds no readable template for the type, or the repository could not be read or written. |
| `2` | The identifier is taken, the use case or page is unknown, the type is unknown or does not belong on the page, or an option is invalid. |

## Examples

Scaffold an automation and reserve its identifier:

```bash
litespec new automation spending.send-approver-reminder-on-expense-submitted
```

Scaffold a use case in the orders module:

```bash
litespec new use-case orders.cancel-order
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
litespec new entry docs/intent/spending/approval.md requirement decided-within-five-days
```

Append a rule under the page's home:

```bash
litespec new entry docs/behavior/rules/spending-rules.md rule receipt-filed-once
```

Append a permission to the permission list:

```bash
litespec new entry docs/behavior/access/permissions.md permission approve-expense
```

Append a setting under its module:

```bash
litespec new entry docs/behavior/modules/spending/settings.md setting receipt-threshold
```

## Underneath

This command reads `templates/consumer/<type>.md` from the release the tool carries and writes the page with its metadata block. The `result` and `scenario` paths read the use-case page instead and splice one entry into its frontmatter records. The `entry` path reads the list page at `<page-id>` instead and splices one entry last into its frontmatter records. Nothing beneath this command is separately runnable. Validate the written page before committing it.
