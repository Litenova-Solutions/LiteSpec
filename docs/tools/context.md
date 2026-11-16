# Context

## Name

`litespec context` writes the consumer's records and explanation files as one compact plain-text file for an AI chat.

## Synopsis

```bash
litespec context [--root <folder>] [--out <file-or-folder>] [--layers <list>] [--modules <list>] [--types <list>] [--include records|explanations|both] [--full]
```

## Description

The command reads every record file under the consumer's record roots, each with the explanation file beside it, and writes them as one UTF-8 text file with LF line endings, so an author can paste the specification into an AI chat to discuss it. It never edits, moves, or deletes a LiteSpec file.

The output opens with a short header, then holds one block per file between `=== BEGIN FILE: <relative path> ===` and `=== END FILE: <relative path> ===`, and closes with `=== END OF CONTEXT: <n> files ===`. Each explanation follows its record file. The product record comes first and is always included, and the rest follow the book's outline order: by record layer first, then by path.

Unless you pass `--full`, the output is compacted to save tokens. From YAML it drops comments, blank lines, and keys with an empty value, and collapses runs of spaces inside scalar text to one while keeping the indentation. From Markdown it drops HTML comments and trailing whitespace, collapses repeated blank lines to one, and drops an explanation's first `# Title` line when it equals the record's title. Values, identifiers, and wording stay untouched. With `--full`, the files are kept as written.

Three options narrow the output by record:

- `--layers` keeps the named record layers from `intent`, `behavior`, `implementation`, `proof`, and `governance`, read from each record's type. The product record stays whatever layers are named.
- `--modules` limits Behavior records to the named modules. Intent and governance records are unaffected.
- `--types` keeps the record files holding one of the named types, such as `use-case,rule`.

`--include records` writes the YAML record files only, `--include explanations` the Markdown explanation files only, and `both` writes each record file followed by its explanation.

The header names the project title and identifier, the LiteSpec version the project reviewed, the filters applied, the record count, the generation time, the file count, and a line describing the markers. A reader, or a model in an AI chat, knows the context is complete only when the closing line is present and its count matches the header. Once written, the command prints the path with the size in characters and an estimated token count at four characters per token.

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
|:--|:--|
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

The command opens the consumer through the same root walk `litespec model check` uses and reads every record file with the record reader, so the types, layers, and modules in the output match the model exactly. It reads the project title and identifier from the product record and the reviewed version from `reviewedLiteSpecVersion` in `.litespec/project.yaml`. Compaction deletes comments, blank lines, and empty keys and collapses repeated spaces and blank lines; it parses nothing and rewrites no value. Nothing beneath this command is separately runnable.
