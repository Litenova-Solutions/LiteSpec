# Projects Claim the Record Layers They Use

LiteSpec describes a product in record layers: Intent, Behavior, Implementation and Proof. This decision lets each project name the layers it uses and the practice it follows for each one, in its project file. A team that wants LiteSpec's architecture with its own tests can take that architecture without taking every layer.

## Context

The project file's `conformance` takes one of three values: `records`, `traced` or `full`. Each value bundles the code practice and the proof practice together. A team that wants LiteSpec's architecture with its own tests must therefore take all of `full` or none of it.

The word `level` also names the check level. `decision/0026-one-name-for-each-concept` keeps that name for the check level alone.

## Options

LiteSpec compared 3 options for claiming layers. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| A `layers` map that names the practice each layer follows | A team names its layers in its own words and keeps its own tests, and each of today's 3 levels stays expressible. | The conformance level leaves the project file and every tool, and each citation of it changes. |
| Keep the conformance levels and add a level for each partial case | No project file changes, and each partial case gets a name. | Each new level lowers every check at once, so the levels multiply the same mismatch. |
| One switch per provision or page | A team switches off exactly the rules it does not use. | A project lists dozens of IDs, and each rename breaks the list. |

## Decision

A project claims each record layer that it uses, and names the practice it follows, in the `layers` map of its project file. The standard states each rule for the layer it governs. Each tool applies the rules for the layers the project claims.

A layer the project does not claim produces no findings. A team leaves a layer out of the claim instead of waiving its rules.

## Consequences

The `layers` map takes the keys `intent`, `behavior`, `implementation` and `proof`. `intent` and `behavior` take `litespec`. `implementation` and `proof` take `litespec` or `own`. A key that is left out is not claimed.

`behavior` needs `intent`, and each of `implementation` and `proof` needs `behavior`. Every provision and finding code carries its `layer` and its `trace`. A project that claims implementation or proof names its stacks.

The mark check, LS4001, runs whenever a project claims proof under either practice. The alert and runbook checks move to the `delivery` extension. The Claimwick example selects no extension, because selecting `delivery` widened the deploy rules past the precision target.

## Evidence

`research/layer-claim-evidence` reads the completion code and the proof checks line by line. It finds that Implementation and Proof each point at Behavior and not at each other, so a project can leave out either one alone. The record rates this evidence with high confidence.

The research also gives the 5 shapes a claim can take: Intent alone, Intent with Behavior, Intent and Behavior with Implementation, Intent and Behavior with Proof, or all 4 layers. These shapes follow from the parent rule, and the research cites no outside source for them.

## Revisit Conditions

This decision reopens when a team needs a claim that the 4 keys cannot state, such as a claim for each stack.

A claim per stack would need a key that the `layers` map does not hold today. The current keys cover the 5 shapes and nothing more.
