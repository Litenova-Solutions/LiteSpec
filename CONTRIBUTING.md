# Contributing

Read `AGENTS.md` before changing this repository. It is the guide for changing the standard, the schemas, the templates, the command line, the analyzers, or the site.

## Making a Change

Write by the [LiteSpec writing and tone guidelines](WRITING.md): plain words, short sentences, the answer first, and each claim stated at the strength the reader can check.

Send every change through a branch and a pull request against `main`. Keep the branch focused: one change, with its records, code, tests, and pages together.

Prepare a release on its own branch named `release/<version>`. CI checks that branch from its first push, and the branch merges to `main` when the release ships.

The pull request that changes a record is its review: a reviewer reads the diff, not a separate document.

Report a defect or propose a change through the issue forms in [.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/). State what the product does now, what it must do instead, and which record or provision decides.

## Verification

Run the checks in the Verification section of [AGENTS.md](AGENTS.md#verification) before you submit a pull request. Name each check you skipped and why in the pull request, the same way the completion report names them.
