# Model Fixtures

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

- `small/` is a library loans system at size small. It holds one module, three aggregates, eight use cases, results, rules, events, actors, roles, permissions, entry points, terms, and two exceptions. It has no goals, problems, features, journeys, scenarios, constraints, research, settings, reactions, jobs, notifications, or runbooks. Seven use cases have handlers. The eighth is covered by the open exception.
- `large/` holds the same records plus one goal, problems, features, one journey, one scenario per use case, constraints, research, one setting, one reaction, one job, one notification, runbooks, one external system, and a frontend route that imports a generated client operation. Each scenario has one proving test.

Both fixtures exit 0 under `check`. Each names the same owner, reviews every Behavior record, approves every Intent record, stamps every approval hash, and resolves every citation.

## Finding Codes

The [Model Builder](../../../../../../docs/tools/model.md) page lists every finding code with its check level, its record layer and trace, its meaning, and its fix.

## Broken Overlays

`broken/<code>/` holds only the files that differ from `large/`. The suite copies `large/` to a temporary folder, copies the overlay files over it, deletes the paths listed in `DELETES.txt` when that file exists, and removes `DELETES.txt` before running the check. Each overlay exits 1 with its one finding code.

An open exception that names the finding codes and the subject reports the finding as excepted and exits 0. An expired exception exits 1.
