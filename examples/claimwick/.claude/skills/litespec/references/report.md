# LiteSpec Completion Report

This reference states the shape of the report that closes a LiteSpec task. It answers what changed, what proves it, and what stays open.

## Commands and Results

Name each exact command run and its result, and what the evidence covers. A check that passed on files the change never touched proves nothing about the change.

## Skipped Checks

Name every check skipped, with its reason. A skipped check without a reason reads as a check never considered.

## Judged Provisions

Name the provisions `rules` returned and how each was judged. Answer each How judged line the `rules --since <base>` skeleton prints, quoting the evidence: the file and the line that follows the provision, or the departure that sets it aside.

## Open Needs

Name what the task leaves open: gaps the specification does not answer, tests believed wrong, and conflicts that need an owner decision. Take each to the owner or the backlog instead of answering it.
