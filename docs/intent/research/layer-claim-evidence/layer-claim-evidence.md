# Which Record Layers a Project Can Leave Out

## Answer

A project can leave out Implementation or Proof on its own, because the code reads show that neither depends on the other. A use case counts as verified from its scenarios' passing proofs alone: `UseCaseVerified` checks the proof links and the stale set, and it never reads code marks (`completion-calculator`, lines 437 to 452). A fulfiller counts as coded when it is verified, or when code is found for it (`completion-calculator`, lines 403 to 407). So Implementation and Proof each point at Behavior, and removing one leaves the other whole.

The five shapes a claim can take follow from the parent rule. A claim names Intent alone, Intent and Behavior, Intent and Behavior with Implementation, Intent and Behavior with Proof, or all four layers. Behavior needs Intent, and each of Implementation and Proof needs Behavior, so no other shape holds a link. This is the record's own reading of the links the code checks, and no source is cited for it.

The `use-case-unverified` finding fires on any use case that has scenarios and no passing proof, whether or not it carries code marks (`proof-checks`, lines 165 to 171). A team that marks code and proves no scenarios therefore cannot reach the verified stage.

The record schema makes `expiresOn` optional in the departure record. It requires `exit` only when `expiresOn` is set (`record-schema`, lines 2823 to 2835).

## Notes on Sources

Every repository citation was read on 10 November 2026 at the line range shown. No outside source was read for the layer claims: the question concerns links between LiteSpec's own record layers, and the evidence for them is the code.
