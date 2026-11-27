# Which Record Layers a Project Can Leave Out

LiteSpec sorts its records into 4 layers: Intent, Behavior, Implementation, and Proof. Each layer below the top links to the layer above it. This research record checks which layers a project can leave out without breaking a link, and what each claim must name. The answer comes from the code that checks those links. It matters because a team that does not mark code, or that writes no scenario proofs, needs to know which layers it can leave out.

## Findings

Implementation and Proof depend on Behavior, and Behavior depends on Intent [completion calculator][completion-calculator] [proof checks][proof-checks]. A project keeps Intent and adds Behavior before Implementation, Proof, or both [completion calculator][completion-calculator] [proof checks][proof-checks].

A use case counts as verified from its scenarios' passing proofs alone [completion calculator][completion-calculator]. `UseCaseVerified` checks proof links and the stale set, never code marks [completion calculator][completion-calculator].

A fulfiller counts as coded when verified or when the calculator finds code [completion calculator][completion-calculator]. Implementation and Proof each point at Behavior, so removing one breaks nothing in the other [completion calculator][completion-calculator].

A claim names Intent alone, Intent and Behavior, Intent and Behavior with Implementation, Intent and Behavior with Proof, or all 4 layers [completion calculator][completion-calculator] [proof checks][proof-checks]. No other shape keeps every link whole [completion calculator][completion-calculator] [proof checks][proof-checks]. The five-shape reading is the record's own and cites no outside source [completion calculator][completion-calculator] [proof checks][proof-checks].

The `use-case-unverified` finding fires on any use case with scenarios and no passing proof [proof checks][proof-checks]. It fires whether or not the use case carries code marks [proof checks][proof-checks]. A team that marks code and proves no scenarios never reaches the verified stage [proof checks][proof-checks].

The record schema leaves `expiresOn` optional in the waiver record [record schema][record-schema]. It requires `exitWhen` only when `expiresOn` is set [record schema][record-schema].

## Source Notes

The completion calculator [completion calculator][completion-calculator] was read at lines 403 to 407 and lines 437 to 452.

The proof checks [proof checks][proof-checks] were read at lines 165 to 171.

The record schema [record schema][record-schema] was read at lines 2823 to 2835.
