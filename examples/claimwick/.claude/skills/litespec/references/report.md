# LiteSpec Completion Report

Every LiteSpec task closes with a report answering three questions: what changed, what proves it, and what stays open.

## Commands and Results

Name each exact command run with its result, and say what the evidence covers. A check that passed on files the change never touched proves nothing about the change. Name the warm-service tools called (`litespec_task_packet`, `litespec_check`, `litespec_impact`, `litespec_verify_slice`) with the target of each, and the `scaffold`, `sync`, `plan-waves`, `upgrade --check`, `docs review`, or `ui review` runs with what each wrote.

## Skipped Checks

Name every check you skipped, with its reason. A skipped check without a reason reads as a check never considered.

## Judged Provisions

Name the provisions `rules` and the `post-edit` hook returned and how each was judged. Quote the evidence: the file and line following the provision, or the waiver setting it aside, with its kind and, for a gap, its closing records in `closedBy`.

## Verification

Name the `model check` or `verify_slice` run that proves the change, the `hook pre-stop` run that gated the stop, and whatever stays red. A route change also names the `ui review` packet and its baselines; a docs change names the `docs review` packet.

## Open Needs

Name what the task leaves open: gaps the specification does not answer, tests believed wrong, and conflicts needing an owner decision. Take each to the owner or the backlog instead of answering it.
