# Contributing

Read `AGENTS.md` before changing this repository. It is the canonical guide for changing the standard, the schemas, the templates, the command line, the analyzers, or the site.

## Making a Change

Write by the [LiteSpec writing and tone guidelines](WRITING.md).

Submit every change through a branch and pull request against `main`.

A release is prepared on its own branch named `release/<version>`. CI checks that branch from its first push, and the branch merges to `main` when the release is complete.

The pull request that changes a record is its review.

Report a defect or propose a change through the issue forms in [.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/).

## Verification

Run the checks in the Verification section of [AGENTS.md](AGENTS.md#verification) before you submit a pull request.
