# Claimwick

Claimwick is the LiteSpec example consumer: a small, realistic SaaS product the docs show as a real, checked project.

Employees submit work expenses with receipts. Managers approve or refuse them. Finance reimburses the approved ones.

## Layout

Records live under `.litespec/`, grouped by layer:

- `.litespec/product.yaml` is the product record, standing above the record layers.
- `.litespec/intent/` holds Intent records: features under the problem they answer in `.litespec/intent/problems/<problem>/features/`, flows under `.litespec/intent/flows/`, and roles under `.litespec/intent/roles/`.
- `.litespec/behavior/` holds Behavior records: read models under `.litespec/behavior/modules/<module>/read-models/`, domain events in the aggregate files, and screens under `.litespec/behavior/entry-points/claimwick-app/screens/`.
- `.litespec/implementation/patterns/` holds the UI pattern catalog, `.litespec/implementation/modules/spending/runbooks/` the runbooks, and `.litespec/governance/` the governance records.
- `.litespec/glossary.yaml` is the glossary.

Code lives under `apps/`: the .NET backend sample under `apps/api/`, the Svelte frontend under `apps/web/`, and the generated API client under `packages/api/`.

The code follows the shape the stack pages require: sealed state records, one `DomainException` subclass per refusal, internal sealed handlers, `Async` method names, acceptance scenarios carrying proves marks, the spending Contracts project publishing its facts and its public read-expense query under one folder per aggregate, and the one Spending port through which Reporting reaches it. It does not compile as a solution: it holds no project or solution files, and its samples exist to give the model builder its marks.

## Checks

Check the example with:

```bash
litespec model check --root examples/claimwick
```

The check reports covered findings only, with nothing open. The waivers park the export that has no handler and no endpoint, the browser-reached use cases and flow steps with no screen yet, the reimbursed-expenses list and the read-expense query no flow step uses, the scenarios with no passing proof, and the weekend export review bypass.
