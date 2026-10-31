# Model fixtures

Two fixture consumers and one broken overlay per check. The suite in `apps/cli/tests/LiteSpec.Cli.Tests/Model/ModelCheckTests.cs` copies a fixture to a temporary folder, applies an overlay, and runs the model builder check.

Run the suite with:

```bash
dotnet test --solution LiteSpec.slnx
```

Or check one fixture directly:

```bash
litespec model check --root apps/cli/tests/LiteSpec.Cli.Tests/Fixtures/Model/small
```

## Fixtures

- `small/` is a library loans system at size small. It holds one module, three aggregates, eight use cases, results, rules, events, actors, roles, permissions, entry points, terms, one deviation, and one known gap. It has no goals, problems, features, journeys, scenarios, constraints, research, settings, reactions, jobs, notifications, or runbooks. Seven use cases have handlers. The eighth is covered by the open known gap.
- `large/` holds the same records plus one goal, problems, features, one journey, one scenario per use case, constraints, research, one setting, one reaction, one job, one notification, runbooks, one external system, and a frontend route that imports a generated client operation. Each scenario has one proving test.

Both fixtures exit 0 under `check`. Each names the same owner, reviews every Behavior record, approves every Intent record, stamps every approval hash, and resolves every citation.

## Finding codes

The builder and the suite use these codes:

- `aggregate-name-clash`
- `approval-reopened`
- `behavior-unrecorded`
- `calculation-table-missing`
- `consistency-cites-missing`
- `decision-decides-missing`
- `approval-invalid`
- `decider-missing`
- `effect-loop-invalid`
- `effect-undeclared`
- `erasure-undeclared`
- `except-fallback-missing`
- `exemption-expired`
- `gap-finding-unknown`
- `identifier-duplicate`
- `identifier-invalid`
- `field-invalid`
- `use-case-journey-missing`
- `known-gap-expired`
- `link-unknown`
- `notification-sender-missing`
- `job-guard-missing`
- `owner-unknown`
- `paging-undeclared`
- `product-count-invalid`
- `proves-unreached`
- `query-output-missing`
- `reaction-trigger-unknown`
- `record-link-missing`
- `record-invalid`
- `reference-unknown`
- `refusal-unimplemented`
- `requirement-fulfillment-missing`
- `requirement-measure-missing`
- `reservation-release-missing`
- `result-undeclared`
- `retention-enforcement-missing`
- `risk-acceptance-missing`
- `rule-name-invalid`
- `scenario-result-unknown`
- `schedule-invalid`
- `setting-requirement-missing`
- `size-type-missing`
- `state-unreached`
- `step-use-case-missing`
- `transcription-expired`
- `transcription-status-invalid`
- `transition-unknown`
- `use-case-name-clash`
- `use-case-unimplemented`
- `use-case-scenario-missing`
- `use-case-success-missing`
- `widening-cites-missing`
- `write-undeclared`

## Broken overlays

`broken/<code>/` holds only the files that differ from `large/`. The suite copies `large/` to a temporary folder, copies the overlay files over it, deletes the paths listed in `DELETES.txt` when that file exists, and removes `DELETES.txt` before running the check. Each overlay exits 1 with its one finding code.

An open known gap that names the finding code and the subject reports the finding as known and exits 0. An expired gap exits 1.
