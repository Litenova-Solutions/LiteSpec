# Which Record Layers a Project Can Leave Out

LiteSpec sorts its records into 4 layers: Intent, Behavior, Implementation and Proof. Each layer links to the layer above it. This research record checks which layers a project can leave out without breaking a link, and what each claim must name. The answer comes from the code that checks those links. It matters because a team that does not mark code, or that writes no scenario proofs, needs to know which layers it may omit.

## Findings

A project can leave out Implementation or Proof on its own, because neither depends on the other. Behavior needs Intent, and each of Implementation and Proof needs Behavior. So a project keeps Intent and Behavior, and it may add either or both of the other 2 layers.

A use case counts as verified from its scenarios' passing proofs alone. `UseCaseVerified` checks the proof links and the stale set, and it never reads code marks. The record cites lines 437 to 452 of the completion calculator. The record gives no confidence for this claim.

A fulfiller counts as coded when it is verified, or when code is found for it. The record cites lines 403 to 407 of the completion calculator for this rule. Implementation and Proof therefore each point at Behavior, and removing one leaves the other whole. The record gives no confidence for this claim.

A claim can take 5 shapes. It names Intent alone, Intent and Behavior, Intent and Behavior with Implementation, Intent and Behavior with Proof, or all 4 layers. No other shape holds a link, because Behavior needs Intent and each of the other 2 layers needs Behavior. The record states that this is its own reading of the links the code checks, and it cites no outside source for it. The record gives no confidence for this claim.

The `use-case-unverified` finding fires on any use case that has scenarios and no passing proof. It fires whether or not the use case carries code marks. The record cites lines 165 to 171 of the proof checks. A team that marks code and proves no scenarios therefore cannot reach the verified stage. The record gives no confidence for this claim.

The record schema makes `expiresOn` optional in the departure record. It requires `exit` only when `expiresOn` is set. The record cites lines 2823 to 2835 of the record schema. The record gives no confidence for this claim.

## Source Notes

The record names 3 repository sources, all read on 10 November 2026. They are the completion calculator (`apps/cli/src/LiteSpec.Cli/Model/CompletionStages/CompletionCalculator.cs`), the proof checks (`apps/cli/src/LiteSpec.Cli/Model/Checks/Proof/ProofChecks.cs`) and the record schema (`schemas/record.schema.yaml`). Each citation gives the line range that was read.

The record reads no outside source for the layer claims. It states that the question concerns links between LiteSpec's own layers, and that the code is the evidence for them. The record names no disagreement between sources and no claim it could not confirm.
