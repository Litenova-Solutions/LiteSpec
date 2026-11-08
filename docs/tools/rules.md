# Rules Query

## Name

`litespec rules` lists the provisions that govern a path or a record identifier at the conformance level the project claims.

## Synopsis

```bash
litespec rules [--root <consumer>] <path or record id> [--full]
```

## Description

An agent runs `rules <path>` before editing a file, and answers each returned provision's Reviewed for question before reporting done, following `litespec/core/agent#load-applicable-provisions-before-editing`. The command derives the target's stack, layer, and artifact, reads the conformance level from `conformance` in `.litespec/project.json`, and returns only the provisions at or below that level: the levels nest, so `records` sits inside `traced`, and `traced` sits inside `full`. Each provision carries its conformance level in the provision index. `rules <...> --full` prints every returned provision as a statement with the rationale and the example under each item.

The target is a file or folder path under the consumer root, or a record identifier such as `use-case/spending.submit-expense`. A path query reads the file's marks: `implements`, `follows`, and `proves` in XML, attribute, typed-constant, and `proves()` call form.

A provision applies when its scope meets the target. An artifact-scoped provision matches the target artifact and each marked record of its artifact. A page-scope provision, one without an artifact, matches the target layer. A marked provision identifier is always returned. Project conventions print beside the LiteSpec provisions when their scope glob matches the path. A live departure exception replaces the provision its covers names in place, so the replacement shows where the original stood; an expired departure changes nothing.

In a project at the `records` conformance level, a code path returns no provisions and prints why: `No provisions apply at the records conformance level for a code path: the project claims the record model only, so provisions about code do not apply. Query a record id for the provisions about records.` A record identifier, and a record file, return the provisions about records at that level. At `traced`, the provisions about records, marks, and proof return. At `full`, every applying provision returns.

Without `--full`, the output prints in three parts. The provisions that need judgment and match the file kind or its record print as statements grouped in strength order: Required, Defaults, Advisory, then Patterns. Each such item shows its identifier, its statement, and its reasons, such as `[path match (artifact endpoint); record use-case/spending.submit-expense]`. The provisions a tool checks print under `Checked by a tool:`, one line per provision with its finding codes and analyzer IDs backticked, such as the line for `litespec/backend/application#implement-one-outcome-path-per-command` naming LS2014; a checked provision that names no code prints its checking command. The layer-wide provisions, which govern only through the target layer, print by identifier only under `Layer-wide:`. With `--full`, every returned provision prints as a statement grouped by strength, with the rationale and the example under each item, and no checked or layer-wide shortening applies.

The layer comes from the layout: the `Domain`, `Application`, `Infrastructure`, and `WebApi` folders, the `Contracts`, `UseCases`, and `Jobs` folders, the role suffix of a use-case file, or the project name under `layer-projects`. A frontend route, module, or frame maps to its frontend layer, a test file to tests, a record file to records, and an operations file to operations. The artifact comes from the file's role suffix, its folder, and the records it marks. A record identifier resolves to the records layer with its type's artifact.

Only provisions on pages the consumer's stacks, shape, and selected extensions apply are returned, minus the provisions a selected extension replaces.

## Arguments

| Argument | Required | Effect |
|:---|:---|:---|
| `<path or record id>` | yes | Lists the provisions that govern this file, folder, or record. |

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | Selects the consumer repository to read. |
| `--full` | off | Prints every returned provision as a statement, with the rationale and the example under each item, instead of the checked and layer-wide shortening. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The provisions are listed. |
| `1` | The target names no file or record. |
| `2` | The arguments are wrong, the root holds no project record, or the carried release cannot be read. |

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

## Underneath

The command reads the provisions, with each provision's conformance level and verification mark, from the generated `docs/reference/provisions.json` in the carried release when it exists, and parses the standards pages themselves otherwise, so it works with or without the generated file. It reads the claimed conformance level from `.litespec/project.json` through the consumer configuration the model builder reads. It resolves the applied pages and provisions through the manifest's stack folders, shape pages, and selected extensions, the same selection `litespec model check` enforces. It reads project conventions from `.litespec/governance/conventions/` and departure exceptions from the governance exception folders. Nothing beneath this command is separately runnable.
