# Audit Semantic

## Name

`litespec audit semantic` maps the files changed since a revision to the use cases they touch and writes one review packet per use case.

## Synopsis

```bash
litespec audit semantic --since <ref> [--root <consumer>]
```

## Description

The command lists the files changed since the revision, from the tracked files differing between the revision and the working tree plus the untracked files, and leaves out the audit output it writes itself under `.litespec/audit/`.

Each changed file maps to the use cases whose marked code or records hold it: a code file maps through the `implements` marks that name the use case, and a record file maps through the use-case record it holds. A changed file that maps to no use case is skipped, and a use case with no changed files gets no packet.

Each mapped use case gets one packet at `.litespec/audit/semantic/<use-case-name>.md`, named from the identifier after `use-case/`, so `use-case/spending.submit-expense` writes `spending.submit-expense.md`. The packet holds the task packet of the use case, the changed files split into record files and code files, and the current content of each changed code file, for an independent reviewer. The command judges nothing else.

A changed line outside the tests that holds `NotImplementedException`, `TODO`, or `throw new Error("not implemented")` is reported as `code-unimplemented`, with the file, the line number, and the first use case the file maps to, or no subject when it maps to none. A test file is any file under a test root, in a test folder, or with a test file name, so a TODO in a test never reports.

The command prints each finding as `path:line [S] [code-unimplemented] message`, writes a JSON report to `.litespec/audit/semantic.json` with the revision, the use-case identifiers, the packet paths, and the findings with their path, line, code, subject, and message, and exits 0 when no changed line holds a stub marker and 1 when one does.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--since <ref>` | none, required | The git revision to diff the working tree against, such as `HEAD~1`. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | No changed line holds a stub marker. |
| `1` | A changed line holds a stub marker. |
| `2` | The arguments are wrong, the root holds no project file, the changes cannot be read, or a file cannot be written. |

## Examples

Write one review packet per use case changed since the last commit:

```bash
litespec audit semantic --since HEAD~1 --root ../claimwick
```

A changed Claimwick handler that still carries `// TODO: check the receipt total.` reports `apps/api/src/Modules/Spending/Settle/SettleExpense/SettleExpenseCommandHandler.cs:42 [S] [code-unimplemented] Changed line holds 'TODO'; implement the behavior or remove the line before review.`, writes `.litespec/audit/semantic/spending.settle-expense.md`, and exits 1.

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses, builds the project model, and reads the packet of each mapped use case from the same builder the `litespec_task_packet` tool answers from. The changed files come from the git revision through an internal runner, so the tests replace the git process with canned output. Nothing beneath this command is separately runnable.
