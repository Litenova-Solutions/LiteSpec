# LiteSpec Commands

This reference states the command forms the LiteSpec skill names. Below, `litespec` stands for the project's command form from its `AGENTS.md` region: `dotnet litespec` or `litespec`.

## Query a Record

`litespec model query <record-id>` prints one record with its links in both directions, showing its identifier and its folder path. Query the record before changing behavior it describes, and query again when a link leads further.

```bash
litespec model query use-case/spending.submit-expense
```

## List the Provisions for a File

`litespec rules <path>` lists the provisions that govern a file or folder before it is edited. The target is a path under the consumer root, or a record identifier. Add `--full` to print each provision with its rationale and example.

```bash
litespec rules apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs
```

## Read One Provision

`litespec rules <provision-id>` prints one provision in full: its strength, statement, rationale, example, and verification. It reads the carried release, so it works offline.

## Check the Change Set

`litespec model check --since <base>` reports the findings about records and code changed since `<base>`, and `litespec rules --since <base>` prints the report skeleton over the same files: one judgment provision per line with its Reviewed for question and an empty How judged line. Answer each line before reporting done.

```bash
litespec model check --since origin/main
litespec rules --since origin/main
```

## Reinforce Rules at Edit Time

The guide [Reinforce Rules at Edit Time](https://litespec.dev/latest/guide/reinforce-rules-at-edit-time/) shows how to run `rules <path>` after every edit and `model check --since <base>` at the end of a session, with one recipe for Claude Code hooks and one for an OpenCode plugin. The same page as plain Markdown lives at `https://litespec.dev/latest/guide/reinforce-rules-at-edit-time.md`.
