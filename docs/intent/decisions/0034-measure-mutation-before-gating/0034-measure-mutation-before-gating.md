# Measure Mutation Testing, Add No Gate

Mutation testing changes small parts of the code on purpose and checks whether the tests fail. The share of changes that the tests catch is the mutation score, and it shows how well the tests catch faults. This decision measures that score once for this repository, and it adds no gate or tool in this release.

## Context

A changed-lines mutation gate would stop each change that the score fails. A gate on a poor score fails every change for a reason that has nothing to do with the change. The team needs one measured run before it decides on a gate for this repository.

## Options

LiteSpec compared 3 options for mutation testing. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Measure once with a scratch tool, and add no gate or tool in this release | The score shows where the tests stand, and no tool or gate fails for the wrong reason. | No mutation result reaches CI in this release, so changed lines carry no mutation check. |
| Add a changed-lines mutation gate now | Each change meets a mutation threshold from the start. | The measured score of 13.0% would fail every change, because the CLI suite that covers these files cannot run in the same mutation run. |
| Decide without measuring | No setup work. | The decision rests on opinion, and no run shows which tests catch faults. |

## Decision

This release measures mutation testing once, and it adds no gate and no tool. The repository gains no mutation tool, and the scratch install that the measurement used leaves the repository.

## Consequences

The consumer default `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines` stays as it is, because it governs consumers and not this repository.

The Delivery extension keeps its mutation job for the projects that select it.

## Evidence

The record reports a measured run on 2026-11-10 with Stryker.NET 5.0.0, run from a scratch path. On Windows, its analysis failed on the .NET 10 SDK. In the SDK 10 container it ran only with the Microsoft Testing Platform runner, because the repository's xunit v3 tests run on that platform.

Over `Identifiers.cs`, `RecordFolders.cs` and `RecordFiles.cs`, with the model test project alone, 282 mutants ran. Of those, 56 were killed and 226 survived, a score of 13.0%. A further 148 mutants had no coverage, and 366 did not compile. The record rates these counts with high confidence.

The score understates the protection. The CLI suite covers those files and cannot run in the same mutation run as the model project. The record rates the confidence that the score measures test quality as low. The cited research, `research/agent-testing-evidence`, does not hold these counts, so the counts rest on the record's own run.

## Revisit Conditions

This decision reopens when a CI job runs the CLI suite and the model suite under one mutation run on Linux, and the measured score reflects the tests that cover the changed lines.

Until that run exists, the score covers only the model project and not the tests that cover the same files. A run that includes those tests is the case that would change the choice.
