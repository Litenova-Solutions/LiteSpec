# Context

## Name

`litespec context` writes the consumer's records and explanation files as one compact plain-text file for an AI chat.

## Synopsis

```bash
litespec context [--root <folder>] [--out <file-or-folder>] [--layers <list>] [--modules <list>] [--types <list>] [--include records|explanations|both] [--full]
```

## Description

An author pastes the specification into an AI chat to discuss it. The command reads every record file under the consumer's record roots with the explanation file beside each one, and writes them as one UTF-8 text file with LF line endings: a short header, then one block per file starting with `== <relative path>`, each explanation following its record file. The product record comes first and is always included; the rest follow the book's outline order by record layer, then by path. The command never edits, moves or deletes a LiteSpec file.

Without `--full` the output is compacted to save tokens: YAML comments and blank lines go, keys with an empty value go, runs of spaces inside scalar text collapse to one while the indentation stays, Markdown HTML comments and trailing whitespace go, repeated blank lines collapse to one, and an explanation's first `# Title` line goes when it equals the record's title. Values, identifiers and wording stay untouched. With `--full` the files are kept as written.

`--layers` keeps the named record layers from `intent`, `behavior`, `implementation`, `proof` and `governance`, read from each record's type; the product record stays whatever layers are named. `--modules` limits Behavior records to the named modules; Intent and governance records are unaffected. `--types` keeps the record files holding one of the named types, such as `use-case,rule`. `--include records` writes the YAML record files only, `--include explanations` the Markdown explanation files only, and `both` writes each record file followed by its explanation.

The header names the project title and identifier, the LiteSpec version the project reviewed, the filters applied, the record count, and the generation time. The command prints the written path with the size in characters and an estimated token count at four characters per token.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <folder>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--out <file-or-folder>` | `litespec-context.txt` in the working directory | The file to write, or a folder that receives `litespec-context.txt`. |
| `--layers <list>` | all layers | Comma list of `intent`, `behavior`, `implementation`, `proof`, `governance`; keeps those record layers. |
| `--modules <list>` | all modules | Comma list of module names; limits Behavior records to those modules. |
| `--types <list>` | all types | Comma list of record types; keeps the record files holding one of them. |
| `--include <kind>` | `both` | One of `records`, `explanations`, or `both`. |
| `--full` | off | Keeps the files as written instead of compacting them. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The file is written. |
| `1` | The records cannot be read, the consumer holds no product record, or the file cannot be written. |
| `2` | The arguments are wrong, an option names an unknown value, or the root or the project file is missing or invalid. |

## Examples

Write the whole specification to one file:

```bash
litespec context --root ../consumer --out ../consumer/litespec-context.txt
```

Write only the Behavior layer of one module, as written:

```bash
litespec context --root ../consumer --layers behavior --modules spending --full
```

Write only the use-case and rule records without explanations:

```bash
litespec context --root ../consumer --types use-case,rule --include records
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses and reads every record file with the record reader, so record types, layers and modules match the model exactly. It reads the project title and identifier from the product record and the reviewed version from `reviewedLiteSpecVersion` in `.litespec/project.yaml`. Compaction deletes comments, blank lines and empty keys and collapses repeated spaces and blank lines; it parses nothing and rewrites no value. Nothing beneath this command is separately runnable.
