# LiteSpec Commands

The LiteSpec skill names several command forms, and this reference collects them in one place. Below, `litespec` stands for the project's command form from its `AGENTS.md` region: `dotnet litespec` or `litespec`.

## Query a Record

`litespec model query <record-id>` prints one record with its links in both directions, showing its identifier and its folder path. Query the record before changing behavior it describes, and query again whenever a link leads further.

```bash
litespec model query use-case/spending.submit-expense
```

## Search Records

`litespec model query --search <text>` lists the written records whose identifier, title, summary or statement holds the text, case-insensitive, with an exact identifier match first. With no match it prints nothing and exits 0.

```bash
litespec model query --search "receipt threshold"
```

## List the Records of a Type

`litespec model query --list <type>` lists every written record of the type, one line each with identifier, title and path. Adding `--module <module>` narrows the list to the records of that module.

```bash
litespec model query --list use-case --module spending
```

## List the Provisions for a File

`litespec rules <path>` lists the provisions governing a file or folder before it is edited. The target is a path under the consumer root or a record identifier. Add `--full` to print each provision with its rationale and example.

```bash
litespec rules apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs
```

## Read One Provision

`litespec rules <provision-id>` prints one provision in full, with its strength, statement, rationale, example, and verification. It reads the carried release, so it works offline.

## Check the Change Set

`litespec model check --since <base>` reports the findings about records and code changed since `<base>`, while `litespec rules --since <base>` prints the report skeleton over the same files: one judgment provision per line with its Reviewed for question and an empty How judged line. Answer each line before reporting done.

```bash
litespec model check --since origin/main
litespec rules --since origin/main
```

Every finding about a missing, unknown or unused link blocks, and planned Intent blocks in every run, local or in the pull-request check. Only a live waiver covers one.

## Check One Module

`litespec model check --module <module>` prints only the findings on the records and files of that module. The exit code follows those findings alone.

```bash
litespec model check --module spending
```

## Report the Module Edges

`litespec model edges` prints each module's written record links to other modules, with counts per link kind and per module, followed by every pair of modules that link each other both ways. With `--module <module>` it prints only that module with the mutual pairs it belongs to. It is read-only.

```bash
litespec model edges
```

## Run Rules After Every Edit

The guide [Run Rules After Every Edit](https://litespec.dev/latest/guide/run-rules-after-every-edit/) shows how to run `rules <path>` after every edit and `model check --since <base>` at the end of a session. It gives one recipe for Claude Code hooks and one for an OpenCode plugin. The same page as plain Markdown lives at `https://litespec.dev/latest/guide/run-rules-after-every-edit.md`.
