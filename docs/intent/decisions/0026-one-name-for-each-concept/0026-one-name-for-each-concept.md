# One Name for Each Concept

## Context

Several words in the standard and the tools named two things. `level` named both the conformance level and the check level. `rule` named both a record and a provision strength. `implemented` and `met` were proposed as completion stages, and a record had both a short identifier and a qualified one. Readers met each overlap in the book and in tool output.

## Decision

Each concept has one name in every layer. The check level keeps its name, since the conformance level it shared the word with is removed by `decision/0027-projects-claim-record-layers`. The stage, strength and identifier names hold in the standard, the tools and the book, and no stage, strength or identifier carries a second name.
