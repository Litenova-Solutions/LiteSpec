# Measure Mutation Testing, Add No Gate

## Context

A changed-lines mutation gate tells a team which tests catch faults, but a gate on a poor score fails each change for a reason unrelated to the change. The team needed one measured run before it could decide on a gate for this repository.

## Decision

This release measures mutation testing once and adds no gate and no tool. Stryker.NET 5.0.0 ran from a scratch path. On Windows, its analysis failed on the .NET 10 SDK. In the SDK 10 container it ran only with the Microsoft Testing Platform runner, because the repository's xunit v3 tests run on that platform. Over `Identifiers.cs`, `RecordFolders.cs` and `RecordFiles.cs`, with the model test project alone, 282 mutants ran: 56 were killed and 226 survived, a score of 13.0 percent. A further 148 had no coverage and 366 did not compile.

## Evidence with Confidence

The counts above come from the measured run of 2026-11-10, with high confidence. The score understates the protection, because the CLI suite covers those files and cannot run in the same mutation run as the model project, so the confidence that the score measures test quality is low.
