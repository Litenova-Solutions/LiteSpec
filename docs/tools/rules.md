# Rules Query

## Name

`litespec rules` lists the provisions that govern a path, a record identifier, a change set, or one provision or page identifier, at the record layers the project claims in [layers](../core/scope.md#layer-claims).

## Synopsis

```bash
litespec rules [--root <consumer>] <path or record id> [--full]
litespec rules [--root <consumer>] --since <ref>
litespec rules [--root <consumer>] <provision id or page id>
```

## Description

An agent runs `rules <path>` before editing a file, and before reporting done answers each judgment provision `rules --since <base>` lists, following `litespec/core/agent#load-applicable-provisions-before-editing`. The command derives the target's stack, concern, and artifact, reads the claimed layers from `layers` in `.litespec/project.yaml`, and returns only the provisions whose layer is `any` or claimed, and, for Implementation and Proof, whose trace is `yes` or whose layer the project claims with `litespec`. Each provision carries its layer and trace in the provision index. `rules <...> --full` prints every returned provision as a statement with the rationale and the example under each item.

The target is a file or folder path under the consumer root, or a record identifier such as `use-case/spending.submit-expense`. A provision identifier or a page identifier such as `litespec/backend/application` looks up the standard itself. A path query reads the file's marks: `implements`, `follows`, and `proves` in XML, attribute, typed-constant, and `proves()` call form.

A provision applies when its scope meets the target. An artifact-scoped provision matches the target artifact and each marked record of its artifact. A page-scope provision, one without an artifact, matches the target layer. A provision with a `when` condition needs its condition met by the marked record on a code path: `query` for a use case with kind `query`, `command` for kind `command`, `upload` for a use case taking a file from the caller, `document-number` for a use case issuing a legal document number, `erasure` for a use case or job erasing personal data, and `audit-adoption` for the decision the audit extension selection names in its `decision` field. `project` meets no record, so a project provision never returns on a code path. A marked provision identifier is always returned. Project conventions print beside the LiteSpec provisions when their scope glob matches the path or their scope vocabulary matches the target's stack, concern, and artifact. A live departure replaces the provision its `covers` names in place, so the replacement shows where the original stood; an expired departure changes nothing.

When the target is code, provisions matched only through a marked record print as one line per record: `use-case/spending.approve-expense: <n> provisions about the record; model check checks <m>; run rules use-case/spending.approve-expense for the rest`. A record path or record ID still prints them in full. A record path or record ID prints a conditional provision with its condition shown. A path that marks no record leaves conditional provisions out.

`rules --since <ref>` takes every file changed since the git revision, returns the union with duplicates removed, leaves out the tool-checked provisions, and prints a report skeleton: one line per judgment provision with its Reviewed for question and an empty `How judged:` line.

`rules <provision-id>` prints that provision in full: strength, statement, rationale, example, Reviewed for or Checked by, and page. `rules <page-id>` prints the page's provision list. Both read the carried provisions file, so they work offline.

In a project that claims only `intent` and `behavior`, a code path returns no provisions and prints why: `No provisions apply to a code path: the project claims neither the Implementation nor the Proof layer, so provisions about code do not apply. Query a record id for the provisions about records.` A record identifier, and a record file, return the provisions about records. When the project claims Implementation with `own`, the provisions about code marks return, and with `litespec` every provision about code returns.

When the target is code, a `Decided:` group prints first: each decision the file follows with its `choice` and `reopenedWhen`, then each departure covering a provision in the result with its `reason` and, for a gap, its `expiresOn`.

Without `--full`, the provisions print in three parts. The provisions that need judgment and match the file kind or its record print as statements grouped in strength order: Required, Defaults, Advisory, then Patterns. Each such item shows its identifier, its statement, and its reasons, such as `[path match (artifact endpoint); record use-case/spending.submit-expense]`. The provisions a tool checks print under `Checked by a tool:`, one line per provision with its finding codes and analyzer IDs backticked, such as the line for `litespec/backend/application#implement-one-outcome-path-per-command` naming LS2014; a checked provision that names no code prints its checking command. The concern-wide provisions, which govern only through the target's concern, print by identifier only under `Concern-wide:`. With `--full`, every returned provision prints as a statement grouped by strength, with the rationale and the example under each item, and no checked or concern-wide shortening applies.

The layer comes from the layout: the `Domain`, `Application`, `Infrastructure`, and `WebApi` folders, the `Contracts`, `UseCases`, and `Jobs` folders, the role suffix of a use-case file, or the project name under `layer-projects`. A frontend route, module, or frame maps to its frontend layer, a test file to tests, a record file to records, and an operations file to operations. The artifact comes from the file's role suffix, its folder, and the records it marks. A record identifier resolves to the records layer with its type's artifact.

Only provisions on pages the consumer's stacks, shape, and selected extensions apply are returned, minus the provisions a selected extension replaces.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<path, record id, provision id or page id>` | yes, unless `--since` is passed | Lists the provisions that govern this file, folder, or record, or prints one provision or page. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--full` | off | Prints every returned provision as a statement, with the rationale and the example under each item, instead of the checked and concern-wide shortening. |
| `--since <ref>` | off | Prints the report skeleton over the files changed since the git revision, instead of one target. `--full` has no effect with it. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The provisions are listed. |
| `1` | The target names no file, record, provision or page, or the records cannot be read. |
| `2` | The arguments are wrong, the root or the project file is missing or invalid, the revision names no commit, or the carried release cannot be read. |

## Examples

List the provisions that govern a handler before editing it:

```bash
litespec rules --root ../consumer apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs
```

Read the rationale and example behind each returned statement:

```bash
litespec rules --root ../consumer apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs --full
```

List the provisions that govern a use case record:

```bash
litespec rules --root ../consumer use-case/spending.submit-expense
```

Print the report skeleton for the change set:

```bash
litespec rules --root ../consumer --since main
```

Read one provision in full:

```bash
litespec rules --root ../consumer litespec/backend/application#keep-command-handlers-to-load-call-and-stage
```

List one page's provisions:

```bash
litespec rules --root ../consumer litespec/backend/application
```

Show the decisions behind a renderer:

```bash
litespec rules --root ../consumer apps/api/src/Claimwick.Modules/Spending/Application/ReceiptPdfRenderer.cs
```

## Underneath

The command reads the provisions, with each provision's layer, trace and verification mark, from the generated `docs/reference/provisions.json` in the carried release when it exists, and parses the standards pages themselves otherwise, so it works with or without the generated file. It reads the claimed layers from `.litespec/project.yaml` through the consumer configuration the model builder reads. It resolves the applied pages and provisions through the manifest's stack folders, shape pages, and selected extensions, the same selection `litespec model check` enforces. It reads project conventions from `.litespec/governance/conventions/` and departure records from the departures folders under `.litespec/governance/`. It evaluates `when` against each marked record file. It reads the use-case kind for `query` and `command`. Upload holds when a caller input names a file: file, upload, photo, image, picture, attachment, receipt, scan, pdf or document, without naming a number. Document-number holds when the record names an invoice, payout, reimbursement, voucher, document number or legal number. Erasure holds when the record names erasure, pseudonymizing or anonymizing. Project holds for no record. Audit-adoption holds when the marked record is a decision the audit selection in `.litespec/project.yaml` names in its `decision` field. The Decided group reads each followed decision's `choice` and `reopenedWhen` from its record file. Change-set mode runs the git reading `model check --since` uses. Identifier lookup reads every provision from the carried provisions file without selection filtering, so it works offline. Nothing beneath this command is separately runnable.
