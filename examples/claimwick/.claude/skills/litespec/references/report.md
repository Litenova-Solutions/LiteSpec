# LiteSpec Completion Report

Every LiteSpec task closes with a report answering three questions: what changed, what proves it, and what stays open.

## Commands and Results

Name each exact command run with its result, and say what the evidence covers. A check that passed on files the change never touched proves nothing about the change.

## Skipped Checks

Name every check you skipped, with its reason. A skipped check without a reason reads as a check never considered.

## Judged Provisions

Name the provisions `rules` returned and how each was judged. Answer each How judged line the `rules --since <base>` skeleton prints, quoting the evidence: the file and line following the provision, or the waiver setting it aside, with its kind and, for a gap, its closing records in `closedBy`.

## Open Needs

Name what the task leaves open: gaps the specification does not answer, tests believed wrong, and conflicts needing an owner decision. Take each to the owner or the backlog instead of answering it.
