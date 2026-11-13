# Projects Claim the Record Layers They Use

## Context

The project file's `conformance` took one of three values, `records`, `traced` and `full`. Each value bundled the code practice and the proof practice together, so a team that wanted LiteSpec's architecture with its own tests took all of `full` or none of it. The word `level` also named the check level, which the same release keeps.

## Decision

A project claims each record layer it uses and names the practice it follows, in the `layers` map of its project file. The standard states each rule for the layer it governs, and each tool applies the rules for the layers the project claims. A layer the project does not claim produces no findings, so a part a team keeps for itself is left out of the claim rather than waived.

## Evidence with Confidence

`research/layer-claim-evidence` reads the completion code and finds that Implementation and Proof each point at Behavior and not at each other, so a project can leave out either one. The research also gives the five shapes a claim can take, which follow from the parent rule, with high confidence, because the claim rests on the completion code read line by line.
